using CleanHome.Application.Features.Administration;

namespace CleanHome.Application.Abstractions.Administration;

public interface IAdministrationService
{
    Task<AdminPermissionResponse> GetPermissionsAsync(
        Guid actorUserId,
        CancellationToken ct);

    Task<IReadOnlyList<AdminWorkerResponse>> GetWorkersAsync(
        Guid actorUserId,
        string? status,
        CancellationToken ct);

    Task<IReadOnlyList<AdminWorkerDocumentResponse>> GetWorkerDocumentsAsync(
        Guid actorUserId,
        Guid workerProfileId,
        CancellationToken ct);

    Task ReviewWorkerAsync(
        Guid actorUserId,
        Guid workerProfileId,
        ReviewWorkerRequest request,
        CancellationToken ct);

    Task ReviewDocumentAsync(
        Guid actorUserId,
        Guid documentId,
        ReviewDocumentRequest request,
        CancellationToken ct);

    Task<IReadOnlyList<AdminIncidentResponse>> GetIncidentsAsync(
        Guid actorUserId,
        string? status,
        CancellationToken ct);

    Task ChangeIncidentStatusAsync(
        Guid actorUserId,
        Guid incidentId,
        ChangeIncidentStatusRequest request,
        CancellationToken ct);

    Task<IReadOnlyList<AdminIncidentCommentResponse>> GetIncidentCommentsAsync(
        Guid actorUserId,
        Guid incidentId,
        CancellationToken ct);

    Task<AdminIncidentCommentResponse> AddIncidentCommentAsync(
        Guid actorUserId,
        Guid incidentId,
        AdminIncidentCommentRequest request,
        CancellationToken ct);
}
