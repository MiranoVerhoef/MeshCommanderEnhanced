using System.Diagnostics;
using MeshCommander.Desktop.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace MeshCommander.Windows;

public sealed partial class MainWindow : Window
{
    private readonly SidecarLauncher sidecar = new();
    private readonly WebView2 browser = new()
    {
        Visibility = Visibility.Collapsed
    };
    private readonly ProgressRing progress = new()
    {
        IsActive = true,
        Width = 36,
        Height = 36
    };
    private readonly TextBlock statusText = new()
    {
        Text = "Starting MeshCommander Enhanced…",
        FontSize = 16,
        TextAlignment = TextAlignment.Center
    };
    private readonly TextBlock errorText = new()
    {
        MaxWidth = 640,
        TextWrapping = TextWrapping.Wrap,
        TextAlignment = TextAlignment.Center,
        Visibility = Visibility.Collapsed
    };
    private readonly Button openLogButton = new()
    {
        Content = "Open diagnostic log",
        HorizontalAlignment = HorizontalAlignment.Center,
        Visibility = Visibility.Collapsed
    };
    private readonly StackPanel statusPanel = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Spacing = 12
    };
    private bool initialized;

    public MainWindow()
    {
        var root = new Grid();
        statusPanel.Children.Add(progress);
        statusPanel.Children.Add(statusText);
        statusPanel.Children.Add(errorText);
        statusPanel.Children.Add(openLogButton);
        root.Children.Add(browser);
        root.Children.Add(statusPanel);
        Content = root;

        openLogButton.Click += OpenLogButton_Click;
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
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(
                null,
                userDataFolder,
                new CoreWebView2EnvironmentOptions());
            await browser.EnsureCoreWebView2Async(environment);

            browser.CoreWebView2.ProcessFailed += (_, eventArgs) =>
                DesktopDiagnostics.Write($"WebView2 process failed: {eventArgs.ProcessFailedKind}.");
            browser.Source = url;
            browser.Visibility = Visibility.Visible;
            statusPanel.Visibility = Visibility.Collapsed;
            DesktopDiagnostics.Write("Desktop startup completed.");
        }
        catch (Exception exception)
        {
            DesktopDiagnostics.Write(exception);
            progress.IsActive = false;
            statusText.Text = "MeshCommander Enhanced could not start";
            errorText.Text = exception.Message;
            errorText.Visibility = Visibility.Visible;
            openLogButton.Visibility = Visibility.Visible;
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
