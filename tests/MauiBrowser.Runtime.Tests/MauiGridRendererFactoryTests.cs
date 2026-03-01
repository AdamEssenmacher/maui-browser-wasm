#pragma warning disable BL0006
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiGridRendererFactoryTests
{
    [Fact]
    public void CanRender_RecognizesGridOnly()
    {
        var registry = new RendererRegistry();
        var factory = new MauiGridRendererFactory(registry);

        Assert.True(factory.CanRender(new Grid()));
        Assert.False(factory.CanRender(new Label()));
    }

    [Fact]
    public void Create_EmitsGridStylesAndRendersChildren()
    {
        var registry = new RendererRegistry();
        var labelFactory = new MauiLabelRendererFactory();
        var gridFactory = new MauiGridRendererFactory(registry);
        registry.Register<Label>(labelFactory);
        registry.Register<Grid>(gridFactory);

        var grid = new Grid
        {
            RowSpacing = 6,
            ColumnSpacing = 10
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40, GridUnitType.Absolute) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });

        var topLeft = new Label { Text = "Top Left" };
        Grid.SetRow(topLeft, 0);
        Grid.SetColumn(topLeft, 0);
        var bottomSpan = new Label { Text = "Bottom Span" };
        Grid.SetRow(bottomSpan, 1);
        Grid.SetColumn(bottomSpan, 0);
        Grid.SetColumnSpan(bottomSpan, 2);

        grid.Children.Add(topLeft);
        grid.Children.Add(bottomSpan);

        var fragment = gridFactory.Create(grid, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var styleAttributes = FindAttributeValues(builder, "style");
        var rootStyle = styleAttributes.FirstOrDefault(style =>
            style.Contains("display:grid", StringComparison.Ordinal));

        Assert.NotNull(rootStyle);
        Assert.Contains("grid-template-columns:1fr auto", rootStyle, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows:40px 2fr", rootStyle, StringComparison.Ordinal);
        Assert.Contains("column-gap:10px", rootStyle, StringComparison.Ordinal);
        Assert.Contains("row-gap:6px", rootStyle, StringComparison.Ordinal);
        Assert.Contains(
            styleAttributes,
            style => style.Contains("grid-row:2", StringComparison.Ordinal)
                && style.Contains("grid-column:1 / span 2", StringComparison.Ordinal));

        var textContents = FindTextFrames(builder);
        Assert.Contains("Top Left", textContents, StringComparer.Ordinal);
        Assert.Contains("Bottom Span", textContents, StringComparer.Ordinal);
    }

    [Fact]
    public void Create_ThrowsWhenChildRendererIsMissing()
    {
        var registry = new RendererRegistry();
        var gridFactory = new MauiGridRendererFactory(registry);
        registry.Register<Grid>(gridFactory);

        var grid = new Grid();
        grid.Children.Add(new Button { Text = "Not yet supported" });

        var fragment = gridFactory.Create(grid, RenderContext.Empty);
        var builder = new RenderTreeBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => fragment(builder));
        var expectedTypeName = typeof(Button).FullName ?? typeof(Button).Name;
        Assert.Contains("No renderer factory registered for child view type", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedTypeName, exception.Message, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> FindAttributeValues(RenderTreeBuilder builder, string attributeName)
    {
        var frames = builder.GetFrames();
        var values = new List<string>();

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute
                && string.Equals(frame.AttributeName, attributeName, StringComparison.Ordinal)
                && frame.AttributeValue?.ToString() is { } value)
            {
                values.Add(value);
            }
        }

        return values;
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
