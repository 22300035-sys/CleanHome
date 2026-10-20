using CleanHome.Application.Abstractions.Administration;
using CleanHome.Application.Features.Administration;
using CleanHome.Domain.Entities.Community;
using CleanHome.Domain.Entities.Identity;
using CleanHome.Domain.Entities.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence.Administration;

public sealed class AdministrationService : IAdministrationService
{
    private readonly CleanHomeDbContext _db;

    public AdministrationService(CleanHomeDbContext db)
    {
        _db = db;
    }

    public async Task<AdminPermissionResponse> GetPermissionsAsync(
        Guid actorUserId,
        CancellationToken ct)
    {
        var role = await RequireRoleAsync(actorUserId, ct);

        return new AdminPermissionResponse(
            role,
            true,
            role is "moderator" or "administrator");
    }

    public async Task<IReadOnlyList<AdminWorkerResponse>> GetWorkersAsync(
        Guid actorUserId,
        string? status,
        CancellationToken ct)
    {
        await RequireModeratorAsync(actorUserId, ct);

        if (!string.IsNullOrWhiteSpace(status) &&
            status is not ("pending" or "approved" or "rejected" or "suspended"))
        {
            throw new ArgumentException("Estado inválido.");
        }

        var query =
            from worker in _db.WorkerProfiles.AsNoTracking()
            join user in _db.Users.AsNoTracking()
                on worker.UserId equals user.Id
            select new { worker, user };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x =>
                x.worker.VerificationStatus == status);

        return await query
            .OrderByDescending(x => x.worker.CreatedAt)
            .Take(100)
            .Select(x => new AdminWorkerResponse(
                x.worker.Id,
                x.worker.UserId,
                x.user.FirstName + " " + x.user.LastName,
                x.worker.ServiceCity,
                x.worker.ServiceArea,
                x.worker.VerificationStatus,
                x.worker.IsPublished,
                x.worker.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AdminWorkerDocumentResponse>>
        GetWorkerDocumentsAsync(
            Guid actorUserId,
            Guid workerProfileId,
            CancellationToken ct)
    {
        await RequireModeratorAsync(actorUserId, ct);

        var worker = await _db.WorkerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == workerProfileId, ct);

        if (worker is null)
            throw new KeyNotFoundException("Trabajador no encontrado.");

        if (worker.UserId == actorUserId)
            throw new AdministrationForbiddenException();

        return await _db.WorkerDocuments
            .AsNoTracking()
            .Where(x => x.WorkerProfileId == workerProfileId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new AdminWorkerDocumentResponse(
                x.Id,
                x.WorkerProfileId,
                x.DocumentType,
                x.ReviewStatus,
                x.UploadedAt))
            .ToListAsync(ct);
    }

    public async Task ReviewWorkerAsync(
        Guid actorUserId,
        Guid workerProfileId,
        ReviewWorkerRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await RequireModeratorAsync(actorUserId, ct);

        if (request.Status is not (
            "approved" or "rejected" or "suspended" or "pending"))
        {
            throw new ArgumentException("Estado de revisión inválido.");
        }

        ValidateReason(request.Reason);

        if (request.Status is "rejected" or "suspended" &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException(
                "Debes indicar el motivo de la decisión.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var rows = await _db.WorkerProfiles
            .FromSqlInterpolated(
                $"SELECT * FROM worker_profiles WHERE id = {workerProfileId} FOR UPDATE")
            .ToListAsync(ct);

        var worker = rows.SingleOrDefault()
            ?? throw new KeyNotFoundException("Trabajador no encontrado.");

        if (worker.UserId == actorUserId)
            throw new AdministrationForbiddenException();

        if (request.Status == "approved")
        {
            var documents = await _db.WorkerDocuments
                .AsNoTracking()
                .Where(x => x.WorkerProfileId == workerProfileId)
                .ToListAsync(ct);

            if (documents.Count == 0 ||
                documents.Any(x => x.ReviewStatus != "approved"))
            {
                throw new AdministrationConflictException(
                    "El trabajador debe contar con documentos aprobados.");
            }

            var ownerEligible = await _db.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == worker.UserId &&
                         x.IsActive &&
                         x.IsEmailVerified,
                    ct);

            if (!ownerEligible)
                throw new AdministrationConflictException(
                    "La cuenta debe estar activa y tener correo verificado.");
        }

        worker.SetVerificationStatus(request.Status);

        _db.SecurityEvents.Add(new SecurityEvent(
            "admin_worker_review",
            request.Status,
            actorUserId));

        _db.UserNotifications.Add(new UserNotification(
            worker.UserId,
            "worker_verification",
            "Actualización de verificación",
            "El estado de verificación de tu perfil fue actualizado.",
            worker.Id));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task ReviewDocumentAsync(
        Guid actorUserId,
        Guid documentId,
        ReviewDocumentRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await RequireModeratorAsync(actorUserId, ct);

        if (request.Status is not ("approved" or "rejected" or "pending"))
            throw new ArgumentException("Estado documental inválido.");

        ValidateReason(request.Reason);

        if (request.Status == "rejected" &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException(
                "Debes indicar el motivo del rechazo.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        // Orden de bloqueos consistente: primero trabajador,
        // después documento, para las operaciones administrativas.
        var workerId = await _db.WorkerDocuments
            .AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => (Guid?)x.WorkerProfileId)
            .SingleOrDefaultAsync(ct);

        if (!workerId.HasValue)
            throw new KeyNotFoundException("Documento no encontrado.");

        var workers = await _db.WorkerProfiles
            .FromSqlInterpolated(
                $"SELECT * FROM worker_profiles WHERE id = {workerId.Value} FOR UPDATE")
            .ToListAsync(ct);

        var worker = workers.SingleOrDefault()
            ?? throw new KeyNotFoundException("Trabajador no encontrado.");

        if (worker.UserId == actorUserId)
            throw new AdministrationForbiddenException();

        var documents = await _db.WorkerDocuments
            .FromSqlInterpolated(
                $"SELECT * FROM worker_documents WHERE id = {documentId} FOR UPDATE")
            .ToListAsync(ct);

        var document = documents.SingleOrDefault()
            ?? throw new KeyNotFoundException("Documento no encontrado.");

        document.SetReviewStatus(request.Status);

        // Una revisión documental desfavorable invalida
        // cualquier aprobación previa del perfil.
        if (request.Status != "approved" &&
            worker.VerificationStatus == "approved")
        {
            worker.SetVerificationStatus("pending");
        }

        _db.SecurityEvents.Add(new SecurityEvent(
            "admin_document_review",
            request.Status,
            actorUserId));

        _db.UserNotifications.Add(new UserNotification(
            worker.UserId,
            "document_review",
            "Actualización de documentos",
            "Se actualizó la revisión de un documento de tu perfil.",
            worker.Id));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<AdminIncidentResponse>>
        GetIncidentsAsync(
            Guid actorUserId,
            string? status,
            CancellationToken ct)
    {
        await RequireRoleAsync(actorUserId, ct);

        if (!string.IsNullOrWhiteSpace(status) &&
            status is not ("open" or "in_review" or "resolved" or "closed"))
        {
            throw new ArgumentException("Estado inválido.");
        }

        var query = _db.SupportIncidents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new AdminIncidentResponse(
                x.Id,
                x.ReporterUserId,
                x.BookingId,
                x.Subject,
                x.Description,
                x.Status,
                x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task ChangeIncidentStatusAsync(
        Guid actorUserId,
        Guid incidentId,
        ChangeIncidentStatusRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await RequireRoleAsync(actorUserId, ct);

        ValidateReason(request.Reason);

        if (request.Status is not (
            "open" or "in_review" or "resolved" or "closed"))
        {
            throw new ArgumentException("Estado inválido.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var rows = await _db.SupportIncidents
            .FromSqlInterpolated(
                $"SELECT * FROM support_incidents WHERE id = {incidentId} FOR UPDATE")
            .ToListAsync(ct);

        var incident = rows.SingleOrDefault()
            ?? throw new KeyNotFoundException("Incidencia no encontrada.");

        incident.ChangeStatus(request.Status);

        _db.SecurityEvents.Add(new SecurityEvent(
            "admin_incident_status",
            request.Status,
            actorUserId));

        _db.UserNotifications.Add(new UserNotification(
            incident.ReporterUserId,
            "incident_status",
            "Actualización de incidencia",
            "El estado de tu reporte de soporte cambió.",
            incident.Id));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<AdminIncidentCommentResponse>>
        GetIncidentCommentsAsync(
            Guid actorUserId,
            Guid incidentId,
            CancellationToken ct)
    {
        await RequireRoleAsync(actorUserId, ct);

        var exists = await _db.SupportIncidents
            .AnyAsync(x => x.Id == incidentId, ct);

        if (!exists)
            throw new KeyNotFoundException("Incidencia no encontrada.");

        return await _db.IncidentComments
            .AsNoTracking()
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .Select(x => new AdminIncidentCommentResponse(
                x.Id,
                x.AuthorUserId,
                x.Content,
                x.IsInternal,
                x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<AdminIncidentCommentResponse> AddIncidentCommentAsync(
        Guid actorUserId,
        Guid incidentId,
        AdminIncidentCommentRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await RequireRoleAsync(actorUserId, ct);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var rows = await _db.SupportIncidents
            .FromSqlInterpolated(
                $"SELECT * FROM support_incidents WHERE id = {incidentId} FOR UPDATE")
            .ToListAsync(ct);

        var incident = rows.SingleOrDefault()
            ?? throw new KeyNotFoundException("Incidencia no encontrada.");

        if (incident.Status == "closed")
            throw new AdministrationConflictException(
                "No puedes responder una incidencia cerrada.");

        var comment = new IncidentComment(
            incidentId,
            actorUserId,
            request.Content,
            request.IsInternal);

        _db.IncidentComments.Add(comment);

        if (!request.IsInternal)
        {
            _db.UserNotifications.Add(new UserNotification(
                incident.ReporterUserId,
                "incident_reply",
                "Nueva respuesta de soporte",
                "El equipo de CleanHome respondió a tu incidencia.",
                incident.Id));
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new AdminIncidentCommentResponse(
            comment.Id,
            comment.AuthorUserId,
            comment.Content,
            comment.IsInternal,
            comment.CreatedAt);
    }

    private async Task<string> RequireRoleAsync(
        Guid userId,
        CancellationToken ct)
    {
        var membership = await _db.AdminMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserId == userId &&
                     x.IsEnabled,
                ct);

        var isActiveUser = await _db.Users
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == userId &&
                     x.IsActive &&
                     x.IsEmailVerified,
                ct);

        if (membership is null || !isActiveUser ||
            membership.Role is not (
                "support" or "moderator" or "administrator"))
        {
            throw new AdministrationForbiddenException();
        }

        return membership.Role;
    }

    private async Task RequireModeratorAsync(
        Guid userId,
        CancellationToken ct)
    {
        var role = await RequireRoleAsync(userId, ct);

        if (role is not ("moderator" or "administrator"))
            throw new AdministrationForbiddenException();
    }

    private static void ValidateReason(string? reason)
    {
        if (reason?.Length > 500)
            throw new ArgumentException(
                "El motivo no puede superar 500 caracteres.");
    }
}
