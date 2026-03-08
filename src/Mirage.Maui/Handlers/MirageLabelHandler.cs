using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageLabelHandler : MirageViewHandler<ILabel>
{
    private const string DefaultLineHeight = "1.3";
    private const double DefaultFontSize = 14;

    public static IPropertyMapper<ILabel, MirageLabelHandler> Mapper =
        new PropertyMapper<ILabel, MirageLabelHandler>(MirageViewMapper.CommonViewMapper)
        {
            // Phase-1 scope intentionally stays narrow: basic text + font-size.
            // Richer label semantics (line-break, formatting runs, text transforms, color) are deferred.
            [nameof(ILabel.Text)] = MapText,
            [nameof(ILabel.Font)] = MapFont
        };

    public MirageLabelHandler()
        : base(ComponentKind.Label, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", "block");
        changed |= SetStyleIfChanged("lineHeight", DefaultLineHeight);
        changed |= ApplyFontSize(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapText(MirageLabelHandler handler, ILabel label)
    {
        if (string.Equals(handler.PlatformView.GetPropertyString("Text"), label.Text, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("Text", label.Text);
        handler.RequestRender();
    }

    public static void MapFont(MirageLabelHandler handler, ILabel label)
    {
        if (handler.ApplyFontSize(label))
        {
            handler.RequestRender();
        }
    }

    private bool ApplyFontSize(ILabel label)
    {
        var fontSize = label.Font.Size > 0 ? label.Font.Size : DefaultFontSize;
        return SetStyleIfChanged("fontSize", MirageViewMapper.ToPx(fontSize));
    }

    private bool SetStyleIfChanged(string name, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(name, value);
        return true;
    }
}
