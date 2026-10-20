using CleanHome.Application.Abstractions.Community;
using CleanHome.Application.Features.Community;
using CleanHome.Domain.Entities.Bookings;
using CleanHome.Domain.Entities.Community;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence.Community;

public sealed class CommunityService : ICommunityService
{
    private readonly CleanHomeDbContext _db;

    public CommunityService(CleanHomeDbContext db)
    {
        _db = db;
    }

    public async Task<ChatMessageResponse> SendMessageAsync(
        Guid userId,
        Guid bookingId,
        SendChatMessageRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var booking = await RequireBookingParticipantAsync(
            userId, bookingId, ct);

        if (booking.Status is not ("confirmed" or "in_progress"))
            throw new CommunityConflictException(
                "El chat de esta reserva ya no permite mensajes.");

        var message = new ChatMessage(
            bookingId, userId, request.Content);

        _db.ChatMessages.Add(message);

        var recipientId = booking.CustomerUserId == userId
            ? await _db.WorkerProfiles
                .Where(x => x.Id == booking.WorkerProfileId)
                .Select(x => x.UserId)
                .SingleAsync(ct)
            : booking.CustomerUserId;

        _db.UserNotifications.Add(
            new UserNotification(
                recipientId,
                "chat_message",
                "Nuevo mensaje",
                "Tienes un mensaje sobre tu reserva.",
                bookingId));

        await _db.SaveChangesAsync(ct);

        return MapMessage(message);
    }

    public async Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(
        Guid userId, Guid bookingId, CancellationToken ct)
    {
        await RequireBookingParticipantAsync(userId, bookingId, ct);

        var messages = await _db.ChatMessages
            .AsNoTracking()
            .Where(x => x.BookingId == bookingId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(100)
            .ToListAsync(ct);

        messages.Reverse();

        return messages.Select(MapMessage).ToList();
    }

    public async Task MarkMessagesReadAsync(
        Guid userId, Guid bookingId, CancellationToken ct)
    {
        await RequireBookingParticipantAsync(userId, bookingId, ct);

        var unread = await _db.ChatMessages
            .Where(x => x.BookingId == bookingId &&
                        x.SenderUserId != userId &&
                        x.ReadAt == null)
            .ToListAsync(ct);

        foreach (var message in unread)
            message.MarkRead();

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationResponse>>
        GetNotificationsAsync(Guid userId, CancellationToken ct)
    {
        var notifications = await _db.UserNotifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return notifications.Select(MapNotification).ToList();
    }

    public async Task MarkNotificationReadAsync(
        Guid userId, Guid notificationId, CancellationToken ct)
    {
        var notification = await _db.UserNotifications
            .SingleOrDefaultAsync(
                x => x.Id == notificationId &&
                     x.UserId == userId,
                ct);

        if (notification is null)
            throw new KeyNotFoundException(
                "Notificación no encontrada.");

        notification.MarkRead();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ReviewResponse> CreateReviewAsync(
        Guid userId,
        Guid bookingId,
        CreateReviewRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var booking = await _db.ServiceBookings
            .FromSqlInterpolated(
                $"SELECT * FROM service_bookings WHERE id = {bookingId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

        if (booking is null || booking.CustomerUserId != userId)
            throw new KeyNotFoundException("Reserva no encontrada.");

        if (booking.Status != "completed")
            throw new CommunityConflictException(
                "Solo puedes calificar servicios finalizados.");

        var exists = await _db.ServiceReviews.AnyAsync(
            x => x.BookingId == bookingId, ct);

        if (exists)
            throw new CommunityConflictException(
                "Ya calificaste esta reserva.");

        var review = new ServiceReview(
            bookingId,
            userId,
            booking.WorkerProfileId,
            request.Rating,
            request.Comment);

        _db.ServiceReviews.Add(review);

        var workerUserId = await _db.WorkerProfiles
            .Where(x => x.Id == booking.WorkerProfileId)
            .Select(x => x.UserId)
            .SingleAsync(ct);

        _db.UserNotifications.Add(new UserNotification(
            workerUserId,
            "service_review",
            "Recibiste una reseña",
            "Un cliente calificó un servicio finalizado.",
            bookingId));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return MapReview(review);
    }

    public async Task<WorkerRatingResponse?> GetWorkerRatingAsync(
        Guid workerProfileId, CancellationToken ct)
    {
        var published = await _db.WorkerProfiles
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == workerProfileId &&
                     x.IsPublished &&
                     x.VerificationStatus == "approved",
                ct);

        if (!published)
            return null;

        var reviews = await _db.ServiceReviews
            .AsNoTracking()
            .Where(x => x.WorkerProfileId == workerProfileId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        var aggregate = await _db.ServiceReviews
            .AsNoTracking()
            .Where(x => x.WorkerProfileId == workerProfileId)
            .GroupBy(x => x.WorkerProfileId)
            .Select(x => new
            {
                Count = x.Count(),
                Average = x.Average(r => r.Rating)
            })
            .SingleOrDefaultAsync(ct);

        return new WorkerRatingResponse(
            workerProfileId,
            aggregate?.Count ?? 0,
            Math.Round(aggregate?.Average ?? 0, 2),
            reviews.Select(MapReview).ToList());
    }

    public async Task<IncidentResponse> CreateIncidentAsync(
        Guid userId, CreateIncidentRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.BookingId.HasValue)
            await RequireBookingParticipantAsync(
                userId, request.BookingId.Value, ct);

        var incident = new SupportIncident(
            userId,
            request.Subject,
            request.Description,
            request.BookingId);

        _db.SupportIncidents.Add(incident);

        await _db.SaveChangesAsync(ct);

        return MapIncident(incident);
    }

    public async Task<IReadOnlyList<IncidentResponse>>
        GetMyIncidentsAsync(Guid userId, CancellationToken ct)
    {
        var incidents = await _db.SupportIncidents
            .AsNoTracking()
            .Where(x => x.ReporterUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return incidents.Select(MapIncident).ToList();
    }

    public async Task<IReadOnlyList<IncidentCommentResponse>>
        GetIncidentCommentsAsync(
            Guid userId, Guid incidentId, CancellationToken ct)
    {
        await RequireOwnIncidentAsync(userId, incidentId, ct);

        var comments = await _db.IncidentComments
            .AsNoTracking()
            .Where(x => x.IncidentId == incidentId &&
                        !x.IsInternal)
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return comments.Select(MapComment).ToList();
    }

    public async Task<IncidentCommentResponse> AddIncidentCommentAsync(
        Guid userId,
        Guid incidentId,
        AddIncidentCommentRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var incident = await RequireOwnIncidentAsync(
            userId, incidentId, ct);

        if (incident.Status == "closed")
            throw new CommunityConflictException(
                "Esta incidencia ya está cerrada.");

        var comment = new IncidentComment(
            incidentId, userId, request.Content);

        _db.IncidentComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        return MapComment(comment);
    }

    private async Task<ServiceBooking> RequireBookingParticipantAsync(
        Guid userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await _db.ServiceBookings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == bookingId, ct);

        if (booking is null)
            throw new KeyNotFoundException("Reserva no encontrada.");

        if (booking.CustomerUserId == userId)
            return booking;

        var isWorker = await _db.WorkerProfiles
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == booking.WorkerProfileId &&
                     x.UserId == userId,
                ct);

        if (!isWorker)
            throw new KeyNotFoundException("Reserva no encontrada.");

        return booking;
    }

    private async Task<SupportIncident> RequireOwnIncidentAsync(
        Guid userId, Guid incidentId, CancellationToken ct)
    {
        return await _db.SupportIncidents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == incidentId &&
                     x.ReporterUserId == userId,
                ct)
            ?? throw new KeyNotFoundException(
                "Incidencia no encontrada.");
    }

    private static ChatMessageResponse MapMessage(ChatMessage x) =>
        new(x.Id, x.BookingId, x.SenderUserId,
            x.Content, x.CreatedAt, x.ReadAt);

    private static NotificationResponse MapNotification(
        UserNotification x) =>
        new(x.Id, x.Type, x.Title, x.Body,
            x.RelatedEntityId, x.CreatedAt, x.ReadAt);

    private static ReviewResponse MapReview(ServiceReview x) =>
        new(x.Id, x.BookingId, x.WorkerProfileId,
            x.Rating, x.Comment, x.CreatedAt);

    private static IncidentResponse MapIncident(SupportIncident x) =>
        new(x.Id, x.BookingId, x.Subject,
            x.Description, x.Status, x.CreatedAt);

    private static IncidentCommentResponse MapComment(
        IncidentComment x) =>
        new(x.Id, x.Content, x.CreatedAt);
}
