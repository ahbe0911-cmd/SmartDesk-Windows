using System.Diagnostics;

namespace SmartDesk.Services;

public static class BrowserUriService
{
    public static bool TryNormalize(string? input, out Uri uri)
    {
        uri = null!;
        var value = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) && IsHttp(absolute))
        {
            uri = absolute;
            return true;
        }

        if (!value.Contains(' ') && value.Contains('.') &&
            Uri.TryCreate("https://" + value, UriKind.Absolute, out absolute) && IsHttp(absolute))
        {
            uri = absolute;
            return true;
        }

        uri = new Uri("https://www.google.com/search?q=" + Uri.EscapeDataString(value));
        return true;
    }

    public static bool IsAllowedInBrowser(Uri uri) =>
        IsHttp(uri) || string.Equals(uri.Scheme, "about", StringComparison.OrdinalIgnoreCase);

    public static bool IsExternalScheme(Uri uri) =>
        uri.Scheme is "mailto" or "tel";

    public static void OpenExternal(Uri uri)
    {
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private static bool IsHttp(Uri uri) =>
        uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
