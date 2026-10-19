using System.Net.Mail;
using System.Text;
using CleanHome.Application.Abstractions.Identity;
using CleanHome.Domain.Entities.Identity;

namespace CleanHome.Application.Features.Identity;

public sealed class UserRegistrationService
{
    private readonly IUserRegistrationRepository _repository;
    private readonly IPasswordHasher _passwordHasher;

    public UserRegistrationService(
        IUserRegistrationRepository repository,
        IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResponse> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var password = request.Password;

        if (firstName.Length is < 1 or > 100)
            throw new ArgumentException(
                "El nombre debe tener entre 1 y 100 caracteres.");

        if (lastName.Length is < 1 or > 100)
            throw new ArgumentException(
                "El apellido debe tener entre 1 y 100 caracteres.");

        if (email.Length is < 3 or > 254 ||
            !IsValidEmail(email))
        {
            throw new ArgumentException(
                "El correo electrónico no es válido.");
        }

        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 12 ||
            Encoding.UTF8.GetByteCount(password) > 1024)
        {
            throw new ArgumentException(
                "La contraseña debe tener al menos 12 caracteres " +
                "y no superar 1024 bytes.");
        }

        var normalizedEmail = email.ToUpperInvariant();

        if (await _repository.EmailExistsAsync(
            normalizedEmail,
            cancellationToken))
        {
            throw new DuplicateUserEmailException();
        }

        var user = new User(
            email,
            firstName,
            lastName);

        var passwordHash = _passwordHasher.HashPassword(password);

        var credential = new UserCredential(
            user.Id,
            passwordHash);

        await _repository.AddAsync(
            user,
            credential,
            cancellationToken);

        return new RegisterUserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.IsEmailVerified,
            user.CreatedAt);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);

            return string.Equals(
                address.Address,
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class DuplicateUserEmailException : Exception
{
    public DuplicateUserEmailException()
        : base("El correo electrónico ya está registrado.")
    {
    }
}
