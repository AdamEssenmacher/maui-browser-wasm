#pragma warning disable BL0006
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiCollectionViewRendererFactoryTests
{
    [Fact]
    public void CanRender_RecognizesCollectionViewOnly()
    {
        var registry = new RendererRegistry();
        var factory = new MauiCollectionViewRendererFactory(registry);

        Assert.True(factory.CanRender(new CollectionView()));
        Assert.False(factory.CanRender(new Label()));
    }

    [Fact]
    public void Create_WithoutItemTemplate_RendersFallbackTextItems()
    {
        var registry = new RendererRegistry();
        var factory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(factory);

        var collectionView = new CollectionView
        {
            ItemsSource = new[] { "First", "Second" }
        };

        var fragment = factory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var classes = FindAttributeValues(builder, "class");
        Assert.Contains(classes, value => value.Contains("maui-collectionview", StringComparison.Ordinal));
        Assert.Contains(classes, value => value.Contains("maui-collectionview-item", StringComparison.Ordinal));

        var textContents = FindTextFrames(builder);
        Assert.Contains("First", textContents, StringComparer.Ordinal);
        Assert.Contains("Second", textContents, StringComparer.Ordinal);
    }

    [Fact]
    public void Create_WithItemTemplate_RendersTemplatedViews()
    {
        var registry = new RendererRegistry();
        var labelFactory = new MauiLabelRendererFactory();
        var collectionFactory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<Label>(labelFactory);
        registry.Register<CollectionView>(collectionFactory);

        var collectionView = new CollectionView
        {
            ItemsSource = new[] { "A", "B" },
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label();
                label.SetBinding(Label.TextProperty, ".");
                return label;
            })
        };

        var fragment = collectionFactory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var textContents = FindTextFrames(builder);
        Assert.Contains("A", textContents, StringComparer.Ordinal);
        Assert.Contains("B", textContents, StringComparer.Ordinal);
    }

    [Fact]
    public void Create_ThrowsWhenItemTemplateViewRendererIsMissing()
    {
        var registry = new RendererRegistry();
        var collectionFactory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(collectionFactory);

        var collectionView = new CollectionView
        {
            ItemsSource = new[] { "A" },
            ItemTemplate = new DataTemplate(() => new Button { Text = "Child button" })
        };

        var fragment = collectionFactory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => fragment(builder));
        var expectedTypeName = typeof(Button).FullName ?? typeof(Button).Name;
        Assert.Contains("No renderer factory registered for item view type", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedTypeName, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ThrowsWhenItemTemplateReturnsUnsupportedType()
    {
        var registry = new RendererRegistry();
        var collectionFactory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(collectionFactory);

        var collectionView = new CollectionView
        {
            ItemsSource = new[] { "A" },
            ItemTemplate = new DataTemplate(() => new object())
        };

        var fragment = collectionFactory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => fragment(builder));
        var expectedTypeName = typeof(object).FullName ?? typeof(object).Name;
        Assert.Contains("CollectionView ItemTemplate must create a View", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedTypeName, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ThrowsWhenItemTemplateReturnsNull()
    {
        var registry = new RendererRegistry();
        var collectionFactory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(collectionFactory);

        var collectionView = new CollectionView
        {
            ItemsSource = new[] { "A" },
            ItemTemplate = new DataTemplate(() => null!)
        };

        var fragment = collectionFactory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => fragment(builder));
        Assert.Contains("ItemTemplate created null content", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_LimitsMaterializedItemsAndEmitsTruncationMessage()
    {
        var registry = new RendererRegistry();
        var collectionFactory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(collectionFactory);

        var items = Enumerable.Range(0, 205).Select(index => $"Item {index:D3}").ToArray();
        var collectionView = new CollectionView
        {
            ItemsSource = items
        };

        var fragment = collectionFactory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var textContents = FindTextFrames(builder);
        Assert.Contains("Item 000", textContents, StringComparer.Ordinal);
        Assert.Contains("Item 199", textContents, StringComparer.Ordinal);
        Assert.DoesNotContain("Item 200", textContents, StringComparer.Ordinal);
        Assert.Contains("Showing first 200 items.", textContents, StringComparer.Ordinal);
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
