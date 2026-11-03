using System.Security.Claims;
using CleanHome.Application.Abstractions.Verification;
using CleanHome.Application.Features.Verification;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanHome.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
public sealed class LocationsController : ControllerBase
{
    private readonly ICustomerLocationService _locations;

    public LocationsController(ICustomerLocationService locations)
    {
        _locations = locations;
    }

    [HttpPost("locations")]
    public async Task<IActionResult> Create(
        [FromBody] CustomerLocationRequest request,
        CancellationToken ct)
    {
        Guid userId;

        if (!TryGetUserId(out userId))
            return Unauthorized();

        try
        {
            var location = await _locations.CreateAsync(
                userId, request, ct);

            return Created(
                $"/api/v1/locations/{location.Id}",
                location);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("locations")]
    public async Task<IActionResult> GetMy(
        CancellationToken ct)
    {
        Guid userId;

        if (!TryGetUserId(out userId))
            return Unauthorized();

        try
        {
            var locations = await _locations.GetMyAsync(userId, ct);

            return Ok(locations);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var subject = User.FindFirstValue("sub");

        return Guid.TryParse(subject, out userId);
    }
}
