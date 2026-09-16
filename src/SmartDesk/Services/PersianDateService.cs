using System.Globalization;

namespace SmartDesk.Services;

public static class PersianDateService
{
    private static readonly PersianCalendar Calendar = new();
    private static readonly string[] Months =
    [
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    ];

    private static readonly Dictionary<DayOfWeek, string> Weekdays = new()
    {
        [DayOfWeek.Saturday] = "شنبه", [DayOfWeek.Sunday] = "یکشنبه",
        [DayOfWeek.Monday] = "دوشنبه", [DayOfWeek.Tuesday] = "سه‌شنبه",
        [DayOfWeek.Wednesday] = "چهارشنبه", [DayOfWeek.Thursday] = "پنج‌شنبه",
        [DayOfWeek.Friday] = "جمعه"
    };

    public static string FormatDate(DateTime value)
    {
        var result = $"{Weekdays[value.DayOfWeek]}  {Calendar.GetDayOfMonth(value)} {Months[Calendar.GetMonth(value) - 1]} {Calendar.GetYear(value)}";
        return ToPersianDigits(result);
    }

    public static string FormatDateTime(DateTime value) => $"{FormatDate(value)}، ساعت {FormatTime(value)}";
    public static string FormatTime(DateTime value) => ToPersianDigits(value.ToString("HH:mm"));
    public static int GetYear(DateTime value) => Calendar.GetYear(value);
    public static int GetMonth(DateTime value) => Calendar.GetMonth(value);
    public static int GetDay(DateTime value) => Calendar.GetDayOfMonth(value);
    public static int GetDaysInMonth(int year, int month) => Calendar.GetDaysInMonth(year, month);
    public static string GetMonthName(int month) => month is >= 1 and <= 12 ? Months[month - 1] : string.Empty;
    public static string GetWeekdayName(DayOfWeek day) => Weekdays[day];

    public static DateTime FromPersianDate(int year, int month, int day, int hour = 0, int minute = 0)
        => Calendar.ToDateTime(year, month, day, hour, minute, 0, 0);

    public static string ToPersianDigits(string input)
    {
        const string english = "0123456789";
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        var chars = input.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            var digit = english.IndexOf(chars[index]);
            if (digit >= 0) chars[index] = persian[digit];
        }
        return new string(chars);
    }
}
