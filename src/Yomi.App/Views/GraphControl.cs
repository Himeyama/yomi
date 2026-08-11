using System.Windows;
using System.Windows.Media;

namespace Yomi.App.Views;

/// <summary>
/// タスクマネージャー風の時系列グラフ。直近 <see cref="HistorySeconds"/> 秒分の
/// リングバッファを保持し、Push() のたびに再描画する軽量な自作コントロール。
/// </summary>
public sealed class GraphControl : FrameworkElement
{
    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush), typeof(Brush), typeof(GraphControl),
        new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender, OnLineBrushChanged));

    public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
        nameof(GridBrush), typeof(Brush), typeof(GraphControl),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), FrameworkPropertyMetadataOptions.AffectsRender, OnGridBrushChanged));

    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue), typeof(double), typeof(GraphControl),
        new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public Brush GridBrush
    {
        get => (Brush)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>LineBrush と同色・単色半透明の塗りつぶしブラシ。LineBrush変更時に自動更新される。</summary>
    private Brush _fillBrush = CreateFillBrush(Brushes.DeepSkyBlue);

    // ペンは毎フレーム同一なので、ブラシ変更時のみ生成し直してキャッシュする(OnRender での new を避ける)。
    private Pen _linePen = CreateFrozenPen(Brushes.DeepSkyBlue, 1.5);
    private Pen _gridPen = CreateFrozenPen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);

    private static void OnLineBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (GraphControl)d;
        control._fillBrush = CreateFillBrush((Brush)e.NewValue);
        control._linePen = CreateFrozenPen((Brush)e.NewValue, 1.5);
    }

    private static void OnGridBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((GraphControl)d)._gridPen = CreateFrozenPen((Brush)e.NewValue, 1);
    }

    private static Pen CreateFrozenPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        if (pen.CanFreeze) pen.Freeze();
        return pen;
    }

    private static Brush CreateFillBrush(Brush lineBrush)
    {
        var color = (lineBrush as SolidColorBrush)?.Color ?? Colors.DeepSkyBlue;
        var brush = new SolidColorBrush(Color.FromArgb(50, color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    private const int HistorySeconds = 60;
    private readonly double[] _history = new double[HistorySeconds];
    private int _count;

    /// <summary>新しい値をリングバッファの末尾に追加し、再描画をリクエストする。</summary>
    public void Push(double value)
    {
        if (_count < HistorySeconds)
        {
            _history[_count] = value;
            _count++;
        }
        else
        {
            Array.Copy(_history, 1, _history, 0, HistorySeconds - 1);
            _history[HistorySeconds - 1] = value;
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

        DrawSeries(dc, width, height);
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

    private void DrawSeries(DrawingContext dc, double width, double height)
    {
        var stepX = width / (HistorySeconds - 1);
        // リングバッファ末尾（最新値）が右端に来るよう、有効なサンプル数ぶんだけ右寄せで描画する。
        var startIndex = HistorySeconds - _count;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var firstX = startIndex * stepX;
            var firstY = ToY(_history[0], height);
            ctx.BeginFigure(new Point(firstX, firstY), isFilled: true, isClosed: true);

            for (var i = 1; i < _count; i++)
            {
                var x = (startIndex + i) * stepX;
                var y = ToY(_history[i], height);
                ctx.LineTo(new Point(x, y), isStroked: true, isSmoothJoin: false);
            }

            var lastX = (startIndex + _count - 1) * stepX;
            ctx.LineTo(new Point(lastX, height), isStroked: false, isSmoothJoin: false);
            ctx.LineTo(new Point(firstX, height), isStroked: false, isSmoothJoin: false);
        }
        geometry.Freeze();

        dc.DrawGeometry(_fillBrush, null, geometry);

        for (var i = 1; i < _count; i++)
        {
            var x0 = (startIndex + i - 1) * stepX;
            var y0 = ToY(_history[i - 1], height);
            var x1 = (startIndex + i) * stepX;
            var y1 = ToY(_history[i], height);
            dc.DrawLine(_linePen, new Point(x0, y0), new Point(x1, y1));
        }
    }

    private double ToY(double value, double height)
    {
        var clamped = Math.Clamp(value, 0, MaxValue);
        return height - clamped / MaxValue * height;
    }
}
