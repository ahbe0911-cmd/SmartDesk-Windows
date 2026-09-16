using SmartDesk.Models;
using SmartDesk.Services;
using SmartDesk.Views;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartDesk;

public partial class MainWindow
{
    private DashboardView? _dashboardView;
    private TabItem? _dashboardTab;

    public void EnableDashboardV2()
    {
        if (_dashboardTab is not null) return;

        _dashboardView = new DashboardView();
        _dashboardView.SetQuickLinks(_quickLinks);
        _dashboardView.QuickLinkRequested += Dashboard_QuickLinkRequested;
        _dashboardView.CalendarRequested += (_, _) => OpenPlanner();
        _dashboardView.NotesRemindersRequested += (_, _) => OpenPlanner();
        _dashboardView.ThemeRequested += Dashboard_ThemeRequested;

        _dashboardTab = new TabItem
        {
            Header = "⌂  صفحه اصلی",
            Content = _dashboardView,
            Tag = _dashboardView,
            Padding = new Thickness(16, 8, 16, 8)
        };

        BrowserTabs.Items.Insert(0, _dashboardTab);
        BrowserTabs.SelectedItem = _dashboardTab;
        _quickLinks.CollectionChanged += QuickLinks_CollectionChangedForDashboard;
        HomeButton.PreviewMouseLeftButtonDown += DashboardHomeButton_PreviewMouseLeftButtonDown;
        WindowsReminderService.RescheduleAll(_data.Reminders);
        StatusText.Text = "داشبورد SmartDesk آماده است.";
        Title = "میزکار هوشمند";
    }

    private void OpenPlanner()
    {
        var window = new PersianPlannerWindow(_data, _dataService) { Owner = this };
        window.ShowDialog();
        StatusText.Text = "تقویم شمسی، یادداشت‌ها و یادآورها به‌روز شدند.";
    }

    private void Dashboard_ThemeRequested(object? sender, string theme)
    {
        _data.Settings.Theme = theme;
        ThemeService.Apply(theme);
        _dataService.Save(_data);
        foreach (var browser in BrowserTabs.Items.OfType<TabItem>().Select(x => x.Tag).OfType<BrowserTabView>()) browser.ApplyTheme();
        StatusText.Text = theme == "Light" ? "تم روشن فعال شد." : "تم تیره فعال شد.";
    }

    private void QuickLinks_CollectionChangedForDashboard(object? sender, NotifyCollectionChangedEventArgs e) => _dashboardView?.SetQuickLinks(_quickLinks);

    private async void Dashboard_QuickLinkRequested(object? sender, QuickLink link)
    {
        var browser = await CreateTabAsync(link.Url, activate: true);
        if (browser is not null) StatusText.Text = $"«{link.Title}» در SmartDesk باز شد.";
    }

    private void DashboardHomeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ShowDashboard();
        e.Handled = true;
    }

    private void ShowDashboard()
    {
        if (_dashboardTab is null) return;
        BrowserTabs.SelectedItem = _dashboardTab;
        AddressBox.Text = string.Empty;
        StatusText.Text = "صفحه اصلی SmartDesk";
        Title = "میزکار هوشمند";
    }
}