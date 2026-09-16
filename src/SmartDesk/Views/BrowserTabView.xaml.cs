using Microsoft.Web.WebView2.Core;
using SmartDesk.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SmartDesk.Views;

public partial class BrowserTabView : UserControl
{
    private bool _configured;
    private string? _lastRequestedUrl;

    public BrowserTabView()
    {
        InitializeComponent();
    }

    public string PageTitle { get; private set; } = "زبانه جدید";
    public string CurrentUrl => Browser.Source?.AbsoluteUri ?? _lastRequestedUrl ?? "about:blank";
    public bool CanGoBack => Browser.CoreWebView2?.CanGoBack == true;
    public bool CanGoForward => Browser.CoreWebView2?.CanGoForward == true;
    public CoreWebView2? CoreWebView => Browser.CoreWebView2;

    public event Action<BrowserTabView, string>? TitleChanged;
    public event Action<BrowserTabView>? NavigationStateChanged;
    public event Action<BrowserTabView, string>? StatusMessage;
    public event EventHandler<CoreWebView2NewWindowRequestedEventArgs>? NewWindowRequested;

    public async Task InitializeAsync(CoreWebView2Environment environment, string? initialUrl = null)
    {
        await Browser.EnsureCoreWebView2Async(environment);

        if (!_configured)
        {
            ConfigureBrowser();
            _configured = true;
        }

        ApplyTheme();

        if (!string.IsNullOrWhiteSpace(initialUrl))
        {
            Navigate(initialUrl);
        }
    }

    public void Navigate(string address)
    {
        if (!BrowserUriService.TryNormalize(address, out var uri))
        {
            StatusMessage?.Invoke(this, "نشانی واردشده معتبر نیست.");
            return;
        }

        _lastRequestedUrl = uri.AbsoluteUri;
        ErrorPanel.Visibility = Visibility.Collapsed;
        Browser.Visibility = Visibility.Visible;

        if (Browser.CoreWebView2 is null)
        {
            return;
        }

        Browser.CoreWebView2.Navigate(uri.AbsoluteUri);
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            Browser.CoreWebView2.GoBack();
        }
    }

    public void GoForward()
    {
        if (CanGoForward)
        {
            Browser.CoreWebView2.GoForward();
        }
    }

    public void Reload()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        Browser.Visibility = Visibility.Visible;
        Browser.CoreWebView2?.Reload();
    }

    public void Stop() => Browser.CoreWebView2?.Stop();

    public void ApplyTheme()
    {
        if (Browser.CoreWebView2?.Profile is null)
        {
            return;
        }

        Browser.CoreWebView2.Profile.PreferredColorScheme = ThemeService.IsDark
            ? CoreWebView2PreferredColorScheme.Dark
            : CoreWebView2PreferredColorScheme.Light;
    }

    public void DisposeBrowser()
    {
        Browser.Dispose();
    }

    private void ConfigureBrowser()
    {
        var core = Browser.CoreWebView2;
        if (core is null)
        {
            return;
        }

        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.AreBrowserAcceleratorKeysEnabled = true;
        core.Settings.IsBuiltInErrorPageEnabled = true;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = true;
        core.Settings.IsPasswordAutosaveEnabled = true;
        core.Settings.IsGeneralAutofillEnabled = true;
        core.Settings.AreDevToolsEnabled = false;

        core.NavigationStarting += Core_NavigationStarting;
        core.SourceChanged += (_, _) => NavigationStateChanged?.Invoke(this);
        core.HistoryChanged += (_, _) => NavigationStateChanged?.Invoke(this);
        core.DocumentTitleChanged += Core_DocumentTitleChanged;
        core.ContentLoading += (_, _) => LoadingProgress.Visibility = Visibility.Visible;
        core.NavigationCompleted += Core_NavigationCompleted;
        core.NewWindowRequested += Core_NewWindowRequested;
        core.DownloadStarting += Core_DownloadStarting;
        core.ProcessFailed += Core_ProcessFailed;
    }

    private void Core_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        LoadingProgress.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        Browser.Visibility = Visibility.Visible;

        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
        {
            e.Cancel = true;
            StatusMessage?.Invoke(this, "نشانی نامعتبر مسدود شد.");
            return;
        }

        if (BrowserUriService.IsExternalScheme(uri))
        {
            e.Cancel = true;
            try
            {
                BrowserUriService.OpenExternal(uri);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "بازکردن پیوند خارجی");
                StatusMessage?.Invoke(this, "امکان بازکردن این پیوند وجود ندارد.");
            }

            return;
        }

        if (!BrowserUriService.IsAllowedInBrowser(uri))
        {
            e.Cancel = true;
            StatusMessage?.Invoke(this, $"پیوند با پروتکل {uri.Scheme} برای حفظ امنیت مسدود شد.");
        }
    }

    private void Core_DocumentTitleChanged(object? sender, object e)
    {
        PageTitle = string.IsNullOrWhiteSpace(Browser.CoreWebView2?.DocumentTitle)
            ? "زبانه جدید"
            : Browser.CoreWebView2.DocumentTitle;
        TitleChanged?.Invoke(this, PageTitle);
    }

    private void Core_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        LoadingProgress.Visibility = Visibility.Collapsed;
        NavigationStateChanged?.Invoke(this);

        if (e.IsSuccess)
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
            Browser.Visibility = Visibility.Visible;
            StatusMessage?.Invoke(this, "صفحه آماده است.");
            return;
        }

        Browser.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ErrorMessageText.Text = $"بارگذاری صفحه انجام نشد. کد خطا: {e.WebErrorStatus}";
        StatusMessage?.Invoke(this, "خطا در بارگذاری صفحه");
    }

    private void Core_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e) =>
        NewWindowRequested?.Invoke(this, e);

    private void Core_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        var fileName = Path.GetFileName(e.ResultFilePath);
        StatusMessage?.Invoke(this, string.IsNullOrWhiteSpace(fileName)
            ? "دانلود آغاز شد."
            : $"دانلود «{fileName}» آغاز شد.");
    }

    private void Core_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        AppLogger.Error(new InvalidOperationException($"WebView2 process failed: {e.ProcessFailedKind}"), "خرابی پردازش مرورگر");
        StatusMessage?.Invoke(this, "پردازش مرورگر متوقف شد؛ صفحه را تازه‌سازی کنید.");
    }

    private void Retry_Click(object sender, RoutedEventArgs e) => Reload();
}
