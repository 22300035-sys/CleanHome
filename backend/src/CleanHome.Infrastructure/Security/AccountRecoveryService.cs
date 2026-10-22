using System.Text;
using CleanHome.Application.Abstractions.Identity;
using CleanHome.Application.Features.Identity;
using CleanHome.Domain.Entities.Identity;
using CleanHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Security;

public sealed class AccountRecoveryService : IAccountRecoveryService
{
    private static readonly TimeSpan VerificationLifetime =
        TimeSpan.FromHours(24);

    private static readonly TimeSpan ResetLifetime =
        TimeSpan.FromMinutes(30);

    private static readonly TimeSpan MinimumResendInterval =
        TimeSpan.FromMinutes(2);

    private readonly CleanHomeDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenGenerator _tokens;
    private readonly IAccountEmailSender _emails;

    public AccountRecoveryService(
        CleanHomeDbContext db,
        IPasswordHasher passwordHasher,
        IRefreshTokenGenerator tokens,
        IAccountEmailSender emails)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _emails = emails;
    }

    public async Task RequestVerificationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);

        if (normalized is null)
            return;

        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.NormalizedEmail == normalized,
                cancellationToken);

        if (user is null || !user.IsActive || user.IsEmailVerified)
            return;

        var now = DateTimeOffset.UtcNow;
        var generated = _tokens.Generate();

        await using (var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken))
        {
            var lockedUser = await LockUserAsync(
                user.Id,
                cancellationToken);

            if (lockedUser is null ||
                !lockedUser.IsActive ||
                lockedUser.IsEmailVerified)
            {
                return;
            }

            var recentlyIssued =
                await _db.EmailVerificationTokens.AnyAsync(
                    x => x.UserId == user.Id &&
                         x.CreatedAt > now - MinimumResendInterval,
                    cancellationToken);

            if (recentlyIssued)
                return;

            _db.EmailVerificationTokens.Add(
                new EmailVerificationToken(
                    user.Id,
                    generated.TokenHash,
                    now.Add(VerificationLifetime)));

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await _emails.SendVerificationAsync(
            user.Email,
            generated.Token,
            cancellationToken);
    }

    public async Task VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var hash = TryHashToken(token);

        if (hash is null)
            throw new InvalidAccountTokenException();

        // Primera consulta: únicamente identifica al propietario.
        var ownerId = await _db.EmailVerificationTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == hash)
            .Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!ownerId.HasValue)
            throw new InvalidAccountTokenException();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        // El usuario es el punto común de sincronización.
        var user = await LockUserAsync(
            ownerId.Value,
            cancellationToken);

        if (user is null || !user.IsActive || user.IsEmailVerified)
            throw new InvalidAccountTokenException();

        var stored = await LockVerificationTokenAsync(
            hash,
            cancellationToken);

        if (stored is null ||
            stored.UserId != user.Id ||
            !stored.IsValid(DateTimeOffset.UtcNow))
        {
            throw new InvalidAccountTokenException();
        }

        stored.MarkAsUsed();
        user.VerifyEmail();

        _db.SecurityEvents.Add(new SecurityEvent(
            "email_verified",
            "success",
            user.Id));

        await _db.SaveChangesAsync(cancellationToken);

        // Los demás enlaces de verificación quedan inutilizados.
        await _db.EmailVerificationTokens
            .Where(x =>
                x.UserId == user.Id &&
                x.Id != stored.Id &&
                x.UsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.UsedAt,
                    (DateTimeOffset?)DateTimeOffset.UtcNow),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);

        if (normalized is null)
            return;

        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.NormalizedEmail == normalized,
                cancellationToken);

        if (user is null || !user.IsActive)
            return;

        var now = DateTimeOffset.UtcNow;
        var generated = _tokens.Generate();

        await using (var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken))
        {
            var lockedUser = await LockUserAsync(
                user.Id,
                cancellationToken);

            if (lockedUser is null || !lockedUser.IsActive)
                return;

            var recentlyIssued =
                await _db.PasswordResetTokens.AnyAsync(
                    x => x.UserId == user.Id &&
                         x.CreatedAt > now - MinimumResendInterval,
                    cancellationToken);

            if (recentlyIssued)
                return;

            _db.PasswordResetTokens.Add(
                new PasswordResetToken(
                    user.Id,
                    generated.TokenHash,
                    now.Add(ResetLifetime)));

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await _emails.SendPasswordResetAsync(
            user.Email,
            generated.Token,
            cancellationToken);
    }

    public async Task ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ValidateNewPassword(newPassword);

        var hash = TryHashToken(token);

        if (hash is null)
            throw new InvalidAccountTokenException();

        // Consulta preliminar sin bloqueo: no autoriza el cambio.
        var ownerId = await _db.PasswordResetTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == hash)
            .Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!ownerId.HasValue)
            throw new InvalidAccountTokenException();

        // Argon2id se calcula antes de abrir la transacción.
        var newPasswordHash =
            _passwordHasher.HashPassword(newPassword);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        // Dos restablecimientos del mismo usuario se serializan
        // mediante el bloqueo de esta fila.
        var user = await LockUserAsync(
            ownerId.Value,
            cancellationToken);

        if (user is null || !user.IsActive)
            throw new InvalidAccountTokenException();

        // Se vuelve a leer el token DESPUÉS del bloqueo.
        var stored = await LockPasswordResetTokenAsync(
            hash,
            cancellationToken);

        if (stored is null ||
            stored.UserId != user.Id ||
            !stored.IsValid(DateTimeOffset.UtcNow))
        {
            throw new InvalidAccountTokenException();
        }

        var credential = await _db.UserCredentials
            .SingleOrDefaultAsync(
                x => x.UserId == user.Id,
                cancellationToken);

        if (credential is null)
            throw new InvalidAccountTokenException();

        stored.MarkAsUsed();
        credential.ChangePasswordHash(newPasswordHash);

        // Invalidar todas las sesiones existentes.
        var sessions = await _db.UserSessions
            .Where(x =>
                x.UserId == user.Id &&
                x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
            session.Revoke("password_reset");

        _db.SecurityEvents.Add(new SecurityEvent(
            "password_reset",
            "success",
            user.Id));

        await _db.SaveChangesAsync(cancellationToken);

        // Invalidar TODOS los demás enlaces anteriores.
        // ExecuteUpdate se ejecuta dentro de la misma transacción.
        await _db.PasswordResetTokens
            .Where(x =>
                x.UserId == user.Id &&
                x.Id != stored.Id &&
                x.UsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.UsedAt,
                    (DateTimeOffset?)DateTimeOffset.UtcNow),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
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

    private async Task<EmailVerificationToken?>
        LockVerificationTokenAsync(
            string hash,
            CancellationToken cancellationToken)
    {
        var rows = await _db.EmailVerificationTokens
            .FromSqlInterpolated(
                $"SELECT * FROM email_verification_tokens WHERE token_hash = {hash} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    private async Task<PasswordResetToken?>
        LockPasswordResetTokenAsync(
            string hash,
            CancellationToken cancellationToken)
    {
        var rows = await _db.PasswordResetTokens
            .FromSqlInterpolated(
                $"SELECT * FROM password_reset_tokens WHERE token_hash = {hash} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    private string? TryHashToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 256)
        {
            return null;
        }

        return _tokens.ComputeHash(token);
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var trimmed = email.Trim();

        if (trimmed.Length > 254 ||
            !trimmed.Contains('@'))
        {
            return null;
        }

        return trimmed.ToUpperInvariant();
    }

    private static void ValidateNewPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 12 ||
            Encoding.UTF8.GetByteCount(password) > 1024)
        {
            throw new ArgumentException(
                "La contraseña debe tener al menos 12 caracteres " +
                "y no superar 1024 bytes.");
        }
    }
}
