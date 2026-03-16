using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageLabelHandler : MirageViewHandler<ILabel>
{
    private const string DefaultLineHeight = "1.3";
    private const double DefaultFontSize = 14;
    private const string DefaultSystemLabelColor = MirageCommonViewProjection.DefaultSystemTextColor;
    private const string DefaultSystemFontFamily = MirageCommonViewProjection.DefaultSystemFontFamily;

    public static IPropertyMapper<ILabel, MirageLabelHandler> Mapper =
        new PropertyMapper<ILabel, MirageLabelHandler>(MirageViewMapper.CommonViewMapper)
        {
            // Phase-1 scope intentionally stays narrow: text + core text-style primitives.
            // Richer label semantics (line-break, formatting runs, text transforms) are deferred.
            [nameof(ILabel.Text)] = MapText,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            ["TextColor"] = MapTextColor,
            [nameof(ILabel.Font)] = MapFont,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment
        };

    public MirageLabelHandler()
        : base(ComponentKind.Label, Mapper)
    {
        DefaultDisplayStyle = "flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", "flex");
        changed |= SetStyleIfChanged("flexDirection", "column");
        changed |= SetStyleIfChanged("justifyContent", "flex-start");
        changed |= SetStyleIfChanged("lineHeight", DefaultLineHeight);
        changed |= MirageCommonViewProjection.MapTextColor(this, VirtualView, DefaultSystemLabelColor);
        changed |= MirageCommonViewProjection.MapFont(this, VirtualView, DefaultFontSize, DefaultSystemFontFamily);
        changed |= UpdateHorizontalTextAlignment(VirtualView);
        changed |= UpdateVerticalTextAlignment(VirtualView);
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

    public static void MapTextColor(MirageLabelHandler handler, ILabel label)
    {
        if (MirageCommonViewProjection.MapTextColor(handler, label, DefaultSystemLabelColor))
        {
            handler.RequestRender();
        }
    }

    public static void MapFont(MirageLabelHandler handler, ILabel label)
    {
        if (MirageCommonViewProjection.MapFont(handler, label, DefaultFontSize, DefaultSystemFontFamily))
        {
            handler.RequestRender();
        }
    }

    public static void MapHorizontalTextAlignment(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.UpdateHorizontalTextAlignment(label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapVerticalTextAlignment(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.UpdateVerticalTextAlignment(label))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateHorizontalTextAlignment(ILabel label)
    {
        var textAlign = label.HorizontalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "right",
            TextAlignment.Justify => "justify",
            _ => "left"
        };

        return SetStyleIfChanged("textAlign", textAlign);
    }

    private bool UpdateVerticalTextAlignment(ILabel label)
    {
        var justifyContent = label.VerticalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "flex-end",
            _ => "flex-start"
        };

        return SetStyleIfChanged("justifyContent", justifyContent);
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
