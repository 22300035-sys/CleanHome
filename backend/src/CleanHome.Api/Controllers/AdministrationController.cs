using System.Security.Claims;
using CleanHome.Application.Abstractions.Administration;
using CleanHome.Application.Features.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanHome.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin")]
[Produces("application/json")]
public sealed class AdministrationController : ControllerBase
{
    private readonly IAdministrationService _service;

    public AdministrationController(IAdministrationService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public Task<IActionResult> MyPermissions(
        CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetPermissionsAsync(UserId(), ct)));

    [HttpGet("workers")]
    public Task<IActionResult> Workers(
        [FromQuery] string? status,
        CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetWorkersAsync(
                UserId(), status, ct)));

    [HttpGet("workers/{workerId:guid}/documents")]
    public Task<IActionResult> WorkerDocuments(
        Guid workerId,
        CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetWorkerDocumentsAsync(
                UserId(), workerId, ct)));

    [HttpPost("workers/{workerId:guid}/review")]
    public Task<IActionResult> ReviewWorker(
        Guid workerId,
        [FromBody] ReviewWorkerRequest request,
        CancellationToken ct) =>
        Execute(async () =>
        {
            await _service.ReviewWorkerAsync(
                UserId(), workerId, request, ct);

            return NoContent();
        });

    [HttpPost("documents/{documentId:guid}/review")]
    public Task<IActionResult> ReviewDocument(
        Guid documentId,
        [FromBody] ReviewDocumentRequest request,
        CancellationToken ct) =>
        Execute(async () =>
        {
            await _service.ReviewDocumentAsync(
                UserId(), documentId, request, ct);

            return NoContent();
        });

    [HttpGet("incidents")]
    public Task<IActionResult> Incidents(
        [FromQuery] string? status,
        CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetIncidentsAsync(
                UserId(), status, ct)));

    [HttpPost("incidents/{incidentId:guid}/status")]
    public Task<IActionResult> ChangeIncidentStatus(
        Guid incidentId,
        [FromBody] ChangeIncidentStatusRequest request,
        CancellationToken ct) =>
        Execute(async () =>
        {
            await _service.ChangeIncidentStatusAsync(
                UserId(), incidentId, request, ct);

            return NoContent();
        });

    [HttpGet("incidents/{incidentId:guid}/comments")]
    public Task<IActionResult> IncidentComments(
        Guid incidentId,
        CancellationToken ct) =>
        Execute(async () => Ok(
            await _service.GetIncidentCommentsAsync(
                UserId(), incidentId, ct)));

    [HttpPost("incidents/{incidentId:guid}/comments")]
    public Task<IActionResult> AddIncidentComment(
        Guid incidentId,
        [FromBody] AdminIncidentCommentRequest request,
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
        catch (AdministrationForbiddenException)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "No tienes permiso para esta operación." });
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
        catch (AdministrationConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
