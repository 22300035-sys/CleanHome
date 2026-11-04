using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CleanHome.IntegrationTests;

public sealed class SwaggerIntegrationTests
{
    [Theory]
    [InlineData("Development", false, true)]
    [InlineData("Testing", false, false)]
    [InlineData("Production", false, false)]
    [InlineData("Testing", true, true)]
    public async Task SwaggerUsesLocalAssetsAndFollowsOpenApiVisibility(string environment, bool enabled, bool visible)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            // Estas comprobaciones HTTP no abren conexion ni modifican PostgreSQL.
            builder.UseSetting("ConnectionStrings:CleanHome", "Host=127.0.0.1;Database=swagger_unused;Username=unused;Password=unused");
            builder.UseSetting("OpenApi:Enabled", enabled.ToString());
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var response = await client.GetAsync("/swagger/index.html");
        Assert.Equal(visible ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
        var document = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(visible ? HttpStatusCode.OK : HttpStatusCode.NotFound, document.StatusCode);
        if (visible)
        {
            Assert.Contains("swagger-ui-bundle.js", await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/swagger-ui-bundle.js")).StatusCode);
            var initializer = await client.GetStringAsync("/swagger/index.js");
            Assert.Contains("/openapi/v1.json", initializer);
            Assert.Contains("\"persistAuthorization\":false", initializer);
        }
    }
}
