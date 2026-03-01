#pragma warning disable BL0006
using MauiBrowser.Plugins.Sample.Rendering;
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Plugins.Sample.Tests;

public sealed class SamplePluginButtonRendererFactoryTests
{
    [Fact]
    public void CanRender_RecognizesButtonOnly()
    {
        var factory = new SamplePluginButtonRendererFactory();

        Assert.True(factory.CanRender(new Button()));
        Assert.False(factory.CanRender(new Label()));
    }

    [Fact]
    public void Create_EmitsPluginMarkerAndTextPrefix()
    {
        var factory = new SamplePluginButtonRendererFactory();
        var button = new Button
        {
            Text = "Plugin action"
        };

        var fragment = factory.Create(button, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.Equal("sample-button", FindAttributeValue(builder, "data-maui-plugin")?.ToString());

        var classes = FindAttributeValue(builder, "class")?.ToString();
        Assert.NotNull(classes);
        Assert.Contains("maui-plugin-button", classes, StringComparison.Ordinal);

        var textFrames = FindTextFrames(builder);
        Assert.Contains("[Sample Plugin] Plugin action", textFrames, StringComparer.Ordinal);
    }

    [Fact]
    public void Create_DisabledSemantics_EmitsDisabledAttributesAndSkipsOnClick()
    {
        var factory = new SamplePluginButtonRendererFactory();
        var button = new Button
        {
            Text = "Disabled"
        };
        var context = new RenderContext
        {
            StyleTokens = StyleTokens.Empty,
            Semantics = new MauiBrowser.Runtime.Rendering.Semantics
            {
                Disabled = true
            },
            Events = EventDispatcher.None
        };

        var fragment = factory.Create(button, context);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.NotNull(FindAttributeValue(builder, "disabled"));
        Assert.Equal("true", FindAttributeValue(builder, "aria-disabled")?.ToString());
        Assert.Null(FindAttributeValue(builder, "onclick"));
    }

    private static object? FindAttributeValue(RenderTreeBuilder builder, string attributeName)
    {
        var frames = builder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute
                && string.Equals(frame.AttributeName, attributeName, StringComparison.Ordinal))
            {
                return frame.AttributeValue;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> FindTextFrames(RenderTreeBuilder builder)
    {
        var frames = builder.GetFrames();
        var text = new List<string>();

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Text && frame.TextContent is not null)
            {
                text.Add(frame.TextContent);
            }
        }

        return text;
    }
}
#pragma warning restore BL0006
