using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Renderer factory for <see cref="Grid"/> controls.
/// </summary>
public sealed class MauiGridRendererFactory : IRendererFactory
{
    private readonly IRendererRegistry _rendererRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MauiGridRendererFactory"/> class.
    /// </summary>
    /// <param name="rendererRegistry">Renderer registry used to resolve child view renderers.</param>
    public MauiGridRendererFactory(IRendererRegistry rendererRegistry)
    {
        _rendererRegistry = rendererRegistry ?? throw new ArgumentNullException(nameof(rendererRegistry));
    }

    /// <inheritdoc />
    public RenderFragment Create(object virtualView, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(virtualView);
        ArgumentNullException.ThrowIfNull(context);

        if (virtualView is not Grid grid)
        {
            throw new ArgumentException("The provided view is not a MAUI Grid.", nameof(virtualView));
        }

        return builder =>
        {
            var sequence = 0;
            builder.OpenElement(sequence++, "div");

            var classes = DomRenderStyleBuilder.BuildClassValue(context.StyleTokens.CssClasses);
            if (!string.IsNullOrWhiteSpace(classes))
            {
                builder.AddAttribute(sequence++, "class", classes);
            }

            var inlineStyles = new Dictionary<string, string>(context.StyleTokens.InlineStyles)
            {
                ["display"] = "grid"
            };

            if (grid.ColumnDefinitions.Count > 0)
            {
                inlineStyles["grid-template-columns"] = BuildTrackTemplate(
                    grid.ColumnDefinitions,
                    static columnDefinition => columnDefinition.Width);
            }

            if (grid.RowDefinitions.Count > 0)
            {
                inlineStyles["grid-template-rows"] = BuildTrackTemplate(
                    grid.RowDefinitions,
                    static rowDefinition => rowDefinition.Height);
            }

            if (grid.ColumnSpacing > 0)
            {
                inlineStyles["column-gap"] = $"{grid.ColumnSpacing.ToString(CultureInfo.InvariantCulture)}px";
            }

            if (grid.RowSpacing > 0)
            {
                inlineStyles["row-gap"] = $"{grid.RowSpacing.ToString(CultureInfo.InvariantCulture)}px";
            }

            var style = DomRenderStyleBuilder.BuildStyleValue(inlineStyles, context.StyleTokens.CssVariables);
            if (!string.IsNullOrWhiteSpace(style))
            {
                builder.AddAttribute(sequence++, "style", style);
            }

            if (!string.IsNullOrWhiteSpace(context.Semantics.Role))
            {
                builder.AddAttribute(sequence++, "role", context.Semantics.Role);
            }

            if (!string.IsNullOrWhiteSpace(context.Semantics.Label))
            {
                builder.AddAttribute(sequence++, "aria-label", context.Semantics.Label);
            }

            if (context.Semantics.Disabled)
            {
                builder.AddAttribute(sequence++, "aria-disabled", "true");
            }

            foreach (var child in grid.Children)
            {
                if (!_rendererRegistry.TryResolve(child, out var childFactory))
                {
                    throw new InvalidOperationException(
                        $"No renderer factory registered for child view type '{child.GetType().FullName}' in Grid.");
                }

                var childContext = RenderContextFactory.CreateFor(child, context.Semantics.Disabled);
                builder.OpenElement(sequence++, "div");
                builder.AddAttribute(sequence++, "style", BuildChildPlacementStyle(child));
                builder.AddContent(sequence++, childFactory.Create(child, childContext));
                builder.CloseElement();
            }

            builder.CloseElement();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is Grid;

    private static string BuildTrackTemplate<TDefinition>(
        IEnumerable<TDefinition> definitions,
        Func<TDefinition, GridLength> getLength)
    {
        var tracks = new List<string>();

        foreach (var definition in definitions)
        {
            tracks.Add(ToCssTrackSize(getLength(definition)));
        }

        return string.Join(" ", tracks);
    }

    private static string BuildChildPlacementStyle(object child)
    {
        var row = 0;
        var column = 0;
        var rowSpan = 1;
        var columnSpan = 1;

        if (child is BindableObject bindableObject)
        {
            row = Grid.GetRow(bindableObject);
            column = Grid.GetColumn(bindableObject);
            rowSpan = Math.Max(1, Grid.GetRowSpan(bindableObject));
            columnSpan = Math.Max(1, Grid.GetColumnSpan(bindableObject));
        }

        var rowStart = row + 1;
        var columnStart = column + 1;

        var rowValue = rowSpan > 1
            ? $"{rowStart.ToString(CultureInfo.InvariantCulture)} / span {rowSpan.ToString(CultureInfo.InvariantCulture)}"
            : rowStart.ToString(CultureInfo.InvariantCulture);
        var columnValue = columnSpan > 1
            ? $"{columnStart.ToString(CultureInfo.InvariantCulture)} / span {columnSpan.ToString(CultureInfo.InvariantCulture)}"
            : columnStart.ToString(CultureInfo.InvariantCulture);

        return $"grid-row:{rowValue};grid-column:{columnValue}";
    }

    private static string ToCssTrackSize(GridLength gridLength)
    {
        if (gridLength.IsAuto)
        {
            return "auto";
        }

        if (gridLength.IsAbsolute)
        {
            return $"{gridLength.Value.ToString(CultureInfo.InvariantCulture)}px";
        }

        if (gridLength.IsStar)
        {
            return $"{gridLength.Value.ToString(CultureInfo.InvariantCulture)}fr";
        }

        return "auto";
    }
}
