using System.Security.Claims;
using CleanHome.Application.Marketplace;
using CleanHome.Infrastructure.Identity;
using CleanHome.Infrastructure.Marketplace;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanHome.Api.Controllers;

[ApiController, Authorize, Route("api/v1")]
public sealed class AccountController(MarketplaceService marketplace, CleanHomeDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications([FromQuery, System.ComponentModel.DataAnnotations.Range(1, 10000)] int page = 1, CancellationToken ct = default)
    {
        var actor = await marketplace.ActorAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        return Ok(await db.Notifications.AsNoTracking().Where(x => x.RecipientId == actor.Id).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct));
    }
    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var actor = await marketplace.ActorAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        var notification = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.RecipientId == actor.Id, ct) ?? throw new BusinessException(404, "Notificación no encontrada.");
        notification.ReadAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpPut("profiles/me")]
    public async Task<IActionResult> Update(ProfileInput input, CancellationToken ct)
    {
        var profile = await marketplace.ActorAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        if (input.Role != profile.Role) throw new BusinessException(409, "No puedes cambiar de rol.");
        if (string.IsNullOrWhiteSpace(input.DisplayName) || input.DisplayName.Trim().Length < 2) throw new BusinessException(400, "Nombre inválido.");
        profile.DisplayName = input.DisplayName.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(profile);
    }
    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded) throw new BusinessException(409, "No fue posible cerrar las sesiones.");
        return NoContent();
    }
}
