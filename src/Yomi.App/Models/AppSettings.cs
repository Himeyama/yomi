namespace Yomi.App.Models;

public sealed class AppSettings
{
    public double Opacity { get; set; } = 0.85;
    public bool ShowCpu { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowVram { get; set; } = true;
    public bool ShowNetwork { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
}
