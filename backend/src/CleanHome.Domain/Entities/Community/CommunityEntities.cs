namespace CleanHome.Domain.Entities.Community;

public sealed class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Content { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private ChatMessage() { }

    public ChatMessage(
        Guid bookingId,
        Guid senderUserId,
        string content)
    {
        if (bookingId == Guid.Empty || senderUserId == Guid.Empty)
            throw new ArgumentException("Identificador inválido.");

        content = content?.Trim() ?? "";

        if (content.Length is < 1 or > 4000)
            throw new ArgumentException(
                "El mensaje debe tener entre 1 y 4000 caracteres.");

        Id = Guid.NewGuid();
        BookingId = bookingId;
        SenderUserId = senderUserId;
        Content = content;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRead()
    {
        ReadAt ??= DateTimeOffset.UtcNow;
    }
}

public sealed class UserNotification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string Body { get; private set; } = "";
    public Guid? RelatedEntityId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private UserNotification() { }

    public UserNotification(
        Guid userId,
        string type,
        string title,
        string body,
        Guid? relatedEntityId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Usuario inválido.");

        type = type?.Trim().ToLowerInvariant() ?? "";
        title = title?.Trim() ?? "";
        body = body?.Trim() ?? "";

        if (type.Length is < 1 or > 60 ||
            title.Length is < 1 or > 150 ||
            body.Length is < 1 or > 1000 ||
            relatedEntityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Los datos de la notificación son inválidos.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
        RelatedEntityId = relatedEntityId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRead()
    {
        ReadAt ??= DateTimeOffset.UtcNow;
    }
}

public sealed class ServiceReview
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid CustomerUserId { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    private ServiceReview() { }

    public ServiceReview(
        Guid bookingId,
        Guid customerUserId,
        Guid workerProfileId,
        int rating,
        string comment)
    {
        if (bookingId == Guid.Empty ||
            customerUserId == Guid.Empty ||
            workerProfileId == Guid.Empty)
            throw new ArgumentException("Identificador inválido.");

        comment = comment?.Trim() ?? "";

        if (rating is < 1 or > 5)
            throw new ArgumentException(
                "La calificación debe estar entre 1 y 5.");

        if (comment.Length > 2000)
            throw new ArgumentException(
                "El comentario no puede superar 2000 caracteres.");

        Id = Guid.NewGuid();
        BookingId = bookingId;
        CustomerUserId = customerUserId;
        WorkerProfileId = workerProfileId;
        Rating = rating;
        Comment = comment;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class SupportIncident
{
    public Guid Id { get; private set; }
    public Guid ReporterUserId { get; private set; }
    public Guid? BookingId { get; private set; }

    public string Subject { get; private set; } = "";
    public string Description { get; private set; } = "";

    // open, in_review, resolved, closed
    public string Status { get; private set; } = "open";

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private SupportIncident() { }

    public SupportIncident(
        Guid reporterUserId,
        string subject,
        string description,
        Guid? bookingId = null)
    {
        if (reporterUserId == Guid.Empty ||
            bookingId == Guid.Empty)
            throw new ArgumentException("Identificador inválido.");

        subject = subject?.Trim() ?? "";
        description = description?.Trim() ?? "";

        if (subject.Length is < 5 or > 150 ||
            description.Length is < 20 or > 4000)
        {
            throw new ArgumentException(
                "Los datos de la incidencia son inválidos.");
        }

        Id = Guid.NewGuid();
        ReporterUserId = reporterUserId;
        BookingId = bookingId;
        Subject = subject;
        Description = description;
        Status = "open";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void ChangeStatus(string status)
    {
        if (status is not (
            "open" or "in_review" or "resolved" or "closed"))
        {
            throw new ArgumentException("Estado inválido.");
        }

        if (Status == "closed" && status != "closed")
        {
            throw new InvalidOperationException(
                "Una incidencia cerrada no puede reabrirse.");
        }

        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class IncidentComment
{
    public Guid Id { get; private set; }
    public Guid IncidentId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Content { get; private set; } = "";
    public bool IsInternal { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private IncidentComment() { }

    public IncidentComment(
        Guid incidentId,
        Guid authorUserId,
        string content,
        bool isInternal = false)
    {
        if (incidentId == Guid.Empty ||
            authorUserId == Guid.Empty)
            throw new ArgumentException("Identificador inválido.");

        content = content?.Trim() ?? "";

        if (content.Length is < 1 or > 3000)
            throw new ArgumentException(
                "Comentario inválido.");

        Id = Guid.NewGuid();
        IncidentId = incidentId;
        AuthorUserId = authorUserId;
        Content = content;
        IsInternal = isInternal;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class AdminMembership
{
    public Guid UserId { get; private set; }

    // support, moderator, administrator
    public string Role { get; private set; } = "";
    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AdminMembership() { }

    public AdminMembership(Guid userId, string role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Usuario inválido.");

        ValidateRole(role);

        UserId = userId;
        Role = role;
        IsEnabled = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void ChangeRole(string role)
    {
        ValidateRole(role);
        Role = role;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateRole(string role)
    {
        if (role is not (
            "support" or "moderator" or "administrator"))
        {
            throw new ArgumentException(
                "Rol administrativo inválido.");
        }
    }
}
