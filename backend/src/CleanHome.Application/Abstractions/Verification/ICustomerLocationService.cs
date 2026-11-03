using CleanHome.Application.Features.Verification;

namespace CleanHome.Application.Abstractions.Verification;

public interface ICustomerLocationService
{
    Task<CustomerLocationResponse> CreateAsync(
        Guid userId,
        CustomerLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerLocationResponse>> GetMyAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
