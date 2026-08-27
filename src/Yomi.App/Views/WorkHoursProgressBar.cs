using System.Windows;
using System.Windows.Media;

namespace Yomi.App.Views;

/// <summary>
/// 業務時間内での現在位置を表す横棒。時間外は塗りつぶさず枠のみ表示する軽量な自作コントロール。
/// </summary>
public sealed class WorkHoursProgressBar : FrameworkElement
{
    private static readonly Brush TrackBrush = CreateFrozenBrush(Color.FromArgb(40, 255, 255, 255));

    public static readonly DependencyProperty ProgressPercentProperty = DependencyProperty.Register(
        nameof(ProgressPercent), typeof(double), typeof(WorkHoursProgressBar),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register(
        nameof(FillBrush), typeof(Brush), typeof(WorkHoursProgressBar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>勤務時間全体に対する経過割合(0-100)。時間外のときは0として扱う。</summary>
    public double ProgressPercent
    {
        get => (double)GetValue(ProgressPercentProperty);
        set => SetValue(ProgressPercentProperty, value);
    }

    /// <summary>塗りつぶし色。null のときは何も塗らない(時間外)。</summary>
    public Brush? FillBrush
    {
        get => (Brush?)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
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

        var fillBrush = FillBrush;
        if (fillBrush is null) return;

        var progress = Math.Clamp(ProgressPercent, 0, 100);
        var fillWidth = width * progress / 100.0;
        if (fillWidth <= 0) return;

        dc.DrawRoundedRectangle(fillBrush, null, new Rect(0, 0, fillWidth, height), radius, radius);
    }
}
