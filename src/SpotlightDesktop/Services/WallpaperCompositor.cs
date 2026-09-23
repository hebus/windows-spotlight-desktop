using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using SpotlightDesktop.Models;

namespace SpotlightDesktop.Services;

public sealed class WallpaperCompositor
{
    private readonly MonitorService _monitorService;

    public WallpaperCompositor(MonitorService monitorService)
    {
        _monitorService = monitorService;
    }

    public void Compose(string sourcePath, string destinationPath, SpotlightImage image)
    {
        using var source = new Bitmap(sourcePath);
        using var canvas = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

        using (var g = Graphics.FromImage(canvas))
        {
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.DrawImage(source, 0, 0, source.Width, source.Height);

            DrawTextBand(g, source.Width, source.Height, image, _monitorService.GetNarrowestAspectRatio());
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        using var stream = File.Create(destinationPath);
        canvas.Save(stream, ImageFormat.Jpeg);
    }

    private static void DrawTextBand(Graphics g, int width, int height, SpotlightImage image, double narrowestScreenRatio)
    {
        string title = string.IsNullOrWhiteSpace(image.Title) ? "Windows Spotlight" : image.Title;
        string description = image.Description ?? string.Empty;
        string copyright = image.Copyright ?? string.Empty;

        // Windows "Fill" recadre symetriquement depuis le centre quand le ratio de l'ecran
        // est plus etroit que celui de l'image : on evite que le bandeau tombe hors du cadrage visible.
        double imageRatio = (double)width / height;
        double cropFraction = 0;
        if (narrowestScreenRatio < imageRatio && imageRatio > 0)
        {
            double visibleWidthFraction = narrowestScreenRatio / imageRatio;
            cropFraction = (1 - visibleWidthFraction) / 2.0;
        }

        int marginX = (int)Math.Max(width * 0.06, width * cropFraction + width * 0.02);
        int marginBottom = (int)(height * 0.08);
        float bandWidth = width * 0.40f;

        using var titleFont = new Font("Segoe UI", height * 0.022f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var bodyFont = new Font("Segoe UI", height * 0.015f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var copyrightFont = new Font("Segoe UI", height * 0.011f, FontStyle.Italic, GraphicsUnit.Pixel);

        float padding = height * 0.02f;
        float maxTextWidth = bandWidth - padding * 2;
        var wrappedDescription = WrapText(g, description, bodyFont, maxTextWidth, maxLines: 4);

        float lineHeight = bodyFont.GetHeight(g);
        float titleHeight = titleFont.GetHeight(g);
        float copyrightHeight = copyrightFont.GetHeight(g);

        float bandHeight = padding * 2 + titleHeight + 8 + wrappedDescription.Count * lineHeight + 6 + copyrightHeight;
        float bandX = marginX;
        float bandY = height - marginBottom - bandHeight;

        var bandRect = new RectangleF(bandX, bandY, bandWidth, bandHeight);
        using (var bandBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
        using (var path = RoundedRect(bandRect, 16))
        {
            g.FillPath(bandBrush, path);
        }

        float textX = bandX + padding;
        float cursorY = bandY + padding;

        using var whiteBrush = new SolidBrush(Color.White);
        g.DrawString(title, titleFont, whiteBrush, textX, cursorY);
        cursorY += titleHeight + 8;

        foreach (var line in wrappedDescription)
        {
            g.DrawString(line, bodyFont, whiteBrush, textX, cursorY);
            cursorY += lineHeight;
        }
        cursorY += 6;

        using var lightBrush = new SolidBrush(Color.FromArgb(210, 255, 255, 255));
        g.DrawString(copyright, copyrightFont, lightBrush, textX, cursorY);
    }

    private static List<string> WrapText(Graphics g, string text, Font font, float maxWidth, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
            var size = g.MeasureString(candidate, font);
            if (size.Width > maxWidth && !string.IsNullOrEmpty(current))
            {
                lines.Add(current);
                current = word;
                if (lines.Count == maxLines) break;
            }
            else
            {
                current = candidate;
            }
        }

        if (lines.Count < maxLines && !string.IsNullOrEmpty(current))
            lines.Add(current);

        if (lines.Count == maxLines && current.Length > 0)
        {
            var last = lines[^1];
            lines[^1] = last.Length > 3 ? last[..^3] + "..." : last;
        }

        return lines;
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
