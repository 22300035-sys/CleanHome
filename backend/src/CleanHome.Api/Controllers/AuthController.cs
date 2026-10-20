using System.Security.Claims;
using CleanHome.Application.Abstractions.Identity;
using CleanHome.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CleanHome.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly UserRegistrationService _registration;
    private readonly IAuthenticationService _authentication;

    public AuthController(
        UserRegistrationService registration,
        IAuthenticationService authentication)
    {
        _registration = registration;
        _authentication = authentication;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _registration.RegisterAsync(
                request,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (DuplicateUserEmailException)
        {
            return Problem(
                statusCode: 409,
                title: "Correo no disponible",
                detail: "No se puede registrar la cuenta con este correo.");
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: 400,
                title: "Datos de registro inválidos",
                detail: exception.Message);
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authentication.LoginAsync(
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (AuthenticationException)
        {
            return Unauthorized(new
            {
                message = "Las credenciales no son válidas."
            });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authentication.RefreshAsync(
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (AuthenticationException)
        {
            return Unauthorized(new
            {
                message = "La sesión no puede renovarse."
            });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var sessionId))
            return Unauthorized();

        await _authentication.LogoutAsync(
            userId,
            sessionId,
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out _))
            return Unauthorized();

        await _authentication.LogoutAllAsync(
            userId,
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions(
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var sessionId))
            return Unauthorized();

        var sessions = await _authentication.GetSessionsAsync(
            userId,
            sessionId,
            cancellationToken);

        return Ok(sessions);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out _))
            return Unauthorized();

        var result = await _authentication.GetCurrentUserAsync(
            userId,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    private bool TryGetIdentity(
        out Guid userId,
        out Guid sessionId)
    {
        userId = Guid.Empty;
        sessionId = Guid.Empty;

        var subject = User.FindFirstValue("sub");
        var session = User.FindFirstValue("sid");

        return Guid.TryParse(subject, out userId) &&
               Guid.TryParse(session, out sessionId);
    }
}
