using CleanHome.Application.Features.Community;

namespace CleanHome.Application.Abstractions.Community;

public interface ICommunityService
{
    Task<ChatMessageResponse> SendMessageAsync(
        Guid userId, Guid bookingId,
        SendChatMessageRequest request, CancellationToken ct);

    Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(
        Guid userId, Guid bookingId, CancellationToken ct);

    Task MarkMessagesReadAsync(
        Guid userId, Guid bookingId, CancellationToken ct);

    Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(
        Guid userId, CancellationToken ct);

    Task MarkNotificationReadAsync(
        Guid userId, Guid notificationId, CancellationToken ct);

    Task<ReviewResponse> CreateReviewAsync(
        Guid userId, Guid bookingId,
        CreateReviewRequest request, CancellationToken ct);

    Task<WorkerRatingResponse?> GetWorkerRatingAsync(
        Guid workerProfileId, CancellationToken ct);

    Task<IncidentResponse> CreateIncidentAsync(
        Guid userId, CreateIncidentRequest request, CancellationToken ct);

    Task<IReadOnlyList<IncidentResponse>> GetMyIncidentsAsync(
        Guid userId, CancellationToken ct);

    Task<IReadOnlyList<IncidentCommentResponse>> GetIncidentCommentsAsync(
        Guid userId, Guid incidentId, CancellationToken ct);

    Task<IncidentCommentResponse> AddIncidentCommentAsync(
        Guid userId, Guid incidentId,
        AddIncidentCommentRequest request, CancellationToken ct);
}
