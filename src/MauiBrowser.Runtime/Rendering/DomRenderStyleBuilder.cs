using System.Collections.Generic;

namespace MauiBrowser.Runtime.Rendering;

internal static class DomRenderStyleBuilder
{
    public static string? BuildClassValue(IReadOnlyCollection<string> classes)
    {
        if (classes.Count == 0)
        {
            return null;
        }

        return string.Join(" ", classes);
    }

    public static string? BuildStyleValue(
        IReadOnlyDictionary<string, string> inlineStyles,
        IReadOnlyDictionary<string, string> cssVariables)
    {
        if (inlineStyles.Count == 0 && cssVariables.Count == 0)
        {
            return null;
        }

        var styleFragments = new List<string>(inlineStyles.Count + cssVariables.Count);
        foreach (var pair in inlineStyles)
        {
            styleFragments.Add($"{pair.Key}:{pair.Value}");
        }

        foreach (var pair in cssVariables)
        {
            styleFragments.Add($"--{pair.Key}:{pair.Value}");
        }

        return string.Join(";", styleFragments);
    }
}
