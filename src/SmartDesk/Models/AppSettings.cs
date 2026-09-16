namespace SmartDesk.Models;

public sealed class AppSettings
{
    public string HomeUrl { get; set; } = "https://www.google.com/";
    public string Theme { get; set; } = "Dark";
    public bool OpenQuickLinksInNewTab { get; set; }
    public bool RestoreLastSession { get; set; } = true;
    public List<string> LastOpenTabs { get; set; } = [];
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 800;
    public bool WindowMaximized { get; set; }
}
