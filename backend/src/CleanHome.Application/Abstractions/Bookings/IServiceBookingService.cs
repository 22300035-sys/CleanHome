using CleanHome.Application.Features.Bookings;

namespace CleanHome.Application.Abstractions.Bookings;

public interface IServiceBookingService
{
    Task<ServiceRequestDto> CreateRequestAsync(
        Guid userId,
        CreateServiceRequestDto request,
        CancellationToken ct);

    Task<IReadOnlyList<ServiceRequestDto>> GetMyRequestsAsync(
        Guid userId,
        CancellationToken ct);

    Task<IReadOnlyList<ServiceRequestDto>> BrowseRequestsAsync(
        Guid userId,
        string? city,
        CancellationToken ct);

    Task<ServiceRequestDetailDto?> GetRequestAsync(
        Guid userId,
        Guid requestId,
        CancellationToken ct);

    Task CancelRequestAsync(
        Guid userId,
        Guid requestId,
        CancellationToken ct);

    Task<ServiceQuoteDto> CreateQuoteAsync(
        Guid userId,
        Guid requestId,
        CreateServiceQuoteDto request,
        CancellationToken ct);

    Task<IReadOnlyList<ServiceQuoteDto>> GetQuotesAsync(
        Guid userId,
        Guid requestId,
        CancellationToken ct);

    Task WithdrawQuoteAsync(
        Guid userId,
        Guid quoteId,
        CancellationToken ct);

    Task<BookingDto> AcceptQuoteAsync(
        Guid userId,
        Guid requestId,
        Guid quoteId,
        CancellationToken ct);

    Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(
        Guid userId,
        CancellationToken ct);

    Task<BookingDetailDto?> GetBookingAsync(
        Guid userId,
        Guid bookingId,
        CancellationToken ct);

    Task ChangeBookingStatusAsync(
        Guid userId,
        Guid bookingId,
        string action,
        string? reason,
        CancellationToken ct);

    Task<ServiceRequestDto> RepeatRequestAsync(
        Guid userId,
        Guid requestId,
        RepeatServiceRequestDto request,
        CancellationToken ct);
}
