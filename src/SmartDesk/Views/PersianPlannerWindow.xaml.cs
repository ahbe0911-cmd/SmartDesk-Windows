using SmartDesk.Models;
using SmartDesk.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartDesk.Views;

public partial class PersianPlannerWindow : Window
{
    private readonly AppData _data;
    private readonly AppDataService _dataService;
    private readonly ObservableCollection<NoteItem> _notes;
    private readonly ObservableCollection<ReminderRow> _reminders = [];
    private int _year;
    private int _month;
    private DateTime _selectedDate;

    public PersianPlannerWindow(AppData data, AppDataService dataService)
    {
        InitializeComponent();
        _data = data;
        _dataService = dataService;
        _notes = new ObservableCollection<NoteItem>(_data.Notes.OrderByDescending(x => x.UpdatedAt));
        NotesList.ItemsSource = _notes;
        RemindersList.ItemsSource = _reminders;

        var today = DateTime.Today;
        _year = PersianDateService.GetYear(today);
        _month = PersianDateService.GetMonth(today);
        _selectedDate = today;
        SetDateInputs(today);
        RefreshCalendar();
        RefreshReminders();
    }

    private void RefreshCalendar()
    {
        CalendarGrid.Children.Clear();
        MonthTitle.Text = $"{PersianDateService.GetMonthName(_month)} {PersianDateService.ToPersianDigits(_year.ToString())}";
        var first = PersianDateService.FromPersianDate(_year, _month, 1);
        var offset = ((int)first.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        for (var i = 0; i < offset; i++) CalendarGrid.Children.Add(new Border());

        var days = PersianDateService.GetDaysInMonth(_year, _month);
        for (var day = 1; day <= days; day++)
        {
            var date = PersianDateService.FromPersianDate(_year, _month, day);
            var button = new Button
            {
                Content = PersianDateService.ToPersianDigits(day.ToString()),
                Tag = date,
                Margin = new Thickness(3),
                MinHeight = 44,
                ToolTip = PersianDateService.FormatDate(date)
            };
            if (date.DayOfWeek == DayOfWeek.Friday) button.Foreground = new SolidColorBrush(Color.FromRgb(224, 108, 117));
            if (date == DateTime.Today) button.FontWeight = FontWeights.Bold;
            if (_data.Reminders.Any(x => x.IsEnabled && x.DueAt.Date == date.Date)) button.Content = $"{PersianDateService.ToPersianDigits(day.ToString())}  •";
            button.Click += CalendarDay_Click;
            CalendarGrid.Children.Add(button);
        }

        SelectedDateText.Text = $"روز انتخاب‌شده: {PersianDateService.FormatDate(_selectedDate)}";
    }

    private void CalendarDay_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not DateTime date) return;
        _selectedDate = date;
        SetDateInputs(date);
        SelectedDateText.Text = $"روز انتخاب‌شده: {PersianDateService.FormatDate(date)}";
    }

    private void SetDateInputs(DateTime date)
    {
        YearBox.Text = PersianDateService.GetYear(date).ToString();
        MonthBox.Text = PersianDateService.GetMonth(date).ToString();
        DayBox.Text = PersianDateService.GetDay(date).ToString();
        if (string.IsNullOrWhiteSpace(HourBox.Text)) HourBox.Text = DateTime.Now.Hour.ToString("00");
        if (string.IsNullOrWhiteSpace(MinuteBox.Text)) MinuteBox.Text = DateTime.Now.Minute.ToString("00");
    }

    private void PreviousMonth_Click(object sender, RoutedEventArgs e)
    {
        _month--;
        if (_month < 1) { _month = 12; _year--; }
        RefreshCalendar();
    }

    private void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        _month++;
        if (_month > 12) { _month = 1; _year++; }
        RefreshCalendar();
    }

    private void AddReminder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ReminderTitleBox.Text))
        {
            MessageBox.Show("عنوان یادآور را وارد کنید.", "یادآور", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var year = int.Parse(YearBox.Text.Trim());
            var month = int.Parse(MonthBox.Text.Trim());
            var day = int.Parse(DayBox.Text.Trim());
            var hour = int.Parse(HourBox.Text.Trim());
            var minute = int.Parse(MinuteBox.Text.Trim());
            var dueAt = PersianDateService.FromPersianDate(year, month, day, hour, minute);
            if (dueAt <= DateTime.Now)
            {
                MessageBox.Show("زمان یادآور باید در آینده باشد.", "یادآور", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reminder = new ReminderItem { Title = ReminderTitleBox.Text.Trim(), Note = ReminderNoteBox.Text.Trim(), DueAt = dueAt, IsEnabled = true };
            WindowsReminderService.Schedule(reminder);
            _data.Reminders.Add(reminder);
            _dataService.Save(_data);
            ReminderTitleBox.Clear();
            ReminderNoteBox.Clear();
            RefreshReminders();
            RefreshCalendar();
            MessageBox.Show("یادآور در سیستم اعلان ویندوز زمان‌بندی شد.", "SmartDesk", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "ثبت یادآور");
            MessageBox.Show("تاریخ/ساعت معتبر نیست یا ویندوز اجازه زمان‌بندی اعلان را نداد.", "خطای یادآور", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshReminders()
    {
        _reminders.Clear();
        foreach (var reminder in _data.Reminders.OrderBy(x => x.DueAt))
            _reminders.Add(new ReminderRow(reminder, PersianDateService.FormatDateTime(reminder.DueAt)));
    }

    private void DeleteReminder_Click(object sender, RoutedEventArgs e)
    {
        if (RemindersList.SelectedItem is not ReminderRow row) return;
        WindowsReminderService.Cancel(row.Item.Id);
        _data.Reminders.RemoveAll(x => x.Id == row.Item.Id);
        _dataService.Save(_data);
        RefreshReminders();
        RefreshCalendar();
    }

    private void RemindersList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NoteTitleBox.Text) && string.IsNullOrWhiteSpace(NoteContentBox.Text)) return;
        if (NotesList.SelectedItem is NoteItem existing)
        {
            existing.Title = string.IsNullOrWhiteSpace(NoteTitleBox.Text) ? "بدون عنوان" : NoteTitleBox.Text.Trim();
            existing.Content = NoteContentBox.Text;
            existing.UpdatedAt = DateTime.Now;
        }
        else
        {
            var note = new NoteItem { Title = string.IsNullOrWhiteSpace(NoteTitleBox.Text) ? "بدون عنوان" : NoteTitleBox.Text.Trim(), Content = NoteContentBox.Text };
            _data.Notes.Add(note);
            _notes.Insert(0, note);
        }
        _dataService.Save(_data);
        NotesList.Items.Refresh();
        NotesList.SelectedItem = null;
        NoteTitleBox.Clear();
        NoteContentBox.Clear();
    }

    private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotesList.SelectedItem is not NoteItem note) return;
        NoteTitleBox.Text = note.Title;
        NoteContentBox.Text = note.Content;
    }

    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (NotesList.SelectedItem is not NoteItem note) return;
        _data.Notes.RemoveAll(x => x.Id == note.Id);
        _notes.Remove(note);
        _dataService.Save(_data);
        NoteTitleBox.Clear();
        NoteContentBox.Clear();
    }

    private sealed record ReminderRow(ReminderItem Item, string DueDisplay)
    {
        public string Title => Item.Title;
    }
}
