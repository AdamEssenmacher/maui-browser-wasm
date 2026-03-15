using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace Mirage.Maui.Hosting;

#pragma warning disable CS0612 // IFontNamedSizeService/NamedSize are obsolete but required for upstream XAML compatibility.
internal sealed class MirageFontNamedSizeService : IFontNamedSizeService
{
    public double GetNamedSize(NamedSize size, Type targetElementType, bool useOldSizes)
    {
        _ = targetElementType;
        _ = useOldSizes;

        return size switch
        {
            NamedSize.Micro => 10,
            NamedSize.Small => 12,
            NamedSize.Medium => 14,
            NamedSize.Large => 18,
            _ => 14
        };
    }
}
#pragma warning restore CS0612
