using System.IO;
using System.Text;

namespace SmartDesk.Services;

public static class AppLogger
{
    private static readonly object Sync = new();

    public static void Error(Exception exception, string context)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);
            var path = Path.Combine(AppPaths.LogDirectory, $"smartdesk-{DateTime.Now:yyyy-MM-dd}.log");
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}{Environment.NewLine}{exception}{Environment.NewLine}{new string('-', 72)}{Environment.NewLine}";
            lock (Sync)
            {
                File.AppendAllText(path, entry, new UTF8Encoding(false));
            }
        }
        catch
        {
            // Logging must never crash the application.
        }
    }
}
