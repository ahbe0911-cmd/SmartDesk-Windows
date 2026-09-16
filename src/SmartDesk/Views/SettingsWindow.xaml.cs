using SmartDesk.Models;
using SmartDesk.Services;
using System.Windows;
using System.Windows.Controls;

namespace SmartDesk.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _source;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _source = settings;

        HomeUrlBox.Text = settings.HomeUrl;
        OpenInNewTabCheck.IsChecked = settings.OpenQuickLinksInNewTab;
        RestoreSessionCheck.IsChecked = settings.RestoreLastSession;

        foreach (var item in ThemeCombo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), settings.Theme, StringComparison.OrdinalIgnoreCase))
            {
                ThemeCombo.SelectedItem = item;
                break;
            }
        }

        ThemeCombo.SelectedIndex = ThemeCombo.SelectedIndex < 0 ? 0 : ThemeCombo.SelectedIndex;
    }

    public AppSettings? Result { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!BrowserUriService.TryNormalize(HomeUrlBox.Text, out var homeUri))
        {
            ValidationText.Text = "نشانی صفحه خانه معتبر نیست.";
            ValidationText.Visibility = Visibility.Visible;
            HomeUrlBox.Focus();
            return;
        }

        var theme = (ThemeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Dark";
        Result = new AppSettings
        {
            HomeUrl = homeUri.AbsoluteUri,
            Theme = theme,
            OpenQuickLinksInNewTab = OpenInNewTabCheck.IsChecked == true,
            RestoreLastSession = RestoreSessionCheck.IsChecked == true,
            LastOpenTabs = _source.LastOpenTabs.ToList(),
            WindowWidth = _source.WindowWidth,
            WindowHeight = _source.WindowHeight,
            WindowMaximized = _source.WindowMaximized
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
