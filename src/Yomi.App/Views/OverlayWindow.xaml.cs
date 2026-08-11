using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Yomi.App.Interop;
using Yomi.App.Models;
using Yomi.App.Services;

namespace Yomi.App.Views;

public partial class OverlayWindow : Window
{
    private readonly ClickThroughWindowBehavior _clickThrough = new();
    private readonly SamplingService _samplingService;

    public OverlayWindow(SamplingService samplingService)
    {
        InitializeComponent();
        _samplingService = samplingService;
        _samplingService.SampleUpdated += OnSampleUpdated;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _clickThrough.Attach(this);
    }

    private void OnSampleUpdated(MetricSample sample)
    {
        Dispatcher.BeginInvoke(() =>
        {
            CpuValueText.Text = FormatCpu(sample.CpuUsagePercent, sample.CpuClockGHz);
            CpuNameText.Text = sample.CpuName ?? "--";
            MemoryValueText.Text = FormatUsageOverTotal(sample.MemoryUsedGiB, sample.MemoryTotalGiB);
            GpuValueText.Text = FormatPercent(sample.GpuUsagePercent);
            GpuNameText.Text = sample.GpuName ?? "--";
            VramValueText.Text = FormatUsageOverTotal(sample.VramUsedGiB, sample.VramTotalGiB);
            IpAddressText.Text = sample.IpAddressWithPrefix ?? "--";
            DnsText.Text = sample.DnsServers ?? "--";
            UploadSpeedText.Text = FormatMbps(sample.UploadMbps);
            DownloadSpeedText.Text = FormatMbps(sample.DownloadMbps);

            DiskItemsControl.ItemsSource = sample.Disks
                .Select(d => new DiskDriveDisplayItem
                {
                    Name = d.Name,
                    UsageText = FormatDiskUsage(d.UsedBytes, d.TotalBytes, d.UsagePercent),
                    UsagePercent = d.UsagePercent,
                })
                .ToList();

            CpuGraph.Push(sample.CpuUsagePercent ?? 0);
            MemoryGraph.Push(sample.MemoryUsagePercent ?? 0);
            GpuGraph.Push(sample.GpuUsagePercent ?? 0);
            VramGraph.Push(sample.VramUsagePercent ?? 0);
            NetworkGraph.Push(sample.DownloadMbps, sample.UploadMbps);
        }, DispatcherPriority.Background);
    }

    private static string FormatPercent(double? value) =>
        value.HasValue ? $"{value.Value:F0}%" : "--%";

    private static string FormatMbps(double value) => $"{value:F1}Mbps";

    private static string FormatDiskUsage(double usedBytes, double totalBytes, double usagePercent) =>
        $"{FormatBytes(usedBytes)}/{FormatBytes(totalBytes)} ({usagePercent:F0}%)";

    private static string FormatBytes(double bytes)
    {
        const double KiB = 1024;
        const double MiB = KiB * 1024;
        const double GiB = MiB * 1024;
        const double TiB = GiB * 1024;

        return bytes switch
        {
            >= TiB => $"{bytes / TiB:F2}TiB",
            >= GiB => $"{bytes / GiB:F1}GiB",
            _ => $"{bytes / MiB:F0}MiB",
        };
    }

    private static string FormatCpu(double? usagePercent, double? clockGHz)
    {
        if (!usagePercent.HasValue) return "--%";
        return clockGHz.HasValue
            ? $"{usagePercent.Value:F0}% {clockGHz.Value:F2} GHz"
            : $"{usagePercent.Value:F0}%";
    }

    private static string FormatUsageOverTotal(double? usedGiB, double? totalGiB)
    {
        if (!usedGiB.HasValue || !totalGiB.HasValue) return "--";
        return $"{usedGiB.Value:F0}GiB/{totalGiB.Value:F0}GiB";
    }

    /// <summary>指定した画面の右上に、余白を空けて配置する。</summary>
    public void PlaceAtTopRight(Rect screenBounds, double margin = 16)
    {
        Left = screenBounds.Right - Width - margin;
        Top = screenBounds.Top + margin;
    }

    public void ApplySettings(AppSettings settings)
    {
        var baseColor = settings.BackgroundColor == OverlayBackgroundColor.White
            ? Colors.White
            : Colors.Black;
        var alpha = (byte)Math.Round(Math.Clamp(settings.Opacity, 0.0, 1.0) * 255);
        BackgroundBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));

        var labelColor = settings.BackgroundColor == OverlayBackgroundColor.White
            ? Brushes.Black
            : Brushes.White;
        CpuLabelText.Foreground = labelColor;
        MemoryLabelText.Foreground = labelColor;
        GpuLabelText.Foreground = labelColor;
        VramLabelText.Foreground = labelColor;
        DiskLabelText.Foreground = labelColor;
        NetworkSpeedLabelText.Foreground = labelColor;
        NetworkIpLabelText.Foreground = labelColor;
        NetworkDnsLabelText.Foreground = labelColor;

        CpuSection.Visibility = settings.ShowCpu ? Visibility.Visible : Visibility.Collapsed;
        MemorySection.Visibility = settings.ShowMemory ? Visibility.Visible : Visibility.Collapsed;
        GpuSection.Visibility = settings.ShowGpu ? Visibility.Visible : Visibility.Collapsed;
        VramSection.Visibility = settings.ShowVram ? Visibility.Visible : Visibility.Collapsed;
        DiskSection.Visibility = settings.ShowDisk ? Visibility.Visible : Visibility.Collapsed;
        NetworkSection.Visibility = settings.ShowNetwork ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        _samplingService.SampleUpdated -= OnSampleUpdated;
        _clickThrough.Dispose();
        base.OnClosed(e);
    }
}
