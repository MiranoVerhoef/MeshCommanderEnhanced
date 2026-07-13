using MeshCommander.Desktop.Shared;
using Microsoft.UI.Xaml;

namespace MeshCommander.Windows;

public sealed partial class MainWindow : Window
{
    private readonly SidecarLauncher sidecar = new();

    public MainWindow()
    {
        InitializeComponent();
        Title = "MeshCommander Enhanced";
        Closed += OnClosed;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var url = await sidecar.StartAsync();
        Browser.Source = url;
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await sidecar.DisposeAsync();
    }
}
