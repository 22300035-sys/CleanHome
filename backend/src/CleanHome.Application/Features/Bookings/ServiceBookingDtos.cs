namespace CleanHome.Application.Features.Bookings;

public sealed record ServiceAddressRequest(
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string Neighborhood,
    string PostalCode,
    string State,
    string? Reference);

public sealed record CreateServiceRequestDto(
    Guid ServiceCategoryId,
    string Title,
    string Description,
    string City,
    DateTimeOffset ScheduledStart,
    int EstimatedDurationMinutes,
    ServiceAddressRequest Address);

public sealed record ServiceRequestDto(
    Guid Id,
    Guid ServiceCategoryId,
    string Title,
    string Description,
    string City,
    DateTimeOffset ScheduledStart,
    int EstimatedDurationMinutes,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record ServiceAddressDto(
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string Neighborhood,
    string PostalCode,
    string State,
    string? Reference);

public sealed record ServiceRequestDetailDto(
    ServiceRequestDto Request,
    ServiceAddressDto? Address);

public sealed record CreateServiceQuoteDto(
    decimal Amount,
    string Description,
    DateTimeOffset ExpiresAt);

public sealed record ServiceQuoteDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid WorkerProfileId,
    decimal Amount,
    string Description,
    string Status,
    DateTimeOffset ExpiresAt);

public sealed record BookingDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid ServiceQuoteId,
    Guid CustomerUserId,
    Guid WorkerProfileId,
    decimal AgreedAmount,
    DateTimeOffset ScheduledStart,
    int EstimatedDurationMinutes,
    string Status,
    ServiceAddressDto? Address);

public sealed record BookingHistoryDto(
    string PreviousStatus,
    string NewStatus,
    string? Reason,
    DateTimeOffset CreatedAt);

public sealed record BookingDetailDto(
    BookingDto Booking,
    IReadOnlyList<BookingHistoryDto> History);

public sealed record CancelBookingDto(string? Reason);

public sealed record RepeatServiceRequestDto(
    DateTimeOffset ScheduledStart);

public sealed class BookingConflictException : Exception
{
    public BookingConflictException(string message) : base(message) { }
}
