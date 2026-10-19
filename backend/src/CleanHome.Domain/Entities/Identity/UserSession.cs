namespace CleanHome.Domain.Entities.Identity;

public sealed class UserSession
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string DeviceName { get; private set; } = string.Empty;

    public string Platform { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastActivityAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? RevocationReason { get; private set; }

    private UserSession()
    {
    }

    public UserSession(
        Guid userId,
        string deviceName,
        string platform,
        DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "El usuario es obligatorio.",
                nameof(userId));

        if (string.IsNullOrWhiteSpace(deviceName))
            throw new ArgumentException(
                "El dispositivo es obligatorio.",
                nameof(deviceName));

        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentException(
                "La plataforma es obligatoria.",
                nameof(platform));

        if (deviceName.Trim().Length > 150 ||
            platform.Trim().Length > 30)
        {
            throw new ArgumentException(
                "Los datos del dispositivo exceden la longitud permitida.");
        }

        var now = DateTimeOffset.UtcNow;

        if (expiresAt <= now)
            throw new ArgumentException(
                "La sesión debe tener una expiración futura.",
                nameof(expiresAt));

        Id = Guid.NewGuid();
        UserId = userId;
        DeviceName = deviceName.Trim();
        Platform = platform.Trim().ToLowerInvariant();
        CreatedAt = now;
        LastActivityAt = now;
        ExpiresAt = expiresAt;
    }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null && now < ExpiresAt;
    }

    public void RegisterActivity()
    {
        if (!IsActive(DateTimeOffset.UtcNow))
            throw new InvalidOperationException(
                "No se puede actualizar una sesión inactiva.");

        LastActivityAt = DateTimeOffset.UtcNow;
    }

    public void Revoke(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Trim().Length > 150)
        {
            throw new ArgumentException(
                "Se requiere un motivo válido de revocación.",
                nameof(reason));
        }

        if (RevokedAt is not null)
            return;

        RevokedAt = DateTimeOffset.UtcNow;
        RevocationReason = reason.Trim();
    }
}
