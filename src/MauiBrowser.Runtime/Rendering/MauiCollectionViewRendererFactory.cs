using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Renderer factory for <see cref="CollectionView"/> controls.
/// </summary>
public sealed class MauiCollectionViewRendererFactory : IRendererFactory
{
    private const int MaxMaterializedItems = 200;
    private readonly IRendererRegistry _rendererRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MauiCollectionViewRendererFactory"/> class.
    /// </summary>
    /// <param name="rendererRegistry">Renderer registry used to resolve item view renderers.</param>
    public MauiCollectionViewRendererFactory(IRendererRegistry rendererRegistry)
    {
        _rendererRegistry = rendererRegistry ?? throw new ArgumentNullException(nameof(rendererRegistry));
    }

    /// <inheritdoc />
    public RenderFragment Create(object virtualView, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(virtualView);
        ArgumentNullException.ThrowIfNull(context);

        if (virtualView is not CollectionView collectionView)
        {
            throw new ArgumentException("The provided view is not a MAUI CollectionView.", nameof(virtualView));
        }

        return builder =>
        {
            var sequence = 0;
            builder.OpenElement(sequence++, "div");

            var classes = BuildClassValue(context.StyleTokens.CssClasses);
            if (!string.IsNullOrWhiteSpace(classes))
            {
                builder.AddAttribute(sequence++, "class", classes);
            }

            var inlineStyles = BuildLayoutInlineStyles(collectionView.ItemsLayout, context.StyleTokens.InlineStyles);
            var style = DomRenderStyleBuilder.BuildStyleValue(inlineStyles, context.StyleTokens.CssVariables);
            if (!string.IsNullOrWhiteSpace(style))
            {
                builder.AddAttribute(sequence++, "style", style);
            }

            if (!string.IsNullOrWhiteSpace(context.Semantics.Label))
            {
                builder.AddAttribute(sequence++, "aria-label", context.Semantics.Label);
            }

            if (context.Semantics.Disabled)
            {
                builder.AddAttribute(sequence++, "aria-disabled", "true");
            }

            builder.AddAttribute(
                sequence++,
                "role",
                string.IsNullOrWhiteSpace(context.Semantics.Role) ? "list" : context.Semantics.Role);

            if (collectionView.ItemsSource is IEnumerable itemsSource)
            {
                var itemIndex = 0;
                var hasMoreItems = false;

                foreach (var item in itemsSource)
                {
                    if (itemIndex >= MaxMaterializedItems)
                    {
                        hasMoreItems = true;
                        break;
                    }

                    builder.OpenElement(sequence++, "div");
                    builder.SetKey(CreateItemKey(item, itemIndex));
                    builder.AddAttribute(sequence++, "class", "maui-collectionview-item");
                    builder.AddAttribute(sequence++, "role", "listitem");
                    builder.AddAttribute(sequence++, "data-maui-item-index", itemIndex.ToString(CultureInfo.InvariantCulture));
                    RenderItemContent(
                        builder,
                        ref sequence,
                        collectionView,
                        item,
                        parentDisabled: context.Semantics.Disabled);
                    builder.CloseElement();

                    itemIndex++;
                }

                if (hasMoreItems)
                {
                    builder.OpenElement(sequence++, "div");
                    builder.AddAttribute(sequence++, "class", "maui-collectionview-truncation");
                    builder.AddAttribute(sequence++, "data-maui-collectionview-truncated", "true");
                    builder.AddContent(sequence++, $"Showing first {MaxMaterializedItems.ToString(CultureInfo.InvariantCulture)} items.");
                    builder.CloseElement();
                }
            }

            builder.CloseElement();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is CollectionView;

    private void RenderItemContent(
        RenderTreeBuilder builder,
        ref int sequence,
        CollectionView collectionView,
        object? item,
        bool parentDisabled)
    {
        var itemTemplate = ResolveItemTemplate(collectionView, item);
        if (itemTemplate is null)
        {
            builder.AddContent(sequence++, item?.ToString() ?? string.Empty);
            return;
        }

        var templatedView = CreateTemplatedView(itemTemplate, item);
        if (!_rendererRegistry.TryResolve(templatedView, out var childFactory))
        {
            throw new InvalidOperationException(
                $"No renderer factory registered for item view type '{templatedView.GetType().FullName}' in CollectionView.");
        }

        var childContext = CreateChildRenderContext(templatedView, parentDisabled);
        builder.AddContent(sequence++, childFactory.Create(templatedView, childContext));
    }

    private static RenderContext CreateChildRenderContext(object templatedView, bool parentDisabled)
    {
        var childContext = RenderContextFactory.CreateFor(templatedView);
        if (!parentDisabled)
        {
            return childContext;
        }

        return new RenderContext
        {
            StyleTokens = childContext.StyleTokens,
            Semantics = new Semantics
            {
                Role = childContext.Semantics.Role,
                Label = childContext.Semantics.Label,
                Disabled = true
            },
            Events = childContext.Events
        };
    }

    private static DataTemplate? ResolveItemTemplate(CollectionView collectionView, object? item)
    {
        var itemTemplate = collectionView.ItemTemplate;
        if (itemTemplate is null)
        {
            return null;
        }

        if (itemTemplate is DataTemplateSelector selector)
        {
            var selectedTemplate = selector.SelectTemplate(item, collectionView);
            if (selectedTemplate is null)
            {
                throw new InvalidOperationException(
                    "CollectionView DataTemplateSelector returned null template.");
            }

            return selectedTemplate;
        }

        return itemTemplate;
    }

    private static object CreateItemKey(object? item, int itemIndex)
    {
        return item ?? $"maui-null-item-{itemIndex.ToString(CultureInfo.InvariantCulture)}";
    }

    private static View CreateTemplatedView(DataTemplate itemTemplate, object? item)
    {
        var templatedObject = itemTemplate.CreateContent();
        if (templatedObject is null)
        {
            throw new InvalidOperationException(
                "CollectionView ItemTemplate created null content; a View instance is required.");
        }

        var templatedView = templatedObject switch
        {
            View view => view,
            _ => throw new InvalidOperationException(
                $"CollectionView ItemTemplate must create a View, but created '{templatedObject.GetType().FullName}'.")
        };

        if (templatedView is BindableObject bindableObject)
        {
            bindableObject.BindingContext = item;
        }

        return templatedView;
    }

    private static string BuildClassValue(IReadOnlyCollection<string> classes)
    {
        var allClasses = new List<string>(classes.Count + 1)
        {
            "maui-collectionview"
        };

        foreach (var cssClass in classes)
        {
            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                allClasses.Add(cssClass);
            }
        }

        return string.Join(" ", allClasses);
    }

    private static Dictionary<string, string> BuildLayoutInlineStyles(
        IItemsLayout? itemsLayout,
        IReadOnlyDictionary<string, string> baseInlineStyles)
    {
        var inlineStyles = new Dictionary<string, string>(baseInlineStyles);

        switch (itemsLayout)
        {
            case GridItemsLayout gridItemsLayout:
            {
                var span = Math.Max(1, gridItemsLayout.Span);
                inlineStyles["display"] = "grid";

                if (gridItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal)
                {
                    inlineStyles["grid-template-rows"] =
                        $"repeat({span.ToString(CultureInfo.InvariantCulture)}, minmax(0, 1fr))";
                    inlineStyles["grid-auto-flow"] = "column";
                }
                else
                {
                    inlineStyles["grid-template-columns"] =
                        $"repeat({span.ToString(CultureInfo.InvariantCulture)}, minmax(0, 1fr))";
                    inlineStyles["grid-auto-flow"] = "row";
                }

                if (gridItemsLayout.HorizontalItemSpacing > 0)
                {
                    inlineStyles["column-gap"] = $"{gridItemsLayout.HorizontalItemSpacing.ToString(CultureInfo.InvariantCulture)}px";
                }

                if (gridItemsLayout.VerticalItemSpacing > 0)
                {
                    inlineStyles["row-gap"] = $"{gridItemsLayout.VerticalItemSpacing.ToString(CultureInfo.InvariantCulture)}px";
                }

                return inlineStyles;
            }
            case LinearItemsLayout linearItemsLayout:
                inlineStyles["display"] = "flex";
                inlineStyles["flex-direction"] =
                    linearItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal ? "row" : "column";

                if (linearItemsLayout.ItemSpacing > 0)
                {
                    inlineStyles["gap"] = $"{linearItemsLayout.ItemSpacing.ToString(CultureInfo.InvariantCulture)}px";
                }

                return inlineStyles;
            default:
                inlineStyles["display"] = "flex";
                inlineStyles["flex-direction"] = "column";
                return inlineStyles;
        }
    }
}
