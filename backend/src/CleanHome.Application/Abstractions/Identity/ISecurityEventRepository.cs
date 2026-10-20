namespace CleanHome.Application.Abstractions.Identity;

public interface ISecurityEventRepository
{
    Task RecordAsync(
        string eventType,
        string outcome,
        Guid? userId = null,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);
}
