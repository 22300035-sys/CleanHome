namespace CleanHome.Application.Abstractions.Identity;

public interface IAccountRecoveryService
{
    Task RequestVerificationAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
