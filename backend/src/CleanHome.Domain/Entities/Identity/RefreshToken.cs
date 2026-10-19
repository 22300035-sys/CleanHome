namespace CleanHome.Domain.Entities.Identity;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid sessionId,
        string tokenHash,
        DateTimeOffset expiresAt)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException(
                "La sesión es obligatoria.",
                nameof(sessionId));

        if (string.IsNullOrWhiteSpace(tokenHash) ||
            tokenHash.Length != 64 ||
            !tokenHash.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "Se requiere un hash SHA-256 hexadecimal.",
                nameof(tokenHash));
        }

        var now = DateTimeOffset.UtcNow;

        if (expiresAt <= now)
            throw new ArgumentException(
                "La expiración debe ser futura.",
                nameof(expiresAt));

        Id = Guid.NewGuid();
        SessionId = sessionId;
        TokenHash = tokenHash.ToLowerInvariant();
        CreatedAt = now;
        ExpiresAt = expiresAt;
    }

    public bool CanRefresh(DateTimeOffset now)
    {
        return ConsumedAt is null &&
               RevokedAt is null &&
               now < ExpiresAt;
    }

    public void Consume(Guid replacementTokenId)
    {
        if (replacementTokenId == Guid.Empty ||
            replacementTokenId == Id)
        {
            throw new ArgumentException(
                "Se requiere un token de reemplazo válido.",
                nameof(replacementTokenId));
        }

        if (!CanRefresh(DateTimeOffset.UtcNow))
            throw new InvalidOperationException(
                "El refresh token ya no puede utilizarse.");

        ConsumedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenId = replacementTokenId;
    }

    public void Revoke()
    {
        RevokedAt ??= DateTimeOffset.UtcNow;
    }
}
