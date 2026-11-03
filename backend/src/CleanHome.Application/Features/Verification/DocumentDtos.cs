namespace CleanHome.Application.Features.Verification;

public sealed record DocumentUploadResponse(
    Guid Id,
    string DocumentType,
    string Status,
    DateTimeOffset UploadedAt);

public sealed class DocumentValidationException : Exception
{
    public DocumentValidationException(string reason)
        : base(reason)
    {
    }

    public DocumentValidationException(string reason, bool isTooLarge)
        : base(reason)
    {
        IsTooLarge = isTooLarge;
    }

    public bool IsTooLarge { get; }
}

public sealed record UserDocumentResponse(
    Guid Id,
    string DocumentType,
    string SecurityStatus,
    string ModerationStatus,
    string ReviewStatus,
    DateTimeOffset UploadedAt);

public sealed class DocumentForbiddenException : Exception
{
    public DocumentForbiddenException()
        : base("No tienes permiso para realizar esta operación.")
    {
    }
}
