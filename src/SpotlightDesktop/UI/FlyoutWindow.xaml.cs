using System.Windows;
using System.Windows.Threading;
using SpotlightDesktop.Services;
using Screen = System.Windows.Forms.Screen;

namespace SpotlightDesktop.UI;

public partial class FlyoutWindow : Window
{
    private readonly DispatcherTimer _autoHideTimer;
    private readonly FlyoutViewModel _viewModel;

    public FlyoutWindow(SpotlightEngine engine, Func<Task> onNext, Func<Task> onLike, Func<Task> onDislike)
    {
        InitializeComponent();

        _viewModel = new FlyoutViewModel(engine, onNext, onLike, onDislike);
        DataContext = _viewModel;

        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _autoHideTimer.Tick += (_, _) => { _autoHideTimer.Stop(); Hide(); };
    }

    public void Refresh() => _viewModel.RefreshFromEngine();

    public void ShowNearCursor()
    {
        _viewModel.RefreshFromEngine();

        var cursorPosition = System.Windows.Forms.Cursor.Position;
        var workingArea = Screen.FromPoint(cursorPosition).WorkingArea;

        Show();
        UpdateLayout();

        Left = workingArea.Right - ActualWidth - 16;
        Top = workingArea.Bottom - ActualHeight - 16;

        Activate();
        RestartAutoHide();
    }

    private void RestartAutoHide()
    {
        _autoHideTimer.Stop();
        _autoHideTimer.Start();
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => _autoHideTimer.Stop();

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => RestartAutoHide();
}
