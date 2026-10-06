namespace Yomi.App.Models;

public enum OverlayBackgroundColor
{
    Black,
    White,
}

public sealed class AppSettings
{
    public double Opacity { get; set; } = 0.2;
    public OverlayBackgroundColor BackgroundColor { get; set; } = OverlayBackgroundColor.Black;
    public bool ShowClock { get; set; } = true;
    public bool ShowWorkHours { get; set; } = true;
    public bool ShowIncome { get; set; } = true;
    public decimal MonthlyBaseSalary { get; set; }
    public TimeSpan WorkStartTime { get; set; } = new(9, 0, 0);
    public TimeSpan WorkEndTime { get; set; } = new(18, 0, 0);
    public TimeSpan LunchStartTime { get; set; } = new(12, 0, 0);
    public TimeSpan LunchEndTime { get; set; } = new(13, 0, 0);
    public bool ShowCpu { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowVram { get; set; } = true;
    public bool ShowDisk { get; set; } = true;
    public bool ShowNetwork { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
}
