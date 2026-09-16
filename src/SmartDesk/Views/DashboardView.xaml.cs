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
    public event EventHandler? QuickNoteRequested;
    public event EventHandler? CalendarRequested;
    public event EventHandler? TodayTasksRequested;
    public event EventHandler? ToolsRequested;

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
        foreach (var link in links.OrderBy(x => x.SortOrder).Take(7))
        {
            _links.Add(link);
        }
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        DashboardDateText.Text = PersianDateService.FormatDate(now);
        DashboardTimeText.Text = PersianDateService.FormatTime(now);
    }

    private void QuickLinkCard_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is QuickLink link)
            QuickLinkRequested?.Invoke(this, link);
    }

    private void QuickNote_Click(object sender, RoutedEventArgs e) => QuickNoteRequested?.Invoke(this, EventArgs.Empty);
    private void Calendar_Click(object sender, RoutedEventArgs e) => CalendarRequested?.Invoke(this, EventArgs.Empty);
    private void TodayTasks_Click(object sender, RoutedEventArgs e) => TodayTasksRequested?.Invoke(this, EventArgs.Empty);
    private void Tools_Click(object sender, RoutedEventArgs e) => ToolsRequested?.Invoke(this, EventArgs.Empty);
}