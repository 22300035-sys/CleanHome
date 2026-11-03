using System.Security.Claims;
using CleanHome.Application.Abstractions.Verification;
using CleanHome.Application.Features.Verification;
using CleanHome.Infrastructure.Persistence;
using CleanHome.Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanHome.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentUploadService _uploads;

    public DocumentsController(IDocumentUploadService uploads)
    {
        _uploads = uploads;
    }

    [Authorize]
    [HttpPost("documents")]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? documentType,
        [FromForm] string? customerLocationId,
        CancellationToken ct)
    {
        if (file is null)
            return BadRequest(new { message = "El archivo es obligatorio." });

        if (file.Length > LocalDocumentFileValidator.MaxPdfBytes)
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new { message = "El archivo supera el tamaño permitido." });

        using var buffer = new MemoryStream();
        await file.OpenReadStream().CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var locationId = Guid.TryParse(
            customerLocationId ?? string.Empty,
            out var parsedLocationId)
            ? parsedLocationId
            : (Guid?)null;

        Guid userId;

        if (!TryGetUserId(out userId))
            return Unauthorized();

        try
        {
            var result = await _uploads.UploadAsync(
                userId,
                documentType ?? string.Empty,
                locationId,
                buffer,
                file.ContentType ?? string.Empty,
                ct);

            return Created(
                $"/api/v1/documents/{result.Id}",
                result);
        }
        catch (DocumentValidationException ex)
        {
            if (ex.IsTooLarge)
                return StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    new { message = ex.Message });

            return BadRequest(new { message = ex.Message });
        }
        catch (DocumentForbiddenException)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "No tienes permiso para subir este documento." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("documents")]
    public async Task<IActionResult> GetMyDocuments(
        CancellationToken ct)
    {
        Guid userId;

        if (!TryGetUserId(out userId))
            return Unauthorized();

        try
        {
            var documents = await _uploads.GetMyDocumentsAsync(
                userId, ct);

            return Ok(documents);
        }
        catch (DocumentForbiddenException)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "No tienes permiso para consultar estos documentos." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("documents/{documentId:guid}")]
    public async Task<IActionResult> GetMyDocument(
        Guid documentId,
        CancellationToken ct)
    {
        Guid userId;

        if (!TryGetUserId(out userId))
            return Unauthorized();

        try
        {
            var document = await _uploads.GetMyDocumentAsync(
                userId, documentId, ct);

            if (document is null)
                return NotFound(new { message = "Documento no encontrado." });

            return Ok(document);
        }
        catch (DocumentForbiddenException)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "No tienes permiso para consultar este documento." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Documento no encontrado." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var subject = User.FindFirstValue("sub");

        return Guid.TryParse(subject, out userId);
    }
}
