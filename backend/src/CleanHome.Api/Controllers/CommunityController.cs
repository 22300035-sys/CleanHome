using System.Security.Claims;
using CleanHome.Application.Abstractions.Community;
using CleanHome.Application.Features.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanHome.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
public sealed class CommunityController : ControllerBase
{
    private readonly ICommunityService _service;

    public CommunityController(ICommunityService service)
    {
        _service = service;
    }

    [HttpGet("bookings/{bookingId:guid}/messages")]
    public Task<IActionResult> Messages(
        Guid bookingId, CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetMessagesAsync(UserId(), bookingId, ct)));

    [HttpPost("bookings/{bookingId:guid}/messages")]
    public Task<IActionResult> SendMessage(
        Guid bookingId,
        [FromBody] SendChatMessageRequest request,
        CancellationToken ct) =>
        Execute(async () =>
        {
            var message = await _service.SendMessageAsync(
                UserId(), bookingId, request, ct);

            return Ok(message);
        });

    [HttpPost("bookings/{bookingId:guid}/messages/read")]
    public Task<IActionResult> ReadMessages(
        Guid bookingId, CancellationToken ct) =>
        Execute(async () =>
        {
            await _service.MarkMessagesReadAsync(
                UserId(), bookingId, ct);

            return NoContent();
        });

    [HttpGet("notifications")]
    public Task<IActionResult> Notifications(CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetNotificationsAsync(UserId(), ct)));

    [HttpPost("notifications/{id:guid}/read")]
    public Task<IActionResult> ReadNotification(
        Guid id, CancellationToken ct) =>
        Execute(async () =>
        {
            await _service.MarkNotificationReadAsync(
                UserId(), id, ct);

            return NoContent();
        });

    [HttpPost("bookings/{bookingId:guid}/reviews")]
    public Task<IActionResult> CreateReview(
        Guid bookingId,
        [FromBody] CreateReviewRequest request,
        CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.CreateReviewAsync(
                UserId(), bookingId, request, ct)));

    [AllowAnonymous]
    [HttpGet("workers/{workerId:guid}/reviews")]
    public Task<IActionResult> WorkerReviews(
        Guid workerId, CancellationToken ct) =>
        Execute(async () =>
        {
            var result = await _service.GetWorkerRatingAsync(
                workerId, ct);

            return result is null ? NotFound() : Ok(result);
        });

    [HttpPost("incidents")]
    public Task<IActionResult> CreateIncident(
        [FromBody] CreateIncidentRequest request,
        CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.CreateIncidentAsync(
                UserId(), request, ct)));

    [HttpGet("incidents/mine")]
    public Task<IActionResult> MyIncidents(CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetMyIncidentsAsync(UserId(), ct)));

    [HttpGet("incidents/{incidentId:guid}/comments")]
    public Task<IActionResult> IncidentComments(
        Guid incidentId, CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetIncidentCommentsAsync(
                UserId(), incidentId, ct)));

    [HttpPost("incidents/{incidentId:guid}/comments")]
    public Task<IActionResult> AddIncidentComment(
        Guid incidentId,
        [FromBody] AddIncidentCommentRequest request,
        CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.AddIncidentCommentAsync(
                UserId(), incidentId, request, ct)));

    private Guid UserId()
    {
        var subject = User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId))
            throw new UnauthorizedAccessException(
                "Identidad inválida.");

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
                message = "Recurso no encontrado."
            });
        }
        catch (CommunityConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
