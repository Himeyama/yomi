using System.Globalization;
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
    private readonly DispatcherTimer _clockTimer;
    private static readonly JapaneseCalendar JapaneseEraCalendar = new();
    private static readonly CultureInfo JapaneseEraCulture = CreateJapaneseEraCulture();
    private TimeSpan _workStartTime = new(9, 0, 0);
    private TimeSpan _workEndTime = new(18, 0, 0);
    private TimeSpan _lunchStartTime = new(12, 0, 0);
    private TimeSpan _lunchEndTime = new(13, 0, 0);
    private AppSettings _incomeSettings = new();

    public OverlayWindow(SamplingService samplingService)
    {
        InitializeComponent();
        _samplingService = samplingService;
        _samplingService.SampleUpdated += OnSampleUpdated;

        _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _clockTimer.Tick += (_, _) => UpdateClockAndWorkHours();
        UpdateClockAndWorkHours();
        _clockTimer.Start();
    }

    private static CultureInfo CreateJapaneseEraCulture()
    {
        var culture = new CultureInfo("ja-JP");
        culture.DateTimeFormat.Calendar = JapaneseEraCalendar;
        return culture;
    }

    private void UpdateClockAndWorkHours()
    {
        var now = DateTime.Now;
        if (_incomeSettings.EMode)
        {
            ClockDateText.Text = now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }
        else
        {
            var era = now.ToString("gg", JapaneseEraCulture);
            var eraYear = JapaneseEraCalendar.GetYear(now);
            ClockDateText.Text = $"{era} {eraYear} 年 ({now.Year} 年) {now.Month} 月 {now.Day} 日";
        }
        ClockTimeText.Text = now.ToString("HH:mm:ss");
        SegmentClock.Time = now;

        UpdateWorkHoursBar(now.TimeOfDay);
        var isWithinWorkHours = Normalize(now.TimeOfDay, _workStartTime)
            < Normalize(_workEndTime, _workStartTime);
        IncomeSection.Visibility = _incomeSettings.ShowIncome && isWithinWorkHours
            ? Visibility.Visible
            : Visibility.Collapsed;
        IncomeValueText.Text = IncomeCalculator.CalculateDailyIncome(_incomeSettings, now)
            .ToString("N2", CultureInfo.GetCultureInfo("ja-JP")) + (_incomeSettings.EMode ? " JPY" : " 円");
    }

    private void UpdateWorkHoursBar(TimeSpan timeOfDay)
    {
        var isNightShift = _workStartTime >= _workEndTime;

        // 開始時刻を 0 起点に正規化し、日またぎ(夜勤)を直線区間として扱う。
        // 日中勤務は正規化しても開始からの経過時間なので同じ式で計算できる。
        var now = Normalize(timeOfDay, _workStartTime);
        var workEnd = Normalize(_workEndTime, _workStartTime);
        var lunchStart = Normalize(_lunchStartTime, _workStartTime);
        var lunchEnd = Normalize(_lunchEndTime, _workStartTime);
        var lunchLabel = _incomeSettings.EMode
            ? (isNightShift ? "BREAK" : "LUNCH") : (isNightShift ? "休憩" : "昼休み");
        var firstHalfLabel = _incomeSettings.EMode
            ? (isNightShift ? "WORKING" : "AM") : (isNightShift ? "勤務中" : "午前");
        var secondHalfLabel = _incomeSettings.EMode
            ? (isNightShift ? "WORKING" : "PM") : (isNightShift ? "勤務中" : "午後");

        var (progressPercent, fillBrushKey, statusText) = now switch
        {
            var t when t >= TimeSpan.Zero && t < lunchStart =>
                (ProgressWithin(t, TimeSpan.Zero, workEnd), "WorkHoursBrush", firstHalfLabel),
            var t when t >= lunchStart && t < lunchEnd =>
                (ProgressWithin(t, TimeSpan.Zero, workEnd), "LunchBrush", lunchLabel),
            var t when t >= lunchEnd && t < workEnd =>
                (ProgressWithin(t, TimeSpan.Zero, workEnd), "WorkHoursBrush", secondHalfLabel),
            _ => (0.0, null, _incomeSettings.EMode ? "OFF HOURS" : "時間外"),
        };

        WorkHoursBar.ProgressPercent = progressPercent;
        WorkHoursBar.FillBrush = fillBrushKey is null ? null : (Brush)BackgroundBorder.Resources[fillBrushKey];
        WorkHoursStatusText.Text = statusText;
    }

    /// <summary>基準時刻からの経過時間に正規化する。基準より前の時刻は翌日分とみなし24時間加算する。</summary>
    private static TimeSpan Normalize(TimeSpan value, TimeSpan basis)
    {
        var diff = value - basis;
        return diff < TimeSpan.Zero ? diff + TimeSpan.FromHours(24) : diff;
    }

    private static double ProgressWithin(TimeSpan current, TimeSpan start, TimeSpan end)
    {
        var span = (end - start).TotalSeconds;
        if (span <= 0) return 0;
        var elapsed = (current - start).TotalSeconds;
        return Math.Clamp(elapsed / span * 100.0, 0, 100);
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
            MemoryValueText.Text = FormatUsageOverTotal(sample.MemoryUsedGiB, sample.MemoryTotalGiB, sample.MemoryUsagePercent);
            GpuValueText.Text = FormatPercent(sample.GpuUsagePercent);
            GpuNameText.Text = sample.GpuName ?? "--";
            VramValueText.Text = FormatUsageOverTotal(sample.VramUsedGiB, sample.VramTotalGiB, sample.VramUsagePercent);
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

    private static string FormatDiskUsage(double usedBytes, double totalBytes, double usagePercent)
    {
        const double KiB = 1024;
        const double MiB = KiB * 1024;
        const double GiB = MiB * 1024;
        const double TiB = GiB * 1024;

        // 使用量・総量は総量の大きさに応じた同一単位・同一桁数で揃えて表示する。
        var (unit, unitLabel, decimals) = totalBytes switch
        {
            >= TiB => (TiB, "TiB", 2),
            >= GiB => (GiB, "GiB", 1),
            _ => (MiB, "MiB", 0),
        };

        var usedText = (usedBytes / unit).ToString($"F{decimals}");
        var totalText = (totalBytes / unit).ToString($"F{decimals}");
        return $"{usedText}/{totalText} {unitLabel} ({usagePercent:F0}%)";
    }

    private static string FormatCpu(double? usagePercent, double? clockGHz)
    {
        if (!usagePercent.HasValue) return "--%";
        return clockGHz.HasValue
            ? $"{usagePercent.Value:F0}% {clockGHz.Value:F2} GHz"
            : $"{usagePercent.Value:F0}%";
    }

    private static string FormatUsageOverTotal(double? usedGiB, double? totalGiB, double? usagePercent)
    {
        if (!usedGiB.HasValue || !totalGiB.HasValue) return "--";
        return usagePercent.HasValue
            ? $"{usedGiB.Value:F0}/{totalGiB.Value:F0} GiB ({usagePercent.Value:F0}%)"
            : $"{usedGiB.Value:F0}/{totalGiB.Value:F0} GiB";
    }

    /// <summary>指定した画面の右上に、余白を空けて配置する。</summary>
    public void PlaceAtTopRight(Rect screenBounds, double margin = 16)
    {
        Left = screenBounds.Right - Width - margin;
        Top = screenBounds.Top + margin;
    }

    // (キー, 黒背景用の色, 白背景用の色)。白背景用は同系色のまま明度を下げてコントラストを確保する。
    private static readonly (string Key, Color Dark, Color Light)[] ThemedBrushes =
    [
        ("CpuBrush", Color.FromRgb(0x29, 0xAB, 0xD4), Color.FromRgb(0x00, 0x64, 0x7F)),
        ("MemoryBrush", Color.FromRgb(0x4D, 0x8F, 0xFF), Color.FromRgb(0x1D, 0x4E, 0xD8)),
        ("GpuBrush", Color.FromRgb(0xA8, 0x55, 0xF7), Color.FromRgb(0x7C, 0x1F, 0xD9)),
        ("VramBrush", Color.FromRgb(0xD1, 0x9C, 0xFF), Color.FromRgb(0x8A, 0x2B, 0xE8)),
        ("DiskBrush", Color.FromRgb(0x3B, 0x9E, 0xFF), Color.FromRgb(0x0B, 0x63, 0xC6)),
        ("DiskWarningBrush", Color.FromRgb(0xF0, 0x4A, 0x38), Color.FromRgb(0xC4, 0x2B, 0x1C)),
        ("NetworkDownloadBrush", Color.FromRgb(0xF0, 0x40, 0x7E), Color.FromRgb(0xB3, 0x17, 0x56)),
        ("NetworkUploadBrush", Color.FromRgb(0xFF, 0xA8, 0x3C), Color.FromRgb(0xC4, 0x6A, 0x00)),
        ("NetworkAddressBrush", Color.FromRgb(0xC9, 0xA8, 0xFF), Color.FromRgb(0x6B, 0x21, 0xA8)),
        ("SecondaryTextBrush", Color.FromRgb(0xCC, 0xCC, 0xCC), Color.FromRgb(0x55, 0x55, 0x55)),
        ("WorkHoursBrush", Color.FromRgb(0x3B, 0x9E, 0xFF), Color.FromRgb(0x0B, 0x63, 0xC6)),
        ("LunchBrush", Color.FromRgb(0xFF, 0xA8, 0x3C), Color.FromRgb(0xC4, 0x6A, 0x00)),
    ];

    public void ApplySettings(AppSettings settings)
    {
        var isEMode = settings.EMode;
        var isWhiteTheme = settings.BackgroundColor == OverlayBackgroundColor.White;
        if (isEMode)
            FontWeight = FontWeights.Bold;
        else
            ClearValue(System.Windows.Controls.Control.FontWeightProperty);

        var baseColor = !isEMode && isWhiteTheme ? Colors.White : Colors.Black;
        var alpha = isEMode ? (byte)255 : (byte)Math.Round(Math.Clamp(settings.Opacity, 0.0, 1.0) * 255);
        BackgroundBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
        BackgroundBorder.CornerRadius = new CornerRadius(isEMode ? 0 : 6);
        WorkHoursBar.IsRounded = !isEMode;
        if (isEMode)
            WorkHoursBar.TrackBrush = Brushes.Transparent;
        else
            WorkHoursBar.ClearValue(WorkHoursProgressBar.TrackBrushProperty);
        BackgroundBorder.Resources["RoundedBars"] = !isEMode;

        var eTextBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD0, 0x44));
        eTextBrush.Freeze();
        BackgroundBorder.BorderBrush = isEMode ? eTextBrush : null;
        BackgroundBorder.BorderThickness = new Thickness(isEMode ? 2 : 0);

        IncomeLabelText.Text = isEMode ? "TODAY'S INCOME" : "今日の収入";
        MemoryLabelText.Text = isEMode ? "MEMORY" : "メモリ";
        DiskLabelText.Text = isEMode ? "VOLUME" : "ディスク";
        NetworkSpeedLabelText.Text = isEMode ? "NETWORK" : "ネットワーク";
        NetworkIpLabelText.Text = isEMode ? "IP ADDRESS" : "IPアドレス";

        Brush labelColor = isEMode ? eTextBrush : isWhiteTheme ? Brushes.Black : Brushes.White;
        ClockDateText.Foreground = labelColor;
        ClockTimeText.Foreground = labelColor;
        SegmentClock.SegmentBrush = labelColor;
        SegmentClock.Visibility = isEMode ? Visibility.Visible : Visibility.Collapsed;
        ClockTimeText.Visibility = isEMode ? Visibility.Collapsed : Visibility.Visible;
        IncomeLabelText.Foreground = labelColor;
        CpuLabelText.Foreground = labelColor;
        MemoryLabelText.Foreground = labelColor;
        GpuLabelText.Foreground = labelColor;
        VramLabelText.Foreground = labelColor;
        DiskLabelText.Foreground = labelColor;
        NetworkSpeedLabelText.Foreground = labelColor;
        NetworkIpLabelText.Foreground = labelColor;
        NetworkDnsLabelText.Foreground = labelColor;

        foreach (var heading in new[]
        {
            ClockDateText, IncomeLabelText, CpuLabelText, MemoryLabelText, GpuLabelText,
            VramLabelText, DiskLabelText, NetworkSpeedLabelText, NetworkIpLabelText, NetworkDnsLabelText,
        })
        {
            if (isEMode)
                heading.FontFamily = new System.Windows.Media.FontFamily("Arial");
            else
                heading.ClearValue(System.Windows.Controls.TextBlock.FontFamilyProperty);
        }
        ClockDateText.TextAlignment = isEMode ? TextAlignment.Left : TextAlignment.Center;

        foreach (var (key, dark, light) in ThemedBrushes)
        {
            var color = isEMode ? key switch
            {
                "CpuBrush" => Color.FromRgb(0x4C, 0xBF, 0xF5),
                "MemoryBrush" => Color.FromRgb(0x00, 0xDB, 0xD6),
                "GpuBrush" or "VramBrush" => Color.FromRgb(0x50, 0xEB, 0x91),
                "DiskBrush" => Color.FromRgb(0x50, 0xC3, 0xF7),
                "DiskWarningBrush" => Color.FromRgb(0xC0, 0x00, 0x00),
                "NetworkDownloadBrush" => Color.FromRgb(0xC0, 0x00, 0x00),
                "NetworkUploadBrush" => Color.FromRgb(0xFF, 0x76, 0x00),
                "NetworkAddressBrush" => Color.FromRgb(0xFF, 0x76, 0x00),
                _ => isWhiteTheme ? light : dark,
            } : isWhiteTheme ? light : dark;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            BackgroundBorder.Resources[key] = brush;
            // 使用率・容量・通信速度はグラフと同じ色で表示する。
            BackgroundBorder.Resources[key + "Text"] = isEMode
                && key is ("SecondaryTextBrush" or "LunchBrush") ? eTextBrush : brush;
        }

        Brush ThemeBrush(string key) => (Brush)BackgroundBorder.Resources[key];
        BackgroundBorder.Resources["CpuNameTextBrush"] = ThemeBrush(isEMode ? "CpuBrush" : "SecondaryTextBrush");
        BackgroundBorder.Resources["GpuNameTextBrush"] = ThemeBrush(isEMode ? "GpuBrush" : "SecondaryTextBrush");
        BackgroundBorder.Resources["DiskNameTextBrush"] = ThemeBrush(isEMode ? "DiskBrush" : "SecondaryTextBrush");
        BackgroundBorder.Resources["DiskWarningNameTextBrush"] = ThemeBrush(isEMode ? "DiskWarningBrush" : "SecondaryTextBrush");
        BackgroundBorder.Resources["DiskWarningValueTextBrush"] = ThemeBrush(isEMode ? "DiskWarningBrush" : "DiskBrush");
        BackgroundBorder.Resources["VolumeTrackBrush"] = isEMode
            ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        if (isEMode)
        {
            IncomeLabelText.Foreground = ThemeBrush("WorkHoursBrush");
            CpuLabelText.Foreground = ThemeBrush("CpuBrush");
            MemoryLabelText.Foreground = ThemeBrush("MemoryBrush");
            GpuLabelText.Foreground = ThemeBrush("GpuBrush");
            VramLabelText.Foreground = ThemeBrush("VramBrush");
            DiskLabelText.Foreground = ThemeBrush("DiskBrush");
            NetworkSpeedLabelText.Foreground = ThemeBrush("NetworkUploadBrush");
        }

        var gridBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xDB, 0xD6));
        gridBrush.Freeze();
        foreach (var graph in new[] { CpuGraph, MemoryGraph, GpuGraph, VramGraph })
        {
            if (isEMode)
            {
                graph.GridBrush = gridBrush;
                graph.FillOpacity = 1;
            }
            else
            {
                graph.ClearValue(GraphControl.GridBrushProperty);
                graph.ClearValue(GraphControl.FillOpacityProperty);
            }
        }
        if (isEMode)
            NetworkGraph.GridBrush = gridBrush;
        else
            NetworkGraph.ClearValue(DualSeriesGraphControl.GridBrushProperty);

        ClockSection.Visibility = settings.ShowClock ? Visibility.Visible : Visibility.Collapsed;
        WorkHoursSection.Visibility = settings.ShowWorkHours ? Visibility.Visible : Visibility.Collapsed;
        CpuSection.Visibility = settings.ShowCpu ? Visibility.Visible : Visibility.Collapsed;
        MemorySection.Visibility = settings.ShowMemory ? Visibility.Visible : Visibility.Collapsed;
        GpuSection.Visibility = settings.ShowGpu ? Visibility.Visible : Visibility.Collapsed;
        VramSection.Visibility = settings.ShowVram ? Visibility.Visible : Visibility.Collapsed;
        DiskSection.Visibility = settings.ShowDisk ? Visibility.Visible : Visibility.Collapsed;
        NetworkSection.Visibility = settings.ShowNetwork ? Visibility.Visible : Visibility.Collapsed;
        CustomImage.Source = settings.ShowCustomImage ? CustomImageLoader.Load(settings.CustomImagePath) : null;
        CustomImage.Visibility = CustomImage.Source is null ? Visibility.Collapsed : Visibility.Visible;

        _workStartTime = settings.WorkStartTime;
        _workEndTime = settings.WorkEndTime;
        _lunchStartTime = settings.LunchStartTime;
        _lunchEndTime = settings.LunchEndTime;
        _incomeSettings = settings;
        UpdateClockAndWorkHours();
    }

    protected override void OnClosed(EventArgs e)
    {
        _clockTimer.Stop();
        _samplingService.SampleUpdated -= OnSampleUpdated;
        _clickThrough.Dispose();
        base.OnClosed(e);
    }
}
