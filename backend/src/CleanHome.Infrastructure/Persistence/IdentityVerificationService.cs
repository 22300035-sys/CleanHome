using CleanHome.Application.Abstractions.Verification;
using CleanHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Infrastructure.Persistence;

public sealed class IdentityVerificationService
    : IIdentityVerificationService
{
    private readonly CleanHomeDbContext _db;

    public IdentityVerificationService(CleanHomeDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasApprovedIdentityAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.PrivateDocuments
            .AsNoTracking()
            .AnyAsync(
                x => x.OwnerUserId == userId &&
                     (x.DocumentType == "identity_front" ||
                      x.DocumentType == "identity_back") &&
                     x.SecurityStatus == "clean" &&
                     x.ModerationStatus == "approved" &&
                     x.ReviewStatus == "approved",
                cancellationToken);
    }
}
