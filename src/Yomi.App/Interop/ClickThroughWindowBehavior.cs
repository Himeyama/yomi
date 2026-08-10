using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Yomi.App.Interop;

/// <summary>
/// ウィンドウをクリックスルー化し、デスクトップの上・他アプリの下に常時固定する。
/// HWND_BOTTOM は一度きりの Z-order 配置のため、フォアグラウンド変化イベントと
/// 保険のタイマーで定期的に再アサートする。
/// </summary>
internal sealed class ClickThroughWindowBehavior : IDisposable
{
    private readonly DispatcherTimer _reassertTimer;
    private IntPtr _hwnd;
    private IntPtr _winEventHook;
    private NativeMethods.WinEventDelegate? _winEventProc;

    public ClickThroughWindowBehavior()
    {
        _reassertTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _reassertTimer.Tick += (_, _) => PushToBottom();
    }

    public void Attach(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;
        if (_hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Window handle is not yet created. Call from OnSourceInitialized.");
        }

        ApplyExtendedStyles();
        PushToBottom();
        HookForegroundChanges();
        _reassertTimer.Start();
    }

    private void ApplyExtendedStyles()
    {
        var exStyle = NativeMethods.GetWindowLongPtr(_hwnd, WindowStyles.GWL_EXSTYLE).ToInt64();
        exStyle |= WindowStyles.WS_EX_TRANSPARENT
                 | WindowStyles.WS_EX_LAYERED
                 | WindowStyles.WS_EX_TOOLWINDOW
                 | WindowStyles.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLongPtr(_hwnd, WindowStyles.GWL_EXSTYLE, new IntPtr(exStyle));
    }

    private void PushToBottom()
    {
        if (_hwnd == IntPtr.Zero) return;
        NativeMethods.SetWindowPos(
            _hwnd, WindowStyles.HWND_BOTTOM,
            0, 0, 0, 0,
            WindowStyles.SWP_NOMOVE | WindowStyles.SWP_NOSIZE | WindowStyles.SWP_NOACTIVATE);
    }

    private void HookForegroundChanges()
    {
        // デリゲートをフィールドに保持し、GCによる回収を防ぐ。
        _winEventProc = (_, _, _, _, _, _, _) => PushToBottom();
        _winEventHook = NativeMethods.SetWinEventHook(
            WindowStyles.EVENT_SYSTEM_FOREGROUND, WindowStyles.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _winEventProc, 0, 0,
            WindowStyles.WINEVENT_OUTOFCONTEXT | WindowStyles.WINEVENT_SKIPOWNPROCESS);
    }

    public void Dispose()
    {
        _reassertTimer.Stop();
        if (_winEventHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
        }
        _winEventProc = null;
    }
}
