namespace CleanHome.Domain.Entities.Identity;

public sealed class User
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public bool IsEmailVerified { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    // Constructor requerido por Entity Framework Core.
    private User()
    {
    }

    public User(
        string email,
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "El correo electrónico es obligatorio.",
                nameof(email));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException(
                "El nombre es obligatorio.",
                nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException(
                "El apellido es obligatorio.",
                nameof(lastName));

        Id = Guid.NewGuid();

        Email = email.Trim();

        NormalizedEmail = Email.ToUpperInvariant();

        FirstName = firstName.Trim();

        LastName = lastName.Trim();

        IsEmailVerified = false;

        IsActive = true;

        CreatedAt = DateTimeOffset.UtcNow;

        UpdatedAt = CreatedAt;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Disable()
    {
        IsActive = false;
        DisabledAt = DateTimeOffset.UtcNow;
        UpdatedAt = DisabledAt.Value;
    }

    public void Reactivate()
    {
        IsActive = true;
        DisabledAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateName(
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException(
                "El nombre es obligatorio.",
                nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException(
                "El apellido es obligatorio.",
                nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
