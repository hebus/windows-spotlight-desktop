using System.IO;

namespace SpotlightDesktop.Services;

public static class AppPaths
{
    public static string RootFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpotlightDesktop");

    public static string ImagesFolder => Path.Combine(RootFolder, "images");
    public static string WallpaperFolder => Path.Combine(RootFolder, "wallpaper");
    public static string CatalogFile => Path.Combine(RootFolder, "catalog.json");
    public static string SettingsFile => Path.Combine(RootFolder, "settings.json");
    public static string LogsFolder => Path.Combine(RootFolder, "logs");
}
