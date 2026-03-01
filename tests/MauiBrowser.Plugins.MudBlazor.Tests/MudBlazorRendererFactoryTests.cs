#pragma warning disable BL0006
using MauiBrowser.Plugins.MudBlazor.Rendering;
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Maui.Controls;
using MudBlazor;

namespace MauiBrowser.Plugins.MudBlazor.Tests;

public sealed class MudBlazorRendererFactoryTests
{
    [Fact]
    public void MudBlazorButtonRendererFactory_CanRenderRecognizesButtonOnly()
    {
        var factory = new MudBlazorButtonRendererFactory();

        Assert.True(factory.CanRender(new Button()));
        Assert.False(factory.CanRender(new Label()));
    }

    [Fact]
    public void MudBlazorButtonRendererFactory_Create_EmitsMudButtonWithPluginAttributes()
    {
        var factory = new MudBlazorButtonRendererFactory();
        var button = new Button
        {
            Text = "Run"
        };

        var fragment = factory.Create(button, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.True(HasComponent<MudButton>(builder));
        var classes = FindAttributeValue(builder, "Class")?.ToString();
        Assert.NotNull(classes);
        Assert.Contains("maui-mudblazor-button", classes, StringComparison.Ordinal);

        var userAttributes = Assert.IsType<Dictionary<string, object>>(FindAttributeValue(builder, "UserAttributes"));
        Assert.Equal("mudblazor-button", userAttributes["data-maui-plugin"]);
        Assert.Equal("Run", GetChildContentText(builder));
    }

    [Fact]
    public void MudBlazorButtonRendererFactory_Create_DisabledSemanticsMapsToDisabledAttribute()
    {
        var factory = new MudBlazorButtonRendererFactory();
        var button = new Button { Text = "Disabled" };
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

        Assert.Equal(true, FindAttributeValue(builder, "Disabled"));
        Assert.Null(FindAttributeValue(builder, "OnClick"));
    }

    [Fact]
    public void MudBlazorEntryRendererFactory_CanRenderRecognizesEntryOnly()
    {
        var factory = new MudBlazorEntryRendererFactory();

        Assert.True(factory.CanRender(new Entry()));
        Assert.False(factory.CanRender(new Label()));
    }

    [Fact]
    public void MudBlazorEntryRendererFactory_Create_EmitsMudTextFieldWithPluginAttributes()
    {
        var factory = new MudBlazorEntryRendererFactory();
        var entry = new Entry
        {
            Text = "Alpha",
            Placeholder = "Type here"
        };

        var fragment = factory.Create(entry, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.True(HasComponent<MudTextField<string>>(builder));
        var classes = FindAttributeValue(builder, "Class")?.ToString();
        Assert.NotNull(classes);
        Assert.Contains("maui-mudblazor-entry", classes, StringComparison.Ordinal);
        Assert.Equal("Alpha", FindAttributeValue(builder, "Value")?.ToString());
        Assert.Equal("Type here", FindAttributeValue(builder, "Placeholder")?.ToString());
        Assert.Equal(InputType.Text, FindAttributeValue(builder, "InputType"));

        var userAttributes = Assert.IsType<Dictionary<string, object>>(FindAttributeValue(builder, "UserAttributes"));
        Assert.Equal("mudblazor-entry", userAttributes["data-maui-plugin"]);
    }

    [Fact]
    public void MudBlazorEntryRendererFactory_Create_MapsPasswordEntryToPasswordInputType()
    {
        var factory = new MudBlazorEntryRendererFactory();
        var entry = new Entry
        {
            IsPassword = true
        };

        var fragment = factory.Create(entry, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.Equal(InputType.Password, FindAttributeValue(builder, "InputType"));
    }

    [Fact]
    public void MudBlazorEntryRendererFactory_Create_MapsReadOnlyEntryToReadOnlyAttribute()
    {
        var factory = new MudBlazorEntryRendererFactory();
        var entry = new Entry
        {
            IsReadOnly = true
        };

        var fragment = factory.Create(entry, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.Equal(true, FindAttributeValue(builder, "ReadOnly"));
    }

    private static bool HasComponent<TComponent>(RenderTreeBuilder builder)
    {
        var frames = builder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Component
                && frame.ComponentType == typeof(TComponent))
            {
                return true;
            }
        }

        return false;
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

    private static string? GetChildContentText(RenderTreeBuilder builder)
    {
        var fragment = Assert.IsType<RenderFragment>(FindAttributeValue(builder, "ChildContent"));
        var childBuilder = new RenderTreeBuilder();
        fragment(childBuilder);

        var frames = childBuilder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Text)
            {
                return frame.TextContent;
            }
        }

        return null;
    }
}
#pragma warning restore BL0006
