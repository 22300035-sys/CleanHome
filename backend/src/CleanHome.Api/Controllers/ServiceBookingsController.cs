using System.Security.Claims;
using CleanHome.Application.Abstractions.Bookings;
using CleanHome.Application.Features.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanHome.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
public sealed class ServiceBookingsController : ControllerBase
{
    private readonly IServiceBookingService _service;

    public ServiceBookingsController(IServiceBookingService service)
    {
        _service = service;
    }

    [HttpPost("service-requests")]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateServiceRequestDto request,
        CancellationToken ct) =>
        await Execute(async () =>
        {
            var result = await _service.CreateRequestAsync(
                UserId(), request, ct);

            return Created(
                $"/api/v1/service-requests/{result.Id}",
                result);
        });

    [HttpGet("service-requests/mine")]
    public async Task<IActionResult> MyRequests(CancellationToken ct) =>
        Ok(await _service.GetMyRequestsAsync(UserId(), ct));

    [HttpGet("service-requests/available")]
    public async Task<IActionResult> AvailableRequests(
        [FromQuery] string? city, CancellationToken ct) =>
        await Execute(async () => Ok(
            await _service.BrowseRequestsAsync(UserId(), city, ct)));

    [HttpGet("service-requests/{id:guid}")]
    public async Task<IActionResult> RequestDetail(
        Guid id, CancellationToken ct) =>
        await Execute(async () =>
        {
            var result = await _service.GetRequestAsync(UserId(), id, ct);
            return result is null ? NotFound() : Ok(result);
        });

    [HttpPost("service-requests/{id:guid}/cancel")]
    public async Task<IActionResult> CancelRequest(
        Guid id, CancellationToken ct) =>
        await Execute(async () =>
        {
            await _service.CancelRequestAsync(UserId(), id, ct);
            return NoContent();
        });

    [HttpPost("service-requests/{id:guid}/quotes")]
    public async Task<IActionResult> CreateQuote(
        Guid id,
        [FromBody] CreateServiceQuoteDto request,
        CancellationToken ct) =>
        await Execute(async () =>
        {
            var result = await _service.CreateQuoteAsync(
                UserId(), id, request, ct);

            return Created(
                $"/api/v1/service-requests/{id}/quotes",
                result);
        });

    [HttpGet("service-requests/{id:guid}/quotes")]
    public async Task<IActionResult> GetQuotes(
        Guid id, CancellationToken ct) =>
        await Execute(async () => Ok(
            await _service.GetQuotesAsync(UserId(), id, ct)));

    [HttpPost("service-quotes/{id:guid}/withdraw")]
    public async Task<IActionResult> WithdrawQuote(
        Guid id, CancellationToken ct) =>
        await Execute(async () =>
        {
            await _service.WithdrawQuoteAsync(UserId(), id, ct);
            return NoContent();
        });

    [HttpPost("service-requests/{requestId:guid}/quotes/{quoteId:guid}/accept")]
    public async Task<IActionResult> AcceptQuote(
        Guid requestId, Guid quoteId, CancellationToken ct) =>
        await Execute(async () =>
        {
            var booking = await _service.AcceptQuoteAsync(
                UserId(), requestId, quoteId, ct);

            return Created($"/api/v1/bookings/{booking.Id}", booking);
        });

    [HttpGet("bookings/mine")]
    public async Task<IActionResult> MyBookings(CancellationToken ct) =>
        Ok(await _service.GetMyBookingsAsync(UserId(), ct));

    [HttpGet("bookings/{id:guid}")]
    public async Task<IActionResult> BookingDetail(
        Guid id, CancellationToken ct) =>
        await Execute(async () =>
        {
            var result = await _service.GetBookingAsync(UserId(), id, ct);
            return result is null ? NotFound() : Ok(result);
        });

    [HttpPost("bookings/{id:guid}/start")]
    public async Task<IActionResult> StartBooking(
        Guid id, CancellationToken ct) =>
        await ChangeStatus(id, "start", null, ct);

    [HttpPost("bookings/{id:guid}/complete")]
    public async Task<IActionResult> CompleteBooking(
        Guid id, CancellationToken ct) =>
        await ChangeStatus(id, "complete", null, ct);

    [HttpPost("bookings/{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(
        Guid id,
        [FromBody] CancelBookingDto request,
        CancellationToken ct) =>
        await ChangeStatus(id, "cancel", request.Reason, ct);

    [HttpPost("service-requests/{id:guid}/repeat")]
    public async Task<IActionResult> RepeatRequest(
        Guid id,
        [FromBody] RepeatServiceRequestDto request,
        CancellationToken ct) =>
        await Execute(async () =>
        {
            var result = await _service.RepeatRequestAsync(
                UserId(), id, request, ct);

            return Created(
                $"/api/v1/service-requests/{result.Id}",
                result);
        });

    private Task<IActionResult> ChangeStatus(
        Guid bookingId,
        string action,
        string? reason,
        CancellationToken ct) =>
        Execute(async () =>
        {
            await _service.ChangeBookingStatusAsync(
                UserId(), bookingId, action, reason, ct);

            return NoContent();
        });

    private Guid UserId()
    {
        if (!Guid.TryParse(
            User.FindFirstValue("sub"), out var userId))
        {
            throw new UnauthorizedAccessException(
                "Token de identidad inválido.");
        }

        return userId;
    }

    private async Task<IActionResult> Execute(
        Func<Task<IActionResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new
            {
                message = "El recurso solicitado no existe."
            });
        }
        catch (BookingConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
