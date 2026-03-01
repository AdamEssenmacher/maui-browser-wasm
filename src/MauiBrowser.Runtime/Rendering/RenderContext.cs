using System.Collections.Generic;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Represents contextual data passed to renderer factories for a render operation.
/// </summary>
public sealed class RenderContext
{
    /// <summary>
    /// Gets the style token set for the current render operation.
    /// </summary>
    public required StyleTokens StyleTokens { get; init; }

    /// <summary>
    /// Gets accessibility semantics for the current render operation.
    /// </summary>
    public required Semantics Semantics { get; init; }

    /// <summary>
    /// Gets the event dispatcher associated with the current render operation.
    /// </summary>
    public required EventDispatcher Events { get; init; }

    /// <summary>
    /// Gets an empty render context.
    /// </summary>
    public static RenderContext Empty { get; } = new()
    {
        StyleTokens = StyleTokens.Empty,
        Semantics = Semantics.Empty,
        Events = EventDispatcher.None
    };
}

/// <summary>
/// Represents style-related tokens provided to a renderer.
/// </summary>
public sealed class StyleTokens
{
    /// <summary>
    /// Gets CSS custom property values keyed by property name.
    /// </summary>
    public IReadOnlyDictionary<string, string> CssVariables { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets inline style values keyed by CSS property name.
    /// </summary>
    public IReadOnlyDictionary<string, string> InlineStyles { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets CSS classes to apply to the rendered DOM element.
    /// </summary>
    public IReadOnlyCollection<string> CssClasses { get; init; } = new List<string>();

    /// <summary>
    /// Gets an empty style token set.
    /// </summary>
    public static StyleTokens Empty { get; } = new();
}

/// <summary>
/// Represents baseline accessibility semantics for a rendered element.
/// </summary>
public sealed class Semantics
{
    /// <summary>
    /// Gets the ARIA role value when one is specified.
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// Gets the accessible label value when one is specified.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets a value indicating whether the view should be treated as disabled.
    /// </summary>
    public bool Disabled { get; init; }

    /// <summary>
    /// Gets an empty semantics instance.
    /// </summary>
    public static Semantics Empty { get; } = new();
}

/// <summary>
/// Placeholder event dispatcher for future command and callback wiring.
/// </summary>
public sealed class EventDispatcher
{
    /// <summary>
    /// Gets a shared no-op dispatcher.
    /// </summary>
    public static EventDispatcher None { get; } = new();
}
