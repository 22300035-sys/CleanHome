using System.Net;
using System.Net.Http.Json;
using CleanHome.IntegrationTests.Infrastructure;
using CleanHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanHome.IntegrationTests;

[Collection("CleanHome database")]
public sealed class UserRegistrationIntegrationTests
{
    private readonly CleanHomeApiFixture _fixture;

    public UserRegistrationIntegrationTests(
        CleanHomeApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_CreatesUserAndArgon2idCredential()
    {
        var email = NewTestEmail();

        try
        {
            using var response = await _fixture.Client.PostAsJsonAsync(
                "/api/v1/auth/register",
                new
                {
                    firstName = "Usuario",
                    lastName = "Integracion",
                    email,
                    password = "ClaveSeguraParaPruebas2026!"
                });

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            var result = await response.Content
                .ReadFromJsonAsync<RegistrationResponse>();

            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(email, result.Email);
            Assert.False(result.IsEmailVerified);

            using var scope = _fixture.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CleanHomeDbContext>();

            var user = await db.Users
                .AsNoTracking()
                .SingleAsync(x => x.NormalizedEmail ==
                    email.ToUpperInvariant());

            var credential = await db.UserCredentials
                .AsNoTracking()
                .SingleAsync(x => x.UserId == user.Id);

            Assert.Equal(result.Id, user.Id);
            Assert.StartsWith(
                "$argon2id$",
                credential.PasswordHash);

            Assert.DoesNotContain(
                "ClaveSeguraParaPruebas2026!",
                credential.PasswordHash);

            var json = await response.Content.ReadAsStringAsync();

            Assert.DoesNotContain(
                "passwordHash",
                json,
                StringComparison.OrdinalIgnoreCase);

            Assert.DoesNotContain(
                "ClaveSeguraParaPruebas2026!",
                json);
        }
        finally
        {
            await DeleteTestUserAsync(email);
        }
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmail()
    {
        var email = NewTestEmail();

        try
        {
            var payload = new
            {
                firstName = "Usuario",
                lastName = "Duplicado",
                email,
                password = "ClaveSeguraParaPruebas2026!"
            };

            using var firstResponse =
                await _fixture.Client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    payload);

            Assert.Equal(
                HttpStatusCode.Created,
                firstResponse.StatusCode);

            using var secondResponse =
                await _fixture.Client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    payload);

            Assert.Equal(
                HttpStatusCode.Conflict,
                secondResponse.StatusCode);

            using var scope = _fixture.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CleanHomeDbContext>();

            var count = await db.Users.CountAsync(
                x => x.NormalizedEmail == email.ToUpperInvariant());

            Assert.Equal(1, count);
        }
        finally
        {
            await DeleteTestUserAsync(email);
        }
    }

    [Fact]
    public async Task Register_RejectsShortPasswordWithoutSavingUser()
    {
        var email = NewTestEmail();

        using var response = await _fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                firstName = "Usuario",
                lastName = "Invalido",
                email,
                password = "12345"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var scope = _fixture.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<CleanHomeDbContext>();

        Assert.False(await db.Users.AnyAsync(
            x => x.NormalizedEmail == email.ToUpperInvariant()));
    }

    [Fact]
    public async Task Register_RejectsInvalidEmailWithoutSavingUser()
    {
        using var response = await _fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                firstName = "Usuario",
                lastName = "Invalido",
                email = "correo-invalido",
                password = "ClaveSeguraParaPruebas2026!"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task DeleteTestUserAsync(string email)
    {
        using var scope = _fixture.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<CleanHomeDbContext>();

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.NormalizedEmail == email.ToUpperInvariant());

        if (user is null)
            return;

        // Las claves foráneas de la migración actual
        // eliminan la credencial al eliminar este usuario.
        db.Users.Remove(user);

        await db.SaveChangesAsync();
    }

    private static string NewTestEmail()
    {
        return $"test-{Guid.NewGuid():N}@cleanhome.test";
    }

    private sealed record RegistrationResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        bool IsEmailVerified,
        DateTimeOffset CreatedAt);
}
