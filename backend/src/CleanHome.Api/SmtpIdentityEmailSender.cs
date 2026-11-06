using System.Net;
using System.Net.Mail;
using CleanHome.Application.Marketplace;
using CleanHome.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CleanHome.Api;

public sealed class SmtpIdentityEmailSender(IConfiguration config, IWebHostEnvironment environment, ILogger<SmtpIdentityEmailSender> logger) : IEmailSender<ApplicationUser>
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
        try
        {
            await client.SendMailAsync(message);
            logger.LogInformation("SMTP accepted message: {Kind}", subject);
        }
        catch (SmtpException exception)
        {
            logger.LogWarning("SMTP rejected message: {Kind}; status {Status}", subject, exception.StatusCode);
            throw new BusinessException(503, "No pudimos enviar el correo. Espera unos minutos y vuelve a intentarlo.");
        }
    }
    // MapIdentityApi supplies an HTML-encoded URL. Normalize once before rendering
    // so an email client decodes &amp; to &, rather than &amp;amp; to &amp;.
    public static string LinkBody(string link, string label) => $"<a href=\"{WebUtility.HtmlEncode(WebUtility.HtmlDecode(link))}\">{WebUtility.HtmlEncode(label)}</a>";
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) => SendAsync(email, "Confirma tu cuenta CleanHome", LinkBody(confirmationLink, "Confirmar cuenta"));
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => SendAsync(email, "Recupera tu cuenta CleanHome", LinkBody(resetLink, "Cambiar contraseña"));
    public static string PasswordResetBody(string email, string code, string? publicBaseUrl)
    {
        var rawCode = WebUtility.HtmlDecode(code);
        var fallback = $"<p>Código de recuperación:</p><p>{WebUtility.HtmlEncode(rawCode)}</p>";
        if (string.IsNullOrWhiteSpace(publicBaseUrl)) return fallback;
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var origin) || origin.Scheme != "https" || !string.IsNullOrEmpty(origin.UserInfo) || origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment))
            throw new BusinessException(503, "La URL pública del correo no está configurada correctamente.");
        // Fragment prevents the email/token from being sent in the request URL or Referer.
        var link = origin.GetLeftPart(UriPartial.Authority) + "/restablecer#email=" + Uri.EscapeDataString(email) + "&code=" + Uri.EscapeDataString(rawCode);
        return "<p>Solicitaste cambiar tu contraseña de CleanHome.</p>" + LinkBody(link, "Restablecer contraseña") + fallback + "<p>Si no lo solicitaste, ignora este mensaje. Tu contraseña no cambia al abrir el enlace.</p>";
    }
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) => SendAsync(email, "Restablece tu contraseña de CleanHome", PasswordResetBody(email, resetCode, config["Email:PublicBaseUrl"]));
}
