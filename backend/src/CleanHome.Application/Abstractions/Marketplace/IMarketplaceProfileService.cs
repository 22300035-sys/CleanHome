using CleanHome.Application.Features.Marketplace;

namespace CleanHome.Application.Abstractions.Marketplace;

public interface IMarketplaceProfileService
{
    Task<UserProfileResponse?> GetUserProfileAsync(
        Guid userId,
        CancellationToken ct);

    Task<UserProfileResponse> UpdateUserProfileAsync(
        Guid userId,
        UpdateUserProfileRequest request,
        CancellationToken ct);

    Task<WorkerDashboardResponse?> GetWorkerDashboardAsync(
        Guid userId,
        CancellationToken ct);

    Task<WorkerProfileResponse> UpsertWorkerProfileAsync(
        Guid userId,
        UpsertWorkerProfileRequest request,
        CancellationToken ct);

    Task<WorkerOfferingResponse> UpsertOfferingAsync(
        Guid userId,
        UpsertOfferingRequest request,
        CancellationToken ct);

    Task SetOfferingActiveAsync(
        Guid userId,
        Guid offeringId,
        bool active,
        CancellationToken ct);

    Task ReplaceAvailabilityAsync(
        Guid userId,
        IReadOnlyList<AvailabilitySlotRequest> slots,
        CancellationToken ct);

    Task SetPublishedAsync(
        Guid userId,
        bool published,
        CancellationToken ct);

    Task<IReadOnlyList<WorkerListingResponse>> BrowseWorkersAsync(
        string? city,
        Guid? categoryId,
        CancellationToken ct);
}
