using System.Windows.Forms;

namespace SpotlightDesktop.Services;

public sealed class MonitorService
{
    public double GetNarrowestAspectRatio()
    {
        double narrowest = double.MaxValue;
        foreach (var screen in Screen.AllScreens)
        {
            double ratio = (double)screen.Bounds.Width / screen.Bounds.Height;
            if (ratio < narrowest) narrowest = ratio;
        }
        return narrowest == double.MaxValue ? 16.0 / 9.0 : narrowest;
    }

    public int ScreenCount => Screen.AllScreens.Length;
}
