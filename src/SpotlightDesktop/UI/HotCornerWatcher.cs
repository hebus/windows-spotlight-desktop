using System.Windows.Threading;
using Cursor = System.Windows.Forms.Cursor;
using Screen = System.Windows.Forms.Screen;

namespace SpotlightDesktop.UI;

/// <summary>Detecte quand la souris entre dans l'angle superieur droit de l'ecran principal.</summary>
public sealed class HotCornerWatcher : IDisposable
{
    private const int CornerSize = 12;
    private const int DwellTicksRequired = 2;

    private readonly DispatcherTimer _timer;
    private bool _wasInCorner;
    private int _dwellTicks;

    public event Action? CornerEntered;

    public HotCornerWatcher()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _timer.Tick += (_, _) => CheckCursor();
        _timer.Start();
    }

    private void CheckCursor()
    {
        var pos = Cursor.Position;
        var screen = Screen.FromPoint(pos);

        var bounds = screen.Bounds;

        bool inCorner = pos.X >= bounds.Right - CornerSize && pos.X <= bounds.Right
                         && pos.Y >= bounds.Top && pos.Y <= bounds.Top + CornerSize;

        if (inCorner)
        {
            _dwellTicks++;
            if (_dwellTicks >= DwellTicksRequired && !_wasInCorner)
            {
                CornerEntered?.Invoke();
                _wasInCorner = true;
            }
        }
        else
        {
            _dwellTicks = 0;
            _wasInCorner = false;
        }
    }

    public void Dispose() => _timer.Stop();
}
