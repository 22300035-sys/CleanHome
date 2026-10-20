namespace CleanHome.Application.Abstractions.Identity;

public interface IAccountEmailSender
{
    Task SendVerificationAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default);
}
