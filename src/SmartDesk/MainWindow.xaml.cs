using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using SmartDesk.Models;
using SmartDesk.Services;
using SmartDesk.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace SmartDesk;

public partial class MainWindow : Window
{
    private readonly AppDataService _dataService;
    private readonly ObservableCollection<QuickLink> _quickLinks = [];
    private readonly DispatcherTimer _clockTimer;
    private ICollectionView? _quickLinksView;
    private AppData _data;
    private CoreWebView2Environment? _browserEnvironment;
    private bool _isClosing;

    public MainWindow(AppData data, AppDataService dataService)
    {
        InitializeComponent();
        _data = data;
        _dataService = dataService;

        Width = Math.Max(MinWidth, data.Settings.WindowWidth);
        Height = Math.Max(MinHeight, data.Settings.WindowHeight);
        if (data.Settings.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }

        LoadQuickLinks();
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        UpdateClock();
        _clockTimer.Start();
    }

    private BrowserTabView? CurrentBrowser =>
        (BrowserTabs.SelectedItem as TabItem)?.Tag as BrowserTabView;

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.BrowserDataDirectory);
            _browserEnvironment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: AppPaths.BrowserDataDirectory,
                options: null);

            // V8 dashboard-first: initialize the persistent WebView2 profile only.
            // Sites are created on demand inside the centered modal, avoiding slow hidden startup tabs.
            StatusText.Text = "مرورگر داخلی آماده است.";
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            AppLogger.Error(ex, "WebView2 Runtime یافت نشد");
            StatusText.Text = "موتور مرورگر WebView2 نصب نیست.";
            MessageBox.Show(
                "موتور Microsoft Edge WebView2 روی ویندوز پیدا نشد. فایل نصب SmartDesk آن را به‌صورت خودکار نصب می‌کند؛ اگر نسخه پرتابل را اجرا کرده‌اید، WebView2 Runtime را نصب کنید.",
                "نیازمندی مرورگر",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "راه‌اندازی مرورگر داخلی");
            StatusText.Text = "راه‌اندازی مرورگر با خطا روبه‌رو شد.";
            MessageBox.Show(
                "مرورگر داخلی راه‌اندازی نشد. گزارش خطا ذخیره شده است.",
                "خطای راه‌اندازی",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task<BrowserTabView?> CreateTabAsync(string? address, bool activate = true)
    {
        if (_browserEnvironment is null)
        {
            return null;
        }

        var browser = new BrowserTabView();
        var tab = new TabItem { Tag = browser, Content = browser };
        tab.Header = CreateTabHeader(tab, "زبانه جدید");

        browser.TitleChanged += Browser_TitleChanged;
        browser.NavigationStateChanged += Browser_NavigationStateChanged;
        browser.StatusMessage += Browser_StatusMessage;
        browser.NewWindowRequested += Browser_NewWindowRequested;

        BrowserTabs.Items.Add(tab);
        if (activate)
        {
            BrowserTabs.SelectedItem = tab;
        }

        try
        {
            await browser.InitializeAsync(_browserEnvironment, address);
            return browser;
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "ساخت زبانه مرورگر");
            BrowserTabs.Items.Remove(tab);
            browser.DisposeBrowser();
            StatusText.Text = "ساخت زبانه جدید انجام نشد.";
            return null;
        }
    }

    private object CreateTabHeader(TabItem tab, string title)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.LeftToRight
        };

        var titleText = new TextBlock
        {
            Text = ShortenTitle(title),
            MaxWidth = 175,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft
        };

        var closeButton = new Button
        {
            Content = "×",
            Width = 25,
            Height = 25,
            Padding = new Thickness(0),
            Margin = new Thickness(8, 0, 0, 0),
            Tag = tab,
            ToolTip = "بستن زبانه"
        };
        closeButton.SetResourceReference(StyleProperty, "TinyActionButtonStyle");
        closeButton.Click += CloseTabButton_Click;

        panel.Children.Add(titleText);
        panel.Children.Add(closeButton);
        return panel;
    }

    private async void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        try
        {
            var newBrowser = await CreateTabAsync(null, activate: true);
            if (newBrowser?.CoreWebView is not null)
            {
                e.NewWindow = newBrowser.CoreWebView;
            }
            else
            {
                e.Handled = true;
                if (!string.IsNullOrWhiteSpace(e.Uri))
                {
                    await CreateTabAsync(e.Uri, activate: true);
                }
            }
        }
        catch (Exception ex)
        {
            e.Handled = true;
            AppLogger.Error(ex, "بازکردن پنجره جدید سایت");
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void Browser_TitleChanged(BrowserTabView browser, string title)
    {
        var tab = BrowserTabs.Items.OfType<TabItem>().FirstOrDefault(item => ReferenceEquals(item.Tag, browser));
        if (tab?.Header is StackPanel panel && panel.Children.OfType<TextBlock>().FirstOrDefault() is { } titleText)
        {
            titleText.Text = ShortenTitle(title);
            titleText.ToolTip = title;
        }

        if (ReferenceEquals(CurrentBrowser, browser))
        {
            Title = $"{title} — میزکار هوشمند";
        }
    }

    private void Browser_NavigationStateChanged(BrowserTabView browser)
    {
        if (ReferenceEquals(CurrentBrowser, browser))
        {
            UpdateNavigationControls();
        }
    }

    private void Browser_StatusMessage(BrowserTabView browser, string message)
    {
        if (ReferenceEquals(CurrentBrowser, browser))
        {
            StatusText.Text = message;
        }
    }

    private void UpdateNavigationControls()
    {
        var browser = CurrentBrowser;
        BackButton.IsEnabled = browser?.CanGoBack == true;
        ForwardButton.IsEnabled = browser?.CanGoForward == true;
        ReloadButton.IsEnabled = browser is not null;
        ExternalButton.IsEnabled = browser is not null;

        if (browser is null)
        {
            AddressBox.Text = string.Empty;
            SecurityText.Text = "●";
            SecurityText.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
            return;
        }

        AddressBox.Text = browser.CurrentUrl;
        if (Uri.TryCreate(browser.CurrentUrl, UriKind.Absolute, out var uri) &&
            uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            SecurityText.Text = "●";
            SecurityText.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
            SecurityText.ToolTip = "اتصال HTTPS";
        }
        else
        {
            SecurityText.Text = "●";
            SecurityText.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
            SecurityText.ToolTip = "اتصال معمولی یا صفحه داخلی";
        }
    }

    private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateNavigationControls();
        if (CurrentBrowser is { } browser)
        {
            Title = $"{browser.PageTitle} — میزکار هوشمند";
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.GoBack();
    private void ForwardButton_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.GoForward();
    private void ReloadButton_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.Reload();
    private void HomeButton_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.Navigate(_data.Settings.HomeUrl);
    private void GoButton_Click(object sender, RoutedEventArgs e) => NavigateFromAddressBar();

    private async void NewTabButton_Click(object sender, RoutedEventArgs e) =>
        await CreateTabAsync(_data.Settings.HomeUrl, activate: true);

    private void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateFromAddressBar();
            e.Handled = true;
        }
    }

    private void AddressBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => AddressBox.SelectAll();

    private void NavigateFromAddressBar()
    {
        CurrentBrowser?.Navigate(AddressBox.Text);
        Keyboard.ClearFocus();
    }

    private void ExternalButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CurrentBrowser is { } browser && Uri.TryCreate(browser.CurrentUrl, UriKind.Absolute, out var uri) &&
                uri.Scheme is "http" or "https")
            {
                BrowserUriService.OpenExternal(uri);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "بازکردن صفحه در مرورگر پیش‌فرض");
            StatusText.Text = "مرورگر پیش‌فرض ویندوز باز نشد.";
        }
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is TabItem tab)
        {
            CloseTab(tab);
            e.Handled = true;
        }
    }

    private async void CloseTab(TabItem tab)
    {
        if (tab.Tag is BrowserTabView browser)
        {
            browser.DisposeBrowser();
        }

        BrowserTabs.Items.Remove(tab);
        if (!_isClosing && BrowserTabs.Items.Count == 0)
        {
            await CreateTabAsync(_data.Settings.HomeUrl, activate: true);
        }
    }

    private async void QuickLink_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not QuickLink link)
        {
            return;
        }

        if (_data.Settings.OpenQuickLinksInNewTab || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            await CreateTabAsync(link.Url, activate: true);
        }
        else if (CurrentBrowser is { } browser)
        {
            browser.Navigate(link.Url);
        }
        else
        {
            await CreateTabAsync(link.Url, activate: true);
        }
    }

    private void AddQuickLink_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new QuickLinkEditorWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        dialog.Result.SortOrder = _quickLinks.Count == 0 ? 0 : _quickLinks.Max(item => item.SortOrder) + 1;
        _quickLinks.Add(dialog.Result);
        SaveQuickLinks();
        StatusText.Text = "میانبر جدید ذخیره شد.";
    }

    private void EditQuickLink_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not QuickLink link)
        {
            return;
        }

        var dialog = new QuickLinkEditorWindow(link) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        var index = _quickLinks.IndexOf(link);
        if (index >= 0)
        {
            _quickLinks[index] = dialog.Result;
            SaveQuickLinks();
            StatusText.Text = "تغییرات میانبر ذخیره شد.";
        }
    }

    private void DeleteQuickLink_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not QuickLink link)
        {
            return;
        }

        var answer = MessageBox.Show(
            $"میانبر «{link.Title}» حذف شود؟",
            "حذف میانبر",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        _quickLinks.Remove(link);
        SaveQuickLinks();
        StatusText.Text = "میانبر حذف شد.";
    }

    private void QuickLinkSearchBox_TextChanged(object sender, TextChangedEventArgs e) => _quickLinksView?.Refresh();

    private void LoadQuickLinks()
    {
        _quickLinks.Clear();
        foreach (var link in _data.QuickLinks.OrderBy(item => item.SortOrder))
        {
            _quickLinks.Add(link);
        }

        _quickLinksView = CollectionViewSource.GetDefaultView(_quickLinks);
        _quickLinksView.Filter = item =>
        {
            if (item is not QuickLink link)
            {
                return false;
            }

            var query = QuickLinkSearchBox?.Text?.Trim();
            return string.IsNullOrWhiteSpace(query) ||
                   link.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                   link.Category.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                   link.Url.Contains(query, StringComparison.OrdinalIgnoreCase);
        };
        QuickLinksList.ItemsSource = _quickLinksView;
    }

    private void SaveQuickLinks()
    {
        _data.QuickLinks = _quickLinks.OrderBy(item => item.SortOrder).ToList();
        _dataService.Save(_data);
        _quickLinksView?.Refresh();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_data.Settings) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        _data.Settings = dialog.Result;
        ThemeService.Apply(_data.Settings.Theme);
        foreach (var browser in BrowserTabs.Items.OfType<TabItem>().Select(item => item.Tag).OfType<BrowserTabView>())
        {
            browser.ApplyTheme();
        }

        _dataService.Save(_data);
        StatusText.Text = "تنظیمات ذخیره شد.";
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "ذخیره نسخه پشتیبان SmartDesk",
            Filter = "فایل پشتیبان SmartDesk (*.json)|*.json",
            FileName = $"SmartDesk-Backup-{DateTime.Now:yyyy-MM-dd}.json",
            AddExtension = true,
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _dataService.Export(_data, dialog.FileName);
            StatusText.Text = "نسخه پشتیبان ذخیره شد.";
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "خروجی نسخه پشتیبان");
            MessageBox.Show("ذخیره نسخه پشتیبان انجام نشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "بازیابی نسخه پشتیبان SmartDesk",
            Filter = "فایل پشتیبان SmartDesk (*.json)|*.json",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var answer = MessageBox.Show(
            "میانبرها و تنظیمات فعلی با اطلاعات فایل پشتیبان جایگزین شوند؟",
            "بازیابی پشتیبان",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _data = _dataService.Import(dialog.FileName);
            ThemeService.Apply(_data.Settings.Theme);
            LoadQuickLinks();
            StatusText.Text = "نسخه پشتیبان با موفقیت بازیابی شد.";
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "بازیابی نسخه پشتیبان");
            MessageBox.Show("فایل انتخاب‌شده معتبر نیست یا خوانده نشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var control = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

        if (control && e.Key == Key.L)
        {
            AddressBox.Focus();
            AddressBox.SelectAll();
            e.Handled = true;
        }
        else if (control && e.Key == Key.T)
        {
            await CreateTabAsync(_data.Settings.HomeUrl, activate: true);
            e.Handled = true;
        }
        else if (control && e.Key == Key.W && BrowserTabs.SelectedItem is TabItem tab)
        {
            CloseTab(tab);
            e.Handled = true;
        }
        else if ((control && e.Key == Key.R) || e.Key == Key.F5)
        {
            CurrentBrowser?.Reload();
            e.Handled = true;
        }
        else if (alt && e.Key == Key.Left)
        {
            CurrentBrowser?.GoBack();
            e.Handled = true;
        }
        else if (alt && e.Key == Key.Right)
        {
            CurrentBrowser?.GoForward();
            e.Handled = true;
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _isClosing = true;
        _clockTimer.Stop();

        try
        {
            var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            _data.Settings.WindowWidth = Math.Max(MinWidth, bounds.Width);
            _data.Settings.WindowHeight = Math.Max(MinHeight, bounds.Height);
            _data.Settings.WindowMaximized = WindowState == WindowState.Maximized;
            _data.Settings.LastOpenTabs = BrowserTabs.Items
                .OfType<TabItem>()
                .Select(item => item.Tag)
                .OfType<BrowserTabView>()
                .Select(browser => browser.CurrentUrl)
                .Where(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
            _dataService.Save(_data);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "ذخیره وضعیت هنگام خروج");
        }

        foreach (var browser in BrowserTabs.Items.OfType<TabItem>().Select(item => item.Tag).OfType<BrowserTabView>().ToList())
        {
            browser.DisposeBrowser();
        }
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        DateText.Text = PersianDateService.FormatDate(now);
        TimeText.Text = PersianDateService.FormatTime(now);
    }

    private static string ShortenTitle(string? title)
    {
        var value = string.IsNullOrWhiteSpace(title) ? "زبانه جدید" : title.Trim();
        return value.Length <= 32 ? value : value[..31] + "…";
    }
}
