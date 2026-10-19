using System.Net;
using System.Net.Http.Json;
using CleanHome.IntegrationTests.Infrastructure;
using CleanHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanHome.IntegrationTests;

[Collection("CleanHome database")]
public sealed class ServiceCategoriesIntegrationTests
{
    private readonly CleanHomeApiFixture _fixture;

    public ServiceCategoriesIntegrationTests(
        CleanHomeApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ServiceCategories_ReturnsSeededCategoriesFromPostgreSql()
    {
        using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<CleanHomeDbContext>();

            var count = await db.ServiceCategories.CountAsync();

            Assert.Equal(3, count);
        }

        using var response = await _fixture.Client.GetAsync(
            "/api/v1/service-categories");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var categories = await response.Content
            .ReadFromJsonAsync<List<ServiceCategoryResponse>>();

        Assert.NotNull(categories);
        Assert.Equal(3, categories.Count);

        Assert.Contains(
            categories,
            category => category.Slug == "limpieza-general");

        Assert.Contains(
            categories,
            category => category.Slug == "limpieza-profunda");

        Assert.Contains(
            categories,
            category => category.Slug == "limpieza-oficinas");
    }

    private sealed record ServiceCategoryResponse(
        Guid Id,
        string Name,
        string Slug,
        string Description);
}
