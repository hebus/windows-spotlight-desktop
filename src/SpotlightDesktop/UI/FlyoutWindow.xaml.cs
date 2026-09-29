using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using SpotlightDesktop.Services;
using Screen = System.Windows.Forms.Screen;

namespace SpotlightDesktop.UI;

public partial class FlyoutWindow : Window
{
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(System.Drawing.Point pt, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint MonitorDefaultToNearest = 2;

    private readonly DispatcherTimer _autoHideTimer;
    private readonly FlyoutViewModel _viewModel;

    public FlyoutWindow(SpotlightEngine engine, Func<string, Task> onSelect, Func<Task> onLike, Func<Task> onDislike)
    {
        InitializeComponent();

        _viewModel = new FlyoutViewModel(engine, onSelect, onLike, onDislike);
        DataContext = _viewModel;

        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _autoHideTimer.Tick += (_, _) => { _autoHideTimer.Stop(); Hide(); };
    }

    public async Task RefreshAsync() => await _viewModel.RefreshFromEngineAsync();

    public async Task ShowNearCursorAsync()
    {
        await _viewModel.RefreshFromEngineAsync();

        var cursorPosition = System.Windows.Forms.Cursor.Position;
        var workingArea = Screen.FromPoint(cursorPosition).WorkingArea;

        Show();

        // Positionnement en pixels physiques via l'API Win32 : Window.Left/Top (DIP) dependent du DPI
        // de l'ecran ou se trouve deja la fenetre, ce qui decalait le flyout (parfois vers le haut).
        // On place d'abord la fenetre dans l'angle de l'ecran cible (ce qui applique son DPI), puis on
        // recale avec sa taille physique reelle. Le haut est toujours ancre : le flyout s'etend vers le bas.
        var hwnd = new WindowInteropHelper(this).Handle;
        const int margin = 16;
        int scaledMargin = (int)Math.Round(margin * GetDpiScaleForPoint(cursorPosition));

        SetWindowPos(hwnd, IntPtr.Zero, workingArea.Right - scaledMargin, workingArea.Top + scaledMargin,
            0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
        UpdateLayout();

        if (GetWindowRect(hwnd, out var rect))
        {
            int width = rect.Right - rect.Left;
            SetWindowPos(hwnd, IntPtr.Zero, workingArea.Right - width - scaledMargin, workingArea.Top + scaledMargin,
                0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
        }

        Activate();
        RestartAutoHide();
    }

    private static double GetDpiScaleForPoint(System.Drawing.Point point)
    {
        try
        {
            var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
            if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, 0, out uint dpiX, out _) == 0 && dpiX > 0)
                return dpiX / 96.0;
        }
        catch
        {
            // Ignore : on retombe sur une echelle de 1.0 (pas de mise a l'echelle).
        }

        return 1.0;
    }

    private void RestartAutoHide()
    {
        _autoHideTimer.Stop();
        _autoHideTimer.Start();
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => _autoHideTimer.Stop();

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => RestartAutoHide();
}
