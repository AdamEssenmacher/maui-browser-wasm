#pragma warning disable BL0006
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiContentPageRendererFactoryTests
{
    [Fact]
    public void CanRender_RecognizesContentPageOnly()
    {
        var registry = new RendererRegistry();
        var factory = new MauiContentPageRendererFactory(registry);

        Assert.True(factory.CanRender(new ContentPage()));
        Assert.False(factory.CanRender(new Label()));
    }

    [Fact]
    public void Create_RendersChildContent()
    {
        var registry = new RendererRegistry();
        var labelFactory = new MauiLabelRendererFactory();
        var contentFactory = new MauiContentPageRendererFactory(registry);
        registry.Register<Label>(labelFactory);
        registry.Register<ContentPage>(contentFactory);

        var page = new ContentPage
        {
            Content = new Label { Text = "Content page body" }
        };

        var fragment = contentFactory.Create(page, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.True(HasElement(builder, "div"));
        Assert.Contains("Content page body", FindTextFrames(builder), StringComparer.Ordinal);
    }

    [Fact]
    public void Create_WithNullContent_RendersEmptyContainer()
    {
        var registry = new RendererRegistry();
        var factory = new MauiContentPageRendererFactory(registry);
        var page = new ContentPage
        {
            Content = null
        };

        var fragment = factory.Create(page, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        var exception = Record.Exception(() => fragment(builder));

        Assert.Null(exception);
        Assert.True(HasElement(builder, "div"));
        Assert.Empty(FindTextFrames(builder));
    }

    [Fact]
    public void Create_ThrowsWhenChildRendererIsMissing()
    {
        var registry = new RendererRegistry();
        var factory = new MauiContentPageRendererFactory(registry);
        registry.Register<ContentPage>(factory);

        var page = new ContentPage
        {
            Content = new Button { Text = "Missing child renderer" }
        };

        var fragment = factory.Create(page, RenderContext.Empty);
        var builder = new RenderTreeBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => fragment(builder));
        var expectedTypeName = typeof(Button).FullName ?? typeof(Button).Name;
        Assert.Contains("No renderer factory registered for child view type", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedTypeName, exception.Message, StringComparison.Ordinal);
    }

    private static bool HasElement(RenderTreeBuilder builder, string elementName)
    {
        var frames = builder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Element
                && string.Equals(frame.ElementName, elementName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
