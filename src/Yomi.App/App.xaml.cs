using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Yomi.App.Services;
using Yomi.App.Services.Providers;
using Yomi.App.Views;

namespace Yomi.App;

public partial class App : Application
{
    private const string RegisterTaskArg = "--register-task";

    private TrayIconService? _trayIcon;
    private readonly List<OverlayWindow> _overlayWindows = [];
    private SettingsWindow? _settingsWindow;
    private readonly SettingsService _settingsService = new();
    private readonly AutoStartService _autoStartService = new();
    private SamplingService? _samplingService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains(RegisterTaskArg))
        {
            // インストーラー、または昇格した子プロセスから呼ばれ、タスク登録のみ行って終了する。
            RegisterAutoStartTask();
            Shutdown();
            return;
        }

        var settings = _settingsService.Load();
        EnsureAutoStartRegistration(settings);

        _samplingService = new SamplingService(
            new CpuMetricsProvider(),
            new MemoryMetricsProvider(),
            new GpuMetricsProvider(),
            new DiskMetricsProvider(),
            new NetworkInfoProvider());

        CreateOverlayWindowsForAllScreens(settings);
        _samplingService.Start();

        _trayIcon = new TrayIconService();
        _trayIcon.ToggleVisibilityRequested += OnToggleVisibilityRequested;
        _trayIcon.SettingsRequested += OnSettingsRequested;
        _trayIcon.ExitRequested += OnExitRequested;
    }

    private void RegisterAutoStartTask()
    {
        var exePath = Environment.ProcessPath;
        if (exePath is not null)
        {
            _autoStartService.Register(exePath);
        }
    }

    /// <summary>
    /// 設定で自動起動が有効なのにタスク未登録の場合、管理者権限の子プロセスを
    /// 起動してタスク登録を行う(UACが一度だけ出る)。以降はタスク経由でUACなし起動できる。
    /// </summary>
    private void EnsureAutoStartRegistration(Models.AppSettings settings)
    {
        if (!settings.StartWithWindows) return;
        if (_autoStartService.IsRegistered()) return;

        var exePath = Environment.ProcessPath;
        if (exePath is null) return;

        try
        {
            Process.Start(new ProcessStartInfo(exePath, RegisterTaskArg)
            {
                UseShellExecute = true,
                Verb = "runas",
            })?.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // ユーザーがUACを拒否した場合は自動起動登録をスキップし、通常起動を継続する。
        }
    }

    private void CreateOverlayWindowsForAllScreens(Models.AppSettings settings)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var window = new OverlayWindow(_samplingService!);
            window.ApplySettings(settings);

            // SourceInitializedの時点でHWNDが存在し、モニターごとの正しいDPIが取得できる。
            // Show()前に位置決めすることで、誤った位置に一瞬表示されるちらつきも防ぐ。
            window.SourceInitialized += (_, _) =>
            {
                var dpi = VisualTreeHelper.GetDpi(window);
                var bounds = screen.Bounds;
                var screenRect = new Rect(
                    bounds.Left / dpi.DpiScaleX, bounds.Top / dpi.DpiScaleY,
                    bounds.Width / dpi.DpiScaleX, bounds.Height / dpi.DpiScaleY);
                window.PlaceAtTopRight(screenRect);
            };
            window.Show();

            _overlayWindows.Add(window);
        }
    }

    private void OnToggleVisibilityRequested()
    {
        foreach (var window in _overlayWindows)
        {
            window.Visibility = window.Visibility == Visibility.Visible
                ? Visibility.Hidden
                : Visibility.Visible;
        }
    }

    private void OnSettingsRequested()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var current = _settingsService.Load();
        _settingsWindow = new SettingsWindow(current);
        _settingsWindow.SettingsSaved += settings =>
        {
            _settingsService.Save(settings);
            foreach (var window in _overlayWindows)
            {
                window.ApplySettings(settings);
            }
            UpdateAutoStartRegistration(settings);
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void UpdateAutoStartRegistration(Models.AppSettings settings)
    {
        if (settings.StartWithWindows)
        {
            EnsureAutoStartRegistration(settings);
        }
        else if (_autoStartService.IsRegistered())
        {
            _autoStartService.Unregister();
        }
    }

    private void OnExitRequested()
    {
        _trayIcon?.Dispose();
        foreach (var window in _overlayWindows)
        {
            window.Close();
        }
        _samplingService?.Dispose();
        Shutdown();
    }
}
