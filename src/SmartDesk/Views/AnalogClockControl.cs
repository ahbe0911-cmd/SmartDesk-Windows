using System.Windows;
using System.Windows.Media;

namespace SmartDesk.Views;

public sealed class AnalogClockControl : FrameworkElement
{
    public AnalogClockControl() => SnapsToDevicePixels = true;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;
        var c = new Point(ActualWidth / 2, ActualHeight / 2);
        var r = size * .45;
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(18, 35, 42)), null, c, r, r);
        for (var i = 0; i < 60; i++)
        {
            var a = i * Math.PI / 30;
            var major = i % 5 == 0;
            var p1 = new Point(c.X + Math.Sin(a) * r * (major ? .80 : .88), c.Y - Math.Cos(a) * r * (major ? .80 : .88));
            var p2 = new Point(c.X + Math.Sin(a) * r * .94, c.Y - Math.Cos(a) * r * .94);
            dc.DrawLine(new Pen(new SolidColorBrush(major ? Colors.White : Color.FromArgb(120,255,255,255)), major ? 2 : 1), p1, p2);
        }
        var now = DateTime.Now;
        DrawHand(dc, c, r * .50, (now.Hour % 12 + now.Minute / 60d) * 30, 5, Colors.White);
        DrawHand(dc, c, r * .70, (now.Minute + now.Second / 60d) * 6, 3, Colors.White);
        DrawHand(dc, c, r * .76, now.Second * 6, 1.5, Color.FromRgb(55,190,155));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(55,190,155)), null, c, 6, 6);
        InvalidateVisual();
    }

    private static void DrawHand(DrawingContext dc, Point c, double length, double degrees, double width, Color color)
    {
        var a = degrees * Math.PI / 180;
        var p = new Point(c.X + Math.Sin(a) * length, c.Y - Math.Cos(a) * length);
        dc.DrawLine(new Pen(new SolidColorBrush(color), width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, c, p);
    }
}