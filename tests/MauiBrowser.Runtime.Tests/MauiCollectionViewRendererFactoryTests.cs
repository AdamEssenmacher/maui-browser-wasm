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
    public void Create_UsesHorizontalItemsLayoutForFlexDirection()
    {
        var registry = new RendererRegistry();
        var factory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(factory);

        var collectionView = new CollectionView
        {
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal),
            ItemsSource = new[] { "One" }
        };

        var fragment = factory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var styleValues = FindAttributeValues(builder, "style");
        var rootStyle = styleValues.FirstOrDefault(value => value.Contains("display:flex", StringComparison.Ordinal));
        Assert.NotNull(rootStyle);
        Assert.Contains("flex-direction:row", rootStyle, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_UsesLinearItemsLayoutItemSpacingAsGap()
    {
        var registry = new RendererRegistry();
        var factory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(factory);

        var collectionView = new CollectionView
        {
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical)
            {
                ItemSpacing = 6
            },
            ItemsSource = new[] { "One", "Two" }
        };

        var fragment = factory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var styleValues = FindAttributeValues(builder, "style");
        var rootStyle = styleValues.FirstOrDefault(value => value.Contains("display:flex", StringComparison.Ordinal));
        Assert.NotNull(rootStyle);
        Assert.Contains("gap:6px", rootStyle, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_UsesGridLayoutForGridItemsLayout()
    {
        var registry = new RendererRegistry();
        var factory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(factory);

        var collectionView = new CollectionView
        {
            ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
            {
                HorizontalItemSpacing = 7,
                VerticalItemSpacing = 5
            },
            ItemsSource = new[] { "One", "Two" }
        };

        var fragment = factory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var styleValues = FindAttributeValues(builder, "style");
        var rootStyle = styleValues.FirstOrDefault(value => value.Contains("display:grid", StringComparison.Ordinal));
        Assert.NotNull(rootStyle);
        Assert.Contains("grid-template-columns:repeat(2, minmax(0, 1fr))", rootStyle, StringComparison.Ordinal);
        Assert.Contains("grid-auto-flow:row", rootStyle, StringComparison.Ordinal);
        Assert.Contains("column-gap:7px", rootStyle, StringComparison.Ordinal);
        Assert.Contains("row-gap:5px", rootStyle, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_UsesHorizontalGridFlowForHorizontalGridItemsLayout()
    {
        var registry = new RendererRegistry();
        var factory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(factory);

        var collectionView = new CollectionView
        {
            ItemsLayout = new GridItemsLayout(3, ItemsLayoutOrientation.Horizontal),
            ItemsSource = new[] { "One" }
        };

        var fragment = factory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var styleValues = FindAttributeValues(builder, "style");
        var rootStyle = styleValues.FirstOrDefault(value => value.Contains("display:grid", StringComparison.Ordinal));
        Assert.NotNull(rootStyle);
        Assert.Contains("grid-template-rows:repeat(3, minmax(0, 1fr))", rootStyle, StringComparison.Ordinal);
        Assert.Contains("grid-auto-flow:column", rootStyle, StringComparison.Ordinal);
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
    public void Create_WithDataTemplateSelector_UsesSelectedTemplate()
    {
        var registry = new RendererRegistry();
        var labelFactory = new MauiLabelRendererFactory();
        var collectionFactory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<Label>(labelFactory);
        registry.Register<CollectionView>(collectionFactory);

        var collectionView = new CollectionView
        {
            ItemsSource = new[] { "A", "B" },
            ItemTemplate = new CollectionTemplateSelector()
        };

        var fragment = collectionFactory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var textContents = FindTextFrames(builder);
        Assert.Contains("Template A", textContents, StringComparer.Ordinal);
        Assert.Contains("Template B", textContents, StringComparer.Ordinal);
    }

    [Fact]
    public void Create_AssignsUniqueKeysForDuplicateItems()
    {
        var registry = new RendererRegistry();
        var factory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<CollectionView>(factory);

        var items = new object?[] { "dup", "dup", null, null };
        var collectionView = new CollectionView
        {
            ItemsSource = items
        };

        var fragment = factory.Create(collectionView, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var keys = FindElementKeys(builder);
        var typedKeys = keys.Select(key => Assert.IsType<ValueTuple<object?, int>>(key)).ToList();

        Assert.Equal(4, typedKeys.Count);
        Assert.Equal(4, typedKeys.Distinct().Count());
        Assert.Equal(("dup", 0), typedKeys[0]);
        Assert.Equal(("dup", 1), typedKeys[1]);
        Assert.Equal((null, 0), typedKeys[2]);
        Assert.Equal((null, 1), typedKeys[3]);
    }

    [Fact]
    public void Create_DisabledCollection_DisablesTemplatedButtons()
    {
        var registry = new RendererRegistry();
        var buttonFactory = new MauiButtonRendererFactory();
        var collectionFactory = new MauiCollectionViewRendererFactory(registry);
        registry.Register<Button>(buttonFactory);
        registry.Register<CollectionView>(collectionFactory);

        var collectionView = new CollectionView
        {
            ItemsSource = new[] { "A" },
            ItemTemplate = new DataTemplate(() => new Button { Text = "Item button" })
        };

        var disabledContext = new RenderContext
        {
            StyleTokens = StyleTokens.Empty,
            Semantics = new MauiBrowser.Runtime.Rendering.Semantics
            {
                Disabled = true
            },
            Events = EventDispatcher.None
        };

        var fragment = collectionFactory.Create(collectionView, disabledContext);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var disabledAttributes = FindAttributeObjects(builder, "disabled");
        Assert.Contains(disabledAttributes, value => value is bool boolValue && boolValue);

        var clickAttributes = FindAttributeObjects(builder, "onclick");
        Assert.Empty(clickAttributes);
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

    private static IReadOnlyList<object?> FindAttributeObjects(RenderTreeBuilder builder, string attributeName)
    {
        var frames = builder.GetFrames();
        var values = new List<object?>();

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute
                && string.Equals(frame.AttributeName, attributeName, StringComparison.Ordinal))
            {
                values.Add(frame.AttributeValue);
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

    private static IReadOnlyList<object> FindElementKeys(RenderTreeBuilder builder)
    {
        var frames = builder.GetFrames();
        var keys = new List<object>();

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Element
                && frame.ElementKey is not null)
            {
                keys.Add(frame.ElementKey);
            }
        }

        return keys;
    }

    private sealed class CollectionTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item?.ToString() switch
            {
                "A" => new DataTemplate(() => new Label { Text = "Template A" }),
                _ => new DataTemplate(() => new Label { Text = "Template B" })
            };
        }
    }
}
#pragma warning restore BL0006
