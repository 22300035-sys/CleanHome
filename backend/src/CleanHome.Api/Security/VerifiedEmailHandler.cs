using System.Security.Claims;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Api.Security;

public sealed class VerifiedEmailHandler
    : AuthorizationHandler<VerifiedEmailRequirement>
{
    private readonly CleanHomeDbContext _db;

    public VerifiedEmailHandler(CleanHomeDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VerifiedEmailRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var subject = context.User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId))
            return;

        var eligible = await _db.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == userId &&
                user.IsActive &&
                user.IsEmailVerified);

        if (eligible)
            context.Succeed(requirement);
    }
}
