namespace CleanHome.Application.Features.Community;

public sealed record SendChatMessageRequest(string Content);

public sealed record ChatMessageResponse(
    Guid Id,
    Guid BookingId,
    Guid SenderUserId,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Body,
    Guid? RelatedEntityId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record CreateReviewRequest(
    int Rating,
    string Comment);

public sealed record ReviewResponse(
    Guid Id,
    Guid BookingId,
    Guid WorkerProfileId,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);

public sealed record WorkerRatingResponse(
    Guid WorkerProfileId,
    int ReviewCount,
    double AverageRating,
    IReadOnlyList<ReviewResponse> Reviews);

public sealed record CreateIncidentRequest(
    Guid? BookingId,
    string Subject,
    string Description);

public sealed record IncidentResponse(
    Guid Id,
    Guid? BookingId,
    string Subject,
    string Description,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record AddIncidentCommentRequest(string Content);

public sealed record IncidentCommentResponse(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt);

public sealed class CommunityConflictException : Exception
{
    public CommunityConflictException(string message)
        : base(message) { }
}
