using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace SmartDesk.Services;

public static class ThemeService
{
    public static bool IsDark { get; private set; } = true;

    public static void Apply(string? theme)
    {
        IsDark = string.Equals(theme, "System", StringComparison.OrdinalIgnoreCase)
            ? IsSystemDark()
            : !string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase);

        var palette = IsDark
            ? new Dictionary<string, string>
            {
                ["WindowBackgroundBrush"] = "#0B1120",
                ["PanelBackgroundBrush"] = "#111827",
                ["ElevatedBackgroundBrush"] = "#172033",
                ["InputBackgroundBrush"] = "#0F172A",
                ["HoverBackgroundBrush"] = "#243047",
                ["SelectedBackgroundBrush"] = "#163D44",
                ["BorderBrush"] = "#2A3850",
                ["TextPrimaryBrush"] = "#F8FAFC",
                ["TextSecondaryBrush"] = "#A9B5C8",
                ["TextMutedBrush"] = "#728096",
                ["AccentBrush"] = "#2DD4BF",
                ["AccentHoverBrush"] = "#5EEAD4",
                ["AccentTextBrush"] = "#062C2B",
                ["DangerBrush"] = "#FB7185",
                ["SuccessBrush"] = "#34D399"
            }
            : new Dictionary<string, string>
            {
                ["WindowBackgroundBrush"] = "#F1F5F9",
                ["PanelBackgroundBrush"] = "#FFFFFF",
                ["ElevatedBackgroundBrush"] = "#F8FAFC",
                ["InputBackgroundBrush"] = "#FFFFFF",
                ["HoverBackgroundBrush"] = "#E2E8F0",
                ["SelectedBackgroundBrush"] = "#CCFBF1",
                ["BorderBrush"] = "#CBD5E1",
                ["TextPrimaryBrush"] = "#0F172A",
                ["TextSecondaryBrush"] = "#475569",
                ["TextMutedBrush"] = "#64748B",
                ["AccentBrush"] = "#0F9F91",
                ["AccentHoverBrush"] = "#0D9488",
                ["AccentTextBrush"] = "#FFFFFF",
                ["DangerBrush"] = "#E11D48",
                ["SuccessBrush"] = "#059669"
            };

        foreach (var (key, hex) in palette)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            Application.Current.Resources[key] = new SolidColorBrush(color);
        }
    }

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return true;
        }
    }
}
