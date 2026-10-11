using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Xml.Linq;
using Size = System.Windows.Size;

namespace Yomi.App.Views;

/// <summary>同梱SVGのセグメントパスを組み合わせて数字を描画する時計。</summary>
public sealed class SevenSegmentClock : FrameworkElement
{
    private const double DigitStep = 330.56085;
    private const double MinutesStart = 729.50335;
    private const double SecondsStart = 1476.809;
    private const double SecondsScale = 0.75;
    private const double WidthRatio = 0.9;

    private static readonly ClockTemplate Template = LoadTemplate();
    private static double DigitHeight => Template.DigitHeight;
    private static double FaceWidth => SecondsStart + (DigitStep + Template.DigitWidth) * SecondsScale;

    // ビット順: 上、右上、右下、下、左下、左上、中央。
    private static readonly int[] DigitMasks =
        [0b0111111, 0b0000110, 0b1011011, 0b1001111, 0b1100110,
         0b1101101, 0b1111101, 0b0000111, 0b1111111, 0b1101111];

    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
        nameof(Time), typeof(DateTime), typeof(SevenSegmentClock),
        new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SegmentBrushProperty = DependencyProperty.Register(
        nameof(SegmentBrush), typeof(Brush), typeof(SevenSegmentClock),
        new FrameworkPropertyMetadata(Brushes.Gold, FrameworkPropertyMetadataOptions.AffectsRender));

    public DateTime Time
    {
        get => (DateTime)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public Brush SegmentBrush
    {
        get => (Brush)GetValue(SegmentBrushProperty);
        set => SetValue(SegmentBrushProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : FaceWidth / WidthRatio;
        return new Size(width, width * WidthRatio * DigitHeight / FaceWidth);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var scale = Math.Min(ActualWidth * WidthRatio / FaceWidth, ActualHeight / DigitHeight);
        if (scale <= 0) return;
        dc.PushTransform(new TranslateTransform((ActualWidth - FaceWidth * scale) / 2, 0));
        dc.PushTransform(new ScaleTransform(scale, scale));

        DrawPair(dc, Time.Hour, 0, 1);
        DrawPair(dc, Time.Minute, MinutesStart, 1);
        DrawPair(dc, Time.Second, SecondsStart, SecondsScale);
        foreach (var colon in Template.Colons)
            dc.DrawGeometry(SegmentBrush, null, colon);

        dc.Pop();
        dc.Pop();
    }

    private void DrawPair(DrawingContext dc, int value, double x, double scale)
    {
        DrawDigit(dc, value / 10, x, scale);
        DrawDigit(dc, value % 10, x + DigitStep * scale, scale);
    }

    private void DrawDigit(DrawingContext dc, int digit, double x, double scale)
    {
        dc.PushTransform(new TranslateTransform(x, DigitHeight * (1 - scale)));
        dc.PushTransform(new ScaleTransform(scale, scale));
        for (var segment = 0; segment < Template.Segments.Length; segment++)
        {
            if ((DigitMasks[digit] & (1 << segment)) != 0)
                dc.DrawGeometry(SegmentBrush, null, Template.Segments[segment]);
        }
        dc.Pop();
        dc.Pop();
    }

    private static ClockTemplate LoadTemplate()
    {
        var resource = Application.GetResourceStream(new Uri("/yomi;component/Assets/digital-clock.svg", UriKind.Relative))
            ?? throw new InvalidOperationException("Clock SVG resource is missing.");
        using var stream = resource.Stream;
        var document = XDocument.Load(stream);
        XNamespace svg = "http://www.w3.org/2000/svg";
        var digit = document.Descendants(svg + "g").Single(element => (string?)element.Attribute("id") == "g6");
        var paths = digit.Elements(svg + "path").ToDictionary(element => (string)element.Attribute("id")!);
        // SVGの最初の数字から、上・右上・右下・下・左下・左上・中央の順に取り出す。
        string[] segmentIds = ["path5-76", "path5-7", "path5-2-1", "path5-76-40-6", "path5-2", "path5", "path5-76-40"];
        var segments = segmentIds.Select(id => Geometry.Parse((string)paths[id].Attribute("d")!)).ToArray();
        var bounds = Rect.Empty;
        foreach (var segment in segments) bounds.Union(segment.Bounds);

        Geometry Normalize(Geometry geometry)
        {
            geometry = geometry.Clone();
            geometry.Transform = new TranslateTransform(-bounds.Left, -bounds.Top);
            geometry.Freeze();
            return geometry;
        }
        var colonIds = new HashSet<string> { "path9", "path10", "path11", "path13" };
        var colons = document.Descendants(svg + "path")
            .Where(element => colonIds.Contains((string?)element.Attribute("id") ?? ""))
            .Select(element => Normalize(Geometry.Parse((string)element.Attribute("d")!))).ToArray();
        return new ClockTemplate(segments.Select(Normalize).ToArray(), colons, bounds.Width, bounds.Height);
    }

    private sealed record ClockTemplate(Geometry[] Segments, Geometry[] Colons, double DigitWidth, double DigitHeight);

    protected override AutomationPeer OnCreateAutomationPeer() => new ClockAutomationPeer(this);

    private sealed class ClockAutomationPeer(SevenSegmentClock owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(SevenSegmentClock);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string GetNameCore() => owner.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
