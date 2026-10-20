using System.Data;
using System.Text;
using CleanHome.Application.Abstractions.Identity;
using CleanHome.Application.Features.Identity;
using CleanHome.Domain.Entities.Identity;
using CleanHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanHome.Infrastructure.Security;

public sealed class AuthenticationService : IAuthenticationService
{
    private static readonly TimeSpan SessionLifetime =
        TimeSpan.FromDays(30);

    private readonly CleanHomeDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokens;
    private readonly IRefreshTokenGenerator _refreshTokens;
    private readonly ISecurityEventRepository _events;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        CleanHomeDbContext db,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokens,
        IRefreshTokenGenerator refreshTokens,
        ISecurityEventRepository events,
        ILogger<AuthenticationService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _accessTokens = accessTokens;
        _refreshTokens = refreshTokens;
        _events = events;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var deviceName = request.DeviceName?.Trim() ?? string.Empty;
        var platform = request.Platform?.Trim() ?? string.Empty;

        if (email.Length is < 3 or > 254 ||
            string.IsNullOrWhiteSpace(password) ||
            Encoding.UTF8.GetByteCount(password) > 1024 ||
            deviceName.Length is < 1 or > 150 ||
            platform.Length is < 1 or > 30)
        {
            throw new AuthenticationException();
        }

        var normalizedEmail = email.ToUpperInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            await RecordRejectedLoginAsync(
                user?.Id,
                cancellationToken);

            throw new AuthenticationException();
        }

        var credential = await _db.UserCredentials
            .SingleOrDefaultAsync(
                x => x.UserId == user.Id,
                cancellationToken);

        if (credential is null ||
            !_passwordHasher.VerifyPassword(
                password,
                credential.PasswordHash))
        {
            await RecordRejectedLoginAsync(
                user.Id,
                cancellationToken);

            throw new AuthenticationException();
        }

        var session = new UserSession(
            user.Id,
            deviceName,
            platform,
            DateTimeOffset.UtcNow.Add(SessionLifetime));

        var refreshValue = _refreshTokens.Generate();

        var refresh = new RefreshToken(
            session.Id,
            refreshValue.TokenHash,
            session.ExpiresAt);

        // Generamos el JWT antes de confirmar la transacción.
        // Si la firma falla, no crearemos una sesión inutilizable.
        var access = _accessTokens.Generate(
            user.Id,
            session.Id);

        await using (var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken))
        {
            if (_passwordHasher.NeedsRehash(
                credential.PasswordHash))
            {
                credential.ChangePasswordHash(
                    _passwordHasher.HashPassword(password));
            }

            _db.UserSessions.Add(session);
            _db.RefreshTokens.Add(refresh);

            _db.SecurityEvents.Add(new SecurityEvent(
                "login_success",
                "success",
                user.Id,
                session.Id));

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Login correcto. UserId={UserId}; SessionId={SessionId}",
            user.Id,
            session.Id);

        return new AuthenticationResponse(
            access.Token,
            access.ExpiresAt,
            refreshValue.Token,
            session.ExpiresAt,
            session.Id);
    }

    public async Task<AuthenticationResponse> RefreshAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tokenValue = request.RefreshToken ?? string.Empty;

        if (tokenValue.Length is < 1 or > 256)
            throw new AuthenticationException();

        var hash = _refreshTokens.ComputeHash(tokenValue);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // Bloqueo de fila para serializar renovaciones
        // concurrentes del mismo refresh token.
        var storedToken = await _db.RefreshTokens
            .FromSqlInterpolated(
                $"SELECT * FROM refresh_tokens WHERE token_hash = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (storedToken is null)
            throw new AuthenticationException();

        var session = await _db.UserSessions
            .SingleOrDefaultAsync(
                x => x.Id == storedToken.SessionId,
                cancellationToken);

        if (session is null)
            throw new AuthenticationException();

        var now = DateTimeOffset.UtcNow;

        // Un token consumido no puede volver a utilizarse.
        // La revocación y el evento se confirman juntos.
        if (storedToken.ConsumedAt is not null)
        {
            session.Revoke("refresh_token_reuse");

            _db.SecurityEvents.Add(new SecurityEvent(
                "refresh_token_reuse",
                "rejected",
                session.UserId,
                session.Id));

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogWarning(
                "Refresh token reutilizado. SessionId={SessionId}",
                session.Id);

            throw new AuthenticationException();
        }

        if (!session.IsActive(now) ||
            !storedToken.CanRefresh(now))
        {
            throw new AuthenticationException();
        }

        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == session.UserId,
                cancellationToken);

        if (user is null || !user.IsActive)
            throw new AuthenticationException();

        var newValue = _refreshTokens.Generate();

        var replacement = new RefreshToken(
            session.Id,
            newValue.TokenHash,
            session.ExpiresAt);

        // Primero comprobamos que se puede emitir el JWT.
        var access = _accessTokens.Generate(
            user.Id,
            session.Id);

        storedToken.Consume(replacement.Id);

        _db.RefreshTokens.Add(replacement);
        session.RegisterActivity();

        _db.SecurityEvents.Add(new SecurityEvent(
            "token_rotated",
            "success",
            user.Id,
            session.Id));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Token renovado. UserId={UserId}; SessionId={SessionId}",
            user.Id,
            session.Id);

        return new AuthenticationResponse(
            access.Token,
            access.ExpiresAt,
            newValue.Token,
            session.ExpiresAt,
            session.Id);
    }

    public async Task LogoutAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.UserSessions
            .SingleOrDefaultAsync(
                x => x.Id == sessionId &&
                     x.UserId == userId,
                cancellationToken);

        if (session is null || session.RevokedAt is not null)
            return;

        session.Revoke("user_logout");

        _db.SecurityEvents.Add(new SecurityEvent(
            "logout",
            "success",
            userId,
            sessionId));

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sesión cerrada. UserId={UserId}; SessionId={SessionId}",
            userId,
            sessionId);
    }

    public async Task LogoutAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _db.UserSessions
            .Where(x =>
                x.UserId == userId &&
                x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
            session.Revoke("logout_all");

        _db.SecurityEvents.Add(new SecurityEvent(
            "logout_all",
            "success",
            userId));

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sesiones revocadas. UserId={UserId}; Count={Count}",
            userId,
            sessions.Count);
    }

    public async Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var sessions = await _db.UserSessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.RevokedAt == null &&
                x.ExpiresAt > now)
            .OrderByDescending(x => x.LastActivityAt)
            .ToListAsync(cancellationToken);

        return sessions
            .Select(x => new SessionResponse(
                x.Id,
                x.DeviceName,
                x.Platform,
                x.CreatedAt,
                x.LastActivityAt,
                x.ExpiresAt,
                x.Id == currentSessionId))
            .ToList();
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == userId && x.IsActive,
                cancellationToken);

        if (user is null)
            return null;

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsEmailVerified);
    }

    private async Task RecordRejectedLoginAsync(
        Guid? userId,
        CancellationToken cancellationToken)
    {
        await _events.RecordAsync(
            "login_rejected",
            "rejected",
            userId,
            cancellationToken: cancellationToken);

        _logger.LogWarning(
            "Intento de inicio de sesión rechazado.");
    }
}
