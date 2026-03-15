using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Microsoft.Maui.Handlers;
using Showcase.Shared.App;

namespace Showcase.Fluent.App;

public sealed class ShowcaseMonacoEditorHandler : MirageViewHandler<ShowcaseXamlEditorView>, IMirageRuntimeEventTarget
{
    public static readonly IPropertyMapper<ShowcaseXamlEditorView, ShowcaseMonacoEditorHandler> Mapper =
        new PropertyMapper<ShowcaseXamlEditorView, ShowcaseMonacoEditorHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ShowcaseXamlEditorView.Text)] = MapText,
            [nameof(ShowcaseXamlEditorView.Language)] = MapLanguage,
            [nameof(ShowcaseXamlEditorView.Theme)] = MapTheme,
            [nameof(ShowcaseXamlEditorView.IsReadOnly)] = MapIsReadOnly
        };

    public ShowcaseMonacoEditorHandler()
        : base(() => MiragePlatformView.CreateWeb("mirage-monaco-editor"), Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "block");
        PlatformView.SetStyle("width", "100%");
        PlatformView.SetStyle("maxWidth", "100%");
        PlatformView.SetStyle("minWidth", "0");
        PlatformView.SetStyle("minHeight", "280px");
        PlatformView.SetStyle("overflow", "hidden");
        PlatformView.SetEvent("input", enabled: true);

        MapText(this, VirtualView);
        MapLanguage(this, VirtualView);
        MapTheme(this, VirtualView);
        MapIsReadOnly(this, VirtualView);
        base.ConnectHandler(platformView);
    }

    public static void MapText(ShowcaseMonacoEditorHandler handler, ShowcaseXamlEditorView view)
    {
        if (string.Equals(handler.PlatformView.GetPropertyString("value"), view.Text, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("value", view.Text);
        handler.RequestRender();
    }

    public static void MapLanguage(ShowcaseMonacoEditorHandler handler, ShowcaseXamlEditorView view)
    {
        var language = string.IsNullOrWhiteSpace(view.Language) ? "xml" : view.Language;
        if (string.Equals(handler.PlatformView.GetPropertyString("language"), language, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("language", language);
        handler.RequestRender();
    }

    public static void MapTheme(ShowcaseMonacoEditorHandler handler, ShowcaseXamlEditorView view)
    {
        var theme = string.IsNullOrWhiteSpace(view.Theme) ? "vs-dark" : view.Theme;
        if (string.Equals(handler.PlatformView.GetPropertyString("theme"), theme, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("theme", theme);
        handler.RequestRender();
    }

    public static void MapIsReadOnly(ShowcaseMonacoEditorHandler handler, ShowcaseXamlEditorView view)
    {
        var readOnlyValue = MirageValue.Bool(view.IsReadOnly);
        if (handler.PlatformView.Properties.TryGetValue("readOnly", out var current) && current.Equals(readOnlyValue))
        {
            return;
        }

        handler.PlatformView.SetProperty("readOnly", readOnlyValue);
        handler.RequestRender();
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (!string.Equals(eventName, "input", StringComparison.Ordinal))
        {
            return;
        }

        var incomingText = payload ?? string.Empty;
        if (string.Equals(VirtualView.Text, incomingText, StringComparison.Ordinal))
        {
            return;
        }

        VirtualView.Text = incomingText;
    }
}
