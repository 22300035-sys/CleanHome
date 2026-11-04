using System.Security.Claims;
using CleanHome.Domain.Entities;
using CleanHome.Application.Marketplace;
using CleanHome.Infrastructure.Marketplace;
using CleanHome.Infrastructure.Payments;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class PaymentsController(PaymentService payments, MarketplaceService marketplace, CleanHomeDbContext db) : ControllerBase
{
    [Authorize, HttpPost("bookings/{id:guid}/checkout")]
    public async Task<ActionResult<Payment>> Checkout(Guid id, CheckoutInput input, CancellationToken ct)
    {
        var actor = await marketplace.ActorAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        return Ok(await payments.CheckoutAsync(actor, id, input.Provider.ToLowerInvariant(), ct));
    }
    [Authorize, HttpGet("bookings/{id:guid}/payment")]
    public async Task<ActionResult<Payment?>> Payment(Guid id, CancellationToken ct)
    {
        var actor = await marketplace.ActorAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        await marketplace.GetBookingAsync(actor, id, ct);
        return Ok(await db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.BookingId == id, ct));
    }
    [AllowAnonymous, HttpPost("payments/webhooks/{provider}"), RequestSizeLimit(256 * 1024)]
    public async Task<IActionResult> Webhook(string provider, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var headers = Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var query = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        await payments.WebhookAsync(provider, body, headers, query, ct);
        return Ok();
    }
}

