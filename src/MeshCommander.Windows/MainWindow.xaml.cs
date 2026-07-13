using System.Diagnostics;
using MeshCommander.Desktop.Shared;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;

namespace MeshCommander.Windows;

public sealed partial class MainWindow : Window
{
    private readonly SidecarLauncher sidecar = new();
    private bool initialized;

    public MainWindow()
    {
        InitializeComponent();
        Title = "MeshCommander Enhanced";
        Activated += OnActivated;
        Closed += OnClosed;
    }

    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        Activated -= OnActivated;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            DesktopDiagnostics.Write("Desktop startup began.");
            var url = await sidecar.StartAsync();
            DesktopDiagnostics.Write($"Sidecar ready at {url}.");

            var userDataFolder = Path.Combine(DesktopDiagnostics.DataDirectory, "WebView2");
            Directory.CreateDirectory(userDataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await Browser.EnsureCoreWebView2Async(environment);

            Browser.CoreWebView2.ProcessFailed += (_, eventArgs) =>
                DesktopDiagnostics.Write($"WebView2 process failed: {eventArgs.ProcessFailedKind}.");
            Browser.Source = url;
            Browser.Visibility = Visibility.Visible;
            StatusPanel.Visibility = Visibility.Collapsed;
            DesktopDiagnostics.Write("Desktop startup completed.");
        }
        catch (Exception exception)
        {
            DesktopDiagnostics.Write(exception);
            Progress.IsActive = false;
            StatusText.Text = "MeshCommander Enhanced could not start";
            ErrorText.Text = exception.Message;
            ErrorText.Visibility = Visibility.Visible;
            OpenLogButton.Visibility = Visibility.Visible;
        }
    }

    private void OpenLogButton_Click(object sender, RoutedEventArgs args)
    {
        Process.Start(new ProcessStartInfo(DesktopDiagnostics.LogPath) { UseShellExecute = true });
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await sidecar.DisposeAsync();
    }
}
