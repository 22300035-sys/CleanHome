using System.Security.Claims;
using CleanHome.Api.Hubs;
using CleanHome.Application.Marketplace;
using CleanHome.Infrastructure.Marketplace;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Api.Controllers;

[ApiController, Authorize, Route("api/v1/bookings/{id:guid}/messages")]
public sealed class ChatController(MarketplaceService service, CleanHomeDbContext db, IHubContext<BookingChatHub> hub) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid id, [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default)
    {
        var actor = await service.ActorAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        await service.GetBookingAsync(actor, id, ct);
        return Ok(await db.Messages.AsNoTracking().Where(x => x.BookingId == id).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct));
    }
    [HttpPost]
    public async Task<IActionResult> Send(Guid id, MessageInput input, CancellationToken ct)
    {
        var actor = await service.ActorAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        var message = await service.SendMessageAsync(actor, id, input.Text, ct);
        var booking = await service.GetBookingAsync(actor, id, ct);
        var recipients = await db.Profiles.Where(x => (x.Id == booking.CustomerId || x.Id == booking.WorkerId) && !x.IsSuspended).Select(x => x.UserId).ToListAsync(ct);
        await hub.Clients.Users(recipients).SendAsync("MessageReceived", message, ct);
        return StatusCode(201, message);
    }
}
