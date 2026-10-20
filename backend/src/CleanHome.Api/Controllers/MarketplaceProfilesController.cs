using System.Security.Claims;
using CleanHome.Application.Abstractions.Marketplace;
using CleanHome.Application.Features.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanHome.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public sealed class MarketplaceProfilesController : ControllerBase
{
    private readonly IMarketplaceProfileService _profiles;

    public MarketplaceProfilesController(
        IMarketplaceProfileService profiles)
    {
        _profiles = profiles;
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(
        CancellationToken ct)
    {
        var result = await _profiles.GetUserProfileAsync(
            CurrentUserId(), ct);

        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _profiles.UpdateUserProfileAsync(
                CurrentUserId(), request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("worker/profile")]
    public async Task<IActionResult> GetWorkerProfile(
        CancellationToken ct)
    {
        var result = await _profiles.GetWorkerDashboardAsync(
            CurrentUserId(), ct);

        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPut("worker/profile")]
    public async Task<IActionResult> UpdateWorkerProfile(
        [FromBody] UpsertWorkerProfileRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _profiles.UpsertWorkerProfileAsync(
                CurrentUserId(), request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("worker/offerings")]
    public async Task<IActionResult> UpsertOffering(
        [FromBody] UpsertOfferingRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _profiles.UpsertOfferingAsync(
                CurrentUserId(), request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPatch("worker/offerings/{offeringId:guid}/active")]
    public async Task<IActionResult> SetOfferingActive(
        Guid offeringId,
        [FromBody] SetActiveRequest request,
        CancellationToken ct)
    {
        try
        {
            await _profiles.SetOfferingActiveAsync(
                CurrentUserId(),
                offeringId,
                request.Active,
                ct);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("worker/availability")]
    public async Task<IActionResult> ReplaceAvailability(
        [FromBody] List<AvailabilitySlotRequest> slots,
        CancellationToken ct)
    {
        try
        {
            await _profiles.ReplaceAvailabilityAsync(
                CurrentUserId(), slots, ct);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPatch("worker/publication")]
    public async Task<IActionResult> SetPublication(
        [FromBody] SetPublishedRequest request,
        CancellationToken ct)
    {
        try
        {
            await _profiles.SetPublishedAsync(
                CurrentUserId(), request.Published, ct);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("workers")]
    public async Task<IActionResult> BrowseWorkers(
        [FromQuery] string? city,
        [FromQuery] Guid? categoryId,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _profiles.BrowseWorkersAsync(
                city, categoryId, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId))
            throw new UnauthorizedAccessException(
                "El JWT no contiene un usuario válido.");

        return userId;
    }

    public sealed record SetActiveRequest(bool Active);

    public sealed record SetPublishedRequest(bool Published);
}
