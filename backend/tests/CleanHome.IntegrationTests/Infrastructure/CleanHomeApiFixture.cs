using CleanHome.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CleanHome.IntegrationTests.Infrastructure;

[CollectionDefinition(
    "CleanHome database",
    DisableParallelization = true)]
public sealed class CleanHomeDatabaseCollection
    : ICollectionFixture<CleanHomeApiFixture>
{
}

public sealed class CleanHomeApiFixture : IDisposable
{
    private const string ApiConnectionVariable =
        "ConnectionStrings__CleanHome";

    private const string JwtKeyVariable =
        "Jwt__SigningKey";

    // Exclusivamente para el proceso automatizado de pruebas.
    // Nunca debe usarse para firmar tokens reales.
    private const string TestSigningKey =
        "CleanHome-IntegrationTests-Only-NotForProduction-2026-Key";

    private readonly string? _previousConnectionString;
    private readonly string? _previousJwtKey;

    private bool _disposed;

    public WebApplicationFactory<Program> Factory { get; }

    public HttpClient Client { get; }

    public CleanHomeApiFixture()
    {
        var connectionString = GetTestConnectionString();

        var connectionBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (!string.Equals(
                connectionBuilder.Database,
                "cleanhome_test",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Las pruebas solamente pueden utilizar cleanhome_test.");
        }

        _previousConnectionString =
            Environment.GetEnvironmentVariable(ApiConnectionVariable);

        _previousJwtKey =
            Environment.GetEnvironmentVariable(JwtKeyVariable);

        Environment.SetEnvironmentVariable(
            ApiConnectionVariable,
            connectionString);

        Environment.SetEnvironmentVariable(
            JwtKeyVariable,
            TestSigningKey);

        try
        {
            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                });

            Client = Factory.CreateClient();

            using var scope = Factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CleanHomeDbContext>();

            db.Database.Migrate();
        }
        catch
        {
            Environment.SetEnvironmentVariable(
                ApiConnectionVariable,
                _previousConnectionString);

            Environment.SetEnvironmentVariable(
                JwtKeyVariable,
                _previousJwtKey);

            throw;
        }
    }

    public IServiceScope CreateScope()
    {
        return Factory.Services.CreateScope();
    }

    private static string GetTestConnectionString()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "CLEANHOME_TEST_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<CleanHomeApiFixture>(optional: true)
            .Build();

        connectionString =
            configuration.GetConnectionString("CleanHomeTest");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la conexión de pruebas. Configura " +
                "CLEANHOME_TEST_CONNECTION_STRING o el User Secret " +
                "ConnectionStrings:CleanHomeTest.");
        }

        return connectionString;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Client.Dispose();
        Factory.Dispose();

        Environment.SetEnvironmentVariable(
            ApiConnectionVariable,
            _previousConnectionString);

        Environment.SetEnvironmentVariable(
            JwtKeyVariable,
            _previousJwtKey);
    }
}
