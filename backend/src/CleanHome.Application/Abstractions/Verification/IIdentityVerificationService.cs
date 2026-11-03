using CleanHome.Application.Features.Verification;

namespace CleanHome.Application.Abstractions.Verification;

public interface IIdentityVerificationService
{
    Task<bool> HasApprovedIdentityAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
