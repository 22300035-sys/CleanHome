using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Api.Controllers;

[ApiController, Authorize, Route("api/v1/workers")]
public sealed class WorkersController(CleanHomeDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkerSummary>> Get(Guid id, CancellationToken ct)
    {
        var worker = await db.Profiles.AsNoTracking().Where(x => x.Id == id && x.Role == AccountRole.Worker && x.IsVerified && !x.IsSuspended)
            .Select(x => new { x.Id, x.DisplayName, x.IsVerified }).SingleOrDefaultAsync(ct)
            ?? throw new BusinessException(404, "Profesional no disponible.");
        var count = await db.Bookings.CountAsync(x => x.WorkerId == id && x.Status == BookingStatus.Completed, ct);
        return Ok(new WorkerSummary(worker.Id, worker.DisplayName, worker.IsVerified, count));
    }
}

public sealed record WorkerSummary(Guid Id, string DisplayName, bool IsVerified, int CompletedServices);
