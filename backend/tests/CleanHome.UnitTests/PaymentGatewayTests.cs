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
    public async Task MercadoPagoUsesOrderWithServerAmountAndIdempotency()
    {
        var payment = new Payment { Amount = 500.50m };
        using var http = new HttpClient(new Handler(async request =>
        {
            Assert.Equal("https://api.mercadopago.com/v1/orders", request.RequestUri!.ToString());
            Assert.Equal(payment.Id.ToString(), Assert.Single(request.Headers.GetValues("X-Idempotency-Key")));
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(payment.Id.ToString(), body.RootElement.GetProperty("external_reference").GetString());
            Assert.Equal("500.50", body.RootElement.GetProperty("items")[0].GetProperty("unit_price").GetString());
            Assert.InRange(body.RootElement.GetProperty("items")[0].GetProperty("external_code").GetString()!.Length, 1, 30);
            Assert.Equal("500.50", body.RootElement.GetProperty("total_amount").GetString());
            Assert.Equal("manual", body.RootElement.GetProperty("processing_mode").GetString());
            Assert.Equal("online", body.RootElement.GetProperty("type").GetString());
            Assert.False(body.RootElement.TryGetProperty("notification_url", out _));
            Assert.Equal("https://frontend.example/payments/success?payment=" + payment.Id, body.RootElement.GetProperty("config").GetProperty("online").GetProperty("success_url").GetString());
            return "{\"id\":\"ORDTST123\",\"checkout_url\":\"https://www.mercadopago.com.mx/checkout/test\",\"currency\":\"MXN\",\"total_amount\":\"500.50\"}";
        }));
        var gateway = new MercadoPagoGateway(http, Config());
        gateway.ValidateConfiguration();
        Assert.Equal("ORDTST123", (await gateway.CreateCheckoutAsync(payment, default)).Id);
    }

    [Fact]
    public async Task MercadoPagoAuthenticatesQueryAndChecksProviderStatus()
    {
        var reference = Guid.NewGuid().ToString();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal("https://api.mercadopago.com/v1/orders/ORDTST123", request.RequestUri!.ToString());
            return Task.FromResult(JsonSerializer.Serialize(new { id = "ORDTST123", external_reference = reference, total_amount = "500.50", total_paid_amount = "500.50", currency = "MXN", status = "processed", status_detail = "accredited" }));
        }));
        var gateway = new MercadoPagoGateway(http, Config());
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Sign($"id:ordtst123;request-id:req-123;ts:{time};");
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["x-signature"] = $"ts={time},v1={signature}", ["x-request-id"] = "req-123" };
        var query = new Dictionary<string, string> { ["data.id"] = "ORDTST123" };
        var verified = await gateway.VerifyWebhookAsync("{\"type\":\"order\",\"data\":{\"id\":\"ignored\",\"total_amount\":\"0.01\"}}", headers, query, default);
        Assert.Equal(reference, verified!.PaymentReference);
        Assert.Equal(PaymentStatus.Paid, verified.Status);
        Assert.Equal("ORDTST123", verified.ProviderPaymentId);
        Assert.Equal(500.50m, verified.Amount);
        query["data.id"] = "456";
        await Assert.ThrowsAsync<BusinessException>(() => gateway.VerifyWebhookAsync("{\"type\":\"order\"}", headers, query, default));
    }

    [Theory]
    [InlineData("created", "created", "0.00", false)]
    [InlineData("processing", "in_process", "0.00", false)]
    [InlineData("failed", "rejected_by_issuer", "0.00", false)]
    [InlineData("processed", "partially_refunded", "500.50", false)]
    [InlineData("processed", "accredited", "1.00", false)]
    [InlineData("refunded", "refunded", "500.50", true)]
    public async Task MercadoPagoOnlyConfirmsFinalFullOrderStates(string state, string detail, string paid, bool refunded)
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(JsonSerializer.Serialize(new { id = "ORDTST123", external_reference = Guid.NewGuid(), total_amount = "500.50", total_paid_amount = paid, currency = "MXN", status = state, status_detail = detail }))));
        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var headers = new Dictionary<string, string> { ["x-signature"] = $"ts={time},v1={Sign($"id:ordtst123;request-id:r;ts:{time};")}", ["x-request-id"] = "r" };
        var result = await new MercadoPagoGateway(http, Config()).VerifyWebhookAsync("{\"type\":\"order\"}", headers, new Dictionary<string, string> { ["data.id"] = "ORDTST123" }, default);
        if (refunded) Assert.Equal(PaymentStatus.Refunded, result!.Status);
        else Assert.Null(result);
    }

    [Fact]
    public async Task MercadoPagoRefundsEntireOrderWithoutPartialAmount()
    {
        var payment = new Payment { Amount = 500.50m, ProviderPaymentId = "ORDTST123" };
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.mercadopago.com/v1/orders/ORDTST123/refund", request.RequestUri!.ToString());
            Assert.Null(request.Content);
            var refundKey = Assert.Single(request.Headers.GetValues("X-Idempotency-Key"));
            Assert.True(Guid.TryParse(refundKey, out var parsed));
            Assert.NotEqual(payment.Id, parsed);
            return Task.FromResult("{\"id\":\"ORDTST123\",\"status\":\"refunded\",\"status_detail\":\"refunded\"}");
        }));
        await new MercadoPagoGateway(http, Config()).RefundAsync(payment, default);
    }

    [Fact]
    public async Task MercadoPagoDetectsChargebackFromFetchedOrderAndRejectsOldSignature()
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(JsonSerializer.Serialize(new { id = "ORDTST123", external_reference = Guid.NewGuid(), total_amount = "500.50", currency = "MXN", status = "charged_back", status_detail = "settled" }))));
        var gateway = new MercadoPagoGateway(http, Config());
        var query = new Dictionary<string, string> { ["data.id"] = "ORDTST123" };
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var headers = new Dictionary<string, string> { ["x-signature"] = $"ts={time},v1={Sign($"id:ordtst123;request-id:r;ts:{time};")}", ["x-request-id"] = "r" };
        Assert.Equal(PaymentStatus.Disputed, (await gateway.VerifyWebhookAsync("{\"type\":\"order\"}", headers, query, default))!.Status);
        headers["x-signature"] = $"ts=0,v1={Sign("id:ordtst123;request-id:r;ts:0;")}";
        await Assert.ThrowsAsync<BusinessException>(() => gateway.VerifyWebhookAsync("{\"type\":\"order\"}", headers, query, default));
    }

    [Fact]
    public async Task MercadoPagoDoesNotTreatPartialRefundAsFullRefund()
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult("{\"id\":\"ORDTST123\",\"status\":\"processed\",\"status_detail\":\"partially_refunded\"}")));
        await Assert.ThrowsAsync<BusinessException>(() => new MercadoPagoGateway(http, Config()).RefundAsync(new Payment { ProviderPaymentId = "ORDTST123" }, default));
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<string>> handle) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => new(HttpStatusCode.OK) { Content = new StringContent(await handle(request), Encoding.UTF8, "application/json") };
    }
}
