using System.IO;

namespace SmartDesk.Services;

public static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartDesk");

    public static string DataFile => Path.Combine(DataDirectory, "smartdesk-data.json");
    public static string BackupDirectory => Path.Combine(DataDirectory, "Backups");
    public static string LogDirectory => Path.Combine(DataDirectory, "Logs");
    public static string BrowserDataDirectory => Path.Combine(DataDirectory, "BrowserProfile");
}
