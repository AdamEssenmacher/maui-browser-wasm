#pragma warning disable BL0006
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiStackLayoutRendererFactoryTests
{
    [Fact]
    public void CanRender_RecognizesStackLayoutOnly()
    {
        var registry = new RendererRegistry();
        var factory = new MauiStackLayoutRendererFactory(registry);

        Assert.True(factory.CanRender(new StackLayout()));
        Assert.False(factory.CanRender(new Label()));
    }

    [Theory]
    [InlineData(StackOrientation.Vertical, "column")]
    [InlineData(StackOrientation.Horizontal, "row")]
    public void Create_EmitsFlexDirectionAndRendersChildren(StackOrientation orientation, string expectedFlexDirection)
    {
        var registry = new RendererRegistry();
        var labelFactory = new MauiLabelRendererFactory();
        var stackFactory = new MauiStackLayoutRendererFactory(registry);
        registry.Register<Label>(labelFactory);
        registry.Register<StackLayout>(stackFactory);

        var stackLayout = new StackLayout
        {
            Orientation = orientation,
            Spacing = 12
        };
        stackLayout.Children.Add(new Label { Text = "First" });
        stackLayout.Children.Add(new Label { Text = "Second" });

        var fragment = stackFactory.Create(stackLayout, RenderContext.Empty);

        var builder = new RenderTreeBuilder();
        fragment(builder);

        var style = FindAttributeValue(builder, "style");
        Assert.NotNull(style);
        Assert.Contains("display:flex", style, StringComparison.Ordinal);
        Assert.Contains($"flex-direction:{expectedFlexDirection}", style, StringComparison.Ordinal);
        Assert.Contains("gap:12", style, StringComparison.Ordinal);

        var textContents = FindTextFrames(builder);
        Assert.Contains("First", textContents, StringComparer.Ordinal);
        Assert.Contains("Second", textContents, StringComparer.Ordinal);
    }

    private static string? FindAttributeValue(RenderTreeBuilder builder, string attributeName)
    {
        var frames = builder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute
                && string.Equals(frame.AttributeName, attributeName, StringComparison.Ordinal))
            {
                return frame.AttributeValue?.ToString();
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
