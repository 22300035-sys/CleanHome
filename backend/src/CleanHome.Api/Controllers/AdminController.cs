using System.Security.Claims;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Payments;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Api.Controllers;

[ApiController, Authorize(Roles = "Admin"), Route("api/v1/admin")]
public sealed class AdminController(CleanHomeDbContext db, PaymentService payments) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private void Audit(string action, Guid id) => db.Audit.Add(new AuditEntry { ActorUserId = UserId, Action = action, ResourceId = id });
    [HttpGet("bookings")]
    public async Task<ActionResult<IReadOnlyList<ServiceBooking>>> Bookings([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default)
        => Ok(await db.Bookings.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct));
    [HttpGet("requests")]
    public async Task<ActionResult<IReadOnlyList<ServiceRequest>>> Requests([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default)
        => Ok(await db.Requests.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct));
    [HttpPost("service-categories")]
    public async Task<IActionResult> CreateCategory(CategoryInput input, CancellationToken ct)
    {
        ServiceCategory category;
        try { category = new ServiceCategory(input.Name, input.Slug, input.Description); }
        catch (ArgumentException ex) { throw new BusinessException(400, ex.Message); }
        db.ServiceCategories.Add(category);
        Audit("category.create", category.Id);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { category.Id, category.Name, category.Slug, category.Description });
    }
    [HttpPost("service-categories/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateCategory(Guid id, CancellationToken ct)
    {
        var category = await db.ServiceCategories.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException(404, "Categoría no encontrada.");
        category.Deactivate();
        Audit("category.deactivate", id);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpGet("profiles")]
    public async Task<IActionResult> Profiles([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default) => Ok(await db.Profiles.AsNoTracking().OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct));
    [HttpPost("profiles/{id:guid}/verify")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var profile = await db.Profiles.SingleOrDefaultAsync(x => x.Id == id && x.Role == AccountRole.Worker, ct) ?? throw new BusinessException(404, "Trabajador no encontrado.");
        if (!await db.Documents.AnyAsync(x => x.OwnerId == id && x.Status == DocumentStatus.Approved, ct)) throw new BusinessException(409, "Se requiere al menos un documento aprobado.");
        profile.IsVerified = true;
        Audit("worker.verify", id);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpPost("profiles/{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromQuery] bool suspended, CancellationToken ct)
    {
        var profile = await db.Profiles.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException(404, "Perfil no encontrado.");
        profile.IsSuspended = suspended;
        Audit(suspended ? "profile.suspend" : "profile.restore", id);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpGet("documents")]
    public async Task<IActionResult> Documents([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default) => Ok(await db.Documents.AsNoTracking().OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).Select(x => new { x.Id, x.OwnerId, x.OriginalName, x.Status, x.CreatedAt }).ToListAsync(ct));
    [HttpPost("documents/{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, ReviewInput input, CancellationToken ct)
    {
        if (input.Status is not (DocumentStatus.Approved or DocumentStatus.Rejected)) throw new BusinessException(400, "Estado de revisión inválido.");
        var doc = await db.Documents.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException(404, "Documento no encontrado.");
        doc.Status = input.Status;
        doc.ReviewNotes = input.Notes?.Trim() ?? "";
        if (input.Status == DocumentStatus.Rejected)
        {
            var profile = await db.Profiles.SingleAsync(x => x.Id == doc.OwnerId, ct);
            profile.IsVerified = false;
            db.Entry(profile).Property(x => x.IsVerified).IsModified = true;
        }
        Audit("document.review." + input.Status, id);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpGet("payments")]
    public async Task<IActionResult> Payments([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default) => Ok(await db.Payments.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct));
    [HttpPost("payments/{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, CancellationToken ct) { await payments.RefundAsync(UserId, id, ct); return NoContent(); }
    [HttpGet("audit")]
    public async Task<IActionResult> AuditLog([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default) => Ok(await db.Audit.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 100).Take(100).ToListAsync(ct));
}
