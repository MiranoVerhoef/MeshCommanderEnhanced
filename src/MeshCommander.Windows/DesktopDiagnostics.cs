namespace MeshCommander.Windows;

internal static class DesktopDiagnostics
{
    private static readonly object Sync = new();

    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MeshCommanderEnhanced");

    public static string LogPath => Path.Combine(DataDirectory, "desktop.log");

    public static void Write(Exception exception) => Write(exception.ToString());

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            lock (Sync)
            {
                File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never cause a second startup failure.
        }
    }
}
