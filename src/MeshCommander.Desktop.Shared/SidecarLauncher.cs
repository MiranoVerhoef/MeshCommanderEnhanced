using System.Diagnostics;

namespace MeshCommander.Desktop.Shared;

public sealed class SidecarLauncher : IAsyncDisposable
{
    private Process? process;

    public async Task<Uri> StartAsync(CancellationToken cancellationToken = default)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var serverDirectory = Path.Combine(baseDirectory, "server");
        var exePath = Path.Combine(serverDirectory, OperatingSystem.IsWindows() ? "MeshCommander.Server.exe" : "MeshCommander.Server");
        var dllPath = Path.Combine(serverDirectory, "MeshCommander.Server.dll");

        ProcessStartInfo startInfo;
        if (File.Exists(exePath))
        {
            startInfo = new ProcessStartInfo(exePath, "--desktop");
        }
        else if (File.Exists(dllPath))
        {
            startInfo = new ProcessStartInfo("dotnet", $"\"{dllPath}\" --desktop");
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
                return new Uri(line["MCE_READY_URL=".Length..].Trim());
            }
        }

        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        throw new InvalidOperationException($"MeshCommander server did not start. {error}".Trim());
    }

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
