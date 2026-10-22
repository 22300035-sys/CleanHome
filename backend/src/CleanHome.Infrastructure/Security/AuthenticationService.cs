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

        var email = request.Email?.Trim() ?? "";
        var password = request.Password ?? "";
        var deviceName = request.DeviceName?.Trim() ?? "";
        var platform = request.Platform?.Trim() ?? "";

        if (email.Length is < 3 or > 254 ||
            string.IsNullOrWhiteSpace(password) ||
            Encoding.UTF8.GetByteCount(password) > 1024 ||
            deviceName.Length is < 1 or > 150 ||
            platform.Length is < 1 or > 30)
        {
            throw new AuthenticationException();
        }

        var normalizedEmail = email.ToUpperInvariant();

        var preliminaryUser = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (preliminaryUser is null || !preliminaryUser.IsActive)
        {
            await RecordRejectedLoginAsync(
                preliminaryUser?.Id,
                cancellationToken);

            throw new AuthenticationException();
        }

        // Verificamos la contraseña antes de abrir una transacción,
        // evitando mantener bloqueos durante Argon2id.
        var credential = await _db.UserCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserId == preliminaryUser.Id,
                cancellationToken);

        if (credential is null ||
            !_passwordHasher.VerifyPassword(
                password,
                credential.PasswordHash))
        {
            await RecordRejectedLoginAsync(
                preliminaryUser.Id,
                cancellationToken);

            throw new AuthenticationException();
        }

        // Si hace falta recalcular el hash, lo hacemos fuera
        // de la transacción.
        var needsRehash = _passwordHasher.NeedsRehash(
            credential.PasswordHash);

        var upgradedHash = needsRehash
            ? _passwordHasher.HashPassword(password)
            : null;

        var session = new UserSession(
            preliminaryUser.Id,
            deviceName,
            platform,
            DateTimeOffset.UtcNow.Add(SessionLifetime));

        var refreshValue = _refreshTokens.Generate();

        var refresh = new RefreshToken(
            session.Id,
            refreshValue.TokenHash,
            session.ExpiresAt);

        var access = _accessTokens.Generate(
            preliminaryUser.Id,
            session.Id);

        await using (var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken))
        {
            // El bloqueo del usuario serializa este login con
            // los cambios de contraseña.
            var user = await LockUserAsync(
                preliminaryUser.Id,
                cancellationToken);

            if (user is null || !user.IsActive)
                throw new AuthenticationException();

            var currentCredential = await _db.UserCredentials
                .SingleOrDefaultAsync(
                    x => x.UserId == user.Id,
                    cancellationToken);

            // Si la contraseña cambió desde la primera lectura,
            // no podemos crear una sesión con credenciales antiguas.
            if (currentCredential is null ||
                currentCredential.PasswordHash != credential.PasswordHash)
            {
                throw new AuthenticationException();
            }

            if (upgradedHash is not null)
                currentCredential.ChangePasswordHash(upgradedHash);

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
            preliminaryUser.Id,
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

        var tokenValue = request.RefreshToken ?? "";

        if (tokenValue.Length is < 1 or > 256)
            throw new AuthenticationException();

        var hash = _refreshTokens.ComputeHash(tokenValue);

        // La primera lectura solamente localiza la sesión.
        // No autoriza todavía la renovación.
        var preliminary = await _db.RefreshTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == hash)
            .Select(x => new
            {
                x.SessionId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (preliminary is null)
            throw new AuthenticationException();

        var ownerId = await _db.UserSessions
            .AsNoTracking()
            .Where(x => x.Id == preliminary.SessionId)
            .Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!ownerId.HasValue)
            throw new AuthenticationException();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // Orden de bloqueo: usuario, sesión, token.
        var user = await LockUserAsync(
            ownerId.Value,
            cancellationToken);

        if (user is null || !user.IsActive)
            throw new AuthenticationException();

        var session = await LockSessionAsync(
            preliminary.SessionId,
            cancellationToken);

        if (session is null || session.UserId != user.Id)
            throw new AuthenticationException();

        var storedToken = await LockRefreshTokenAsync(
            hash,
            cancellationToken);

        if (storedToken is null ||
            storedToken.SessionId != session.Id)
        {
            throw new AuthenticationException();
        }

        var now = DateTimeOffset.UtcNow;

        // Reutilizar un token consumido provoca la revocación
        // de toda la sesión, incluso si existe otro token vigente.
        if (storedToken.ConsumedAt is not null)
        {
            session.Revoke("refresh_token_reuse");

            _db.SecurityEvents.Add(new SecurityEvent(
                "refresh_token_reuse",
                "rejected",
                user.Id,
                session.Id));

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogWarning(
                "Reutilización de refresh token. SessionId={SessionId}",
                session.Id);

            throw new AuthenticationException();
        }

        if (!session.IsActive(now) ||
            !storedToken.CanRefresh(now))
        {
            throw new AuthenticationException();
        }

        var newValue = _refreshTokens.Generate();

        var replacement = new RefreshToken(
            session.Id,
            newValue.TokenHash,
            session.ExpiresAt);

        var access = _accessTokens.Generate(
            user.Id,
            session.Id);

        storedToken.Consume(replacement.Id);
        session.RegisterActivity();

        _db.RefreshTokens.Add(replacement);

        _db.SecurityEvents.Add(new SecurityEvent(
            "token_rotated",
            "success",
            user.Id,
            session.Id));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Refresh token renovado. UserId={UserId}; SessionId={SessionId}",
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
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        var user = await LockUserAsync(
            userId,
            cancellationToken);

        if (user is null)
            return;

        var session = await LockSessionAsync(
            sessionId,
            cancellationToken);

        if (session is null ||
            session.UserId != userId ||
            session.RevokedAt is not null)
        {
            return;
        }

        session.Revoke("user_logout");

        _db.SecurityEvents.Add(new SecurityEvent(
            "logout",
            "success",
            userId,
            sessionId));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Sesión cerrada. UserId={UserId}; SessionId={SessionId}",
            userId,
            sessionId);
    }

    public async Task LogoutAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        var user = await LockUserAsync(
            userId,
            cancellationToken);

        if (user is null)
            return;

        var sessions = await _db.UserSessions
            .Where(x =>
                x.UserId == userId &&
                x.RevokedAt == null)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
            session.Revoke("logout_all");

        _db.SecurityEvents.Add(new SecurityEvent(
            "logout_all",
            "success",
            userId));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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

    private async Task<User?> LockUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Users
            .FromSqlInterpolated(
                $"SELECT * FROM users WHERE id = {userId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    private async Task<UserSession?> LockSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.UserSessions
            .FromSqlInterpolated(
                $"SELECT * FROM user_sessions WHERE id = {sessionId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    private async Task<RefreshToken?> LockRefreshTokenAsync(
        string hash,
        CancellationToken cancellationToken)
    {
        var rows = await _db.RefreshTokens
            .FromSqlInterpolated(
                $"SELECT * FROM refresh_tokens WHERE token_hash = {hash} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
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
