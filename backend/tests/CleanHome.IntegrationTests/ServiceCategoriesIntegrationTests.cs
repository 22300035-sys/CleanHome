using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CleanHome.Application.Marketplace;
using CleanHome.Domain.Entities;
using CleanHome.Infrastructure.Identity;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace CleanHome.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class ServiceCategoriesIntegrationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    [Fact]
    public async Task CompleteMarketplaceFlowEnforcesOwnershipPaymentsAndDocuments()
    {
        var connectionString = Environment.GetEnvironmentVariable("CLEANHOME_TEST_CONNECTION_STRING");
        Assert.False(string.IsNullOrWhiteSpace(connectionString), "Configura CLEANHOME_TEST_CONNECTION_STRING contra una instancia aislada de PostgreSQL.");
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Equal("cleanhome_test", connection.Database);
        Assert.Contains(connection.Host, new[] { "localhost", "127.0.0.1" });
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:CleanHome", connectionString);
            builder.UseSetting("Cors:AllowedOrigins:0", "https://allowed.example");
            builder.UseSetting("Storage:Documents", Path.Combine(Path.GetTempPath(), "cleanhome-tests", Guid.NewGuid().ToString("N")));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentGateway>();
                services.AddSingleton<IPaymentGateway>(new TestPaymentGateway("stripe"));
                services.AddSingleton<IPaymentGateway>(new TestPaymentGateway("mercadopago"));
            });
        });
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CleanHomeDbContext>();
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
        }
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/bookings")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/admin/profiles")).StatusCode);
        var catalog = await anonymous.GetFromJsonAsync<JsonElement>("/api/v1/service-categories");
        Assert.True(catalog.GetArrayLength() >= 3);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/v1/service-categories/00000000-0000-0000-0000-000000000000")).StatusCode);
        using var allowedCors = new HttpRequestMessage(HttpMethod.Options, "/api/v1/requests");
        allowedCors.Headers.Add("Origin", "https://allowed.example");
        allowedCors.Headers.Add("Access-Control-Request-Method", "POST");
        allowedCors.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        var allowed = await anonymous.SendAsync(allowedCors);
        Assert.Equal("https://allowed.example", Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));
        using var rejectedCors = new HttpRequestMessage(HttpMethod.Options, "/api/v1/requests");
        rejectedCors.Headers.Add("Origin", "https://evil.example");
        rejectedCors.Headers.Add("Access-Control-Request-Method", "POST");
        Assert.False((await anonymous.SendAsync(rejectedCors)).Headers.Contains("Access-Control-Allow-Origin"));

        var suffix = Guid.NewGuid().ToString("N");
        var password = "CleanHome-Test!2026";
        async Task<HttpClient> Account(string name, string role)
        {
            var email = $"{name}-{suffix}@example.test";
            await Expect(await anonymous.PostAsJsonAsync("/api/v1/auth/register", new { email, password }), HttpStatusCode.OK);
            var login = await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            await Expect(login, HttpStatusCode.OK);
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await Expect(await client.PostAsJsonAsync("/api/v1/profiles/me", new { displayName = name, role }), HttpStatusCode.Created);
            return client;
        }
        using var customer = await Account("customer", "Customer");
        using var worker = await Account("worker", "Worker");
        using var stranger = await Account("stranger", "Customer");
        // Estas tres altas y logins consumen 6 de las 15 peticiones de identidad/minuto.
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync("Admin")) Assert.True((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded);
            var admin = new ApplicationUser { UserName = $"admin-{suffix}@example.test", Email = $"admin-{suffix}@example.test", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(admin, password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(admin, "Admin")).Succeeded);
        }
        var adminLogin = await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = $"admin-{suffix}@example.test", password });
        await Expect(adminLogin, HttpStatusCode.OK);
        using var adminClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await adminLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        await Expect(await customer.GetAsync("/api/v1/admin/profiles"), HttpStatusCode.Forbidden);
        await Expect(await customer.PutAsJsonAsync("/api/v1/profiles/me", new { displayName = "Escalation", role = "Worker" }), HttpStatusCode.Conflict);

        using var upload = new MultipartFormDataContent();
        upload.Add(new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4\nTest document\n%%EOF")), "file", "identity.pdf");
        var docResponse = await worker.PostAsync("/api/v1/documents", upload);
        await Expect(docResponse, HttpStatusCode.Created);
        var docId = (await docResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Expect(await stranger.GetAsync($"/api/v1/documents/{docId}/download"), HttpStatusCode.NotFound);
        await Expect(await worker.GetAsync($"/api/v1/documents/{docId}/download"), HttpStatusCode.OK);
        using var invalidUpload = new MultipartFormDataContent();
        invalidUpload.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("<script>bad</script>")), "file", "fake.pdf");
        await Expect(await worker.PostAsync("/api/v1/documents", invalidUpload), HttpStatusCode.BadRequest);
        async Task<HttpResponseMessage> UploadEvidence()
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4\nConcurrent evidence\n%%EOF")), "file", "evidence.pdf");
            return await worker.PostAsync("/api/v1/documents", content);
        }
        for (var i = 0; i < 8; i++) await Expect(await UploadEvidence(), HttpStatusCode.Created);
        var quotaRace = await Task.WhenAll(UploadEvidence(), UploadEvidence());
        Assert.Single(quotaRace, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(quotaRace, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(10, (await worker.GetFromJsonAsync<JsonElement>("/api/v1/documents")).GetArrayLength());
        await Expect(await UploadEvidence(), HttpStatusCode.Conflict);
        await Expect(await adminClient.PostAsJsonAsync($"/api/v1/admin/documents/{docId}/review", new { status = "Approved", notes = "Revisado en prueba" }), HttpStatusCode.NoContent);
        var workerProfile = await worker.GetFromJsonAsync<JsonElement>("/api/v1/profiles/me");
        var workerId = workerProfile.GetProperty("id").GetGuid();
        await Expect(await customer.GetAsync($"/api/v1/workers/{workerId}"), HttpStatusCode.NotFound);
        await Expect(await adminClient.PostAsync($"/api/v1/admin/profiles/{workerId}/verify", null), HttpStatusCode.NoContent);
        var publicWorker = await customer.GetAsync($"/api/v1/workers/{workerId}");
        await Expect(publicWorker, HttpStatusCode.OK);
        var publicProfile = await publicWorker.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(publicProfile.TryGetProperty("email", out _));
        Assert.False(publicProfile.TryGetProperty("userId", out _));

        var locationResult = await customer.PostAsJsonAsync("/api/v1/locations", new { label = "Casa", address = "Dirección privada de prueba 123", city = "CDMX" });
        await Expect(locationResult, HttpStatusCode.Created);
        var locationId = (await locationResult.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Expect(await stranger.PutAsJsonAsync($"/api/v1/locations/{locationId}", new { label = "Ajena", address = "No autorizado", city = "CDMX" }), HttpStatusCode.NotFound);
        await Expect(await customer.PutAsJsonAsync($"/api/v1/locations/{locationId}", new { label = "Casa", address = "Dirección privada de prueba 123", city = "CDMX" }), HttpStatusCode.OK);
        var temporaryLocation = await customer.PostAsJsonAsync("/api/v1/locations", new { label = "Temporal", address = "Dirección sin solicitudes", city = "CDMX" });
        await Expect(temporaryLocation, HttpStatusCode.Created);
        var temporaryId = (await temporaryLocation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Expect(await stranger.DeleteAsync($"/api/v1/locations/{temporaryId}"), HttpStatusCode.NotFound);
        await Expect(await customer.DeleteAsync($"/api/v1/locations/{temporaryId}"), HttpStatusCode.NoContent);
        var categoryId = Guid.Parse("a1111111-1111-4111-8111-111111111111");
        var start = DateTimeOffset.UtcNow.AddDays(3);
        await Expect(await stranger.PostAsJsonAsync("/api/v1/requests", new { locationId, categoryId, description = "Solicitud ajena inválida", startsAt = start, durationMinutes = 120 }), HttpStatusCode.NotFound);
        async Task<Guid> NewRequest(DateTimeOffset begins)
        {
            var response = await customer.PostAsJsonAsync("/api/v1/requests", new { locationId, categoryId, description = "Limpieza completa de casa", startsAt = begins, durationMinutes = 120 });
            await Expect(response, HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }
        async Task<Guid> NewQuote(Guid requestId)
        {
            var response = await worker.PostAsJsonAsync($"/api/v1/requests/{requestId}/quotes", new { amount = 500.50m, notes = "Incluye insumos", expiresAt = DateTimeOffset.UtcNow.AddDays(1) });
            await Expect(response, HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }
        var requestId = await NewRequest(start);
        await Expect(await customer.PutAsJsonAsync($"/api/v1/locations/{locationId}", new { label = "Cambiar historial", address = "No debe cambiar", city = "CDMX" }), HttpStatusCode.Conflict);
        await Expect(await customer.DeleteAsync($"/api/v1/locations/{locationId}"), HttpStatusCode.Conflict);
        var listings = await worker.GetStringAsync("/api/v1/requests");
        Assert.DoesNotContain("Dirección privada", listings);
        var quoteId = await NewQuote(requestId);
        await Expect(await worker.PostAsJsonAsync($"/api/v1/requests/{requestId}/quotes", new { amount = 600, notes = "Duplicada", expiresAt = DateTimeOffset.UtcNow.AddDays(1) }), HttpStatusCode.Conflict);
        await Expect(await stranger.PostAsync($"/api/v1/quotes/{quoteId}/accept", null), HttpStatusCode.NotFound);
        var accepted = await customer.PostAsync($"/api/v1/quotes/{quoteId}/accept", null);
        await Expect(accepted, HttpStatusCode.Created);
        var bookingId = (await accepted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Expect(await customer.PostAsync($"/api/v1/quotes/{quoteId}/accept", null), HttpStatusCode.Conflict);
        await Expect(await stranger.GetAsync($"/api/v1/bookings/{bookingId}"), HttpStatusCode.NotFound);
        await Expect(await stranger.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/messages", new { text = "Intruso" }), HttpStatusCode.NotFound);
        await using var chat = new HubConnectionBuilder().WithUrl("https://localhost/hubs/chat", options =>
        {
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            options.AccessTokenProvider = () => Task.FromResult(customer.DefaultRequestHeaders.Authorization!.Parameter);
            options.Transports = HttpTransportType.LongPolling;
        }).Build();
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        chat.On<JsonElement>("MessageReceived", m => received.TrySetResult(m));
        await chat.StartAsync();
        await chat.InvokeAsync("JoinBooking", bookingId);
        await Expect(await worker.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/messages", new { text = "Servicio confirmado" }), HttpStatusCode.Created);
        Assert.Equal("Servicio confirmado", (await received.Task.WaitAsync(TimeSpan.FromSeconds(10))).GetProperty("text").GetString());
        var messages = await customer.GetFromJsonAsync<JsonElement>($"/api/v1/bookings/{bookingId}/messages");
        Assert.Equal(1, messages.GetArrayLength());
        await Expect(await worker.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/status", new { status = "InProgress" }), HttpStatusCode.Conflict);
        var overlapRequest = await NewRequest(start.AddMinutes(10));
        var overlapQuote = await NewQuote(overlapRequest);
        await Expect(await customer.PostAsync($"/api/v1/quotes/{overlapQuote}/accept", null), HttpStatusCode.Conflict);

        var checkout = await customer.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/checkout", new { provider = "stripe" });
        await Expect(checkout, HttpStatusCode.OK);
        var paymentId = (await checkout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Expect(await customer.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/checkout", new { provider = "mercadopago" }), HttpStatusCode.Conflict);
        await Expect(await customer.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/status", new { status = "Cancelled" }), HttpStatusCode.Conflict);
        var webhook = new { paymentReference = paymentId.ToString(), providerPaymentId = "test-" + paymentId, amount = 500.50m, currency = "MXN", status = "Paid" };
        await Expect(await anonymous.PostAsJsonAsync("/api/v1/payments/webhooks/stripe", webhook), HttpStatusCode.BadRequest);
        anonymous.DefaultRequestHeaders.Add("X-Test-Webhook", "valid");
        await Expect(await anonymous.PostAsJsonAsync("/api/v1/payments/webhooks/stripe", new { paymentReference = paymentId.ToString(), providerPaymentId = "test-" + paymentId, amount = 1, currency = "MXN", status = "Paid" }), HttpStatusCode.Conflict);
        await Expect(await anonymous.PostAsJsonAsync("/api/v1/payments/webhooks/stripe", webhook), HttpStatusCode.OK);
        await Expect(await anonymous.PostAsJsonAsync("/api/v1/payments/webhooks/stripe", webhook), HttpStatusCode.OK);
        await Expect(await worker.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/status", new { status = "InProgress" }), HttpStatusCode.OK);
        await Expect(await worker.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/status", new { status = "Completed" }), HttpStatusCode.Conflict);
        await Expect(await customer.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/status", new { status = "Completed" }), HttpStatusCode.OK);
        var notifications = await customer.GetFromJsonAsync<JsonElement>("/api/v1/notifications");
        Assert.True(notifications.GetArrayLength() >= 3);
        // Dos solicitudes diferentes compiten por el mismo horario: solo una se confirma.
        var concurrentStart = start.AddDays(10);
        var concurrentRequestA = await NewRequest(concurrentStart);
        var concurrentRequestB = await NewRequest(concurrentStart.AddMinutes(5));
        var concurrentQuoteA = await NewQuote(concurrentRequestA);
        var concurrentQuoteB = await NewQuote(concurrentRequestB);
        var concurrent = await Task.WhenAll(customer.PostAsync($"/api/v1/quotes/{concurrentQuoteA}/accept", null), customer.PostAsync($"/api/v1/quotes/{concurrentQuoteB}/accept", null));
        Assert.Single(concurrent, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(concurrent, x => x.StatusCode == HttpStatusCode.Conflict);

        // Segundo proveedor, reembolso completo y protección frente a eventos desordenados.
        var mpRequest = await NewRequest(start.AddDays(20));
        var mpQuote = await NewQuote(mpRequest);
        var mpAccepted = await customer.PostAsync($"/api/v1/quotes/{mpQuote}/accept", null);
        await Expect(mpAccepted, HttpStatusCode.Created);
        var mpBooking = (await mpAccepted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var mpCheckout = await customer.PostAsJsonAsync($"/api/v1/bookings/{mpBooking}/checkout", new { provider = "mercadopago" });
        await Expect(mpCheckout, HttpStatusCode.OK);
        var mpPayment = (await mpCheckout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var mpWebhook = new { paymentReference = mpPayment.ToString(), providerPaymentId = "test-" + mpPayment, amount = 500.50m, currency = "MXN", status = "Paid" };
        await Expect(await anonymous.PostAsJsonAsync("/api/v1/payments/webhooks/mercadopago", mpWebhook), HttpStatusCode.OK);
        await Expect(await adminClient.PostAsync($"/api/v1/admin/payments/{mpPayment}/refund", null), HttpStatusCode.NoContent);
        await Expect(await anonymous.PostAsJsonAsync("/api/v1/payments/webhooks/mercadopago", mpWebhook), HttpStatusCode.OK);
        var refunded = await customer.GetFromJsonAsync<JsonElement>($"/api/v1/bookings/{mpBooking}/payment");
        Assert.Equal("Refunded", refunded.GetProperty("status").GetString());
        await Expect(await customer.PostAsJsonAsync($"/api/v1/bookings/{mpBooking}/status", new { status = "Cancelled" }), HttpStatusCode.OK);
        await Expect(await adminClient.GetAsync("/api/v1/admin/audit"), HttpStatusCode.OK);
        await Expect(await adminClient.GetAsync("/api/v1/admin/bookings"), HttpStatusCode.OK);
        await Expect(await adminClient.GetAsync("/api/v1/admin/requests"), HttpStatusCode.OK);
        await Expect(await stranger.GetAsync("/api/v1/admin/bookings"), HttpStatusCode.Forbidden);
        await Expect(await adminClient.PostAsync($"/api/v1/admin/profiles/{workerId}/suspend?suspended=true", null), HttpStatusCode.NoContent);
        await Expect(await worker.GetAsync("/api/v1/bookings"), HttpStatusCode.Forbidden);
        await Expect(await customer.GetAsync($"/api/v1/workers/{workerId}"), HttpStatusCode.NotFound);
        await Expect(await anonymous.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email = $"customer-{suffix}@example.test", password }), HttpStatusCode.BadRequest);
        var finalLogin = await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = $"customer-{suffix}@example.test", password });
        await Expect(finalLogin, HttpStatusCode.OK);
        var refreshToken = (await finalLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString();
        var refresh = await anonymous.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        await Expect(refresh, HttpStatusCode.OK);
        refreshToken = (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString();
        await Expect(await customer.PostAsync("/api/v1/auth/logout", null), HttpStatusCode.NoContent);
        await Expect(await anonymous.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken }), HttpStatusCode.Unauthorized);
    }

    private static async Task Expect(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
    private sealed class TestPaymentGateway(string name) : IPaymentGateway
    {
        public string Name => name;
        public void ValidateConfiguration() { }
        public Task<CheckoutResult> CreateCheckoutAsync(Payment payment, CancellationToken ct) => Task.FromResult(new CheckoutResult("test-" + payment.Id, "https://checkout.example/" + payment.Id));
        public Task RefundAsync(Payment payment, CancellationToken ct) => Task.CompletedTask;
        public Task<VerifiedPayment?> VerifyWebhookAsync(string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query, CancellationToken ct)
        {
            if (headers.GetValueOrDefault("X-Test-Webhook") != "valid") throw new BusinessException(400, "Invalid test signature");
            return Task.FromResult(JsonSerializer.Deserialize<VerifiedPayment>(body, Json));
        }
    }
}
