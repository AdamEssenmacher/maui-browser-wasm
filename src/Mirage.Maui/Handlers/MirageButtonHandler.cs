using Mirage.Core.Semantics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageButtonHandler : MirageViewHandler<ITextButton>, IMirageRuntimeEventTarget
{
    public static IPropertyMapper<ITextButton, MirageButtonHandler> Mapper =
        new PropertyMapper<ITextButton, MirageButtonHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ITextButton.Text)] = MapText,
            [nameof(IView.IsEnabled)] = MapIsEnabled
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
        PlatformView.SetStyle("minHeight", "40px");
        PlatformView.SetStyle("padding", "10px 14px");
        PlatformView.SetStyle("cursor", VirtualView.IsEnabled ? "pointer" : "default");
        PlatformView.SetEvent("click", enabled: true);
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
        handler.PlatformView.SetProperty("Text", button.Text);
        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageButtonHandler handler, ITextButton button)
    {
        handler.PlatformView.SetStyle("cursor", button.IsEnabled ? "pointer" : "default");
        handler.RequestRender();
    }
}
