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

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(DiskUsageBar),
        new FrameworkPropertyMetadata(CreateFrozenBrush(Color.FromArgb(40, 255, 255, 255)), FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public static readonly DependencyProperty IsRoundedProperty = DependencyProperty.Register(
        nameof(IsRounded), typeof(bool), typeof(DiskUsageBar),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsRounded
    {
        get => (bool)GetValue(IsRoundedProperty);
        set => SetValue(IsRoundedProperty, value);
    }

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

        var radius = IsRounded ? height / 2 : 0;
        dc.DrawRoundedRectangle(TrackBrush, null, new Rect(0, 0, width, height), radius, radius);

        var usage = Math.Clamp(UsagePercent, 0, 100);
        var freePercent = 100 - usage;
        var fillWidth = width * usage / 100.0;
        if (fillWidth <= 0) return;

        var fillBrush = freePercent < LowSpaceThresholdPercent ? WarningBrush : NormalBrush;
        dc.DrawRoundedRectangle(fillBrush, null, new Rect(0, 0, fillWidth, height), radius, radius);
    }
}
