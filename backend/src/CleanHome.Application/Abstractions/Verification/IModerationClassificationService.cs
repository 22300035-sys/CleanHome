namespace CleanHome.Application.Abstractions.Verification;

public enum ModerationClassificationStatus
{
    Allowed,
    Prohibited,
    Uncertain
}

public sealed record ModerationClassificationResult(
    ModerationClassificationStatus Status,
    string? Reason = null);

public interface IModerationClassificationService
{
    Task<ModerationClassificationResult> ClassifyAsync(
        Stream content,
        string mimeType,
        CancellationToken cancellationToken = default);
}
