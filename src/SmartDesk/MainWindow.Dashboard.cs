using SmartDesk.Models;
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
        if (_dashboardTab is not null)
        {
            return;
        }

        _dashboardView = new DashboardView();
        _dashboardView.SetQuickLinks(_quickLinks);
        _dashboardView.QuickLinkRequested += Dashboard_QuickLinkRequested;
        _dashboardView.QuickNoteRequested += (_, _) => ShowDashboardFeatureMessage("یادداشت سریع", "ماژول یادداشت و یادآور در مرحله بعد فعال می‌شود.");
        _dashboardView.CalendarRequested += (_, _) => ShowDashboardFeatureMessage("تقویم شمسی", "تقویم شمسی و یادآورها در مرحله بعد به داشبورد متصل می‌شوند.");
        _dashboardView.TodayTasksRequested += (_, _) => ShowDashboardFeatureMessage("کارهای امروز", "مدیریت کارهای روزانه در مرحله بعد فعال می‌شود.");
        _dashboardView.ToolsRequested += (_, _) => ShowDashboardFeatureMessage("ابزارها", "ابزارهای کاربردی در مرحله بعد اضافه می‌شوند.");

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
        StatusText.Text = "داشبورد SmartDesk آماده است.";
        Title = "میزکار هوشمند";
    }

    private void QuickLinks_CollectionChangedForDashboard(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _dashboardView?.SetQuickLinks(_quickLinks);
    }

    private async void Dashboard_QuickLinkRequested(object? sender, QuickLink link)
    {
        var browser = await CreateTabAsync(link.Url, activate: true);
        if (browser is not null)
        {
            StatusText.Text = $"«{link.Title}» در SmartDesk باز شد.";
        }
    }

    private void DashboardHomeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ShowDashboard();
        e.Handled = true;
    }

    private void ShowDashboard()
    {
        if (_dashboardTab is null)
        {
            return;
        }

        BrowserTabs.SelectedItem = _dashboardTab;
        AddressBox.Text = string.Empty;
        StatusText.Text = "صفحه اصلی SmartDesk";
        Title = "میزکار هوشمند";
    }

    private void ShowDashboardFeatureMessage(string title, string message)
    {
        StatusText.Text = message;
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
