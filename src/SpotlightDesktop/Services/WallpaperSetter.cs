using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SpotlightDesktop.Interop;

namespace SpotlightDesktop.Services;

public sealed class WallpaperSetter
{
    private const string DesktopKeyPath = @"Control Panel\Desktop";

    public void Apply(string imagePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(DesktopKeyPath, writable: true)
            ?? throw new InvalidOperationException("Impossible d'ouvrir la cle de registre du bureau.");

        // WallpaperStyle=10 (Fill) applique la MEME image en entier sur chaque ecran.
        // Ne jamais utiliser 22 (Span) qui etale une portion differente de l'image par ecran.
        key.SetValue("WallpaperStyle", "10");
        key.SetValue("TileWallpaper", "0");

        bool ok = NativeMethods.SystemParametersInfo(
            NativeMethods.SPI_SETDESKWALLPAPER, 0, imagePath,
            NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);

        if (!ok)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
}
