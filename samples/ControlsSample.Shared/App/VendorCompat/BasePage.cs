using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Controls.Sample.Pages.Base;

// Minimal upstream-compatible base page used by selectively imported vendor pages.
// We intentionally avoid the full upstream dependency surface (navigation models/settings shell)
// while preserving page type identity expected by XAML roots.
public class BasePage : ContentPage
{
    public BasePage()
    {
        if (Application.Current?.Resources is null)
        {
            return;
        }

        var resources = Application.Current.Resources;
        if (resources.TryGetValue("LightBackgroundColor", out var light)
            && resources.TryGetValue("DarkBackgroundColor", out var dark)
            && light is Color lightColor
            && dark is Color darkColor)
        {
            this.SetAppThemeColor(BackgroundColorProperty, lightColor, darkColor);
        }
    }
}
