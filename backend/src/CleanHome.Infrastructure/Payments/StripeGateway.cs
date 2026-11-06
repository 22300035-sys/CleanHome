using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CleanHome.Infrastructure.Payments;

public sealed class StripeGateway(HttpClient http, IConfiguration config) : IPaymentGateway
{
    public string Name => "stripe";
    public void ValidateConfiguration()
    {
        Required("Payments:Stripe:SecretKey");
        Required("Payments:Stripe:WebhookSecret");
        if (!Uri.TryCreate(Required("Payments:ReturnBaseUrl"), UriKind.Absolute, out var uri) || uri.Scheme != "https") throw new BusinessException(503, "Configura Payments:ReturnBaseUrl con HTTPS.");
    }
    private string Required(string key) => !string.IsNullOrWhiteSpace(config[key]) ? config[key]! : throw new BusinessException(503, "Stripe no está configurado.");
    private async Task<JsonDocument> CallAsync(HttpMethod method, string path, Dictionary<string, string>? form, string? idempotency, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, "https://api.stripe.com/v1/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Required("Payments:Stripe:SecretKey"));
        if (idempotency is not null) request.Headers.Add("Idempotency-Key", idempotency);
        if (form is not null) request.Content = new FormUrlEncodedContent(form);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new BusinessException(502, "Stripe rechazó la operación; revisa la configuración y reintenta.");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }
    public async Task<CheckoutResult> CreateCheckoutAsync(Payment payment, CancellationToken ct)
    {
        var baseUrl = Required("Payments:ReturnBaseUrl").TrimEnd('/');
        using var json = await CallAsync(HttpMethod.Post, "checkout/sessions", new()
        {
            ["mode"] = "payment", ["success_url"] = baseUrl + "/payments/success?payment=" + payment.Id,
            ["cancel_url"] = baseUrl + "/payments/cancel?payment=" + payment.Id,
            ["client_reference_id"] = payment.Id.ToString(), ["payment_intent_data[metadata][payment_id]"] = payment.Id.ToString(),
            ["line_items[0][price_data][currency]"] = payment.Currency.ToLowerInvariant(),
            ["line_items[0][price_data][unit_amount]"] = decimal.ToInt64(payment.Amount * 100).ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = "Servicio CleanHome",
            ["line_items[0][quantity]"] = "1"
        }, payment.Id.ToString(), ct);
        return new(json.RootElement.GetProperty("id").GetString()!, json.RootElement.GetProperty("url").GetString()!);
    }
    public async Task<VerifiedPayment?> VerifyWebhookAsync(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query, CancellationToken ct)
    {
        var signature = headers.GetValueOrDefault("Stripe-Signature", "");
        var timestamp = WebhookSignatures.Values(signature, "t").FirstOrDefault() ?? "";
        if (!WebhookSignatures.IsRecent(timestamp) || !WebhookSignatures.Verify(Required("Payments:Stripe:WebhookSecret"), timestamp + "." + body, WebhookSignatures.Values(signature, "v1")))
            throw new BusinessException(400, "Firma de webhook inválida.");
        using var eventJson = JsonDocument.Parse(body);
        var type = eventJson.RootElement.GetProperty("type").GetString();
        if (type is not ("checkout.session.completed" or "checkout.session.async_payment_succeeded" or "charge.refunded" or "charge.dispute.created")) return null;
        var obj = eventJson.RootElement.GetProperty("data").GetProperty("object");
        if (type!.StartsWith("checkout", StringComparison.Ordinal))
        {
            var id = obj.GetProperty("id").GetString()!;
            using var session = await CallAsync(HttpMethod.Get, "checkout/sessions/" + Uri.EscapeDataString(id), null, null, ct);
            var s = session.RootElement;
            if (s.GetProperty("payment_status").GetString() != "paid") return null;
            return new(s.GetProperty("client_reference_id").GetString()!, s.GetProperty("payment_intent").GetString()!, s.GetProperty("amount_total").GetDecimal() / 100, s.GetProperty("currency").GetString()!.ToUpperInvariant(), PaymentStatus.Paid);
        }
        var intent = obj.GetProperty("payment_intent").GetString()!;
        using var intentJson = await CallAsync(HttpMethod.Get, "payment_intents/" + Uri.EscapeDataString(intent), null, null, ct);
        var p = intentJson.RootElement;
        if (type == "charge.refunded" && obj.GetProperty("amount_refunded").GetDecimal() != obj.GetProperty("amount").GetDecimal()) return null;
        return new(p.GetProperty("metadata").GetProperty("payment_id").GetString()!, intent, p.GetProperty("amount").GetDecimal() / 100, p.GetProperty("currency").GetString()!.ToUpperInvariant(), type == "charge.refunded" ? PaymentStatus.Refunded : PaymentStatus.Disputed);
    }
    public async Task RefundAsync(Payment payment, CancellationToken ct)
    {
        if (payment.ProviderPaymentId is null) throw new BusinessException(409, "Pago no confirmado.");
        using var json = await CallAsync(HttpMethod.Post, "refunds", new() { ["payment_intent"] = payment.ProviderPaymentId }, "refund-" + payment.Id, ct);
        if (json.RootElement.GetProperty("status").GetString() != "succeeded") throw new BusinessException(409, "Reembolso pendiente: esperar confirmación del proveedor.");
    }
}
