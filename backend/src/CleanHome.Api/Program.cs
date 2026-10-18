using CleanHome.Application.Abstractions.Catalog;
using CleanHome.Application.Features.Catalog;
using CleanHome.Infrastructure.Persistence;
using CleanHome.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controladores y documentación OpenAPI.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Servicios de salud.
builder.Services.AddHealthChecks();

// Configuración de PostgreSQL.
// La cadena proviene de User Secrets o variables de entorno.
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

// Dependencias de la aplicación.
builder.Services.AddScoped<ServiceCategoryService>();

// Implementaciones de infraestructura.
builder.Services.AddScoped<
    IServiceCategoryRepository,
    ServiceCategoryRepository>();

var app = builder.Build();

// OpenAPI únicamente en desarrollo.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// En producción, HTTPS debe aplicarse también
// desde el proxy o plataforma de alojamiento.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Liveness: confirma que el proceso HTTP responde.
// No verifica aún PostgreSQL.
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

// Permite pruebas de integración con WebApplicationFactory.
public partial class Program
{
}
