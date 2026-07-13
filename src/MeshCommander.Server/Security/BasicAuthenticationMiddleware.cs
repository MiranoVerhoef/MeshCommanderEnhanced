using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MeshCommander.Server.Security;

public sealed class BasicAuthenticationMiddleware(RequestDelegate next)
{
    private const string UserName = "meshcommander";

    public async Task InvokeAsync(HttpContext context)
    {
        var token = Environment.GetEnvironmentVariable("MCE_ADMIN_TOKEN");
        if (string.IsNullOrWhiteSpace(token) || context.Request.Path.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (IsAuthorized(context.Request.Headers.Authorization, token))
        {
            await next(context);
            return;
        }

        context.Response.Headers.WWWAuthenticate = "Basic realm=\"MeshCommander Enhanced\"";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Authentication required.");
    }

    private static bool IsAuthorized(string? authorization, string token)
    {
        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var encoded = authorization["Basic ".Length..].Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var separator = decoded.IndexOf(':');
            if (separator < 0)
            {
                return false;
            }

            var user = decoded[..separator];
            var password = decoded[(separator + 1)..];
            return FixedTimeEquals(user, UserName) && FixedTimeEquals(password, token);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
