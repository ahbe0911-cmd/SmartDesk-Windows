using Microsoft.Web.WebView2.Core;
using SmartDesk.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace SmartDesk.Views;

public partial class QuickLinkBrowserWindow : Window
{
    private readonly BrowserTabView _browser = new();
    private readonly CoreWebView2Environment _environment;
    private readonly string _initialUrl;

    public QuickLinkBrowserWindow(CoreWebView2Environment environment, string title, string url)
    {
        InitializeComponent();
        _environment = environment;
        _initialUrl = url;
        Title = string.IsNullOrWhiteSpace(title) ? "SmartDesk" : $"{title} - SmartDesk";
        BrowserHost.Children.Add(_browser);
        Loaded += Window_Loaded;
        SourceInitialized += (_, _) => FitToOwner();
        Closed += (_, _) => _browser.DisposeBrowser();
    }

    private void FitToOwner()
    {
        // Fit the modal to the usable screen while keeping it visually centered.
        var work = SystemParameters.WorkArea;
        const double edgeGap = 18;
        Width = Math.Max(800, work.Width - (edgeGap * 2));
        Height = Math.Max(600, work.Height - (edgeGap * 2));
        MaxWidth = work.Width - 8;
        MaxHeight = work.Height - 8;
        Left = work.Left + (work.Width - Width) / 2;
        Top = work.Top + (work.Height - Height) / 2;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FitToOwner();
        try
        {
            _browser.NavigationStateChanged += Browser_NavigationStateChanged;
            _browser.TitleChanged += (_, title) => Title = $"{title} - SmartDesk";
            _browser.StatusMessage += (_, _) => Browser_NavigationStateChanged(_browser);
            _browser.NewWindowRequested += Browser_NewWindowRequested;
            await _browser.InitializeAsync(_environment, _initialUrl);
            Browser_NavigationStateChanged(_browser);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "راه‌اندازی مرورگر تمام‌صفحه");
            MessageBox.Show("مرورگر داخلی SmartDesk راه‌اندازی نشد.", "SmartDesk", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void Browser_NavigationStateChanged(BrowserTabView browser)
    {
        AddressText.Text = browser.CurrentUrl;
        BackButton.IsEnabled = browser.CanGoBack;
        ForwardButton.IsEnabled = browser.CanGoForward;
    }

    private void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        _browser.Navigate(e.Uri);
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _browser.GoBack();
    private void Forward_Click(object sender, RoutedEventArgs e) => _browser.GoForward();
    private void Reload_Click(object sender, RoutedEventArgs e) => _browser.Reload();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void External_Click(object sender, RoutedEventArgs e)
    {
        if (Uri.TryCreate(_browser.CurrentUrl, UriKind.Absolute, out var uri))
        {
            try { BrowserUriService.OpenExternal(uri); }
            catch (Exception ex) { AppLogger.Error(ex, "بازکردن صفحه در مرورگر خارجی"); }
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            _browser.Reload();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Left)
        {
            _browser.GoBack();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Right)
        {
            _browser.GoForward();
            e.Handled = true;
        }
    }
}