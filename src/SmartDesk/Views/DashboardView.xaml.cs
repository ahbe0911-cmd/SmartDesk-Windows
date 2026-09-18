using SmartDesk.Models;
using SmartDesk.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmartDesk.Views;

public partial class DashboardView : UserControl
{
    private readonly ObservableCollection<QuickLink> _links = [];
    private readonly DispatcherTimer _clockTimer;
    private readonly PersianCalendar _pc = new();
    private DateTime _calendarDate = DateTime.Today;
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
        SidebarQuickLinks.ItemsSource = _links;
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        Loaded += (_, _) => { UpdateClock(); RenderCalendar(); _clockTimer.Start(); };
        Unloaded += (_, _) => _clockTimer.Stop();
    }

    public void SetQuickLinks(IEnumerable<QuickLink> links)
    {
        _links.Clear();
        foreach (var link in links.OrderBy(x => x.SortOrder).Take(7)) _links.Add(link);
        QuickLinkCountText.Text = $"{_links.Count} از ۷ • دسترسی سریع";
        AddLinkButton.IsEnabled = _links.Count < 7;
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        DashboardDateText.Text = PersianDateService.FormatDate(now);
        DashboardTimeText.Text = PersianDateService.FormatTime(now);
    }

    private void RenderCalendar()
    {
        CalendarDaysGrid.Children.Clear();
        var y = _pc.GetYear(_calendarDate); var m = _pc.GetMonth(_calendarDate);
        var names = new[]{"فروردین","اردیبهشت","خرداد","تیر","مرداد","شهریور","مهر","آبان","آذر","دی","بهمن","اسفند"};
        CalendarTitleText.Text = $"{names[m-1]} {ToFa(y.ToString())}";
        var first = _pc.ToDateTime(y,m,1,0,0,0,0);
        var offset = ((int)first.DayOfWeek + 1) % 7;
        for (var i=0;i<offset;i++) CalendarDaysGrid.Children.Add(new TextBlock());
        var days = _pc.GetDaysInMonth(y,m);
        for (var d=1;d<=days;d++)
        {
            var date = _pc.ToDateTime(y,m,d,0,0,0,0);
            var today = date.Date == DateTime.Today;
            var friday = date.DayOfWeek == DayOfWeek.Friday;
            var b = new Border { Width=32, Height=32, CornerRadius=new CornerRadius(16), Margin=new Thickness(2), Background=today ? new SolidColorBrush(Color.FromRgb(54,169,137)) : Brushes.Transparent };
            b.Child = new TextBlock { Text=ToFa(d.ToString()), HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center, Foreground=today ? Brushes.White : friday ? new SolidColorBrush(Color.FromRgb(210,92,92)) : new SolidColorBrush(Color.FromRgb(73,87,92)), FontWeight=today ? FontWeights.Bold : FontWeights.Normal };
            CalendarDaysGrid.Children.Add(b);
        }
    }

    private static string ToFa(string s) { const string e="0123456789", f="۰۱۲۳۴۵۶۷۸۹"; foreach(var i in Enumerable.Range(0,10)) s=s.Replace(e[i],f[i]); return s; }
    private void PreviousMonth_Click(object s, RoutedEventArgs e) { _calendarDate=_pc.AddMonths(_calendarDate,-1); RenderCalendar(); }
    private void NextMonth_Click(object s, RoutedEventArgs e) { _calendarDate=_pc.AddMonths(_calendarDate,1); RenderCalendar(); }
    private void Today_Click(object s, RoutedEventArgs e) { _calendarDate=DateTime.Today; RenderCalendar(); }
    private void DashboardSearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (DashboardSearchBox is null) return; var q=DashboardSearchBox.Text.Trim(); foreach(var item in DashboardQuickLinks.Items) if(DashboardQuickLinks.ItemContainerGenerator.ContainerFromItem(item) is ContentPresenter cp) cp.Visibility = item is QuickLink l && (string.IsNullOrWhiteSpace(q) || l.Title.Contains(q,StringComparison.CurrentCultureIgnoreCase) || l.Category.Contains(q,StringComparison.CurrentCultureIgnoreCase)) ? Visibility.Visible : Visibility.Collapsed; }
    private void QuickLinkCard_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.Tag is QuickLink link) QuickLinkRequested?.Invoke(this, link); }
    private void EditLink_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.Tag is QuickLink link) EditLinkRequested?.Invoke(this, link); e.Handled=true; }
    private void DeleteLink_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.Tag is QuickLink link) DeleteLinkRequested?.Invoke(this, link); e.Handled=true; }
    private void AddLink_Click(object sender, RoutedEventArgs e) => AddLinkRequested?.Invoke(this, EventArgs.Empty);
    private void Planner_Click(object sender, RoutedEventArgs e) => PlannerRequested?.Invoke(this, EventArgs.Empty);
    private void LightTheme_Click(object sender, RoutedEventArgs e) => ThemeRequested?.Invoke(this, "Light");
    private void DarkTheme_Click(object sender, RoutedEventArgs e) => ThemeRequested?.Invoke(this, "Dark");
}