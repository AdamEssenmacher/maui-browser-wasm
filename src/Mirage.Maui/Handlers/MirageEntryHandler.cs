using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageEntryHandler : MirageViewHandler<IEntry>, IMirageRuntimeEventTarget
{
    public static IPropertyMapper<IEntry, MirageEntryHandler> Mapper =
        new PropertyMapper<IEntry, MirageEntryHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IEntry.Text)] = MapText,
            [nameof(IPlaceholder.Placeholder)] = MapPlaceholder
        };

    private bool _isApplyingRuntimeInput;

    public MirageEntryHandler()
        : base(ComponentKind.Entry, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "block");
        PlatformView.SetStyle("width", "100%");
        PlatformView.SetStyle("minHeight", "36px");
        PlatformView.SetEvent("input", enabled: true);
        base.ConnectHandler(platformView);
        RequestRender();
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (!string.Equals(eventName, "input", StringComparison.Ordinal))
        {
            return;
        }

        var value = payload ?? string.Empty;
        if (string.Equals(VirtualView.Text, value, StringComparison.Ordinal))
        {
            return;
        }

        _isApplyingRuntimeInput = true;
        try
        {
            VirtualView.Text = value;
        }
        finally
        {
            _isApplyingRuntimeInput = false;
        }
    }

    public static void MapText(MirageEntryHandler handler, IEntry entry)
    {
        if (handler._isApplyingRuntimeInput && string.Equals(entry.Text, handler.PlatformView.GetPropertyString("Text"), StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("Text", entry.Text);
        handler.RequestRender();
    }

    public static void MapPlaceholder(MirageEntryHandler handler, IEntry entry)
    {
        handler.PlatformView.SetProperty("Placeholder", entry.Placeholder);
        handler.RequestRender();
    }
}
