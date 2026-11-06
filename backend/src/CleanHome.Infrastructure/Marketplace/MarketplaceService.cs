using System.Data;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Marketplace;

public sealed class MarketplaceService(CleanHomeDbContext db)
{
    public async Task<UserProfile> ActorAsync(string userId, CancellationToken ct)
    {
        var profile = await db.Profiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new BusinessException(409, "Completa tu perfil primero.");
        if (profile.IsSuspended) throw new BusinessException(403, "Cuenta suspendida.");
        return profile;
    }

    public async Task<UserProfile> CreateProfileAsync(string userId, ProfileInput input, CancellationToken ct)
    {
        if (!Enum.IsDefined(input.Role)) throw new BusinessException(400, "Rol inválido.");
        if (await db.Profiles.AnyAsync(x => x.UserId == userId, ct)) throw new BusinessException(409, "El perfil ya existe; su rol no puede cambiarse.");
        var profile = new UserProfile { UserId = userId, DisplayName = input.DisplayName.Trim(), Role = input.Role };
        if (profile.DisplayName.Length < 2) throw new BusinessException(400, "Nombre inválido.");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public static void RequireRole(UserProfile profile, AccountRole role)
    {
        if (profile.Role != role) throw new BusinessException(403, "Rol no autorizado.");
        if (role == AccountRole.Worker && !profile.IsVerified) throw new BusinessException(403, "El trabajador debe ser verificado por administración.");
    }

    public async Task<CustomerLocation> CreateLocationAsync(UserProfile actor, LocationInput input, CancellationToken ct)
    {
        RequireRole(actor, AccountRole.Customer);
        if (string.IsNullOrWhiteSpace(input.Label) || string.IsNullOrWhiteSpace(input.Address) || string.IsNullOrWhiteSpace(input.City))
            throw new BusinessException(400, "Domicilio incompleto.");
        var location = new CustomerLocation { CustomerId = actor.Id, Label = input.Label.Trim(), Address = input.Address.Trim(), City = input.City.Trim() };
        db.Locations.Add(location);
        await db.SaveChangesAsync(ct);
        return location;
    }

    public async Task<ServiceRequest> CreateRequestAsync(UserProfile actor, RequestInput input, CancellationToken ct)
    {
        RequireRole(actor, AccountRole.Customer);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (input.StartsAt <= DateTimeOffset.UtcNow.AddMinutes(30) || input.StartsAt > DateTimeOffset.UtcNow.AddMonths(6))
            throw new BusinessException(400, "La fecha debe estar entre 30 minutos y 6 meses en el futuro.");
        if (input.Description.Trim().Length < 10) throw new BusinessException(400, "Descripción demasiado corta.");
        if (!await db.Locations.AnyAsync(x => x.Id == input.LocationId && x.CustomerId == actor.Id, ct)) throw new BusinessException(404, "Domicilio no encontrado.");
        if (!await db.ServiceCategories.AnyAsync(x => x.Id == input.CategoryId && x.IsActive, ct)) throw new BusinessException(404, "Categoría no encontrada.");
        var request = new ServiceRequest { CustomerId = actor.Id, LocationId = input.LocationId, CategoryId = input.CategoryId, Description = input.Description.Trim(), StartsAt = input.StartsAt.ToUniversalTime(), DurationMinutes = input.DurationMinutes };
        db.Requests.Add(request);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return request;
    }

    public async Task<Page<RequestView>> ListRequestsAsync(UserProfile actor, int page, int size, CancellationToken ct)
    {
        var query = db.Requests.AsNoTracking().AsQueryable();
        if (actor.Role == AccountRole.Customer) query = query.Where(x => x.CustomerId == actor.Id);
        else { RequireRole(actor, AccountRole.Worker); query = query.Where(x => x.Status == RequestStatus.Open && x.StartsAt > DateTimeOffset.UtcNow); }
        var items = await (from r in query join l in db.Locations on r.LocationId equals l.Id orderby r.CreatedAt descending, r.Id select new RequestView(r.Id, r.CategoryId, r.Description, l.City, r.StartsAt, r.DurationMinutes, r.Status, r.CreatedAt)).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(items, page, size);
    }

    public async Task<ServiceQuote> QuoteAsync(UserProfile actor, Guid requestId, QuoteInput input, CancellationToken ct)
    {
        RequireRole(actor, AccountRole.Worker);
        var request = await db.Requests.SingleOrDefaultAsync(x => x.Id == requestId, ct) ?? throw new BusinessException(404, "Solicitud no encontrada.");
        if (request.Status != RequestStatus.Open || request.StartsAt <= DateTimeOffset.UtcNow) throw new BusinessException(409, "Solicitud cerrada.");
        if (input.ExpiresAt <= DateTimeOffset.UtcNow || input.ExpiresAt > request.StartsAt || decimal.Round(input.Amount, 2) != input.Amount)
            throw new BusinessException(400, "Vigencia o importe inválido (máximo dos decimales).");
        var quote = new ServiceQuote { RequestId = requestId, WorkerId = actor.Id, Amount = input.Amount, Notes = input.Notes?.Trim() ?? "", ExpiresAt = input.ExpiresAt.ToUniversalTime() };
        db.Quotes.Add(quote);
        db.Notifications.Add(new Notification { RecipientId = request.CustomerId, Kind = "quote.received", ResourceId = quote.Id });
        await db.SaveChangesAsync(ct);
        return quote;
    }

    public async Task<ServiceBooking> AcceptAsync(UserProfile actor, Guid quoteId, CancellationToken ct)
    {
        RequireRole(actor, AccountRole.Customer);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var quote = await db.Quotes.SingleOrDefaultAsync(x => x.Id == quoteId, ct) ?? throw new BusinessException(404, "Cotización no encontrada.");
        var request = await db.Requests.SingleAsync(x => x.Id == quote.RequestId, ct);
        if (request.CustomerId != actor.Id) throw new BusinessException(404, "Cotización no encontrada.");
        if (quote.ExpiresAt <= DateTimeOffset.UtcNow) throw new BusinessException(409, "Cotización vencida.");
        var worker = await db.Profiles.SingleAsync(x => x.Id == quote.WorkerId, ct);
        if (!worker.IsVerified || worker.IsSuspended) throw new BusinessException(409, "Trabajador no disponible.");
        var end = request.StartsAt.AddMinutes(request.DurationMinutes);
        if (await db.Bookings.AnyAsync(x => (x.WorkerId == worker.Id || x.CustomerId == actor.Id) && x.Status != BookingStatus.Cancelled && x.StartsAt < end && x.EndsAt > request.StartsAt, ct))
            throw new BusinessException(409, "El horario se superpone con otra reserva.");
        request.Book();
        var booking = new ServiceBooking { RequestId = request.Id, QuoteId = quote.Id, CustomerId = actor.Id, WorkerId = worker.Id, StartsAt = request.StartsAt, EndsAt = end, Amount = quote.Amount, Currency = quote.Currency };
        db.Bookings.Add(booking);
        db.Notifications.Add(new Notification { RecipientId = worker.Id, Kind = "booking.confirmed", ResourceId = booking.Id });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return booking;
    }

    public async Task<ServiceBooking> GetBookingAsync(UserProfile actor, Guid id, CancellationToken ct)
        => await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && (x.CustomerId == actor.Id || x.WorkerId == actor.Id), ct) ?? throw new BusinessException(404, "Reserva no encontrada.");

    public async Task<ServiceBooking> TransitionAsync(UserProfile actor, Guid id, BookingStatus status, CancellationToken ct)
    {
        // Serializa la cancelación con la creación de checkout; nunca cancelar mientras se cobra.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var booking = await GetBookingAsync(actor, id, ct);
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.BookingId == id, ct);
        if (status == BookingStatus.Cancelled && payment is not null && payment.Status != PaymentStatus.Refunded)
            throw new BusinessException(409, "La reserva tiene un checkout o pago; administración debe resolverlo antes de cancelar.");
        booking.Transition(status, booking.CustomerId == actor.Id, booking.WorkerId == actor.Id, payment?.Status == PaymentStatus.Paid);
        db.Notifications.Add(new Notification { RecipientId = booking.CustomerId == actor.Id ? booking.WorkerId : booking.CustomerId, Kind = "booking." + status, ResourceId = id });
        if (status is BookingStatus.Completed or BookingStatus.Cancelled)
        {
            var request = await db.Requests.SingleAsync(x => x.Id == booking.RequestId, ct);
            request.Status = status == BookingStatus.Completed ? RequestStatus.Completed : RequestStatus.Cancelled;
            request.Version = Guid.NewGuid();
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return booking;
    }

    public async Task<ChatMessage> SendMessageAsync(UserProfile actor, Guid id, string text, CancellationToken ct)
    {
        var booking = await GetBookingAsync(actor, id, ct);
        if (booking.Status == BookingStatus.Cancelled) throw new BusinessException(409, "La reserva está cancelada.");
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000) throw new BusinessException(400, "Mensaje inválido.");
        var message = new ChatMessage { BookingId = id, SenderId = actor.Id, Text = text.Trim() };
        db.Messages.Add(message);
        db.Notifications.Add(new Notification { RecipientId = booking.CustomerId == actor.Id ? booking.WorkerId : booking.CustomerId, Kind = "chat.message", ResourceId = id });
        await db.SaveChangesAsync(ct);
        return message;
    }
}
