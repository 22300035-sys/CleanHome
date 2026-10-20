using System.Net;
using System.Net.Mail;
using CleanHome.Application.Abstractions.Identity;
using Microsoft.Extensions.Configuration;

namespace CleanHome.Infrastructure.Security;

public sealed class SmtpAccountEmailSender : IAccountEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpAccountEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task SendVerificationAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        var link = CreateLink("verify-email", token);

        return SendAsync(
            email,
            "Verifica tu correo de CleanHome",
            $"Para verificar tu correo, abre este enlace:\n\n{link}\n\n" +
            "Caduca en 24 horas. Si no solicitaste esto, ignora el mensaje.",
            cancellationToken);
    }

    public Task SendPasswordResetAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        var link = CreateLink("reset-password", token);

        return SendAsync(
            email,
            "Recuperación de contraseña de CleanHome",
            $"Para establecer una nueva contraseña, abre:\n\n{link}\n\n" +
            "Caduca en 30 minutos. Si no lo solicitaste, ignora el mensaje.",
            cancellationToken);
    }

    private string CreateLink(string route, string token)
    {
        var baseUrl = _configuration["AccountLinks:BaseUrl"];

        if (!Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             !(uri.Scheme == Uri.UriSchemeHttp &&
               uri.IsLoopback)))
        {
            throw new InvalidOperationException(
                "Configura AccountLinks:BaseUrl con una URL HTTPS " +
                "o una URL HTTP de localhost para desarrollo.");
        }

        return $"{baseUrl!.TrimEnd('/')}/{route}" +
               $"?token={Uri.EscapeDataString(token)}";
    }

    private async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var host = _configuration["Smtp:Host"];
        var sender = _configuration["Smtp:From"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(sender))
        {
            throw new InvalidOperationException(
                "El servicio SMTP no está configurado.");
        }

        if (!int.TryParse(
                _configuration["Smtp:Port"],
                out var port) ||
            port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "El puerto SMTP no es válido.");
        }

        var useTls = bool.TryParse(
            _configuration["Smtp:EnableSsl"],
            out var enabled) && enabled;

        // Fuera del desarrollo local exigimos STARTTLS.
        if (!useTls &&
            host is not ("localhost" or "127.0.0.1" or "::1"))
        {
            throw new InvalidOperationException(
                "El correo remoto requiere SMTP con TLS.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(sender),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(recipient);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = useTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(username)
                ? null
                : new NetworkCredential(username, password),
            Timeout = 15000
        };

        cancellationToken.ThrowIfCancellationRequested();

        await client.SendMailAsync(message, cancellationToken);
    }
}
