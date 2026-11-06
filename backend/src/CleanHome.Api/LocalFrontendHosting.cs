using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;

namespace CleanHome.Api;

public static class LocalFrontendHosting
{
    public static void AddLocalFrontendHosting(this WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue<bool>("Hosting:ServeFrontend")) return;
        var root=builder.Configuration["Hosting:FrontendRoot"];
        if (string.IsNullOrWhiteSpace(root) || !File.Exists(Path.Combine(Path.GetFullPath(root),"index.html")))
            throw new InvalidOperationException("Hosting:FrontendRoot debe apuntar al build React con index.html.");
        builder.Services.AddSingleton(new FrontendFiles(new PhysicalFileProvider(Path.GetFullPath(root))));
        if (builder.Configuration.GetValue<bool>("Hosting:TrustLoopbackProxy"))
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders=ForwardedHeaders.XForwardedFor|ForwardedHeaders.XForwardedProto;
                options.ForwardLimit=1;
                options.KnownProxies.Add(IPAddress.Loopback);
                options.KnownProxies.Add(IPAddress.IPv6Loopback);
            });
    }
    public static void UseLocalFrontendHosting(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Hosting:ServeFrontend")) return;
        if(app.Configuration.GetValue<bool>("Hosting:TrustLoopbackProxy")) app.UseForwardedHeaders();
        var canonicalOrigin = app.Configuration["Hosting:CanonicalOrigin"];
        var legacyHost = app.Configuration["Hosting:LegacyHost"];
        if (!string.IsNullOrWhiteSpace(canonicalOrigin))
        {
            if (!Uri.TryCreate(canonicalOrigin, UriKind.Absolute, out var origin) || origin.Scheme != "https" || origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.UserInfo) || !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment))
                throw new InvalidOperationException("Hosting:CanonicalOrigin debe ser un origen HTTPS.");
            app.Use(async (context, next) =>
            {
                // Keep old API/webhook/email endpoints working; only move browser pages.
                if (HttpMethods.IsGet(context.Request.Method) && string.Equals(context.Request.Host.Host, legacyHost, StringComparison.OrdinalIgnoreCase)
                    && !Path.HasExtension(context.Request.Path) && !new[] { "/api", "/hubs", "/health" }.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
                {
                    context.Response.Headers["Referrer-Policy"] = "no-referrer";
                    context.Response.Redirect(origin.GetLeftPart(UriPartial.Authority) + context.Request.PathBase + context.Request.Path + context.Request.QueryString);
                    return;
                }
                await next();
            });
        }
        var files=app.Services.GetRequiredService<FrontendFiles>().Provider;
        app.UseDefaultFiles(new DefaultFilesOptions{FileProvider=files});
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider=files,
            OnPrepareResponse=context =>
            {
                context.Context.Response.Headers["X-Content-Type-Options"]="nosniff";
                context.Context.Response.Headers.CacheControl=context.Context.Request.Path.StartsWithSegments("/assets")
                    ? "public,max-age=31536000,immutable" : "no-cache";
            }
        });
    }
    public static void MapLocalFrontendFallback(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Hosting:ServeFrontend")) return;
        app.MapFallback(async context =>
        {
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)
                || Path.HasExtension(context.Request.Path)
                || new[]{"/api","/hubs","/health","/swagger","/openapi"}.Any(prefix=>context.Request.Path.StartsWithSegments(prefix)))
            { context.Response.StatusCode=404;return; }
            context.Response.ContentType="text/html; charset=utf-8";
            context.Response.Headers.CacheControl="no-cache";
            context.Response.Headers["X-Content-Type-Options"]="nosniff";
            await context.Response.SendFileAsync(app.Services.GetRequiredService<FrontendFiles>().Provider.GetFileInfo("index.html"),context.RequestAborted);
        }).AllowAnonymous();
    }
    private sealed class FrontendFiles(PhysicalFileProvider provider) : IDisposable
    {
        public PhysicalFileProvider Provider { get; }=provider;
        public void Dispose()=>Provider.Dispose();
    }
}
