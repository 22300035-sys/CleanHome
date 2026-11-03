namespace CleanHome.Application.Abstractions.Verification;

public enum ContentScanStatus
{
    Clean,
    Blocked,
    Pending
}

public sealed record ContentScanResult(
    ContentScanStatus Status,
    string? Reason = null);

public interface IContentScanService
{
    Task<ContentScanResult> ScanAsync(
        Stream content,
        CancellationToken cancellationToken = default);
}
