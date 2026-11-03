using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CleanHome.Api.Security;
using CleanHome.Application.Abstractions.Administration;
using CleanHome.Application.Abstractions.Bookings;
using CleanHome.Application.Abstractions.Catalog;
using CleanHome.Application.Abstractions.Community;
using CleanHome.Application.Abstractions.Identity;
using CleanHome.Application.Abstractions.Marketplace;
using CleanHome.Application.Abstractions.Verification;
using CleanHome.Application.Features.Catalog;
using CleanHome.Application.Features.Identity;
using CleanHome.Infrastructure.Persistence;
using CleanHome.Infrastructure.Persistence.Administration;
using CleanHome.Infrastructure.Persistence.Bookings;
using CleanHome.Infrastructure.Persistence.Community;
using CleanHome.Infrastructure.Persistence.Repositories;
using CleanHome.Infrastructure.Persistence.Repositories.Identity;
using CleanHome.Infrastructure.Security;
using CleanHome.Infrastructure.Security.Tokens;
using CleanHome.Infrastructure.Storage;
using CleanHome.Infrastructure.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Los datos enviados no son válidos.",
            Type = "https://www.rfc-editor.org/rfc/rfc9110.html#name-400-bad-request",
            Instance = context.HttpContext.Request.Path
        };

        problem.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

const string frontendCorsPolicy = "CleanHomeFrontend";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length == 0)
    throw new InvalidOperationException(
        "Configura Cors:AllowedOrigins.");

foreach (var origin in allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        uri.AbsolutePath != "/" ||
        !string.IsNullOrEmpty(uri.Query) ||
        !string.IsNullOrEmpty(uri.Fragment) ||
        (uri.Scheme != Uri.UriSchemeHttps &&
         !(builder.Environment.IsDevelopment() ||
           builder.Environment.IsEnvironment("Testing"))))
    {
        throw new InvalidOperationException(
            "CORS requiere orígenes válidos y HTTPS fuera de desarrollo.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type");
    });
});

var connectionString =
    builder.Configuration.GetConnectionString("CleanHome");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Falta ConnectionStrings:CleanHome.");

builder.Services.AddDbContext<CleanHomeDbContext>(
    options => options.UseNpgsql(connectionString));

// Catálogo.
builder.Services.AddScoped<ServiceCategoryService>();
builder.Services.AddScoped<
    IServiceCategoryRepository,
    ServiceCategoryRepository>();

// Identidad.
builder.Services.AddScoped<UserRegistrationService>();
builder.Services.AddScoped<
    IUserRegistrationRepository,
    UserRegistrationRepository>();

builder.Services.AddSingleton<
    IPasswordHasher,
    Argon2PasswordHasher>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddSingleton<
    IAccessTokenGenerator,
    JwtAccessTokenGenerator>();

builder.Services.AddSingleton<
    IRefreshTokenGenerator,
    SecureRefreshTokenGenerator>();

builder.Services.AddScoped<
    IAuthenticationService,
    AuthenticationService>();

// Auditoría.
builder.Services.AddScoped<
    ISecurityEventRepository,
    SecurityEventRepository>();

// Recuperación de cuentas.
builder.Services.AddScoped<
    IAccountRecoveryService,
    AccountRecoveryService>();

builder.Services.AddScoped<
    IAccountEmailSender,
    SmtpAccountEmailSender>();

// Bloques A, B y C.
builder.Services.AddScoped<
    IMarketplaceProfileService,
    MarketplaceProfileService>();

builder.Services.AddScoped<
    IServiceBookingService,
    ServiceBookingService>();

builder.Services.AddScoped<
    ICommunityService,
    CommunityService>();

// Documentos y cuarentena.
builder.Services.AddScoped<IDocumentFileValidator, LocalDocumentFileValidator>();
builder.Services.AddScoped<IPrivateQuarantineStorage, LocalPrivateQuarantineStorage>();
builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();
builder.Services.AddScoped<IContentScanService, LocalContentScanService>();
builder.Services.AddScoped<
    IModerationClassificationService,
    LocalModerationClassificationService>();
builder.Services.AddScoped<
    ICustomerLocationService,
    CustomerLocationService>();

builder.Services.AddScoped<
    IAdministrationService,
    AdministrationService>();

// JWT.
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección Jwt.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.SigningKey) ||
    jwtOptions.AccessTokenMinutes is < 1 or > 15)
{
    throw new InvalidOperationException(
        "La configuración JWT es inválida.");
}

var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);

if (keyBytes.Length < 32)
    throw new InvalidOperationException(
        "La clave JWT debe contener al menos 32 bytes.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(keyBytes),

                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.Zero,

                ValidAlgorithms =
                    [SecurityAlgorithms.HmacSha256]
            };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject =
                    context.Principal?.FindFirstValue("sub");

                var sessionValue =
                    context.Principal?.FindFirstValue("sid");

                if (!Guid.TryParse(subject, out var userId) ||
                    !Guid.TryParse(sessionValue, out var sessionId))
                {
                    context.Fail("Identidad inválida.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<CleanHomeDbContext>();

                var now = DateTimeOffset.UtcNow;

                var validSession = await db.UserSessions
                    .AsNoTracking()
                    .AnyAsync(
                        session =>
                            session.Id == sessionId &&
                            session.UserId == userId &&
                            session.RevokedAt == null &&
                            session.ExpiresAt > now &&
                            db.Users.Any(user =>
                                user.Id == userId &&
                                user.IsActive),
                        context.HttpContext.RequestAborted);

                if (!validSession)
                    context.Fail("La sesión fue revocada o expiró.");
            }
        };
    });

// Política basada en el estado ACTUAL de PostgreSQL.
builder.Services.AddScoped<
    IAuthorizationHandler,
    VerifiedEmailHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VerifiedEmail", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new VerifiedEmailRequirement());
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType =
            "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Demasiadas solicitudes.",
                Detail = "Espera antes de intentar nuevamente.",
                Instance = context.HttpContext.Request.Path
            },
            cancellationToken);
    };

    options.AddPolicy("auth-register", context =>
        CreateIpLimiter(context, "register", 5));

    options.AddPolicy("auth-login", context =>
        CreateIpLimiter(context, "login", 10));

    options.AddPolicy("auth-refresh", context =>
        CreateIpLimiter(context, "refresh", 20));
});

static RateLimitPartition<string> CreateIpLimiter(
    HttpContext context,
    string policy,
    int requestsPerMinute)
{
    var ip = context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    return RateLimitPartition.GetFixedWindowLimiter(
        $"{policy}:{ip}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = requestsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
}

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        if (exception is not null)
        {
            logger.LogError(
                exception,
                "Error no controlado. TraceId={TraceId}",
                context.TraceIdentifier);
        }

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error interno del servidor.",
            Detail = "No fue posible completar la operación.",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] =
            context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(
            problem,
            context.RequestAborted);
    });
});

if (app.Environment.IsDevelopment() ||
    app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment() &&
    !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors(frontendCorsPolicy);

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "healthy",
        application = "CleanHome API",
        version = "1.0.0",
        timestamp = DateTimeOffset.UtcNow
    }))
    .AllowAnonymous()
    .WithName("GetHealth")
    .WithTags("Health");

app.Run();

public partial class Program
{
}
