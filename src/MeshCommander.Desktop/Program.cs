using System.Drawing;
using MeshCommander.Desktop.Shared;
using Photino.NET;

await using var sidecar = new SidecarLauncher();
var url = await sidecar.StartAsync();

var window = new PhotinoWindow()
    .SetTitle("MeshCommander Enhanced")
    .SetUseOsDefaultSize(false)
    .SetSize(new Size(1280, 900))
    .SetResizable(true)
    .Load(url);

window.WaitForClose();
