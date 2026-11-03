using CleanHome.Application.Abstractions.Verification;
using CleanHome.Application.Features.Verification;
using CleanHome.Domain.Entities.Identity;
using CleanHome.Domain.Entities.Verification;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence;

public sealed class DocumentUploadService : IDocumentUploadService
{
    private readonly CleanHomeDbContext _db;
    private readonly IPrivateQuarantineStorage _storage;
    private readonly IDocumentFileValidator _validator;
    private readonly IContentScanService _contentScanService;

    public DocumentUploadService(
        CleanHomeDbContext db,
        IPrivateQuarantineStorage storage,
        IDocumentFileValidator validator,
        IContentScanService contentScanService)
    {
        _db = db;
        _storage = storage;
        _validator = validator;
        _contentScanService = contentScanService;
    }

    public async Task<DocumentUploadResponse> UploadAsync(
        Guid userId,
        string documentType,
        Guid? customerLocationId,
        Stream content,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new DocumentForbiddenException();

        using var contentBuffer = new MemoryStream();
        await content.CopyToAsync(contentBuffer, cancellationToken);
        contentBuffer.Position = 0;

        var validation = await _validator.ValidateAsync(
            contentBuffer, cancellationToken);

        if (!validation.IsValid)
            throw new DocumentValidationException(
                validation.Reason, validation.IsTooLarge);

        contentBuffer.Position = 0;

        var stored = await _storage.SaveAsync(
            contentBuffer, cancellationToken);

        var resolvedMimeType = string.IsNullOrWhiteSpace(validation.MimeType)
            ? mimeType
            : validation.MimeType;

        var document = new PrivateDocument(
            userId,
            documentType,
            customerLocationId,
            stored.StorageKey,
            resolvedMimeType,
            stored.ByteLength,
            stored.Sha256);

        try
        {
            if (document.CustomerLocationId is Guid locationId)
            {
                var location = await _db.CustomerLocations
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.Id == locationId &&
                             x.CustomerUserId == userId,
                        cancellationToken);

                if (location is null)
                    throw new DocumentForbiddenException();
            }

            contentBuffer.Position = 0;

            var scan = await _contentScanService.ScanAsync(
                contentBuffer, cancellationToken);

            document.SetSecurityStatus(
                scan.Status switch
                {
                    ContentScanStatus.Blocked => "blocked",
                    ContentScanStatus.Clean => "clean",
                    _ => "pending",
                });

            _db.PrivateDocuments.Add(document);
            _db.SecurityEvents.Add(new SecurityEvent(
                "document_upload",
                document.SecurityStatus,
                userId));

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Si la persistencia falla, se elimina el archivo huérfano
            // generado en cuarentena para no dejar residuos privados.
            await _storage.DeleteAsync(stored.StorageKey, cancellationToken);

            throw;
        }

        return new DocumentUploadResponse(
            document.Id,
            document.DocumentType,
            document.SecurityStatus,
            document.UploadedAt);
    }

    public async Task<IReadOnlyList<UserDocumentResponse>> GetMyDocumentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new DocumentForbiddenException();

        var documents = await _db.PrivateDocuments
            .AsNoTracking()
            .Where(x => x.OwnerUserId == userId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new UserDocumentResponse(
                x.Id,
                x.DocumentType,
                x.SecurityStatus,
                x.ModerationStatus,
                x.ReviewStatus,
                x.UploadedAt))
            .ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            _db.DocumentAccessAudits.Add(new DocumentAccessAudit(
                document.Id, userId, "owner", "view"));
        }

        if (documents.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return documents;
    }

    public async Task<UserDocumentResponse?> GetMyDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new DocumentForbiddenException();

        var document = await _db.PrivateDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == documentId &&
                     x.OwnerUserId == userId,
                cancellationToken);

        if (document is null)
            throw new KeyNotFoundException("Documento no encontrado.");

        _db.DocumentAccessAudits.Add(new DocumentAccessAudit(
            document.Id, userId, "owner", "view"));

        await _db.SaveChangesAsync(cancellationToken);

        return new UserDocumentResponse(
            document.Id,
            document.DocumentType,
            document.SecurityStatus,
            document.ModerationStatus,
            document.ReviewStatus,
            document.UploadedAt);
    }
}
