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
        [DayOfWeek.Saturday] = "شنبه",
        [DayOfWeek.Sunday] = "یکشنبه",
        [DayOfWeek.Monday] = "دوشنبه",
        [DayOfWeek.Tuesday] = "سه‌شنبه",
        [DayOfWeek.Wednesday] = "چهارشنبه",
        [DayOfWeek.Thursday] = "پنج‌شنبه",
        [DayOfWeek.Friday] = "جمعه"
    };

    public static string FormatDate(DateTime value)
    {
        var result = $"{Weekdays[value.DayOfWeek]}  {Calendar.GetDayOfMonth(value)} {Months[Calendar.GetMonth(value) - 1]} {Calendar.GetYear(value)}";
        return ToPersianDigits(result);
    }

    public static string FormatTime(DateTime value) => ToPersianDigits(value.ToString("HH:mm"));

    public static string ToPersianDigits(string input)
    {
        const string english = "0123456789";
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        var chars = input.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            var digit = english.IndexOf(chars[index]);
            if (digit >= 0)
            {
                chars[index] = persian[digit];
            }
        }

        return new string(chars);
    }
}
