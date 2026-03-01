using System.Collections.Generic;

namespace MauiBrowser.Plugins.MudBlazor.Rendering;

internal static class MudBlazorRenderStyleBuilder
{
    public static string BuildClassValue(IReadOnlyCollection<string> classes, string pluginClass)
    {
        var allClasses = new List<string>(classes.Count + 1)
        {
            pluginClass
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
