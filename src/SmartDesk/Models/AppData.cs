namespace SmartDesk.Models;

public sealed class AppData
{
    public int SchemaVersion { get; set; } = 1;
    public List<QuickLink> QuickLinks { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}
