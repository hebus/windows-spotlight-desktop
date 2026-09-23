namespace SpotlightDesktop.Models;

public sealed class AppSettings
{
    public int RotationIntervalMinutes { get; set; } = 240;
    public int MaxImages { get; set; } = 50;
    public int BatchCount { get; set; } = 4;
    public string Locale { get; set; } = "fr-FR";
    public string Country { get; set; } = "FR";
    public int LikeWeightMultiplier { get; set; } = 3;
    public int FlyoutAutoHideSeconds { get; set; } = 6;
    public bool RunAtStartup { get; set; } = true;
}
