using MeshCommander.Server.Relay;
using MeshCommander.Server.Security;
using MeshCommander.Server.Migration;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

var desktopMode = args.Contains("--desktop", StringComparer.OrdinalIgnoreCase);
if (desktopMode)
{
    builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("MCE_DESKTOP_URL") ?? "http://127.0.0.1:16990");
}
else if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")) &&
         string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_URLS")))
{
    builder.WebHost.UseUrls("http://127.0.0.1:3000");
}

builder.Services.AddSingleton<TargetPolicy>();
builder.Services.AddSingleton<WebSocketTcpRelay>();
builder.Services.AddSingleton<LegacyConfigurationImporter>();

var app = builder.Build();

EnsureSharedModeHasAuthentication(app);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseMiddleware<BasicAuthenticationMiddleware>();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });

app.MapGet("/healthz", () => Results.Ok(new
{
    status = "ok",
    name = "MeshCommander Enhanced",
    utc = DateTimeOffset.UtcNow
}));

if (desktopMode)
{
    app.MapGet("/api/desktop/bootstrap", (LegacyConfigurationImporter importer) => Results.Ok(importer.Import()));
}

app.Map("/webrelay.ashx", async context =>
{
    var relay = context.RequestServices.GetRequiredService<WebSocketTcpRelay>();
    await relay.HandleAsync(context);
});

app.Map("/relay", async context =>
{
    var relay = context.RequestServices.GetRequiredService<WebSocketTcpRelay>();
    await relay.HandleAsync(context);
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-store";
        }
    }
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
    var address = addresses?.FirstOrDefault() ?? "http://127.0.0.1:3000";
    Console.WriteLine($"MCE_READY_URL={address}");
});

await app.RunAsync();

static void EnsureSharedModeHasAuthentication(WebApplication app)
{
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ??
               Environment.GetEnvironmentVariable("DOTNET_URLS") ??
               string.Empty;
    var shared = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(url => url.Contains("://0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
                    url.Contains("://*", StringComparison.OrdinalIgnoreCase) ||
                    url.Contains("://+", StringComparison.OrdinalIgnoreCase));

    if (shared && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCE_ADMIN_TOKEN")))
    {
        throw new InvalidOperationException("MCE_ADMIN_TOKEN is required when binding MeshCommander Enhanced to a shared interface.");
    }
}

public partial class Program;
