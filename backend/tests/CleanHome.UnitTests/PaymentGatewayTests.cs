using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;

namespace CleanHome.UnitTests;

public sealed class PaymentGatewayTests
{
    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Payments:Stripe:SecretKey"] = "test-placeholder-key",
        ["Payments:Stripe:WebhookSecret"] = "test-webhook-secret",
        ["Payments:MercadoPago:AccessToken"] = "test-placeholder-key",
        ["Payments:MercadoPago:WebhookSecret"] = "test-webhook-secret",
        ["Payments:ReturnBaseUrl"] = "https://frontend.example",
        ["Payments:PublicApiUrl"] = "https://api.example",
        ["Payments:MercadoPago:Sandbox"] = "true"
    }).Build();
    private static string Sign(string data) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("test-webhook-secret"), Encoding.UTF8.GetBytes(data))).ToLowerInvariant();

    [Fact]
    public async Task StripeCheckoutUsesServerAmountAndIdempotencyKey()
    {
        var payment = new Payment { Amount = 500.50m, Currency = "MXN" };
        using var http = new HttpClient(new Handler(async request =>
        {
            Assert.Equal("https://api.stripe.com/v1/checkout/sessions", request.RequestUri!.ToString());
            Assert.Equal(payment.Id.ToString(), Assert.Single(request.Headers.GetValues("Idempotency-Key")));
            var body = Uri.UnescapeDataString(await request.Content!.ReadAsStringAsync());
            Assert.Contains("line_items[0][price_data][unit_amount]=50050", body);
            Assert.Contains("client_reference_id=" + payment.Id, body);
            return "{\"id\":\"cs_test_123\",\"url\":\"https://checkout.stripe.com/test\"}";
        }));
        var gateway = new StripeGateway(http, Config());
        gateway.ValidateConfiguration();
        var result = await gateway.CreateCheckoutAsync(payment, default);
        Assert.Equal("cs_test_123", result.Id);
    }

    [Fact]
    public async Task StripeRequiresSignatureAndFetchesActualPaidSession()
    {
        var reference = Guid.NewGuid().ToString();
        using var http = new HttpClient(new Handler(request => Task.FromResult(JsonSerializer.Serialize(new
        {
            client_reference_id = reference, payment_intent = "pi_test", amount_total = 50050, currency = "mxn", payment_status = "paid"
        }))));
        var gateway = new StripeGateway(http, Config());
        var body = "{\"type\":\"checkout.session.completed\",\"data\":{\"object\":{\"id\":\"cs_test\"}}}";
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Stripe-Signature"] = $"t={time},v1={Sign(time + "." + body)}" };
        var verified = await gateway.VerifyWebhookAsync(body, headers, new Dictionary<string, string>(), default);
        Assert.Equal(reference, verified!.PaymentReference);
        Assert.Equal(500.50m, verified.Amount);
        Assert.Equal("MXN", verified.Currency);
        Assert.Equal(PaymentStatus.Paid, verified.Status);
        await Assert.ThrowsAsync<BusinessException>(() => gateway.VerifyWebhookAsync(body + " ", headers, new Dictionary<string, string>(), default));
        headers["Stripe-Signature"] = "t=0,v1=" + Sign("0." + body);
        await Assert.ThrowsAsync<BusinessException>(() => gateway.VerifyWebhookAsync(body, headers, new Dictionary<string, string>(), default));
    }

    [Fact]
    public async Task MercadoPagoUsesPreferenceWithExternalReferenceAndPrivateWebhook()
    {
        var payment = new Payment { Amount = 500.50m };
        using var http = new HttpClient(new Handler(async request =>
        {
            Assert.Equal("https://api.mercadopago.com/checkout/preferences", request.RequestUri!.ToString());
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(payment.Id.ToString(), body.RootElement.GetProperty("external_reference").GetString());
            Assert.Equal(500.50m, body.RootElement.GetProperty("items")[0].GetProperty("unit_price").GetDecimal());
            Assert.Equal("https://api.example/api/v1/payments/webhooks/mercadopago", body.RootElement.GetProperty("notification_url").GetString());
            return "{\"id\":\"preference-test\",\"sandbox_init_point\":\"https://sandbox.mercadopago.com/test\"}";
        }));
        var gateway = new MercadoPagoGateway(http, Config());
        gateway.ValidateConfiguration();
        Assert.Equal("preference-test", (await gateway.CreateCheckoutAsync(payment, default)).Id);
    }

    [Fact]
    public async Task MercadoPagoAuthenticatesQueryAndChecksProviderStatus()
    {
        var reference = Guid.NewGuid().ToString();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal("https://api.mercadopago.com/v1/payments/123", request.RequestUri!.ToString());
            return Task.FromResult(JsonSerializer.Serialize(new { external_reference = reference, transaction_amount = 500.50m, currency_id = "MXN", status = "approved" }));
        }));
        var gateway = new MercadoPagoGateway(http, Config());
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Sign($"id:123;request-id:req-123;ts:{time};");
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["x-signature"] = $"ts={time},v1={signature}", ["x-request-id"] = "req-123" };
        var query = new Dictionary<string, string> { ["data.id"] = "123" };
        var verified = await gateway.VerifyWebhookAsync("{\"type\":\"payment\"}", headers, query, default);
        Assert.Equal(reference, verified!.PaymentReference);
        Assert.Equal(PaymentStatus.Paid, verified.Status);
        query["data.id"] = "456";
        await Assert.ThrowsAsync<BusinessException>(() => gateway.VerifyWebhookAsync("{\"type\":\"payment\"}", headers, query, default));
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<string>> handle) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => new(HttpStatusCode.OK) { Content = new StringContent(await handle(request), Encoding.UTF8, "application/json") };
    }
}
