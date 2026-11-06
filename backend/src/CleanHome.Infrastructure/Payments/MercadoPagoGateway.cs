using System.Globalization;
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
        foreach (var key in new[] { "Payments:ReturnBaseUrl" })
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
        if (payment.Currency != "MXN") throw new BusinessException(400, "Esta integración de Mercado Pago México requiere MXN.");
        var amount = payment.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        using var json = await CallAsync(HttpMethod.Post, "v1/orders", new
        {
            type = "online", processing_mode = "manual", total_amount = amount,
            external_reference = payment.Id.ToString(),
            items = new[] { new { external_code = payment.BookingId.ToString("N")[..30], title = "Servicio CleanHome", quantity = 1, unit_price = amount } },
            config = new { online = new {
                success_url = returnUrl + "/payments/success?payment=" + payment.Id,
                failure_url = returnUrl + "/payments/cancel?payment=" + payment.Id,
                pending_url = returnUrl + "/payments/pending?payment=" + payment.Id,
                auto_return = "approved"
            } }
        }, payment.Id.ToString(), ct);
        var order = json.RootElement;
        if (order.GetProperty("currency").GetString() != payment.Currency || ReadAmount(order, "total_amount") != payment.Amount)
            throw new BusinessException(502, "La moneda o importe de la order no coincide con la reserva.");
        return new(order.GetProperty("id").GetString()!, order.GetProperty("checkout_url").GetString()!);
    }
    public async Task<VerifiedPayment?> VerifyWebhookAsync(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query, CancellationToken ct)
    {
        var dataId = query.GetValueOrDefault("data.id", "");
        var signature = headers.GetValueOrDefault("x-signature", "");
        var requestId = headers.GetValueOrDefault("x-request-id", "");
        var timestamp = WebhookSignatures.Values(signature, "ts").FirstOrDefault() ?? "";
        // MP puede enviar epoch en segundos o milisegundos según la integración.
        if (dataId.Length == 0 || requestId.Length == 0 || !(WebhookSignatures.IsRecent(timestamp) || WebhookSignatures.IsRecent(timestamp, true)) || !WebhookSignatures.Verify(Required("Payments:MercadoPago:WebhookSecret"), $"id:{dataId.ToLowerInvariant()};request-id:{requestId};ts:{timestamp};", WebhookSignatures.Values(signature, "v1")))
            throw new BusinessException(400, "Firma de webhook inválida.");
        using var notification = JsonDocument.Parse(body);
        if (notification.RootElement.GetProperty("type").GetString() != "order") return null;
        // La firma vincula el ID del query; nunca se confía en importes/estado del body.
        using var order = await CallAsync(HttpMethod.Get, "v1/orders/" + Uri.EscapeDataString(dataId), null, null, ct);
        var p = order.RootElement;
        if (!string.Equals(p.GetProperty("id").GetString(), dataId, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException(502, "Mercado Pago devolvió una order diferente.");
        var state = p.GetProperty("status").GetString();
        var detail = p.GetProperty("status_detail").GetString();
        PaymentStatus status;
        if (state == "refunded" && detail == "refunded") status = PaymentStatus.Refunded;
        else if (state == "charged_back") status = PaymentStatus.Disputed;
        else if (state == "processed" && detail == "accredited" && ReadAmount(p, "total_paid_amount") == ReadAmount(p, "total_amount")) status = PaymentStatus.Paid;
        else return null; // Pendientes, fallidos y reembolsos parciales no confirman un cobro total.
        return new(p.GetProperty("external_reference").GetString()!, p.GetProperty("id").GetString()!, ReadAmount(p, "total_amount"), p.GetProperty("currency").GetString()!, status);
    }
    public async Task RefundAsync(Payment payment, CancellationToken ct)
    {
        if (payment.ProviderPaymentId is null) throw new BusinessException(409, "Pago no confirmado.");
        // El identificador confirmado ahora es una order, no un payment de la API anterior.
        if (!payment.ProviderPaymentId.StartsWith("ORD", StringComparison.Ordinal)) throw new BusinessException(409, "Pago legacy: requiere conciliación antes de reembolsar.");
        var refundKey = new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("refund:" + payment.Id)).AsSpan(0, 16)).ToString();
        using var json = await CallAsync(HttpMethod.Post, "v1/orders/" + Uri.EscapeDataString(payment.ProviderPaymentId) + "/refund", null, refundKey, ct);
        if (json.RootElement.GetProperty("id").GetString() != payment.ProviderPaymentId || json.RootElement.GetProperty("status").GetString() != "refunded" || json.RootElement.GetProperty("status_detail").GetString() != "refunded")
            throw new BusinessException(409, "Reembolso pendiente: esperar confirmación del proveedor.");
    }
    private static decimal ReadAmount(JsonElement obj, string name)
    {
        var value = obj.GetProperty(name);
        return value.ValueKind == JsonValueKind.String
            ? decimal.Parse(value.GetString()!, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
            : value.GetDecimal();
    }
}
