using System.Security.Claims;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Marketplace;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Api.Controllers;

[ApiController, Authorize, Route("api/v1")]
public sealed class MarketplaceController(MarketplaceService service, CleanHomeDbContext db) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private Task<UserProfile> Actor(CancellationToken ct) => service.ActorAsync(UserId, ct);

    [HttpPost("profiles/me")]
    public async Task<ActionResult<UserProfile>> CreateProfile(ProfileInput input, CancellationToken ct) => StatusCode(201, await service.CreateProfileAsync(UserId, input, ct));
    [HttpGet("profiles/me")]
    public async Task<ActionResult<UserProfile>> Profile(CancellationToken ct) => Ok(await Actor(ct));

    [HttpPost("locations")]
    public async Task<ActionResult<CustomerLocation>> Location(LocationInput input, CancellationToken ct) => StatusCode(201, await service.CreateLocationAsync(await Actor(ct), input, ct));
    [HttpGet("locations")]
    public async Task<ActionResult<IReadOnlyList<CustomerLocation>>> Locations(CancellationToken ct)
    {
        var actor = await Actor(ct);
        return Ok(await db.Locations.AsNoTracking().Where(x => x.CustomerId == actor.Id).OrderBy(x => x.Label).Take(100).ToListAsync(ct));
    }
    [HttpPut("locations/{id:guid}")]
    public async Task<ActionResult<CustomerLocation>> UpdateLocation(Guid id, LocationInput input, CancellationToken ct)
    {
        var actor = await Actor(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var location = await db.Locations.SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == actor.Id, ct)
            ?? throw new BusinessException(404, "Dirección no encontrada.");
        if (await db.Requests.AnyAsync(x => x.LocationId == id, ct)) throw new BusinessException(409, "Esta dirección forma parte del historial de una solicitud. Guarda una dirección nueva para próximos servicios.");
        if (string.IsNullOrWhiteSpace(input.Label) || string.IsNullOrWhiteSpace(input.Address) || string.IsNullOrWhiteSpace(input.City)) throw new BusinessException(400, "Dirección incompleta.");
        location.Label = input.Label.Trim(); location.Address = input.Address.Trim(); location.City = input.City.Trim();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(location);
    }
    [HttpDelete("locations/{id:guid}")]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken ct)
    {
        var actor = await Actor(ct);
        var location = await db.Locations.SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == actor.Id, ct)
            ?? throw new BusinessException(404, "Dirección no encontrada.");
        if (await db.Requests.AnyAsync(x => x.LocationId == id, ct)) throw new BusinessException(409, "No se puede eliminar una dirección con solicitudes asociadas.");
        db.Locations.Remove(location);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpPost("requests")]
    public async Task<ActionResult<ServiceRequest>> RequestService(RequestInput input, CancellationToken ct) => StatusCode(201, await service.CreateRequestAsync(await Actor(ct), input, ct));
    [HttpGet("requests")]
    public async Task<ActionResult<Page<RequestView>>> Requests([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 100)] int size = 20, CancellationToken ct = default)
        => Ok(await service.ListRequestsAsync(await Actor(ct), page, size, ct));

    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<IActionResult> CancelRequest(Guid id, CancellationToken ct)
    {
        var actor = await Actor(ct);
        var request = await db.Requests.SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == actor.Id, ct) ?? throw new BusinessException(404, "Solicitud no encontrada.");
        if (request.Status != RequestStatus.Open) throw new BusinessException(409, "Solo se pueden cancelar solicitudes abiertas.");
        request.Status = RequestStatus.Cancelled;
        request.Version = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult<RequestView>> RequestDetail(Guid id, CancellationToken ct)
    {
        var actor = await Actor(ct);
        var request = await db.Requests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException(404, "Solicitud no encontrada.");
        if (request.CustomerId != actor.Id)
        {
            MarketplaceService.RequireRole(actor, AccountRole.Worker);
            if (request.Status != RequestStatus.Open && !await db.Bookings.AnyAsync(x => x.RequestId == id && x.WorkerId == actor.Id, ct)) throw new BusinessException(404, "Solicitud no encontrada.");
        }
        var city = await db.Locations.Where(x => x.Id == request.LocationId).Select(x => x.City).SingleAsync(ct);
        return Ok(new RequestView(request.Id, request.CategoryId, request.Description, city, request.StartsAt, request.DurationMinutes, request.Status, request.CreatedAt));
    }
    [HttpPost("requests/{id:guid}/quotes")]
    public async Task<ActionResult<ServiceQuote>> Quote(Guid id, QuoteInput input, CancellationToken ct) => StatusCode(201, await service.QuoteAsync(await Actor(ct), id, input, ct));
    [HttpGet("requests/{id:guid}/quotes")]
    public async Task<ActionResult<IReadOnlyList<ServiceQuote>>> Quotes(Guid id, CancellationToken ct)
    {
        var actor = await Actor(ct);
        var request = await db.Requests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException(404, "Solicitud no encontrada.");
        var query = db.Quotes.AsNoTracking().Where(x => x.RequestId == id);
        if (request.CustomerId != actor.Id) query = query.Where(x => x.WorkerId == actor.Id);
        return Ok(await query.OrderBy(x => x.Amount).Take(100).ToListAsync(ct));
    }
    [HttpPost("quotes/{id:guid}/accept")]
    public async Task<ActionResult<ServiceBooking>> Accept(Guid id, CancellationToken ct) => StatusCode(201, await service.AcceptAsync(await Actor(ct), id, ct));
    [HttpGet("bookings")]
    public async Task<ActionResult<IReadOnlyList<ServiceBooking>>> Bookings([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default)
    {
        var actor = await Actor(ct);
        return Ok(await db.Bookings.AsNoTracking().Where(x => x.CustomerId == actor.Id || x.WorkerId == actor.Id).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 20).Take(20).ToListAsync(ct));
    }
    [HttpGet("bookings/{id:guid}")]
    public async Task<IActionResult> Booking(Guid id, CancellationToken ct)
    {
        var actor = await Actor(ct);
        var booking = await service.GetBookingAsync(actor, id, ct);
        var request = await db.Requests.AsNoTracking().SingleAsync(x => x.Id == booking.RequestId, ct);
        var location = await db.Locations.AsNoTracking().SingleAsync(x => x.Id == request.LocationId, ct);
        var customerName = await db.Profiles.Where(x => x.Id == booking.CustomerId).Select(x => x.DisplayName).SingleAsync(ct);
        var workerName = await db.Profiles.Where(x => x.Id == booking.WorkerId).Select(x => x.DisplayName).SingleAsync(ct);
        return Ok(new { booking, location, participants = new { customerName, workerName } });
    }
    [HttpPost("bookings/{id:guid}/status")]
    public async Task<ActionResult<ServiceBooking>> Transition(Guid id, BookingTransition input, CancellationToken ct) => Ok(await service.TransitionAsync(await Actor(ct), id, input.Status, ct));
}

