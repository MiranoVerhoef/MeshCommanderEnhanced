using System.Diagnostics;
using System.Net.Http.Json;

namespace MeshCommander.Desktop.Shared;

public sealed class SidecarLauncher : IAsyncDisposable
{
    private static readonly Uri DesktopUri = new("http://127.0.0.1:16990");
    private static readonly HttpClient HealthClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private Process? process;

    public async Task<Uri> StartAsync(CancellationToken cancellationToken = default)
    {
        if (await IsHealthyAsync(cancellationToken))
        {
            return DesktopUri;
        }

        var baseDirectory = AppContext.BaseDirectory;
        var serverDirectory = Path.Combine(baseDirectory, "server");
        var exePath = Path.Combine(serverDirectory, OperatingSystem.IsWindows() ? "MeshCommander.Server.exe" : "MeshCommander.Server");
        var dllPath = Path.Combine(serverDirectory, "MeshCommander.Server.dll");

        ProcessStartInfo startInfo;
        if (File.Exists(exePath))
        {
            startInfo = new ProcessStartInfo(exePath, "--desktop") { WorkingDirectory = serverDirectory };
        }
        else if (File.Exists(dllPath))
        {
            startInfo = new ProcessStartInfo("dotnet", $"\"{dllPath}\" --desktop") { WorkingDirectory = serverDirectory };
        }
        else
        {
            startInfo = new ProcessStartInfo("dotnet", "run --project src/MeshCommander.Server -- --desktop")
            {
                WorkingDirectory = FindRepositoryRoot(baseDirectory)
            };
        }

        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        startInfo.Environment["MCE_DESKTOP_URL"] = DesktopUri.ToString().TrimEnd('/');

        process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start MeshCommander server.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        while (!timeout.IsCancellationRequested)
        {
            var line = await process.StandardOutput.ReadLineAsync(timeout.Token);
            if (line is null)
            {
                break;
            }

            if (line.StartsWith("MCE_READY_URL=", StringComparison.Ordinal))
            {
                return DesktopUri;
            }
        }

        if (await IsHealthyAsync(cancellationToken))
        {
            return DesktopUri;
        }

        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        throw new InvalidOperationException($"MeshCommander server did not start. {error}".Trim());
    }

    private static async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var health = await HealthClient.GetFromJsonAsync<HealthResponse>(new Uri(DesktopUri, "/healthz"), cancellationToken);
            return string.Equals(health?.Name, "MeshCommander Enhanced", StringComparison.Ordinal);
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private sealed record HealthResponse(string Name);

    public async ValueTask DisposeAsync()
    {
        if (process is null)
        {
            return;
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        process.Dispose();
    }

    private static string FindRepositoryRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "package.json")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src", "MeshCommander.Server")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return start;
    }
}
