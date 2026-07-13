using System.Buffers;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MeshCommander.Server.Security;

namespace MeshCommander.Server.Relay;

public sealed class WebSocketTcpRelay(TargetPolicy targetPolicy, ILogger<WebSocketTcpRelay> logger)
{
    private static readonly HashSet<int> DefaultAllowedPorts = [16992, 16993, 16994, 16995, 623, 664];

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket upgrade required.");
            return;
        }

        var host = context.Request.Query["host"].ToString();
        var portValue = context.Request.Query["port"].ToString();
        var tls = IsTruthy(context.Request.Query["tls"].ToString());

        if (!int.TryParse(portValue, out var port) || !DefaultAllowedPorts.Contains(port))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Unsupported relay port.");
            return;
        }

        try
        {
            var endpoint = await targetPolicy.ResolveAllowedEndpointAsync(host, port, context.RequestAborted);
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            using var tcp = new TcpClient(endpoint.AddressFamily);

            await tcp.ConnectAsync(endpoint.Address, endpoint.Port, context.RequestAborted).AsTask().WaitAsync(TimeSpan.FromSeconds(10), context.RequestAborted);
            await using var network = tcp.GetStream();
            await using var stream = tls ? await CreateTlsStreamAsync(network, host, context.RequestAborted) : network;

            using var relayCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            var clientToTarget = PumpWebSocketToStreamAsync(webSocket, stream, relayCts.Token);
            var targetToClient = PumpStreamToWebSocketAsync(stream, webSocket, relayCts.Token);

            await Task.WhenAny(clientToTarget, targetToClient);
            await relayCts.CancelAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or SocketException or IOException or AuthenticationException or TimeoutException)
        {
            logger.LogWarning(ex, "Relay failed for {Host}:{Port}", host, port);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                await context.Response.WriteAsync(ex.Message);
            }
        }
    }

    private static async Task<Stream> CreateTlsStreamAsync(NetworkStream network, string host, CancellationToken cancellationToken)
    {
        var allowUntrusted = !string.Equals(Environment.GetEnvironmentVariable("MCE_ALLOW_UNTRUSTED_AMT_TLS"), "false", StringComparison.OrdinalIgnoreCase);
        var ssl = new SslStream(network, leaveInnerStreamOpen: false, (_, certificate, chain, errors) =>
            allowUntrusted || errors == SslPolicyErrors.None);

        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = host,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck
        }, cancellationToken);

        return ssl;
    }

    private static async Task PumpWebSocketToStreamAsync(WebSocket webSocket, Stream stream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task PumpStreamToWebSocketAsync(Stream stream, WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await webSocket.SendAsync(buffer.AsMemory(0, read), WebSocketMessageType.Binary, true, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static bool IsTruthy(string value) =>
        value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("yes", StringComparison.OrdinalIgnoreCase);
}
