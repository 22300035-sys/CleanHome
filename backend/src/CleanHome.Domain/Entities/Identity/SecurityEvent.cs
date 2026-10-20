namespace CleanHome.Domain.Entities.Identity;

public sealed class SecurityEvent
{
    public Guid Id { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? SessionId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    private SecurityEvent()
    {
    }

    public SecurityEvent(
        string eventType,
        string outcome,
        Guid? userId = null,
        Guid? sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(eventType) ||
            eventType.Length > 80)
        {
            throw new ArgumentException(
                "El tipo de evento no es válido.",
                nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(outcome) ||
            outcome.Length > 30)
        {
            throw new ArgumentException(
                "El resultado del evento no es válido.",
                nameof(outcome));
        }

        if (userId == Guid.Empty || sessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Los identificadores no pueden estar vacíos.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        SessionId = sessionId;
        EventType = eventType.Trim().ToLowerInvariant();
        Outcome = outcome.Trim().ToLowerInvariant();
        OccurredAt = DateTimeOffset.UtcNow;
    }
}
