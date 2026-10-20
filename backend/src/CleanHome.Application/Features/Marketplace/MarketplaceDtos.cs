namespace CleanHome.Application.Features.Marketplace;

public sealed record UpdateUserProfileRequest(
    string City,
    string? Phone);

public sealed record UserProfileResponse(
    Guid UserId,
    string City,
    string? Phone);

public sealed record UpsertWorkerProfileRequest(
    string Description,
    string ServiceCity,
    string ServiceArea);

public sealed record WorkerProfileResponse(
    Guid Id,
    Guid UserId,
    string Description,
    string ServiceCity,
    string ServiceArea,
    string VerificationStatus,
    bool IsPublished);

public sealed record UpsertOfferingRequest(
    Guid ServiceCategoryId,
    decimal ReferencePrice,
    string Description);

public sealed record WorkerOfferingResponse(
    Guid Id,
    Guid ServiceCategoryId,
    string CategoryName,
    decimal ReferencePrice,
    string Description,
    bool IsActive);

public sealed record AvailabilitySlotRequest(
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record AvailabilitySlotResponse(
    Guid Id,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record WorkerListingResponse(
    Guid Id,
    string DisplayName,
    string Description,
    string ServiceCity,
    string ServiceArea,
    IReadOnlyList<WorkerOfferingResponse> Offerings);

public sealed record WorkerDashboardResponse(
    WorkerProfileResponse Profile,
    IReadOnlyList<WorkerOfferingResponse> Offerings,
    IReadOnlyList<AvailabilitySlotResponse> Availability);
