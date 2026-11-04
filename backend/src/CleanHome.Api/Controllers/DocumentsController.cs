using System.Security.Claims;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Marketplace;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Api.Controllers;

[ApiController, Authorize, Route("api/v1/documents")]
public sealed class DocumentsController(CleanHomeDbContext db, MarketplaceService marketplace, IPrivateFileStore files) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static DocumentView View(PrivateDocument d) => new(d.Id, d.OriginalName, d.ContentType, d.Length, d.Status, d.ReviewNotes, d.CreatedAt);

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        var actor = await marketplace.ActorAsync(UserId, ct);
        if (file.Length is <= 0 or > 5 * 1024 * 1024) throw new BusinessException(400, "El archivo debe pesar entre 1 byte y 5 MB.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (await db.Documents.CountAsync(x => x.OwnerId == actor.Id, ct) >= 10) throw new BusinessException(409, "Máximo 10 documentos por perfil.");
        await using var source = file.OpenReadStream();
        var header = new byte[8];
        var read = await source.ReadAsync(header, ct);
        var type = read >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8) ? "application/pdf"
            : read == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? "image/png"
            : read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255 ? "image/jpeg" : null;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (type is null || !(type == "application/pdf" && ext == ".pdf" || type == "image/png" && ext == ".png" || type == "image/jpeg" && ext is ".jpg" or ".jpeg"))
            throw new BusinessException(400, "Solo PDF, PNG y JPEG con firma y extensión coincidentes.");
        source.Position = 0;
        var key = await files.WriteAsync(source, ct);
        try
        {
            var doc = new PrivateDocument { OwnerId = actor.Id, StorageKey = key, OriginalName = Path.GetFileName(file.FileName)[..Math.Min(Path.GetFileName(file.FileName).Length, 200)], ContentType = type, Length = file.Length };
            db.Documents.Add(doc);
            db.Audit.Add(new AuditEntry { ActorUserId = UserId, Action = "document.upload", ResourceId = doc.Id });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return StatusCode(201, View(doc));
        }
        catch { files.Delete(key); throw; }
    }
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var actor = await marketplace.ActorAsync(UserId, ct);
        return Ok((await db.Documents.AsNoTracking().Where(x => x.OwnerId == actor.Id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(View));
    }
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var doc = await db.Documents.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException(404, "Documento no encontrado.");
        if (!User.IsInRole("Admin"))
        {
            var actor = await marketplace.ActorAsync(UserId, ct);
            if (actor.Id != doc.OwnerId) throw new BusinessException(404, "Documento no encontrado.");
        }
        db.Audit.Add(new AuditEntry { ActorUserId = UserId, Action = "document.download", ResourceId = id });
        await db.SaveChangesAsync(ct);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(files.Read(doc.StorageKey), "application/octet-stream", doc.OriginalName);
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var actor = await marketplace.ActorAsync(UserId, ct);
        var doc = await db.Documents.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == actor.Id, ct) ?? throw new BusinessException(404, "Documento no encontrado.");
        db.Documents.Remove(doc);
        // Al retirar evidencia, la verificación del trabajador debe revisarse otra vez.
        if (actor.Role == AccountRole.Worker)
        {
            actor.IsVerified = false;
            db.Entry(actor).Property(x => x.IsVerified).IsModified = true;
        }
        db.Audit.Add(new AuditEntry { ActorUserId = UserId, Action = "document.delete", ResourceId = id });
        await db.SaveChangesAsync(ct);
        files.Delete(doc.StorageKey);
        return NoContent();
    }
}
