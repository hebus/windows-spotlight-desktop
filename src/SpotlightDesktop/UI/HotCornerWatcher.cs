using System.Windows.Threading;
using Cursor = System.Windows.Forms.Cursor;
using Screen = System.Windows.Forms.Screen;

namespace SpotlightDesktop.UI;

/// <summary>Detecte quand la souris entre dans l'angle superieur droit de l'ecran principal.</summary>
public sealed class HotCornerWatcher : IDisposable
{
    private const int CornerSize = 60;

    private readonly DispatcherTimer _timer;
    private bool _wasInCorner;

    public event Action? CornerEntered;

    public HotCornerWatcher()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _timer.Tick += (_, _) => CheckCursor();
        _timer.Start();
    }

    private void CheckCursor()
    {
        var primary = Screen.PrimaryScreen;
        if (primary is null) return;

        var bounds = primary.Bounds;
        var pos = Cursor.Position;

        bool inCorner = pos.X >= bounds.Right - CornerSize && pos.X <= bounds.Right
                         && pos.Y >= bounds.Top && pos.Y <= bounds.Top + CornerSize;

        if (inCorner && !_wasInCorner)
            CornerEntered?.Invoke();

        _wasInCorner = inCorner;
    }

    public void Dispose() => _timer.Stop();
}
