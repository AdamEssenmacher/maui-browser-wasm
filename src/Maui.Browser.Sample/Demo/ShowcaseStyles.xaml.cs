using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maui.Browser.Sample.Demo;

public partial class ShowcaseStyles : ResourceDictionary
{
    public ShowcaseStyles()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseStyles));
#pragma warning restore IL2026
    }
}
