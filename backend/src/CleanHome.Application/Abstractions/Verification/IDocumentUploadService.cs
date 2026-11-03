using CleanHome.Application.Features.Verification;

namespace CleanHome.Application.Abstractions.Verification;

public interface IDocumentUploadService
{
    Task<DocumentUploadResponse> UploadAsync(
        Guid userId,
        string documentType,
        Guid? customerLocationId,
        Stream content,
        string mimeType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDocumentResponse>> GetMyDocumentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserDocumentResponse?> GetMyDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
