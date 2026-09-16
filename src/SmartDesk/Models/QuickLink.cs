namespace SmartDesk.Models;

public sealed class QuickLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = "عمومی";
    public string Icon { get; set; } = "●";
    public string AccentHex { get; set; } = "#18A999";
    public int SortOrder { get; set; }

    public QuickLink Clone() => new()
    {
        Id = Id,
        Title = Title,
        Url = Url,
        Category = Category,
        Icon = Icon,
        AccentHex = AccentHex,
        SortOrder = SortOrder
    };
}
