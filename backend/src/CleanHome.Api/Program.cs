using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CleanHome.Api;
using CleanHome.Api.Hubs;
using CleanHome.Application.Abstractions.Catalog;
using CleanHome.Application.Features.Catalog;
using CleanHome.Application.Marketplace;
using CleanHome.Infrastructure.Identity;
using CleanHome.Infrastructure.Marketplace;
using CleanHome.Infrastructure.Payments;
using CleanHome.Infrastructure.Persistence;
using CleanHome.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "CleanHome API";
        document.Info.Version = "1.1.0";
        document.Info.Description = "Contratación de limpieza, documentos privados, chat, Stripe y Mercado Pago. Autenticación Identity con tokens opacos.";
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "Opaque", Description = "accessToken de POST /api/v1/auth/login" };
        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, context, ct) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] }];
            operation.Responses ??= new OpenApiResponses();
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Falta token bearer válido." });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Rol o estado de cuenta no autorizado." });
        }
        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSignalR(o => { o.MaximumReceiveMessageSize = 8192; o.MaximumParallelInvocationsPerClient = 1; });

var connectionString = builder.Configuration.GetConnectionString("CleanHome");
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Falta ConnectionStrings:CleanHome. Configura PostgreSQL mediante User Secrets o variables de entorno.");
builder.Services.AddDbContext<CleanHomeDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddIdentityApiEndpoints<ApplicationUser>(o =>
{
    o.Password.RequiredLength = 12;
    o.User.RequireUniqueEmail = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    o.SignIn.RequireConfirmedEmail = !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing");
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<CleanHomeDbContext>();
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
    o.DefaultChallengeScheme = IdentityConstants.BearerScheme;
});
builder.Services.AddTransient<IEmailSender<ApplicationUser>, SmtpIdentityEmailSender>();
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, o =>
{
    o.BearerTokenExpiration = TimeSpan.FromMinutes(15);
    o.RefreshTokenExpiration = TimeSpan.FromDays(7);
    o.Events.OnMessageReceived = context =>
    {
        if (context.Request.Path.StartsWithSegments("/hubs/chat") && context.Request.Query.TryGetValue("access_token", out var token)) context.Token = token;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization(o => o.DefaultPolicy = new AuthorizationPolicyBuilder(IdentityConstants.BearerScheme).RequireAuthenticatedUser().Build());
var keys = builder.Configuration["Storage:DataProtectionKeys"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keys))).SetApplicationName("CleanHome");

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
{
    if (origins.Length > 0) p.WithOrigins(origins).WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS").WithHeaders("Accept", "Content-Type", "Authorization", "X-SignalR-User-Agent");
}));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 180, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddScoped<ServiceCategoryService>();
builder.Services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
builder.Services.AddScoped<MarketplaceService>();
builder.Services.AddScoped<PaymentService>();
var documentRoot = builder.Configuration["Storage:Documents"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "documents");
builder.Services.AddSingleton<IPrivateFileStore>(new LocalPrivateFileStore(documentRoot));
builder.Services.AddHttpClient<StripeGateway>(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHttpClient<MercadoPagoGateway>(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddTransient<IPaymentGateway>(sp => sp.GetRequiredService<StripeGateway>());
builder.Services.AddTransient<IPaymentGateway>(sp => sp.GetRequiredService<MercadoPagoGateway>());

var app = builder.Build();
// Comando administrativo explícito: no crea usuarios ni modifica el esquema en arranque normal.
if (builder.Configuration.GetValue<bool>("bootstrap-admin"))
{
    using var scope = app.Services.CreateScope();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var email = builder.Configuration["Bootstrap:Email"] ?? throw new InvalidOperationException("Falta Bootstrap:Email.");
    var password = builder.Configuration["Bootstrap:Password"] ?? throw new InvalidOperationException("Falta Bootstrap:Password.");
    if (await users.FindByEmailAsync(email) is not null) throw new InvalidOperationException("El correo ya existe. No se elevarán permisos de una cuenta existente.");
    if (!await roles.RoleExistsAsync("Admin"))
    {
        var roleResult = await roles.CreateAsync(new IdentityRole("Admin"));
        if (!roleResult.Succeeded) throw new InvalidOperationException("No se pudo crear el rol Admin.");
    }
    var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
    var created = await users.CreateAsync(admin, password);
    if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x => x.Description)));
    var assigned = await users.AddToRoleAsync(admin, "Admin");
    if (!assigned.Succeeded) throw new InvalidOperationException("No se pudo asignar Admin.");
    Console.WriteLine("Administrador creado. Elimina Bootstrap__Password del entorno.");
    return;
}
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CleanHome API v1");
        options.DocumentTitle = "CleanHome API — Swagger";
        options.ConfigObject.PersistAuthorization = false;
        options.EnableTryItOutByDefault();
    });
}
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseCors("Frontend");
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    if (context.Request.Path.StartsWithSegments("/api/v1/auth") && context.Request.Query.Any(x => (x.Key.Equals("useCookies", StringComparison.OrdinalIgnoreCase) || x.Key.Equals("useSessionCookies", StringComparison.OrdinalIgnoreCase)) && x.Value.ToString().Equals("true", StringComparison.OrdinalIgnoreCase)))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { error = "Esta API utiliza tokens bearer; cookies deshabilitadas." });
        return;
    }
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.MapGroup("/api/v1/auth").RequireRateLimiting("auth").MapIdentityApi<ApplicationUser>();
app.MapControllers();
app.MapHub<BookingChatHub>("/hubs/chat", o => { o.CloseOnAuthenticationExpiration = true; });
app.MapGet("/health", () => Results.Ok(new { status = "healthy", application = "CleanHome API", version = "1.1.0", timestamp = DateTimeOffset.UtcNow })).AllowAnonymous();
app.MapGet("/health/ready", async Task<IResult> (CleanHomeDbContext db, CancellationToken ct) =>
{
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeout.CancelAfter(TimeSpan.FromSeconds(3));
    try
    {
        if (!await db.Database.CanConnectAsync(timeout.Token) || (await db.Database.GetPendingMigrationsAsync(timeout.Token)).Any()) return Results.StatusCode(503);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception) { return Results.StatusCode(503); }
}).AllowAnonymous();
app.Run();
public partial class Program { }
