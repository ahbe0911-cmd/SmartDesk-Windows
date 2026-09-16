using SmartDesk.Models;
using SmartDesk.Services;
using System.Windows;

namespace SmartDesk.Views;

public partial class QuickLinkEditorWindow : Window
{
    private readonly QuickLink? _original;

    public QuickLinkEditorWindow(QuickLink? link = null)
    {
        InitializeComponent();
        _original = link;

        AccentCombo.ItemsSource = new[]
        {
            new AccentOption("فیروزه‌ای", "#14B8A6"),
            new AccentOption("آبی", "#3B82F6"),
            new AccentOption("بنفش", "#8B5CF6"),
            new AccentOption("نارنجی", "#F59E0B"),
            new AccentOption("صورتی", "#EC4899"),
            new AccentOption("سبز", "#22C55E")
        };

        if (link is null)
        {
            AccentCombo.SelectedIndex = 0;
            CategoryBox.Text = "عمومی";
            return;
        }

        DialogTitle.Text = "ویرایش میانبر";
        TitleBox.Text = link.Title;
        UrlBox.Text = link.Url;
        CategoryBox.Text = link.Category;
        IconBox.Text = link.Icon;
        AccentCombo.SelectedValue = link.AccentHex;
        if (AccentCombo.SelectedIndex < 0)
        {
            AccentCombo.SelectedIndex = 0;
        }
    }

    public QuickLink? Result { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Visibility = Visibility.Collapsed;
        var title = TitleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ShowValidation("عنوان میانبر را وارد کنید.");
            TitleBox.Focus();
            return;
        }

        if (!BrowserUriService.TryNormalize(UrlBox.Text, out var uri))
        {
            ShowValidation("نشانی سایت معتبر نیست.");
            UrlBox.Focus();
            return;
        }

        var icon = IconBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(icon))
        {
            icon = title[..1];
        }

        Result = new QuickLink
        {
            Id = _original?.Id ?? Guid.NewGuid(),
            Title = title,
            Url = uri.AbsoluteUri,
            Category = string.IsNullOrWhiteSpace(CategoryBox.Text) ? "عمومی" : CategoryBox.Text.Trim(),
            Icon = icon,
            AccentHex = AccentCombo.SelectedValue?.ToString() ?? "#14B8A6",
            SortOrder = _original?.SortOrder ?? 0
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ShowValidation(string message)
    {
        ValidationText.Text = message;
        ValidationText.Visibility = Visibility.Visible;
    }

    private sealed record AccentOption(string Name, string Hex);
}
