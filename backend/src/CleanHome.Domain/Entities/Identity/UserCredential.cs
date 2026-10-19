namespace CleanHome.Domain.Entities.Identity;

public sealed class UserCredential
{
    public Guid UserId { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset PasswordChangedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private UserCredential()
    {
    }

    public UserCredential(Guid userId, string passwordHash)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "El usuario es obligatorio.",
                nameof(userId));

        ValidateHash(passwordHash);

        UserId = userId;
        PasswordHash = passwordHash;
        CreatedAt = DateTimeOffset.UtcNow;
        PasswordChangedAt = CreatedAt;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        ValidateHash(passwordHash);

        PasswordHash = passwordHash;
        PasswordChangedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "El hash de contraseña es obligatorio.",
                nameof(passwordHash));

        if (!passwordHash.StartsWith(
                "$argon2id$",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Se requiere un hash Argon2id.",
                nameof(passwordHash));
        }

        if (passwordHash.Length > 512)
            throw new ArgumentException(
                "El hash excede la longitud permitida.",
                nameof(passwordHash));
    }
}
