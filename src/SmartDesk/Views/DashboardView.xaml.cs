using SmartDesk.Models;
using SmartDesk.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SmartDesk.Views;

public partial class DashboardView : UserControl
{
    private readonly ObservableCollection<QuickLink> _links = [];
    private readonly DispatcherTimer _clockTimer;

    public event EventHandler<QuickLink>? QuickLinkRequested;
    public event EventHandler<QuickLink>? EditLinkRequested;
    public event EventHandler<QuickLink>? DeleteLinkRequested;
    public event EventHandler? AddLinkRequested;
    public event EventHandler? PlannerRequested;
    public event EventHandler<string>? ThemeRequested;

    public DashboardView()
    {
        InitializeComponent();
        DashboardQuickLinks.ItemsSource = _links;
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        Loaded += (_, _) => { UpdateClock(); _clockTimer.Start(); };
        Unloaded += (_, _) => _clockTimer.Stop();
    }

    public void SetQuickLinks(IEnumerable<QuickLink> links)
    {
        _links.Clear();
        foreach (var link in links.OrderBy(x => x.SortOrder).Take(7)) _links.Add(link);
        QuickLinkCountText.Text = $"{_links.Count} از ۷";
        AddLinkButton.IsEnabled = _links.Count < 7;
        AddLinkButton.ToolTip = _links.Count < 7 ? "افزودن میانبر جدید" : "حداکثر ۷ لینک سریع ثبت شده است";
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var date = PersianDateService.FormatDate(now);
        DashboardDateText.Text = date;
        PlannerDateText.Text = date;
        DashboardTimeText.Text = PersianDateService.FormatTime(now);
    }

    private void QuickLinkCard_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is QuickLink link) QuickLinkRequested?.Invoke(this, link);
    }

    private void EditLink_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is QuickLink link) EditLinkRequested?.Invoke(this, link);
        e.Handled = true;
    }

    private void DeleteLink_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is QuickLink link) DeleteLinkRequested?.Invoke(this, link);
        e.Handled = true;
    }

    private void AddLink_Click(object sender, RoutedEventArgs e) => AddLinkRequested?.Invoke(this, EventArgs.Empty);
    private void Planner_Click(object sender, RoutedEventArgs e) => PlannerRequested?.Invoke(this, EventArgs.Empty);
    private void LightTheme_Click(object sender, RoutedEventArgs e) => ThemeRequested?.Invoke(this, "Light");
    private void DarkTheme_Click(object sender, RoutedEventArgs e) => ThemeRequested?.Invoke(this, "Dark");
}