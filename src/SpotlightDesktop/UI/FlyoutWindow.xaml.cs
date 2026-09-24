using System.Runtime.InteropServices;
using System.Windows;
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

        // Screen.WorkingArea est exprime en pixels physiques, mais Window.Left/Top de WPF
        // attend des unites independantes de la resolution (DIP a 96 DPI). Sans conversion,
        // sur un ecran mis a l'echelle (125%, 150%, ...), le flyout se retrouve hors champ.
        double scale = GetDpiScaleForPoint(cursorPosition);

        Show();
        UpdateLayout();

        Left = workingArea.Right / scale - ActualWidth - 16;
        Top = workingArea.Top / scale + 16;

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
