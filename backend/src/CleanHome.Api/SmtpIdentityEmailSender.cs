using System.Net;
using System.Net.Mail;
using CleanHome.Application.Marketplace;
using CleanHome.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CleanHome.Api;

public sealed class SmtpIdentityEmailSender(IConfiguration config, IWebHostEnvironment environment) : IEmailSender<ApplicationUser>
{
    private async Task SendAsync(string email, string subject, string html)
    {
        var host = config["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            if (environment.IsDevelopment() || environment.IsEnvironment("Testing")) return;
            throw new BusinessException(503, "Correo no configurado; configura Email antes de solicitar confirmación o recuperación.");
        }
        using var client = new SmtpClient(host, config.GetValue("Email:Port", 587))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(config["Email:Username"], config["Email:Password"]),
            Timeout = 15000
        };
        using var message = new MailMessage(config["Email:From"] ?? throw new InvalidOperationException("Falta Email:From."), email, subject, html) { IsBodyHtml = true };
        await client.SendMailAsync(message);
    }
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) => SendAsync(email, "Confirma tu cuenta CleanHome", $"<a href=\"{WebUtility.HtmlEncode(confirmationLink)}\">Confirmar cuenta</a>");
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => SendAsync(email, "Recupera tu cuenta CleanHome", $"<a href=\"{WebUtility.HtmlEncode(resetLink)}\">Cambiar contraseña</a>");
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) => SendAsync(email, "Código de recuperación CleanHome", WebUtility.HtmlEncode(resetCode));
}
