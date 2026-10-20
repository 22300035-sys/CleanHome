namespace CleanHome.Domain.Entities.Identity;

public sealed class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private PasswordResetToken()
    {
    }

    public PasswordResetToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("El usuario es obligatorio.");

        if (string.IsNullOrWhiteSpace(tokenHash) ||
            tokenHash.Length != 64 ||
            !tokenHash.All(Uri.IsHexDigit))
            throw new ArgumentException("El hash del token no es válido.");

        var now = DateTimeOffset.UtcNow;

        if (expiresAt <= now)
            throw new ArgumentException("La expiración debe ser futura.");

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash.ToLowerInvariant();
        CreatedAt = now;
        ExpiresAt = expiresAt;
    }

    public bool IsValid(DateTimeOffset now)
    {
        return UsedAt is null && now < ExpiresAt;
    }

    public void MarkAsUsed()
    {
        if (!IsValid(DateTimeOffset.UtcNow))
            throw new InvalidOperationException(
                "El token de recuperación ya no es válido.");

        UsedAt = DateTimeOffset.UtcNow;
    }
}
