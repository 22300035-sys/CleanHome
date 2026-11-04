using System.Security.Claims;
using CleanHome.Infrastructure.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CleanHome.Api.Hubs;

[Authorize]
public sealed class BookingChatHub(MarketplaceService service) : Hub
{
    public static string Group(Guid bookingId) => "booking:" + bookingId;
    public async Task JoinBooking(Guid bookingId)
    {
        var actor = await service.ActorAsync(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!, Context.ConnectionAborted);
        await service.GetBookingAsync(actor, bookingId, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(bookingId));
    }
    public Task LeaveBooking(Guid bookingId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(bookingId));
}
