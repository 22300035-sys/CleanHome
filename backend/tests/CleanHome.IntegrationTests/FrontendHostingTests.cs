using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CleanHome.IntegrationTests;

public sealed class FrontendHostingTests
{
    [Fact]
    public async Task PublishedFrontendSupportsSpaRoutesWithoutMaskingPrivateEndpointsOrExposingSwagger()
    {
        var folder=Path.Combine(Path.GetTempPath(),"cleanhome-hosting-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder,"index.html"),"<!doctype html><title>CleanHome hosting fixture</title>");
        await File.WriteAllTextAsync(Path.Combine(folder,"sw.js"),"/* fixture */");
        using var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:CleanHome","Host=127.0.0.1;Database=hosting_unused;Username=unused;Password=unused");
            builder.UseSetting("Hosting:ServeFrontend","true");
            builder.UseSetting("Hosting:FrontendRoot",folder);
            builder.UseSetting("Hosting:TrustLoopbackProxy","true");
            builder.UseSetting("Hosting:CanonicalOrigin","https://cleanhome.example");
            builder.UseSetting("Hosting:LegacyHost","legacy.example");
        });
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        var page=await client.GetAsync("/reservas");
        Assert.Equal(HttpStatusCode.OK,page.StatusCode);
        Assert.Contains("CleanHome hosting fixture",await page.Content.ReadAsStringAsync());
        Assert.Contains("no-cache",page.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/sw.js")).StatusCode);
        foreach(var path in new[]{"/api/v1/inexistente","/swagger/index.html","/openapi/v1.json","/hubs/inexistente","/.env","/package.json"})
            Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/v1/bookings")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsync("/ruta-frontend",null)).StatusCode);
        var moved = await client.GetAsync("https://legacy.example/acceso?confirmacion=correcta");
        Assert.Equal(HttpStatusCode.Found, moved.StatusCode);
        Assert.Equal("https://cleanhome.example/acceso?confirmacion=correcta", moved.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("https://legacy.example/api/v1/bookings")).StatusCode);
        // The random temp fixture is left to OS cleanup; no user paths are removed.
    }
}
