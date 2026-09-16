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

            var startupTabs = _data.Settings.RestoreLastSession
                ? _data.Settings.LastOpenTabs
                    .Where(address => BrowserUriService.TryNormalize(address, out _))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList()
                : [];

            if (startupTabs.Count == 0)
            {
                startupTabs.Add(_data.Settings.HomeUrl);
            }

            foreach (var address in startupTabs)
            {
                await CreateTabAsync(address, activate: BrowserTabs.Items.Count == 0);
            }

            if (BrowserTabs.Items.Count > 0)
            {
                BrowserTabs.SelectedIndex = 0;
            }

            EnableDashboardV2();
            StatusText.Text = "داشبورد SmartDesk آماده است.";
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
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var titleBlock = new TextBlock
        {
            Text = Shorten(title, 28),
            MinWidth = 90,
            MaxWidth = 180,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var closeButton = new Button
        {
            Content = "×",
            Tag = tab,
            Style = (Style)FindResource("TabCloseButtonStyle"),
            ToolTip = "بستن زبانه"
        };
        closeButton.Click += CloseTabButton_Click;
        panel.Children.Add(titleBlock);
        panel.Children.Add(closeButton);
        return panel;
    }

    private static string Shorten(string? value, int maxLength)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "زبانه جدید" : value.Trim();
        return text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";
    }

    private void Browser_TitleChanged(object? sender, string title)
    {
        if (sender is not BrowserTabView browser)
        {
            return;
        }

        var tab = BrowserTabs.Items.OfType<TabItem>().FirstOrDefault(item => ReferenceEquals(item.Tag, browser));
        if (tab?.Header is StackPanel panel && panel.Children.OfType<TextBlock>().FirstOrDefault() is { } titleBlock)
        {
            titleBlock.Text = Shorten(title, 28);
        }

        if (ReferenceEquals(CurrentBrowser, browser))
        {
            Title = $"{title} — میزکار هوشمند";
        }
    }

    private void Browser_NavigationStateChanged(object? sender, BrowserNavigationState state)
    {
        if (!ReferenceEquals(sender, CurrentBrowser))
        {
            return;
        }

        AddressBox.Text = state.Address;
        BackButton.IsEnabled = state.CanGoBack;
        ForwardButton.IsEnabled = state.CanGoForward;
        StatusText.Text = state.IsLoading ? "در حال بارگذاری…" : "آماده";
    }

    private void Browser_StatusMessage(object? sender, string message)
    {
        if (ReferenceEquals(sender, CurrentBrowser))
        {
            StatusText.Text = message;
        }
    }

    private async void Browser_NewWindowRequested(object? sender, string address)
    {
        await CreateTabAsync(address, true);
    }

    private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentBrowser is not { } browser)
        {
            BackButton.IsEnabled = false;
            ForwardButton.IsEnabled = false;
            return;
        }

        AddressBox.Text = browser.Address;
        BackButton.IsEnabled = browser.CanGoBack;
        ForwardButton.IsEnabled = browser.CanGoForward;
        Title = $"{browser.Title} — میزکار هوشمند";
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.GoBack();

    private void ForwardButton_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.GoForward();

    private void ReloadButton_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.Reload();

    private void HomeButton_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.Navigate(_data.Settings.HomeUrl);

    private async void NewTabButton_Click(object sender, RoutedEventArgs e) => await CreateTabAsync(_data.Settings.HomeUrl, true);

    private void GoButton_Click(object sender, RoutedEventArgs e) => NavigateFromAddressBar();

    private void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateFromAddressBar();
        }
    }

    private void NavigateFromAddressBar()
    {
        var browser = CurrentBrowser;
        if (browser is null)
        {
            return;
        }

        if (BrowserUriService.TryNormalize(AddressBox.Text, out var address))
        {
            browser.Navigate(address);
        }
        else
        {
            StatusText.Text = "نشانی واردشده معتبر نیست.";
        }
    }

    private void OpenExternalButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentBrowser is null || !BrowserUriService.TryNormalize(CurrentBrowser.Address, out var address))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "باز کردن مرورگر پیش‌فرض");
            StatusText.Text = "باز کردن مرورگر پیش‌فرض انجام نشد.";
        }
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is TabItem tab)
        {
            CloseTab(tab);
        }
    }

    private void CloseTab(TabItem tab)
    {
        if (tab.Tag is BrowserTabView browser)
        {
            browser.DisposeBrowser();
        }

        BrowserTabs.Items.Remove(tab);
        if (BrowserTabs.Items.Count == 0)
        {
            _ = CreateTabAsync(_data.Settings.HomeUrl, true);
        }
    }

    private async void QuickLink_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not QuickLink link)
        {
            return;
        }

        if (_data.Settings.OpenLinksInNewTab || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            await CreateTabAsync(link.Url, true);
        }
        else if (CurrentBrowser is not null)
        {
            CurrentBrowser.Navigate(link.Url);
        }
        else
        {
            await CreateTabAsync(link.Url, true);
        }
    }

    private void AddQuickLinkButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new QuickLinkDialog { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        dialog.Result.SortOrder = _quickLinks.Count;
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

        var dialog = new QuickLinkDialog(link) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        link.Title = dialog.Result.Title;
        link.Url = dialog.Result.Url;
        link.Category = dialog.Result.Category;
        link.Icon = dialog.Result.Icon;
        link.AccentHex = dialog.Result.AccentHex;
        SaveQuickLinks();
        _quickLinksView?.Refresh();
        StatusText.Text = "میانبر ویرایش شد.";
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
            MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        _quickLinks.Remove(link);
        SaveQuickLinks();
        StatusText.Text = "میانبر حذف شد.";
    }

    private void QuickLinkSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _quickLinksView?.Refresh();
    }

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

            var search = QuickLinkSearchBox.Text?.Trim();
            return string.IsNullOrWhiteSpace(search)
                   || link.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || link.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || link.Url.Contains(search, StringComparison.OrdinalIgnoreCase);
        };
        QuickLinksList.ItemsSource = _quickLinksView;
    }

    private void SaveQuickLinks()
    {
        for (var index = 0; index < _quickLinks.Count; index++)
        {
            _quickLinks[index].SortOrder = index;
        }

        _data.QuickLinks = _quickLinks.ToList();
        _dataService.Save(_data);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_data.Settings) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        _data.Settings = dialog.Result;
        _dataService.Save(_data);
        ThemeService.Apply(_data.Settings.Theme);
        foreach (var browser in BrowserTabs.Items.OfType<TabItem>().Select(item => item.Tag).OfType<BrowserTabView>())
        {
            browser.ApplyTheme(_data.Settings.Theme);
        }
        StatusText.Text = "تنظیمات ذخیره شد.";
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "ذخیره نسخه پشتیبان",
            Filter = "SmartDesk Backup (*.json)|*.json",
            FileName = $"SmartDesk-Backup-{DateTime.Now:yyyyMMdd-HHmm}.json"
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
            AppLogger.Error(ex, "خروجی گرفتن از داده‌ها");
            MessageBox.Show("ذخیره نسخه پشتیبان انجام نشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "بازیابی نسخه پشتیبان",
            Filter = "SmartDesk Backup (*.json)|*.json"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _data = _dataService.Import(dialog.FileName);
            LoadQuickLinks();
            ThemeService.Apply(_data.Settings.Theme);
            StatusText.Text = "نسخه پشتیبان بازیابی شد.";
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "بازیابی داده‌ها");
            MessageBox.Show("فایل پشتیبان معتبر نیست یا قابل خواندن نیست.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        PersianDateText.Text = PersianDateService.FormatDate(now);
        TimeText.Text = PersianDateService.FormatTime(now);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
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
            _ = CreateTabAsync(_data.Settings.HomeUrl, true);
            e.Handled = true;
        }
        else if (control && e.Key == Key.W && BrowserTabs.SelectedItem is TabItem tab)
        {
            CloseTab(tab);
            e.Handled = true;
        }
        else if (control && e.Key == Key.R || e.Key == Key.F5)
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
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        _clockTimer.Stop();

        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;

        _data.Settings.WindowWidth = Math.Max(MinWidth, bounds.Width);
        _data.Settings.WindowHeight = Math.Max(MinHeight, bounds.Height);
        _data.Settings.WindowMaximized = WindowState == WindowState.Maximized;
        _data.Settings.LastOpenTabs = BrowserTabs.Items
            .OfType<TabItem>()
            .Select(item => item.Tag)
            .OfType<BrowserTabView>()
            .Select(browser => browser.Address)
            .Where(address => BrowserUriService.TryNormalize(address, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        _data.QuickLinks = _quickLinks.ToList();
        _dataService.Save(_data);

        foreach (var browser in BrowserTabs.Items.OfType<TabItem>().Select(item => item.Tag).OfType<BrowserTabView>())
        {
            browser.DisposeBrowser();
        }
    }
}
