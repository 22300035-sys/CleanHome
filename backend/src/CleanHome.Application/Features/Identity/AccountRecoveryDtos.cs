namespace CleanHome.Application.Features.Identity;

public sealed record EmailAddressRequest(string Email);

public sealed record VerifyEmailRequest(string Token);

public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword);

public sealed record AccountRecoveryMessage(
    string Message);

public sealed class InvalidAccountTokenException : Exception
{
    public InvalidAccountTokenException()
        : base("El enlace es inválido o ha expirado.")
    {
    }
}
