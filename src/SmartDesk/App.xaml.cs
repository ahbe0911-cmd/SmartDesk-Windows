using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SmartDesk.Services;

namespace SmartDesk;

public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, "Local\\SmartDesk.Windows.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            MessageBox.Show("برنامه هم‌اکنون در حال اجراست.", "میزکار هوشمند", MessageBoxButton.OK, MessageBoxImage.Information);
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        DispatcherUnhandledException += HandleDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += HandleDomainException;
        TaskScheduler.UnobservedTaskException += HandleTaskException;

        var dataService = new AppDataService();
        var data = dataService.Load();
        ThemeService.Apply(data.Settings.Theme);

        var window = new MainWindow(data, dataService);
        window.EnableDashboardV2();
        MainWindow = window;
        window.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLogger.Error(e.Exception, "خطای مدیریت‌نشده رابط کاربری");
        MessageBox.Show(
            "برنامه با یک خطای غیرمنتظره روبه‌رو شد. گزارش خطا ذخیره شد.",
            "خطای میزکار هوشمند",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void HandleDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            AppLogger.Error(exception, "خطای مدیریت‌نشده برنامه");
        }
    }

    private static void HandleTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppLogger.Error(e.Exception, "خطای مدیریت‌نشده عملیات پس‌زمینه");
        e.SetObserved();
    }
}
