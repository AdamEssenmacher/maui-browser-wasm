using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageButtonHandler : MirageViewHandler<ITextButton>, IMirageRuntimeEventTarget
{
    public static IPropertyMapper<ITextButton, MirageButtonHandler> Mapper =
        new PropertyMapper<ITextButton, MirageButtonHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ITextButton.Text)] = MapText,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            [nameof(IButtonStroke.StrokeColor)] = MapStrokeColor,
            [nameof(IButtonStroke.StrokeThickness)] = MapStrokeThickness,
            [nameof(IButtonStroke.CornerRadius)] = MapCornerRadius,
            ["BorderColor"] = MapStrokeColor,
            ["BorderWidth"] = MapStrokeThickness
        };

    public MirageButtonHandler()
        : base(ComponentKind.Button, Mapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "inline-flex");
        PlatformView.SetStyle("alignItems", "center");
        PlatformView.SetStyle("justifyContent", "center");
        PlatformView.SetStyle("cursor", VirtualView.IsEnabled ? "pointer" : "default");
        PlatformView.SetEvent("click", enabled: true);
        UpdateStrokeStyles(VirtualView);
        UpdateCornerRadius(VirtualView);
        base.ConnectHandler(platformView);
        RequestRender();
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = payload;
        if (!string.Equals(eventName, "click", StringComparison.Ordinal))
        {
            return;
        }

        if (!VirtualView.IsEnabled)
        {
            return;
        }

        VirtualView.Clicked();
    }

    public static void MapText(MirageButtonHandler handler, ITextButton button)
    {
        if (string.Equals(handler.PlatformView.GetPropertyString("Text"), button.Text, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("Text", button.Text);
        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageButtonHandler handler, ITextButton button)
    {
        if (!handler.SetStyleIfChanged("cursor", button.IsEnabled ? "pointer" : "default"))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeColor(MirageButtonHandler handler, ITextButton button)
    {
        if (!handler.UpdateStrokeStyles(button))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeThickness(MirageButtonHandler handler, ITextButton button)
    {
        if (!handler.UpdateStrokeStyles(button))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapCornerRadius(MirageButtonHandler handler, ITextButton button)
    {
        if (!handler.UpdateCornerRadius(button))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateStrokeStyles(ITextButton button)
    {
        if (button is not IButtonStroke strokedButton)
        {
            var cleared = SetStyleIfChanged("borderStyle", null);
            cleared |= SetStyleIfChanged("borderWidth", null);
            cleared |= SetStyleIfChanged("borderColor", null);
            return cleared;
        }

        var strokeThickness = strokedButton.StrokeThickness;
        var normalizedThickness = double.IsFinite(strokeThickness) && strokeThickness > 0
            ? strokeThickness
            : 0;

        if (normalizedThickness <= 0)
        {
            var cleared = SetStyleIfChanged("borderStyle", null);
            cleared |= SetStyleIfChanged("borderWidth", null);
            cleared |= SetStyleIfChanged("borderColor", null);
            return cleared;
        }

        var strokeColor = MirageCommonViewProjection.ToCssColor(strokedButton.StrokeColor) ?? "currentColor";
        var changed = SetStyleIfChanged("borderStyle", "solid");
        changed |= SetStyleIfChanged("borderWidth", MirageViewMapper.ToPx(normalizedThickness));
        changed |= SetStyleIfChanged("borderColor", strokeColor);
        return changed;
    }

    private bool UpdateCornerRadius(ITextButton button)
    {
        if (button is not IButtonStroke strokedButton)
        {
            return SetStyleIfChanged("borderRadius", null);
        }

        var cornerRadius = strokedButton.CornerRadius;
        if (!double.IsFinite(cornerRadius) || cornerRadius < 0)
        {
            return SetStyleIfChanged("borderRadius", null);
        }

        return SetStyleIfChanged("borderRadius", MirageViewMapper.ToPx(cornerRadius));
    }

    private bool SetStyleIfChanged(string styleName, string? styleValue)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), styleValue, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, styleValue);
        return true;
    }
}
