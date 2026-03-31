using System.Globalization;

namespace ControlsSample.Shared.App;

internal static class ControlsSampleCategories
{
    public static IReadOnlyList<ControlsSampleCategory> DisplayOrder { get; } =
    [
        ControlsSampleCategory.Layouts,
        ControlsSampleCategory.Controls,
        ControlsSampleCategory.Core,
        ControlsSampleCategory.ParityProbes,
        ControlsSampleCategory.Compatibility
    ];

    public static string GetTitle(ControlsSampleCategory category)
    {
        return category switch
        {
            ControlsSampleCategory.Layouts => "Layouts",
            ControlsSampleCategory.Controls => "Controls",
            ControlsSampleCategory.Core => "Core",
            ControlsSampleCategory.ParityProbes => "Parity Probes",
            ControlsSampleCategory.Compatibility => "Compatibility",
            _ => category.ToString()
        };
    }

    public static string GetDescription(ControlsSampleCategory category)
    {
        return category switch
        {
            ControlsSampleCategory.Layouts => "Foundational layout pages and galleries for arranging, sizing, clipping, scrolling, and updating view trees.",
            ControlsSampleCategory.Controls => "Core control pages and higher-level control galleries, including maps and collection view scenarios.",
            ControlsSampleCategory.Core => "Core visual and page-level behavior samples such as brushes, borders, clipping, transforms, and content pages.",
            ControlsSampleCategory.ParityProbes => "Mirage-specific runtime probes that stress parity-sensitive behavior beyond the upstream sample surface.",
            ControlsSampleCategory.Compatibility => "Compatibility-focused pages retained for wrapper validation alongside the upstream sample set.",
            _ => string.Empty
        };
    }

    public static string GetAutomationSuffix(ControlsSampleCategory category)
    {
        return category.ToString().ToLowerInvariant();
    }

    public static string FormatEntryCount(int count)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{count} {(count == 1 ? "page" : "pages")}");
    }
}
