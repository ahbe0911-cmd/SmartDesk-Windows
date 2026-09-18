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
        _dashboardView.AddLinkRequested += Dashboard_AddLinkRequested;
        _dashboardView.EditLinkRequested += Dashboard_EditLinkRequested;
        _dashboardView.DeleteLinkRequested += Dashboard_DeleteLinkRequested;
        _dashboardView.PlannerRequested += (_, _) => OpenPlanner();
        _dashboardView.ThemeRequested += Dashboard_ThemeRequested;

        _dashboardTab = new TabItem
        {
            Header = "⌂  صفحه اصلی",
            Content = _dashboardView,
            Tag = _dashboardView,
            Padding = new Thickness(16, 8, 16, 8)
        };

        // Dashboard is the primary desktop surface. Legacy browser chrome stays out of the visual tree.
        Content = _dashboardView;
        _quickLinks.CollectionChanged += QuickLinks_CollectionChangedForDashboard;
        WindowsReminderService.RescheduleAll(_data.Reminders);
        StatusText.Text = "داشبورد SmartDesk V8 آماده است.";
        Title = "میزکار هوشمند";
    }

    private void Dashboard_AddLinkRequested(object? sender, EventArgs e)
    {
        if (_quickLinks.Count >= 7)
        {
            MessageBox.Show("حداکثر ۷ لینک سریع می‌توانید داشته باشید. برای افزودن لینک جدید، یکی از لینک‌های فعلی را حذف یا ویرایش کنید.", "لینک‌های سریع", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new QuickLinkEditorWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null) return;
        dialog.Result.SortOrder = _quickLinks.Count == 0 ? 0 : _quickLinks.Max(item => item.SortOrder) + 1;
        _quickLinks.Add(dialog.Result);
        SaveQuickLinks();
        _dashboardView?.SetQuickLinks(_quickLinks);
        StatusText.Text = "لینک سریع جدید ذخیره شد.";
    }

    private void Dashboard_EditLinkRequested(object? sender, QuickLink link)
    {
        var dialog = new QuickLinkEditorWindow(link) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null) return;
        var index = _quickLinks.IndexOf(link);
        if (index < 0) return;
        _quickLinks[index] = dialog.Result;
        SaveQuickLinks();
        _dashboardView?.SetQuickLinks(_quickLinks);
        StatusText.Text = "لینک سریع ویرایش شد.";
    }

    private void Dashboard_DeleteLinkRequested(object? sender, QuickLink link)
    {
        var answer = MessageBox.Show($"لینک «{link.Title}» حذف شود؟", "حذف لینک سریع", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;
        _quickLinks.Remove(link);
        SaveQuickLinks();
        _dashboardView?.SetQuickLinks(_quickLinks);
        StatusText.Text = "لینک سریع حذف شد.";
    }

    private void OpenPlanner()
    {
        var window = new PersianPlannerWindow(_data, _dataService) { Owner = this };
        window.ShowDialog();
        WindowsReminderService.RescheduleAll(_data.Reminders);
        StatusText.Text = "برنامه‌ریز و یادآورها به‌روز شدند.";
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

    private void Dashboard_QuickLinkRequested(object? sender, QuickLink link)
    {
        if (_browserEnvironment is null)
        {
            StatusText.Text = "مرورگر داخلی هنوز آماده نیست.";
            return;
        }

        var browserWindow = new QuickLinkBrowserWindow(_browserEnvironment, link.Title, link.Url) { Owner = this };
        StatusText.Text = $"«{link.Title}» در پنجره مرکزی SmartDesk باز شد.";
        browserWindow.ShowDialog();
        ShowDashboard();
    }

    private void DashboardHomeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ShowDashboard();
        e.Handled = true;
    }

    private void ShowDashboard()
    {
        if (_dashboardView is null) return;
        if (!ReferenceEquals(Content, _dashboardView)) Content = _dashboardView;
        AddressBox.Text = string.Empty;
        StatusText.Text = "صفحه اصلی SmartDesk V8";
        Title = "میزکار هوشمند";
    }
}