using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SmartDesk.Views;

public sealed class AnalogClockControl : FrameworkElement
{
    private readonly Typeface _typeface = new("Vazirmatn");
    public AnalogClockControl() => SnapsToDevicePixels = true;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;
        var c = new Point(ActualWidth / 2, ActualHeight / 2);
        var r = size * .45;
        var face = new RadialGradientBrush(Color.FromRgb(18, 67, 63), Color.FromRgb(4, 28, 30));
        dc.DrawEllipse(face, new Pen(new SolidColorBrush(Color.FromArgb(90,255,255,255)), 1), c, r, r);

        for (var i = 0; i < 60; i++)
        {
            var a = i * Math.PI / 30;
            var major = i % 5 == 0;
            var p1 = new Point(c.X + Math.Sin(a) * r * (major ? .79 : .88), c.Y - Math.Cos(a) * r * (major ? .79 : .88));
            var p2 = new Point(c.X + Math.Sin(a) * r * .94, c.Y - Math.Cos(a) * r * .94);
            dc.DrawLine(new Pen(new SolidColorBrush(major ? Colors.White : Color.FromArgb(100,255,255,255)), major ? 2 : 1), p1, p2);
        }

        var fa = new[]{"۱۲","۱","۲","۳","۴","۵","۶","۷","۸","۹","۱۰","۱۱"};
        for (var i=0;i<12;i++)
        {
            var a=i*Math.PI/6;
            var text=new FormattedText(fa[i], CultureInfo.GetCultureInfo("fa-IR"), FlowDirection.RightToLeft, _typeface, Math.Max(10,r*.12), Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var x=c.X+Math.Sin(a)*r*.66-text.Width/2;
            var y=c.Y-Math.Cos(a)*r*.66-text.Height/2;
            dc.DrawText(text,new Point(x,y));
        }

        var now = DateTime.Now;
        DrawHand(dc, c, r * .48, (now.Hour % 12 + now.Minute / 60d) * 30, 5, Colors.White);
        DrawHand(dc, c, r * .66, (now.Minute + now.Second / 60d) * 6, 3, Colors.White);
        DrawHand(dc, c, r * .72, now.Second * 6, 1.4, Color.FromRgb(20,210,174));
        dc.DrawEllipse(Brushes.White, null, c, 5, 5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(20,210,174)), null, c, 2.5, 2.5);
    }

    private static void DrawHand(DrawingContext dc, Point c, double length, double degrees, double width, Color color)
    {
        var a = degrees * Math.PI / 180;
        var p = new Point(c.X + Math.Sin(a) * length, c.Y - Math.Cos(a) * length);
        dc.DrawLine(new Pen(new SolidColorBrush(color), width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, c, p);
    }
}