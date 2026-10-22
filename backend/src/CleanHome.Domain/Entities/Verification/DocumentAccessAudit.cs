namespace CleanHome.Domain.Entities.Verification;

public sealed class DocumentAccessAudit
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid ActorUserId { get; private set; }

    // owner, administrator, support
    public string AccessRole { get; private set; } = "";

    // view, download, review
    public string Action { get; private set; } = "";

    public Guid? SupportIncidentId { get; private set; }
    public DateTimeOffset AccessedAt { get; private set; }

    private DocumentAccessAudit() { }

    public DocumentAccessAudit(
        Guid documentId,
        Guid actorUserId,
        string accessRole,
        string action,
        Guid? supportIncidentId = null)
    {
        if (documentId == Guid.Empty ||
            actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Identificador inválido.");
        }

        if (accessRole is not (
            "owner" or "administrator" or "support"))
        {
            throw new ArgumentException("Rol inválido.");
        }

        if (action is not ("view" or "download" or "review"))
            throw new ArgumentException("Acción inválida.");

        if (accessRole == "support" &&
            !supportIncidentId.HasValue)
        {
            throw new ArgumentException(
                "El soporte requiere una incidencia.");
        }

        if (accessRole != "support" &&
            supportIncidentId.HasValue)
        {
            throw new ArgumentException(
                "Incidencia no aplicable a este acceso.");
        }

        Id = Guid.NewGuid();
        DocumentId = documentId;
        ActorUserId = actorUserId;
        AccessRole = accessRole;
        Action = action;
        SupportIncidentId = supportIncidentId;
        AccessedAt = DateTimeOffset.UtcNow;
    }
}
