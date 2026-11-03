using System.Net;
using CleanHome.IntegrationTests.Infrastructure;

namespace CleanHome.IntegrationTests;

[Collection("CleanHome database")]
public sealed class OpenApiIntegrationTests
{
    private readonly CleanHomeApiFixture _fixture;

    public OpenApiIntegrationTests(CleanHomeApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OpenApi_ReturnsDocument()
    {
        var response = await _fixture.Client.GetAsync(
            "/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"openapi\"", document);
        Assert.Contains("/api/v1/documents", document);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _fixture.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
