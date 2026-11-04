using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CleanHome.Infrastructure.Payments;

public sealed class MercadoPagoGateway(HttpClient http, IConfiguration config) : IPaymentGateway
{
    public string Name => "mercadopago";
    public void ValidateConfiguration()
    {
        Required("Payments:MercadoPago:AccessToken");
        Required("Payments:MercadoPago:WebhookSecret");
        foreach (var key in new[] { "Payments:ReturnBaseUrl", "Payments:PublicApiUrl" })
            if (!Uri.TryCreate(Required(key), UriKind.Absolute, out var uri) || uri.Scheme != "https") throw new BusinessException(503, "Configura URLs de pagos con HTTPS.");
    }
    private string Required(string key) => !string.IsNullOrWhiteSpace(config[key]) ? config[key]! : throw new BusinessException(503, "Mercado Pago no está configurado.");
    private async Task<JsonDocument> CallAsync(HttpMethod method, string path, object? body, string? key, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, "https://api.mercadopago.com/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Required("Payments:MercadoPago:AccessToken"));
        if (key is not null) request.Headers.Add("X-Idempotency-Key", key);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new BusinessException(502, "Mercado Pago rechazó la operación; revisa la configuración y reintenta.");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }
    public async Task<CheckoutResult> CreateCheckoutAsync(Payment payment, CancellationToken ct)
    {
        var returnUrl = Required("Payments:ReturnBaseUrl").TrimEnd('/');
        using var json = await CallAsync(HttpMethod.Post, "checkout/preferences", new
        {
            external_reference = payment.Id.ToString(),
            items = new[] { new { id = payment.BookingId.ToString(), title = "Servicio CleanHome", quantity = 1, currency_id = payment.Currency, unit_price = payment.Amount } },
            notification_url = Required("Payments:PublicApiUrl").TrimEnd('/') + "/api/v1/payments/webhooks/mercadopago",
            back_urls = new { success = returnUrl + "/payments/success", failure = returnUrl + "/payments/cancel", pending = returnUrl + "/payments/pending" },
            auto_return = "approved"
        }, payment.Id.ToString(), ct);
        var urlField = config.GetValue<bool>("Payments:MercadoPago:Sandbox") ? "sandbox_init_point" : "init_point";
        return new(json.RootElement.GetProperty("id").GetString()!, json.RootElement.GetProperty(urlField).GetString()!);
    }
    public async Task<VerifiedPayment?> VerifyWebhookAsync(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query, CancellationToken ct)
    {
        var dataId = query.GetValueOrDefault("data.id", "").ToLowerInvariant();
        var signature = headers.GetValueOrDefault("x-signature", "");
        var requestId = headers.GetValueOrDefault("x-request-id", "");
        var timestamp = WebhookSignatures.Values(signature, "ts").FirstOrDefault() ?? "";
        // MP puede enviar epoch en segundos o milisegundos según la integración.
        if (dataId.Length == 0 || requestId.Length == 0 || !(WebhookSignatures.IsRecent(timestamp) || WebhookSignatures.IsRecent(timestamp, true)) || !WebhookSignatures.Verify(Required("Payments:MercadoPago:WebhookSecret"), $"id:{dataId};request-id:{requestId};ts:{timestamp};", WebhookSignatures.Values(signature, "v1")))
            throw new BusinessException(400, "Firma de webhook inválida.");
        using var notification = JsonDocument.Parse(body);
        if (notification.RootElement.GetProperty("type").GetString() != "payment") return null;
        using var payment = await CallAsync(HttpMethod.Get, "v1/payments/" + Uri.EscapeDataString(dataId), null, null, ct);
        var p = payment.RootElement;
        var state = p.GetProperty("status").GetString();
        if (state is not ("approved" or "refunded" or "charged_back")) return null;
        if (state == "refunded" && p.GetProperty("transaction_amount_refunded").GetDecimal() != p.GetProperty("transaction_amount").GetDecimal()) return null;
        return new(p.GetProperty("external_reference").GetString()!, dataId, p.GetProperty("transaction_amount").GetDecimal(), p.GetProperty("currency_id").GetString()!, state == "approved" ? PaymentStatus.Paid : state == "refunded" ? PaymentStatus.Refunded : PaymentStatus.Disputed);
    }
    public async Task RefundAsync(Payment payment, CancellationToken ct)
    {
        if (payment.ProviderPaymentId is null) throw new BusinessException(409, "Pago no confirmado.");
        using var json = await CallAsync(HttpMethod.Post, "v1/payments/" + Uri.EscapeDataString(payment.ProviderPaymentId) + "/refunds", new { amount = payment.Amount }, "refund-" + payment.Id, ct);
        if (json.RootElement.GetProperty("status").GetString() != "approved") throw new BusinessException(409, "Reembolso pendiente: esperar confirmación del proveedor.");
    }
}
