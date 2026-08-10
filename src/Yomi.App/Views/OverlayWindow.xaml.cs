using System.Windows;
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

            CpuGraph.Push(sample.CpuUsagePercent ?? 0);
            MemoryGraph.Push(sample.MemoryUsagePercent ?? 0);
            GpuGraph.Push(sample.GpuUsagePercent ?? 0);
            VramGraph.Push(sample.VramUsagePercent ?? 0);
        }, DispatcherPriority.Background);
    }

    private static string FormatPercent(double? value) =>
        value.HasValue ? $"{value.Value:F0}%" : "--%";

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
        Opacity = settings.Opacity;
        CpuSection.Visibility = settings.ShowCpu ? Visibility.Visible : Visibility.Collapsed;
        MemorySection.Visibility = settings.ShowMemory ? Visibility.Visible : Visibility.Collapsed;
        GpuSection.Visibility = settings.ShowGpu ? Visibility.Visible : Visibility.Collapsed;
        VramSection.Visibility = settings.ShowVram ? Visibility.Visible : Visibility.Collapsed;
        NetworkSection.Visibility = settings.ShowNetwork ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        _samplingService.SampleUpdated -= OnSampleUpdated;
        _clickThrough.Dispose();
        base.OnClosed(e);
    }
}
