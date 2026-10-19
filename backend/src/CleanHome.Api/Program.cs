using CleanHome.Application.Abstractions.Catalog;
using CleanHome.Application.Abstractions.Identity;
using CleanHome.Application.Features.Catalog;
using CleanHome.Application.Features.Identity;
using CleanHome.Infrastructure.Persistence;
using CleanHome.Infrastructure.Persistence.Repositories;
using CleanHome.Infrastructure.Persistence.Repositories.Identity;
using CleanHome.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controladores y documentación OpenAPI.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Comprobaciones de salud.
builder.Services.AddHealthChecks();

// Conexión segura a PostgreSQL.
// La cadena se obtiene de User Secrets o variables de entorno.
var connectionString = builder.Configuration
    .GetConnectionString("CleanHome");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:CleanHome. " +
        "Configura PostgreSQL mediante User Secrets " +
        "o variables de entorno.");
}

builder.Services.AddDbContext<CleanHomeDbContext>(
    options => options.UseNpgsql(connectionString));

// Catálogo.
builder.Services.AddScoped<ServiceCategoryService>();

builder.Services.AddScoped<
    IServiceCategoryRepository,
    ServiceCategoryRepository>();

// Registro e identidad.
builder.Services.AddScoped<UserRegistrationService>();

builder.Services.AddScoped<
    IUserRegistrationRepository,
    UserRegistrationRepository>();

// Hash seguro de contraseñas.
builder.Services.AddSingleton<
    IPasswordHasher,
    Argon2PasswordHasher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment() &&
    !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        application = "CleanHome API",
        version = "1.0.0",
        timestamp = DateTimeOffset.UtcNow
    });
})
.AllowAnonymous()
.WithName("GetHealth")
.WithTags("Health");

app.Run();

public partial class Program
{
}
