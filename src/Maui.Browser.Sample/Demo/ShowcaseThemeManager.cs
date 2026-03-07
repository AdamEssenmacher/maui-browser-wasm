using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

internal enum ShowcaseThemeMode
{
    Launch,
    Night
}

internal static class ShowcaseThemeManager
{
    public const string PageBackgroundColorKey = "ShowcaseColorPageBackground";
    public const string PageGradientStartColorKey = "ShowcaseColorPageGradientStart";
    public const string PageGradientEndColorKey = "ShowcaseColorPageGradientEnd";
    public const string SurfaceColorKey = "ShowcaseColorSurface";
    public const string SurfaceAltColorKey = "ShowcaseColorSurfaceAlt";
    public const string SurfaceStrokeColorKey = "ShowcaseColorSurfaceStroke";
    public const string TextPrimaryColorKey = "ShowcaseColorTextPrimary";
    public const string TextMutedColorKey = "ShowcaseColorTextMuted";
    public const string AccentColorKey = "ShowcaseColorAccent";
    public const string AccentStrongColorKey = "ShowcaseColorAccentStrong";
    public const string AccentForegroundColorKey = "ShowcaseColorAccentForeground";
    public const string AccentSoftColorKey = "ShowcaseColorAccentSoft";
    public const string DangerColorKey = "ShowcaseColorDanger";
    public const string InputColorKey = "ShowcaseColorInput";
    public const string InputFocusedColorKey = "ShowcaseColorInputFocused";
    public const string HeroStartColorKey = "ShowcaseColorHeroStart";
    public const string HeroEndColorKey = "ShowcaseColorHeroEnd";
    public const string FontBodyKey = "ShowcaseFontBody";
    public const string FontDisplayKey = "ShowcaseFontDisplay";

    public static ShowcaseThemeMode CurrentMode { get; private set; } = ShowcaseThemeMode.Launch;

    public static event EventHandler<ShowcaseThemeMode>? ThemeChanged;

    public static void Initialize(Application application)
    {
        ApplyTheme(application, CurrentMode, raiseEvent: false);
    }

    public static void Toggle(Application application)
    {
        var next = CurrentMode == ShowcaseThemeMode.Launch
            ? ShowcaseThemeMode.Night
            : ShowcaseThemeMode.Launch;

        ApplyTheme(application, next, raiseEvent: true);
    }

    public static string DescribeMode(ShowcaseThemeMode mode)
    {
        return mode == ShowcaseThemeMode.Launch ? "launch" : "night";
    }

    private static void ApplyTheme(Application application, ShowcaseThemeMode mode, bool raiseEvent)
    {
        var resources = application.Resources;
        if (resources is null)
        {
            resources = new ResourceDictionary();
            application.Resources = resources;
        }

        if (mode == ShowcaseThemeMode.Launch)
        {
            SetResource(resources, PageBackgroundColorKey, Color.FromArgb("#EEF3FF"));
            SetResource(resources, PageGradientStartColorKey, Color.FromArgb("#F4F8FF"));
            SetResource(resources, PageGradientEndColorKey, Color.FromArgb("#EAE4FF"));
            SetResource(resources, SurfaceColorKey, Color.FromArgb("#FFFFFF"));
            SetResource(resources, SurfaceAltColorKey, Color.FromArgb("#E7EEFF"));
            SetResource(resources, SurfaceStrokeColorKey, Color.FromArgb("#C3CDF2"));
            SetResource(resources, TextPrimaryColorKey, Color.FromArgb("#1A2342"));
            SetResource(resources, TextMutedColorKey, Color.FromArgb("#586391"));
            SetResource(resources, AccentColorKey, Color.FromArgb("#7C9CFF"));
            SetResource(resources, AccentStrongColorKey, Color.FromArgb("#6E5FD6"));
            SetResource(resources, AccentForegroundColorKey, Color.FromArgb("#F8FAFF"));
            SetResource(resources, AccentSoftColorKey, Color.FromArgb("#DFE6FF"));
            SetResource(resources, DangerColorKey, Color.FromArgb("#C2416C"));
            SetResource(resources, InputColorKey, Color.FromArgb("#F8FAFF"));
            SetResource(resources, InputFocusedColorKey, Color.FromArgb("#E7EDFF"));
            SetResource(resources, HeroStartColorKey, Color.FromArgb("#76BEFF"));
            SetResource(resources, HeroEndColorKey, Color.FromArgb("#A07BFF"));
        }
        else
        {
            SetResource(resources, PageBackgroundColorKey, Color.FromArgb("#101432"));
            SetResource(resources, PageGradientStartColorKey, Color.FromArgb("#141A3A"));
            SetResource(resources, PageGradientEndColorKey, Color.FromArgb("#1E1A44"));
            SetResource(resources, SurfaceColorKey, Color.FromArgb("#171D43"));
            SetResource(resources, SurfaceAltColorKey, Color.FromArgb("#20285C"));
            SetResource(resources, SurfaceStrokeColorKey, Color.FromArgb("#3B4685"));
            SetResource(resources, TextPrimaryColorKey, Color.FromArgb("#EBEEFF"));
            SetResource(resources, TextMutedColorKey, Color.FromArgb("#A9B1DD"));
            SetResource(resources, AccentColorKey, Color.FromArgb("#8FB2FF"));
            SetResource(resources, AccentStrongColorKey, Color.FromArgb("#A78BFA"));
            SetResource(resources, AccentForegroundColorKey, Color.FromArgb("#141733"));
            SetResource(resources, AccentSoftColorKey, Color.FromArgb("#2B346E"));
            SetResource(resources, DangerColorKey, Color.FromArgb("#FF9AAE"));
            SetResource(resources, InputColorKey, Color.FromArgb("#151B3E"));
            SetResource(resources, InputFocusedColorKey, Color.FromArgb("#2C3774"));
            SetResource(resources, HeroStartColorKey, Color.FromArgb("#4A83D8"));
            SetResource(resources, HeroEndColorKey, Color.FromArgb("#8A6CE2"));
        }

        SetResource(resources, FontBodyKey, "Trebuchet MS");
        SetResource(resources, FontDisplayKey, "Palatino Linotype");

        CurrentMode = mode;
        if (raiseEvent)
        {
            ThemeChanged?.Invoke(null, mode);
        }
    }

    private static void SetResource(ResourceDictionary resources, string key, object value)
    {
        if (resources.ContainsKey(key))
        {
            resources[key] = value;
            return;
        }

        resources.Add(key, value);
    }
}
