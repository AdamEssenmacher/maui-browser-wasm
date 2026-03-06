using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Utils;

public static class CssUtil
{
    public static string Px(double value) => $"{value.ToString("0.###", CultureInfo.InvariantCulture)}px";

    public static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    public static string BoolToDisplay(bool isVisible) => isVisible ? "" : "none";

    public static string CalcRemainingPx(double usedSize) => $"calc(100% - {Px(Math.Max(0, usedSize))})";

    public static string Rgba(byte r, byte g, byte b, double a)
        => $"rgba({r},{g},{b},{a.ToString("0.###", CultureInfo.InvariantCulture)})";

    public static string? ToCssColor(Color color)
    {
        if (color == null)
        {
            return null;
        }

        var r = ToByte(color.Red);
        var g = ToByte(color.Green);
        var b = ToByte(color.Blue);
        return Rgba(r, g, b, Clamp01(color.Alpha));
    }

    public static string? ToCssColor(Color color, double alphaMultiplier)
    {
        if (color == null)
        {
            return null;
        }

        var r = ToByte(color.Red);
        var g = ToByte(color.Green);
        var b = ToByte(color.Blue);
        return Rgba(r, g, b, Clamp01(color.Alpha * alphaMultiplier));
    }

    public static string ToCssFont(Microsoft.Maui.Font font, double fallbackSize = 14)
    {
        var style = font.Slant switch
        {
            FontSlant.Italic => "italic",
            FontSlant.Oblique => "oblique",
            _ => "normal"
        };

        var weight = (int)font.Weight;
        if (weight <= 0)
        {
            weight = (int)FontWeight.Regular;
        }

        var size = font.Size > 0 && !double.IsNaN(font.Size) ? font.Size : fallbackSize;
        var family = string.IsNullOrWhiteSpace(font.Family) ? "system-ui, sans-serif" : font.Family;
        return $"{style} {weight} {size.ToString("0.###", CultureInfo.InvariantCulture)}px {family}";
    }

    private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

    private static byte ToByte(float component)
    {
        var scaled = Math.Round(component * 255.0);
        if (scaled <= 0)
        {
            return 0;
        }

        if (scaled >= 255)
        {
            return 255;
        }

        return (byte)scaled;
    }
}
