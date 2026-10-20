using CleanHome.Application.Abstractions.Identity;
using CleanHome.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CleanHome.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AccountRecoveryController : ControllerBase
{
    private readonly IAccountRecoveryService _recovery;

    public AccountRecoveryController(
        IAccountRecoveryService recovery)
    {
        _recovery = recovery;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] EmailAddressRequest request,
        CancellationToken cancellationToken)
    {
        await _recovery.RequestVerificationAsync(
            request.Email,
            cancellationToken);

        return Accepted(new AccountRecoveryMessage(
            "Si la cuenta cumple los requisitos, " +
            "recibirás un correo con las instrucciones."));
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _recovery.VerifyEmailAsync(
                request.Token,
                cancellationToken);

            return Ok(new AccountRecoveryMessage(
                "El correo electrónico fue verificado."));
        }
        catch (InvalidAccountTokenException)
        {
            return Problem(
                statusCode: 400,
                title: "Enlace inválido o expirado");
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] EmailAddressRequest request,
        CancellationToken cancellationToken)
    {
        await _recovery.RequestPasswordResetAsync(
            request.Email,
            cancellationToken);

        return Accepted(new AccountRecoveryMessage(
            "Si existe una cuenta elegible, " +
            "recibirás instrucciones de recuperación."));
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _recovery.ResetPasswordAsync(
                request.Token,
                request.NewPassword,
                cancellationToken);

            return Ok(new AccountRecoveryMessage(
                "La contraseña fue actualizada. " +
                "Inicia sesión nuevamente."));
        }
        catch (InvalidAccountTokenException)
        {
            return Problem(
                statusCode: 400,
                title: "Enlace inválido o expirado");
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: 400,
                title: "Contraseña inválida",
                detail: exception.Message);
        }
    }
}
