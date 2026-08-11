using System.Windows;
using System.Windows.Media;

namespace Yomi.App.Views;

/// <summary>
/// ディスク使用率を表す横棒。空き容量が <see cref="LowSpaceThresholdPercent"/> を
/// 下回ると警告色で塗りつぶす軽量な自作コントロール。
/// </summary>
public sealed class DiskUsageBar : FrameworkElement
{
    private const double LowSpaceThresholdPercent = 10.0;

    private static readonly Brush TrackBrush = CreateFrozenBrush(Color.FromArgb(40, 255, 255, 255));

    public static readonly DependencyProperty UsagePercentProperty = DependencyProperty.Register(
        nameof(UsagePercent), typeof(double), typeof(DiskUsageBar),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NormalBrushProperty = DependencyProperty.Register(
        nameof(NormalBrush), typeof(Brush), typeof(DiskUsageBar),
        new FrameworkPropertyMetadata(CreateFrozenBrush(Color.FromArgb(255, 0x3B, 0x9E, 0xFF)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WarningBrushProperty = DependencyProperty.Register(
        nameof(WarningBrush), typeof(Brush), typeof(DiskUsageBar),
        new FrameworkPropertyMetadata(CreateFrozenBrush(Color.FromArgb(255, 0xF0, 0x4A, 0x38)), FrameworkPropertyMetadataOptions.AffectsRender));

    public double UsagePercent
    {
        get => (double)GetValue(UsagePercentProperty);
        set => SetValue(UsagePercentProperty, value);
    }

    public Brush NormalBrush
    {
        get => (Brush)GetValue(NormalBrushProperty);
        set => SetValue(NormalBrushProperty, value);
    }

    public Brush WarningBrush
    {
        get => (Brush)GetValue(WarningBrushProperty);
        set => SetValue(WarningBrushProperty, value);
    }

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        var radius = height / 2;
        dc.DrawRoundedRectangle(TrackBrush, null, new Rect(0, 0, width, height), radius, radius);

        var usage = Math.Clamp(UsagePercent, 0, 100);
        var freePercent = 100 - usage;
        var fillWidth = width * usage / 100.0;
        if (fillWidth <= 0) return;

        var fillBrush = freePercent < LowSpaceThresholdPercent ? WarningBrush : NormalBrush;
        dc.DrawRoundedRectangle(fillBrush, null, new Rect(0, 0, fillWidth, height), radius, radius);
    }
}
