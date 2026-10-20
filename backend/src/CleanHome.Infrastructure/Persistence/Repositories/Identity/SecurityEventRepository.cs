using CleanHome.Application.Abstractions.Identity;
using CleanHome.Domain.Entities.Identity;

namespace CleanHome.Infrastructure.Persistence.Repositories.Identity;

public sealed class SecurityEventRepository
    : ISecurityEventRepository
{
    private readonly CleanHomeDbContext _db;

    public SecurityEventRepository(CleanHomeDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(
        string eventType,
        string outcome,
        Guid? userId = null,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var securityEvent = new SecurityEvent(
            eventType,
            outcome,
            userId,
            sessionId);

        _db.SecurityEvents.Add(securityEvent);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
