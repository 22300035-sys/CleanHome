namespace CleanHome.Application.Features.Administration;

public sealed record AdminWorkerResponse(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string ServiceCity,
    string ServiceArea,
    string VerificationStatus,
    bool IsPublished,
    DateTimeOffset CreatedAt);

public sealed record AdminWorkerDocumentResponse(
    Guid Id,
    Guid WorkerProfileId,
    string DocumentType,
    string ReviewStatus,
    DateTimeOffset UploadedAt);

public sealed record ReviewWorkerRequest(
    string Status,
    string? Reason);

public sealed record ReviewDocumentRequest(
    string Status,
    string? Reason);

public sealed record AdminIncidentResponse(
    Guid Id,
    Guid ReporterUserId,
    Guid? BookingId,
    string Subject,
    string Description,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record ChangeIncidentStatusRequest(
    string Status,
    string? Reason);

public sealed record AdminIncidentCommentRequest(
    string Content,
    bool IsInternal);

public sealed record AdminIncidentCommentResponse(
    Guid Id,
    Guid AuthorUserId,
    string Content,
    bool IsInternal,
    DateTimeOffset CreatedAt);

public sealed record AdminPermissionResponse(
    string Role,
    bool CanManageIncidents,
    bool CanReviewWorkers);

public sealed class AdministrationForbiddenException : Exception
{
    public AdministrationForbiddenException()
        : base("No tienes permiso para realizar esta operación.")
    {
    }
}

public sealed class AdministrationConflictException : Exception
{
    public AdministrationConflictException(string message)
        : base(message)
    {
    }
}
