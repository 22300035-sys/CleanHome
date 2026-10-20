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

    private readonly CleanHomeDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenGenerator _tokenGenerator;
    private readonly IAccountEmailSender _emails;

    public AccountRecoveryService(
        CleanHomeDbContext db,
        IPasswordHasher passwordHasher,
        IRefreshTokenGenerator tokenGenerator,
        IAccountEmailSender emails)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
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

        var generated = _tokenGenerator.Generate();

        _db.EmailVerificationTokens.Add(
            new EmailVerificationToken(
                user.Id,
                generated.TokenHash,
                DateTimeOffset.UtcNow.Add(VerificationLifetime)));

        await _db.SaveChangesAsync(cancellationToken);

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

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        var stored = await _db.EmailVerificationTokens
            .FromSqlInterpolated(
                $"SELECT * FROM email_verification_tokens WHERE token_hash = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (stored is null ||
            !stored.IsValid(DateTimeOffset.UtcNow))
        {
            throw new InvalidAccountTokenException();
        }

        var user = await _db.Users.SingleOrDefaultAsync(
            x => x.Id == stored.UserId && x.IsActive,
            cancellationToken);

        if (user is null)
            throw new InvalidAccountTokenException();

        stored.MarkAsUsed();
        user.VerifyEmail();

        _db.SecurityEvents.Add(new SecurityEvent(
            "email_verified",
            "success",
            user.Id));

        await _db.SaveChangesAsync(cancellationToken);
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

        var generated = _tokenGenerator.Generate();

        _db.PasswordResetTokens.Add(
            new PasswordResetToken(
                user.Id,
                generated.TokenHash,
                DateTimeOffset.UtcNow.Add(ResetLifetime)));

        await _db.SaveChangesAsync(cancellationToken);

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

        // El hash Argon2id se calcula antes de bloquear filas.
        var newPasswordHash =
            _passwordHasher.HashPassword(newPassword);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        var stored = await _db.PasswordResetTokens
            .FromSqlInterpolated(
                $"SELECT * FROM password_reset_tokens WHERE token_hash = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (stored is null ||
            !stored.IsValid(DateTimeOffset.UtcNow))
        {
            throw new InvalidAccountTokenException();
        }

        var user = await _db.Users.SingleOrDefaultAsync(
            x => x.Id == stored.UserId && x.IsActive,
            cancellationToken);

        var credential = await _db.UserCredentials
            .SingleOrDefaultAsync(
                x => x.UserId == stored.UserId,
                cancellationToken);

        if (user is null || credential is null)
            throw new InvalidAccountTokenException();

        stored.MarkAsUsed();
        credential.ChangePasswordHash(newPasswordHash);

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
        await transaction.CommitAsync(cancellationToken);
    }

    private string? TryHashToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 256)
        {
            return null;
        }

        return _tokenGenerator.ComputeHash(token);
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
