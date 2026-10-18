using System.Net;
using System.Net.Http.Json;
using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CleanHome.IntegrationTests;

public sealed class ServiceCategoriesIntegrationTests
{
    [Fact]
    public async Task ServiceCategories_ReturnsSeededCategoriesFromPostgreSql()
    {
        const string testVariable =
            "CLEANHOME_TEST_CONNECTION_STRING";

        const string aspNetConnectionVariable =
            "ConnectionStrings__CleanHome";

        var connectionString =
            Environment.GetEnvironmentVariable(testVariable);

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            $"Falta configurar {testVariable}."
        );

        // Verificamos que la base de destino sea exclusivamente
        // la que reservamos para las pruebas.
        var connectionBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal(
            "cleanhome_test",
            connectionBuilder.Database
        );

        // Conservamos cualquier valor anterior para restaurarlo.
        var previousConnectionString =
            Environment.GetEnvironmentVariable(
                aspNetConnectionVariable
            );

        try
        {
            // Esta variable debe existir ANTES de arrancar
            // WebApplicationFactory y ejecutar Program.cs.
            Environment.SetEnvironmentVariable(
                aspNetConnectionVariable,
                connectionString
            );

            using var factory =
                new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.UseEnvironment("Testing");
                    });

            using var client = factory.CreateClient();

            // Aplicar las migraciones reales a PostgreSQL.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<CleanHomeDbContext>();

                await db.Database.MigrateAsync();

                var databaseCount =
                    await db.ServiceCategories.CountAsync();

                Assert.Equal(3, databaseCount);
            }

            // Consultar el endpoint real de la API.
            using var response = await client.GetAsync(
                "/api/v1/service-categories"
            );

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode
            );

            var categories = await response.Content
                .ReadFromJsonAsync<
                    List<ServiceCategoryResponse>
                >();

            Assert.NotNull(categories);
            Assert.Equal(3, categories.Count);

            Assert.Contains(
                categories,
                category =>
                    category.Slug == "limpieza-general"
            );

            Assert.Contains(
                categories,
                category =>
                    category.Slug == "limpieza-profunda"
            );

            Assert.Contains(
                categories,
                category =>
                    category.Slug == "limpieza-oficinas"
            );
        }
        finally
        {
            // No dejamos la conexión de pruebas configurada
            // para otras pruebas del mismo proceso.
            Environment.SetEnvironmentVariable(
                aspNetConnectionVariable,
                previousConnectionString
            );
        }
    }

    private sealed record ServiceCategoryResponse(
        Guid Id,
        string Name,
        string Slug,
        string Description
    );
}
