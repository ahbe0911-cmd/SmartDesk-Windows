namespace SmartDesk.Models;

public sealed class ReminderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime DueAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
