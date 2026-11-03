namespace CleanHome.Application.Abstractions.Verification;

public sealed record DocumentValidationResult(
    bool IsValid,
    string Reason,
    string? MimeType = null,
    bool IsTooLarge = false);

public interface IDocumentFileValidator
{
    Task<DocumentValidationResult> ValidateAsync(
        Stream content,
        CancellationToken cancellationToken = default);
}
