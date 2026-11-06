using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CleanHome.Api;
using CleanHome.Infrastructure.Identity;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CleanHome.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class EmailConfirmationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProductionRegistrationConfirmsRenderedLinkAndSupportsOldEmail(bool oldLink)
    {
        var connection = Environment.GetEnvironmentVariable("CLEANHOME_TEST_CONNECTION_STRING");
        Assert.False(string.IsNullOrWhiteSpace(connection));
        Assert.Equal("cleanhome_test", new NpgsqlConnectionStringBuilder(connection).Database);
        var mail = new CaptureEmail();
        var frontend = Path.Combine(Path.GetTempPath(), "cleanhome-mail-frontend", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(frontend);
        await File.WriteAllTextAsync(Path.Combine(frontend, "index.html"), "<!doctype html><title>Confirmation fixture</title>");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:CleanHome", connection);
            builder.UseSetting("Hosting:ServeFrontend", "true");
            builder.UseSetting("Hosting:FrontendRoot", frontend);
            builder.UseSetting("Storage:DataProtectionKeys", Path.Combine(Path.GetTempPath(), "cleanhome-mail-keys", Guid.NewGuid().ToString("N")));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSender<ApplicationUser>>();
                services.AddSingleton<IEmailSender<ApplicationUser>>(mail);
            });
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<CleanHomeDbContext>().Database.MigrateAsync();
        var email = "confirmation-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var password = "Test!Mail9" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password })).StatusCode);
        Assert.NotNull(mail.Html);
        Assert.DoesNotContain("&amp;amp;", mail.Html);
        var href = Regex.Match(mail.Html!, "href=\"([^\"]+)\"").Groups[1].Value;
        var url = WebUtility.HtmlDecode(href);
        Assert.Contains("&code=", url);
        if (oldLink) url = url.Replace("&code=", "&amp;code=");
        using var browserRequest = new HttpRequestMessage(HttpMethod.Get, url);
        browserRequest.Headers.Accept.ParseAdd("text/html");
        var confirmed = await client.SendAsync(browserRequest);
        Assert.Equal(HttpStatusCode.SeeOther, confirmed.StatusCode);
        Assert.Equal("/acceso?confirmacion=correcta", confirmed.Headers.Location!.ToString());
        Assert.Contains("no-store", confirmed.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password })).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True(await users.IsEmailConfirmedAsync((await users.FindByEmailAsync(email))!));
        }
        // Recovery still requires a valid token; no external mail is sent by this test.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/forgotPassword", new { email })).StatusCode);
        Assert.NotNull(mail.ResetCode);
        Assert.NotNull(mail.ResetHtml);
        var resetHref = WebUtility.HtmlDecode(Regex.Match(mail.ResetHtml!, "href=\"([^\"]+)\"").Groups[1].Value);
        var resetUri = new Uri(resetHref);
        Assert.Equal("/restablecer", resetUri.AbsolutePath);
        Assert.Empty(resetUri.Query);
        var resetFields = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(resetUri.Fragment.TrimStart('#'));
        Assert.Equal(email, resetFields["email"].ToString());
        var newPassword = "Changed!Mail9" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/resetPassword", new { email, resetCode = resetFields["code"].ToString(), newPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = newPassword })).StatusCode);
        var bad = new Uri(url);
        var badUrl = bad.GetLeftPart(UriPartial.Path) + "?userId=missing&code=invalid";
        using var badRequest = new HttpRequestMessage(HttpMethod.Get, badUrl);
        badRequest.Headers.Accept.ParseAdd("text/html");
        var rejected = await client.SendAsync(badRequest);
        Assert.Equal("/acceso?confirmacion=error", rejected.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(badUrl)).StatusCode);
    }

    private sealed class CaptureEmail : IEmailSender<ApplicationUser>
    {
        public string? Html { get; private set; }
        public string? ResetCode { get; private set; }
        public string? ResetHtml { get; private set; }
        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        { Html = SmtpIdentityEmailSender.LinkBody(confirmationLink, "Confirmar cuenta"); return Task.CompletedTask; }
        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => Task.CompletedTask;
        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        { ResetCode = resetCode; ResetHtml = SmtpIdentityEmailSender.PasswordResetBody(email, resetCode, "https://frontend.example"); return Task.CompletedTask; }
    }
}
