using System.Windows;
using System.Windows.Media;

namespace Yomi.App.Views;

/// <summary>
/// 2系列（送信/受信など）を重ねて描画するタスクマネージャー風の時系列グラフ。
/// 値の上限が不定なため、直近の履歴内の最大値に応じてY軸を自動スケールする。
/// </summary>
public sealed class DualSeriesGraphControl : FrameworkElement
{
    public static readonly DependencyProperty PrimaryLineBrushProperty = DependencyProperty.Register(
        nameof(PrimaryLineBrush), typeof(Brush), typeof(DualSeriesGraphControl),
        new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender, OnPrimaryPenChanged));

    public static readonly DependencyProperty SecondaryLineBrushProperty = DependencyProperty.Register(
        nameof(SecondaryLineBrush), typeof(Brush), typeof(DualSeriesGraphControl),
        new FrameworkPropertyMetadata(Brushes.OrangeRed, FrameworkPropertyMetadataOptions.AffectsRender, OnSecondaryPenChanged));

    /// <summary>プライマリ系列の線の太さ。2系列を同色にする場合の見分けに使う。</summary>
    public static readonly DependencyProperty PrimaryLineThicknessProperty = DependencyProperty.Register(
        nameof(PrimaryLineThickness), typeof(double), typeof(DualSeriesGraphControl),
        new FrameworkPropertyMetadata(1.5, FrameworkPropertyMetadataOptions.AffectsRender, OnPrimaryPenChanged));

    /// <summary>セカンダリ系列の線の太さ。2系列を同色にする場合の見分けに使う。</summary>
    public static readonly DependencyProperty SecondaryLineThicknessProperty = DependencyProperty.Register(
        nameof(SecondaryLineThickness), typeof(double), typeof(DualSeriesGraphControl),
        new FrameworkPropertyMetadata(1.5, FrameworkPropertyMetadataOptions.AffectsRender, OnSecondaryPenChanged));

    public double PrimaryLineThickness
    {
        get => (double)GetValue(PrimaryLineThicknessProperty);
        set => SetValue(PrimaryLineThicknessProperty, value);
    }

    public double SecondaryLineThickness
    {
        get => (double)GetValue(SecondaryLineThicknessProperty);
        set => SetValue(SecondaryLineThicknessProperty, value);
    }

    public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
        nameof(GridBrush), typeof(Brush), typeof(DualSeriesGraphControl),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), FrameworkPropertyMetadataOptions.AffectsRender, OnGridBrushChanged));

    // ペンは毎フレーム同一なので、ブラシ/太さ変更時のみ生成し直してキャッシュする(OnRender での new を避ける)。
    private Pen _primaryPen = CreateFrozenPen(Brushes.DeepSkyBlue, 1.5);
    private Pen _secondaryPen = CreateFrozenPen(Brushes.OrangeRed, 1.5);
    private Pen _gridPen = CreateFrozenPen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);

    private static void OnPrimaryPenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DualSeriesGraphControl)d;
        control._primaryPen = CreateFrozenPen(control.PrimaryLineBrush, control.PrimaryLineThickness);
    }

    private static void OnSecondaryPenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DualSeriesGraphControl)d;
        control._secondaryPen = CreateFrozenPen(control.SecondaryLineBrush, control.SecondaryLineThickness);
    }

    private static void OnGridBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((DualSeriesGraphControl)d)._gridPen = CreateFrozenPen((Brush)e.NewValue, 1);
    }

    private static Pen CreateFrozenPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        if (pen.CanFreeze) pen.Freeze();
        return pen;
    }

    public Brush PrimaryLineBrush
    {
        get => (Brush)GetValue(PrimaryLineBrushProperty);
        set => SetValue(PrimaryLineBrushProperty, value);
    }

    public Brush SecondaryLineBrush
    {
        get => (Brush)GetValue(SecondaryLineBrushProperty);
        set => SetValue(SecondaryLineBrushProperty, value);
    }

    public Brush GridBrush
    {
        get => (Brush)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    private const int HistorySeconds = 60;
    private const double MinScaleMax = 1.0;

    private readonly double[] _primaryHistory = new double[HistorySeconds];
    private readonly double[] _secondaryHistory = new double[HistorySeconds];
    private int _count;

    /// <summary>新しい値の組をリングバッファの末尾に追加し、再描画をリクエストする。</summary>
    public void Push(double primaryValue, double secondaryValue)
    {
        if (_count < HistorySeconds)
        {
            _primaryHistory[_count] = primaryValue;
            _secondaryHistory[_count] = secondaryValue;
            _count++;
        }
        else
        {
            Array.Copy(_primaryHistory, 1, _primaryHistory, 0, HistorySeconds - 1);
            Array.Copy(_secondaryHistory, 1, _secondaryHistory, 0, HistorySeconds - 1);
            _primaryHistory[HistorySeconds - 1] = primaryValue;
            _secondaryHistory[HistorySeconds - 1] = secondaryValue;
        }

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        DrawGrid(dc, width, height);
        if (_count < 2) return;

        var scaleMax = MinScaleMax;
        for (var i = 0; i < _count; i++)
        {
            scaleMax = Math.Max(scaleMax, Math.Max(_primaryHistory[i], _secondaryHistory[i]));
        }

        DrawSeries(dc, width, height, _secondaryHistory, _secondaryPen, scaleMax);
        DrawSeries(dc, width, height, _primaryHistory, _primaryPen, scaleMax);
    }

    private void DrawGrid(DrawingContext dc, double width, double height)
    {
        const int horizontalLines = 4;
        for (var i = 1; i < horizontalLines; i++)
        {
            var y = height * i / horizontalLines;
            dc.DrawLine(_gridPen, new Point(0, y), new Point(width, y));
        }
    }

    private void DrawSeries(DrawingContext dc, double width, double height, double[] history, Pen linePen, double scaleMax)
    {
        var stepX = width / (HistorySeconds - 1);
        // リングバッファ末尾（最新値）が右端に来るよう、有効なサンプル数ぶんだけ右寄せで描画する。
        var startIndex = HistorySeconds - _count;

        for (var i = 1; i < _count; i++)
        {
            var x0 = (startIndex + i - 1) * stepX;
            var y0 = ToY(history[i - 1], height, scaleMax);
            var x1 = (startIndex + i) * stepX;
            var y1 = ToY(history[i], height, scaleMax);
            dc.DrawLine(linePen, new Point(x0, y0), new Point(x1, y1));
        }
    }

    private static double ToY(double value, double height, double scaleMax)
    {
        var clamped = Math.Clamp(value, 0, scaleMax);
        return height - clamped / scaleMax * height;
    }
}
