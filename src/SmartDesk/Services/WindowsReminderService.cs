using Microsoft.Toolkit.Uwp.Notifications;
using SmartDesk.Models;

namespace SmartDesk.Services;

public static class WindowsReminderService
{
    private const string ReminderGroup = "SmartDeskReminders";

    public static void Schedule(ReminderItem reminder)
    {
        Cancel(reminder.Id);
        if (!reminder.IsEnabled || reminder.DueAt <= DateTime.Now) return;

        new ToastContentBuilder()
            .AddText("یادآور میزکار هوشمند")
            .AddText(reminder.Title)
            .AddText(string.IsNullOrWhiteSpace(reminder.Note) ? PersianDateService.FormatDateTime(reminder.DueAt) : reminder.Note)
            .SetToastScenario(ToastScenario.Reminder)
            .Schedule(new DateTimeOffset(reminder.DueAt), toast =>
            {
                toast.Tag = reminder.Id.ToString("N");
                toast.Group = ReminderGroup;
            });
    }

    public static void Cancel(Guid reminderId)
    {
        try
        {
            var notifier = ToastNotificationManagerCompat.CreateToastNotifier();
            var tag = reminderId.ToString("N");
            foreach (var toast in notifier.GetScheduledToastNotifications().Where(x => x.Tag == tag && x.Group == ReminderGroup).ToList())
                notifier.RemoveFromSchedule(toast);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "لغو یادآور ویندوز");
        }
    }

    public static void RescheduleAll(IEnumerable<ReminderItem> reminders)
    {
        foreach (var reminder in reminders.Where(x => x.IsEnabled && x.DueAt > DateTime.Now))
        {
            try { Schedule(reminder); }
            catch (Exception ex) { AppLogger.Error(ex, $"زمان‌بندی یادآور {reminder.Title}"); }
        }
    }
}
