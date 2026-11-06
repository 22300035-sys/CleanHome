using Microsoft.AspNetCore.Http;

namespace CleanHome.Api;

public static class EmailConfirmationNavigation
{
    public static void UseEmailConfirmationNavigation(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (!HttpMethods.IsGet(context.Request.Method) || context.Request.Path != "/api/v1/auth/confirmEmail")
            {
                await next();
                return;
            }

            // Compatibility with emails previously rendered with double HTML encoding.
            // Only repair known parameter names; Identity still verifies the actual token.
            var query = context.Request.Query.ToDictionary(x => x.Key, x => x.Value);
            foreach (var name in new[] { "code", "changedEmail" })
                if (!query.ContainsKey(name) && query.Remove("amp;" + name, out var value)) query[name] = value;
            context.Request.Query = new QueryCollection(query);
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";

            if (!app.Configuration.GetValue<bool>("Hosting:ServeFrontend") || !context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var originalBody = context.Response.Body;
            await using var body = new MemoryStream();
            context.Response.Body = body;
            try { await next(); }
            finally { context.Response.Body = originalBody; }

            if (context.Response.StatusCode is 200 or 400 or 401)
            {
                var confirmed = context.Response.StatusCode == 200;
                context.Response.ContentType = null;
                context.Response.ContentLength = 0;
                context.Response.StatusCode = StatusCodes.Status303SeeOther;
                context.Response.Headers.Location = confirmed ? "/acceso?confirmacion=correcta" : "/acceso?confirmacion=error";
            }
            else
            {
                body.Position = 0;
                await body.CopyToAsync(originalBody, context.RequestAborted);
            }
        });
    }
}
