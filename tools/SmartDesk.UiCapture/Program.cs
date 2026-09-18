using SmartDesk.Models;
using SmartDesk.Views;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SmartDesk.UiCapture;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new SmartDesk.App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.InitializeComponent();

        var dashboard = new DashboardView();
        dashboard.SetQuickLinks(new[]
        {
            new QuickLink { Title="اعضا", Category="کتابخانه", Icon="👥", AccentHex="#18A999", SortOrder=0 },
            new QuickLink { Title="امانت", Category="کتابخانه", Icon="📚", AccentHex="#4C8BF5", SortOrder=1 },
            new QuickLink { Title="بازگشت", Category="کتابخانه", Icon="↩", AccentHex="#F0A44B", SortOrder=2 },
            new QuickLink { Title="برنامه‌های فرهنگی", Category="فرهنگی", Icon="✦", AccentHex="#9B7AE8", SortOrder=3 },
            new QuickLink { Title="ثبت کتاب", Category="کتابخانه", Icon="＋", AccentHex="#E56B8A", SortOrder=4 },
            new QuickLink { Title="گزارش‌ها", Category="مدیریت", Icon="▥", AccentHex="#46A6A1", SortOrder=5 },
            new QuickLink { Title="سامانه نهاد", Category="وب", Icon="⌂", AccentHex="#637381", SortOrder=6 }
        });

        var window = new Window
        {
            Title = "SmartDesk V8 Render Capture",
            Width = 1536,
            Height = 1024,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Content = dashboard,
            Background = Brushes.White,
            Left = -20000,
            Top = -20000
        };

        window.Show();
        window.UpdateLayout();
        DoEvents();
        dashboard.UpdateLayout();
        DoEvents();

        const int width = 1536;
        const int height = 1024;
        dashboard.Measure(new Size(width, height));
        dashboard.Arrange(new Rect(0, 0, width, height));
        dashboard.UpdateLayout();

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(dashboard);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var outDir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, "SmartDesk-V8-real-render.png");
        using (var fs = File.Create(outPath)) encoder.Save(fs);
        Console.WriteLine(outPath);
        window.Close();
        app.Shutdown();
    }

    private static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(_ => { frame.Continue = false; return null; }), null);
        Dispatcher.PushFrame(frame);
    }
}
