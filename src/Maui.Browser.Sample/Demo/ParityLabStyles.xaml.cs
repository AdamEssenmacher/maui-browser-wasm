using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maui.Browser.Sample.Demo;

public partial class ParityLabStyles : ResourceDictionary
{
    public ParityLabStyles()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ParityLabStyles));
#pragma warning restore IL2026
    }
}
