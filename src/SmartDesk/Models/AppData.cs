namespace SmartDesk.Models;

public sealed class AppData
{
    public int SchemaVersion { get; set; } = 2;
    public List<QuickLink> QuickLinks { get; set; } = [];
    public List<NoteItem> Notes { get; set; } = [];
    public List<ReminderItem> Reminders { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}
