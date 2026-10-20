using CleanHome.Application.Abstractions.Bookings;
using CleanHome.Application.Features.Bookings;
using CleanHome.Domain.Entities.Bookings;
using CleanHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence.Bookings;

public sealed class ServiceBookingService : IServiceBookingService
{
    private readonly CleanHomeDbContext _db;

    public ServiceBookingService(CleanHomeDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceRequestDto> CreateRequestAsync(
        Guid userId, CreateServiceRequestDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Address);

        var categoryExists = await _db.ServiceCategories
            .AnyAsync(x => x.Id == request.ServiceCategoryId && x.IsActive, ct);

        if (!categoryExists)
            throw new ArgumentException("La categoría no está disponible.");

        var serviceRequest = new ServiceRequest(
            userId,
            request.ServiceCategoryId,
            request.Title,
            request.Description,
            request.City,
            request.ScheduledStart,
            request.EstimatedDurationMinutes);

        var address = CreateAddress(serviceRequest.Id, request.Address);

        _db.ServiceRequests.Add(serviceRequest);
        _db.ServiceRequestAddresses.Add(address);
        await _db.SaveChangesAsync(ct);

        return MapRequest(serviceRequest);
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> GetMyRequestsAsync(
        Guid userId, CancellationToken ct)
    {
        var requests = await _db.ServiceRequests.AsNoTracking()
            .Where(x => x.CustomerUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return requests.Select(MapRequest).ToList();
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> BrowseRequestsAsync(
        Guid userId, string? city, CancellationToken ct)
    {
        var worker = await GetApprovedWorkerAsync(userId, ct);

        if (worker is null)
            throw new BookingConflictException(
                "Necesitas un perfil profesional aprobado.");

        var query = _db.ServiceRequests.AsNoTracking()
            .Where(x => x.Status == "open" &&
                        x.ScheduledStart > DateTimeOffset.UtcNow &&
                        x.CustomerUserId != userId &&
                        x.City == worker.ServiceCity &&
                        _db.WorkerOfferings.Any(o =>
                            o.WorkerProfileId == worker.Id &&
                            o.ServiceCategoryId == x.ServiceCategoryId &&
                            o.IsActive));

        if (!string.IsNullOrWhiteSpace(city))
        {
            city = city.Trim();
            if (city.Length > 120)
                throw new ArgumentException("Ciudad inválida.");

            query = query.Where(x => x.City == city);
        }

        var requests = await query
            .OrderBy(x => x.ScheduledStart)
            .Take(100)
            .ToListAsync(ct);

        return requests.Select(MapRequest).ToList();
    }

    public async Task<ServiceRequestDetailDto?> GetRequestAsync(
        Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await _db.ServiceRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == requestId, ct);

        if (request is null)
            return null;

        if (request.CustomerUserId == userId)
        {
            var address = await _db.ServiceRequestAddresses.AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ServiceRequestId == requestId, ct);

            return new ServiceRequestDetailDto(
                MapRequest(request),
                address is null ? null : MapAddress(address));
        }

        var worker = await GetApprovedWorkerAsync(userId, ct);

        if (worker is null)
            return null;

        var eligible = await _db.WorkerOfferings.AnyAsync(
            x => x.WorkerProfileId == worker.Id &&
                 x.ServiceCategoryId == request.ServiceCategoryId &&
                 x.IsActive, ct);

        if (!eligible || request.CustomerUserId == userId)
            return null;

        var isBookedWorker = await _db.ServiceBookings.AnyAsync(
            x => x.ServiceRequestId == requestId &&
                 x.WorkerProfileId == worker.Id, ct);

        if (!isBookedWorker &&
            (request.Status != "open" ||
             request.ScheduledStart <= DateTimeOffset.UtcNow))
            return null;

        if (!isBookedWorker)
            return new ServiceRequestDetailDto(MapRequest(request), null);

        var privateAddress = await _db.ServiceRequestAddresses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ServiceRequestId == requestId, ct);

        return new ServiceRequestDetailDto(
            MapRequest(request),
            privateAddress is null ? null : MapAddress(privateAddress));
    }

    public async Task CancelRequestAsync(
        Guid userId, Guid requestId, CancellationToken ct)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var request = await LockRequestAsync(requestId, ct);

        if (request is null || request.CustomerUserId != userId)
            throw new KeyNotFoundException("Solicitud no encontrada.");

        if (request.Status != "open")
            throw new BookingConflictException(
                "Solo puedes cancelar solicitudes abiertas.");

        request.Cancel();

        var quotes = await _db.ServiceQuotes
            .Where(x => x.ServiceRequestId == requestId &&
                        x.Status == "pending")
            .ToListAsync(ct);

        foreach (var quote in quotes)
            quote.Reject();

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<ServiceQuoteDto> CreateQuoteAsync(
        Guid userId, Guid requestId,
        CreateServiceQuoteDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var serviceRequest = await LockRequestAsync(requestId, ct);

        if (serviceRequest is null ||
            serviceRequest.Status != "open" ||
            serviceRequest.ScheduledStart <= DateTimeOffset.UtcNow)
            throw new BookingConflictException(
                "Esta solicitud ya no acepta cotizaciones.");

        if (serviceRequest.CustomerUserId == userId)
            throw new BookingConflictException(
                "No puedes cotizar tu propia solicitud.");

        var worker = await GetApprovedWorkerAsync(userId, ct);

        if (worker is null)
            throw new BookingConflictException(
                "Tu perfil profesional aún no está aprobado.");

        var eligible = await _db.WorkerOfferings.AnyAsync(
            x => x.WorkerProfileId == worker.Id &&
                 x.ServiceCategoryId == serviceRequest.ServiceCategoryId &&
                 x.IsActive, ct);

        if (!eligible)
            throw new BookingConflictException(
                "No ofreces la categoría solicitada.");

        if (!string.Equals(
            worker.ServiceCity,
            serviceRequest.City,
            StringComparison.OrdinalIgnoreCase))
            throw new BookingConflictException(
                "La solicitud está fuera de tu ciudad de servicio.");

        var existing = await _db.ServiceQuotes.AnyAsync(
            x => x.ServiceRequestId == requestId &&
                 x.WorkerProfileId == worker.Id, ct);

        if (existing)
            throw new BookingConflictException(
                "Ya enviaste una cotización para esta solicitud.");

        if (request.ExpiresAt > serviceRequest.ScheduledStart)
            throw new ArgumentException(
                "La cotización debe vencer antes del servicio.");

        var quote = new ServiceQuote(
            requestId,
            worker.Id,
            request.Amount,
            request.Description,
            request.ExpiresAt);

        _db.ServiceQuotes.Add(quote);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return MapQuote(quote);
    }

    public async Task<IReadOnlyList<ServiceQuoteDto>> GetQuotesAsync(
        Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await _db.ServiceRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == requestId, ct);

        if (request is null)
            throw new KeyNotFoundException("Solicitud no encontrada.");

        if (request.CustomerUserId == userId)
        {
            var all = await _db.ServiceQuotes.AsNoTracking()
                .Where(x => x.ServiceRequestId == requestId)
                .OrderBy(x => x.Amount)
                .ToListAsync(ct);

            return all.Select(MapQuote).ToList();
        }

        var workerId = await _db.WorkerProfiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(ct);

        if (!workerId.HasValue)
            throw new KeyNotFoundException("Solicitud no encontrada.");

        var own = await _db.ServiceQuotes.AsNoTracking()
            .Where(x => x.ServiceRequestId == requestId &&
                        x.WorkerProfileId == workerId.Value)
            .ToListAsync(ct);

        return own.Select(MapQuote).ToList();
    }

    public async Task WithdrawQuoteAsync(
        Guid userId, Guid quoteId, CancellationToken ct)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var preliminary = await _db.ServiceQuotes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == quoteId, ct);

        if (preliminary is null)
            throw new KeyNotFoundException("Cotización no encontrada.");

        var serviceRequest = await LockRequestAsync(
            preliminary.ServiceRequestId, ct);

        if (serviceRequest?.Status != "open")
            throw new BookingConflictException(
                "La solicitud ya está cerrada.");

        var worker = await _db.WorkerProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

        if (worker is null)
            throw new KeyNotFoundException("Cotización no encontrada.");

        var quote = await _db.ServiceQuotes
            .SingleOrDefaultAsync(x => x.Id == quoteId &&
                                       x.WorkerProfileId == worker.Id, ct);

        if (quote is null)
            throw new KeyNotFoundException("Cotización no encontrada.");

        quote.Withdraw();

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<BookingDto> AcceptQuoteAsync(
        Guid userId, Guid requestId, Guid quoteId,
        CancellationToken ct)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        // Serializa las aceptaciones de una misma solicitud.
        var request = await LockRequestAsync(requestId, ct);

        if (request is null || request.CustomerUserId != userId)
            throw new KeyNotFoundException("Solicitud no encontrada.");

        if (request.Status != "open" ||
            request.ScheduledStart <= DateTimeOffset.UtcNow)
            throw new BookingConflictException(
                "La solicitud ya no está disponible.");

        var quote = await _db.ServiceQuotes.SingleOrDefaultAsync(
            x => x.Id == quoteId &&
                 x.ServiceRequestId == requestId, ct);

        if (quote is null)
            throw new KeyNotFoundException("Cotización no encontrada.");

        if (quote.Status != "pending" ||
            quote.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new BookingConflictException(
                "La cotización expiró o ya no está disponible.");

        var worker = await _db.WorkerProfiles.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == quote.WorkerProfileId &&
                     x.VerificationStatus == "approved" &&
                     x.IsPublished, ct);

        if (worker is null)
            throw new BookingConflictException(
                "El trabajador ya no está disponible.");

        var workerActive = await _db.Users.AnyAsync(
            x => x.Id == worker.UserId &&
                 x.IsActive &&
                 x.IsEmailVerified, ct);

        var offersCategory = await _db.WorkerOfferings.AnyAsync(
            x => x.WorkerProfileId == worker.Id &&
                 x.ServiceCategoryId == request.ServiceCategoryId &&
                 x.IsActive, ct);

        if (!workerActive || !offersCategory)
            throw new BookingConflictException(
                "El trabajador no cumple los requisitos actuales.");

        // Bloquea también el perfil profesional para serializar
        // aceptaciones concurrentes de este mismo trabajador.
        await _db.WorkerProfiles
            .FromSqlInterpolated(
                $"SELECT * FROM worker_profiles WHERE id = {worker.Id} FOR UPDATE")
            .ToListAsync(ct);

        var proposedEnd = request.ScheduledStart.AddMinutes(
            request.EstimatedDurationMinutes);

        var activeBookings = await _db.ServiceBookings.AsNoTracking()
            .Where(x => x.WorkerProfileId == worker.Id &&
                        (x.Status == "confirmed" ||
                         x.Status == "in_progress") &&
                        x.ScheduledStart < proposedEnd)
            .ToListAsync(ct);

        if (activeBookings.Any(x =>
            x.ScheduledStart.AddMinutes(
                x.EstimatedDurationMinutes) > request.ScheduledStart))
            throw new BookingConflictException(
                "El trabajador ya tiene una reserva en ese horario.");

        quote.Accept();
        request.MarkBooked();

        var otherQuotes = await _db.ServiceQuotes
            .Where(x => x.ServiceRequestId == requestId &&
                        x.Id != quoteId &&
                        x.Status == "pending")
            .ToListAsync(ct);

        foreach (var other in otherQuotes)
            other.Reject();

        var booking = new ServiceBooking(request, quote);

        _db.ServiceBookings.Add(booking);

        _db.BookingStatusHistories.Add(
            new BookingStatusHistory(
                booking.Id, userId, "none", "confirmed"));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await MapBookingAsync(booking, true, ct);
    }

    public async Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(
        Guid userId, CancellationToken ct)
    {
        var workerId = await _db.WorkerProfiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(ct);

        var bookings = await _db.ServiceBookings.AsNoTracking()
            .Where(x => x.CustomerUserId == userId ||
                        (workerId.HasValue &&
                         x.WorkerProfileId == workerId.Value))
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var result = new List<BookingDto>();

        foreach (var booking in bookings)
            result.Add(await MapBookingAsync(booking, true, ct));

        return result;
    }

    public async Task<BookingDetailDto?> GetBookingAsync(
        Guid userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await FindOwnedBookingAsync(userId, bookingId, ct);

        if (booking is null)
            return null;

        var history = await _db.BookingStatusHistories.AsNoTracking()
            .Where(x => x.BookingId == bookingId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new BookingHistoryDto(
                x.PreviousStatus,
                x.NewStatus,
                x.Reason,
                x.CreatedAt))
            .ToListAsync(ct);

        return new BookingDetailDto(
            await MapBookingAsync(booking, true, ct),
            history);
    }

    public async Task ChangeBookingStatusAsync(
        Guid userId, Guid bookingId,
        string action, string? reason, CancellationToken ct)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        // Bloquea la reserva para evitar transiciones concurrentes.
        var locked = await _db.ServiceBookings
            .FromSqlInterpolated(
                $"SELECT * FROM service_bookings WHERE id = {bookingId} FOR UPDATE")
            .ToListAsync(ct);

        var booking = locked.SingleOrDefault();

        if (booking is null)
            throw new KeyNotFoundException("Reserva no encontrada.");

        var workerUserId = await _db.WorkerProfiles
            .Where(x => x.Id == booking.WorkerProfileId)
            .Select(x => x.UserId)
            .SingleAsync(ct);

        var isCustomer = booking.CustomerUserId == userId;
        var isWorker = workerUserId == userId;

        if (!isCustomer && !isWorker)
            throw new KeyNotFoundException("Reserva no encontrada.");

        var previous = booking.Status;

        switch (action)
        {
            case "start" when isWorker:
                booking.Start();
                break;

            case "complete" when isWorker:
                booking.Complete();
                break;

            case "cancel" when isCustomer || isWorker:
                if (booking.Status == "in_progress")
                    throw new BookingConflictException(
                        "Una reserva en curso requiere revisión de soporte.");

                booking.Cancel();
                break;

            default:
                throw new BookingConflictException(
                    "La transición no está permitida para tu perfil.");
        }

        _db.BookingStatusHistories.Add(
            new BookingStatusHistory(
                booking.Id,
                userId,
                previous,
                booking.Status,
                reason));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<ServiceRequestDto> RepeatRequestAsync(
        Guid userId, Guid requestId,
        RepeatServiceRequestDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var original = await _db.ServiceRequests.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == requestId &&
                     x.CustomerUserId == userId, ct);

        if (original is null)
            throw new KeyNotFoundException("Solicitud no encontrada.");

        if (original.Status == "open")
            throw new BookingConflictException(
                "La solicitud original todavía está abierta.");

        var address = await _db.ServiceRequestAddresses.AsNoTracking()
            .SingleAsync(x => x.ServiceRequestId == requestId, ct);

        var copied = new ServiceRequest(
            userId,
            original.ServiceCategoryId,
            original.Title,
            original.Description,
            original.City,
            request.ScheduledStart,
            original.EstimatedDurationMinutes,
            original.Id);

        var copiedAddress = new ServiceRequestAddress(
            copied.Id,
            address.Street,
            address.ExteriorNumber,
            address.InteriorNumber,
            address.Neighborhood,
            address.PostalCode,
            address.State,
            address.Reference);

        _db.ServiceRequests.Add(copied);
        _db.ServiceRequestAddresses.Add(copiedAddress);

        await _db.SaveChangesAsync(ct);

        return MapRequest(copied);
    }

    private async Task<ServiceRequest?> LockRequestAsync(
        Guid id, CancellationToken ct)
    {
        var rows = await _db.ServiceRequests
            .FromSqlInterpolated(
                $"SELECT * FROM service_requests WHERE id = {id} FOR UPDATE")
            .ToListAsync(ct);

        return rows.SingleOrDefault();
    }

    private async Task<CleanHome.Domain.Entities.Marketplace.WorkerProfile?>
        GetApprovedWorkerAsync(Guid userId, CancellationToken ct)
    {
        return await _db.WorkerProfiles.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserId == userId &&
                     x.VerificationStatus == "approved" &&
                     x.IsPublished &&
                     _db.Users.Any(u =>
                         u.Id == userId &&
                         u.IsActive &&
                         u.IsEmailVerified), ct);
    }

    private async Task<ServiceBooking?> FindOwnedBookingAsync(
        Guid userId, Guid bookingId, CancellationToken ct)
    {
        return await _db.ServiceBookings.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == bookingId &&
                     (x.CustomerUserId == userId ||
                      _db.WorkerProfiles.Any(w =>
                          w.Id == x.WorkerProfileId &&
                          w.UserId == userId)), ct);
    }

    private async Task<BookingDto> MapBookingAsync(
        ServiceBooking booking, bool includeAddress, CancellationToken ct)
    {
        ServiceAddressDto? address = null;

        if (includeAddress)
        {
            var stored = await _db.ServiceRequestAddresses.AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ServiceRequestId == booking.ServiceRequestId, ct);

            if (stored is not null)
                address = MapAddress(stored);
        }

        return new BookingDto(
            booking.Id,
            booking.ServiceRequestId,
            booking.ServiceQuoteId,
            booking.CustomerUserId,
            booking.WorkerProfileId,
            booking.AgreedAmount,
            booking.ScheduledStart,
            booking.EstimatedDurationMinutes,
            booking.Status,
            address);
    }

    private static ServiceRequestAddress CreateAddress(
        Guid requestId, ServiceAddressRequest address)
    {
        return new ServiceRequestAddress(
            requestId,
            address.Street,
            address.ExteriorNumber,
            address.InteriorNumber,
            address.Neighborhood,
            address.PostalCode,
            address.State,
            address.Reference);
    }

    private static ServiceRequestDto MapRequest(ServiceRequest x) =>
        new(
            x.Id,
            x.ServiceCategoryId,
            x.Title,
            x.Description,
            x.City,
            x.ScheduledStart,
            x.EstimatedDurationMinutes,
            x.Status,
            x.CreatedAt);

    private static ServiceQuoteDto MapQuote(ServiceQuote x) =>
        new(
            x.Id,
            x.ServiceRequestId,
            x.WorkerProfileId,
            x.Amount,
            x.Description,
            x.Status,
            x.ExpiresAt);

    private static ServiceAddressDto MapAddress(ServiceRequestAddress x) =>
        new(
            x.Street,
            x.ExteriorNumber,
            x.InteriorNumber,
            x.Neighborhood,
            x.PostalCode,
            x.State,
            x.Reference);
}
