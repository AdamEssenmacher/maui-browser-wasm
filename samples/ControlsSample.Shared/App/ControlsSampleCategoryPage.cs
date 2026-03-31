using Microsoft.Maui.Controls;

namespace ControlsSample.Shared.App;

public sealed class ControlsSampleCategoryPage : ControlsSampleCatalogPageBase
{
    private readonly IServiceProvider _services;
    private readonly ControlsSampleCategory _category;
    private readonly IReadOnlyList<ControlsSampleEntry> _entries;

    public ControlsSampleCategoryPage(
        IServiceProvider services,
        ControlsSampleCategory category,
        IReadOnlyList<ControlsSampleEntry> entries)
    {
        _services = services;
        _category = category;
        _entries = entries;

        Title = ControlsSampleCategories.GetTitle(category);
        AutomationId = $"controls-sample-category-{ControlsSampleCategories.GetAutomationSuffix(category)}";

        Content = BuildContent();
    }

    private View BuildContent()
    {
        var stack = CreateRootStack();
        var categorySuffix = ControlsSampleCategories.GetAutomationSuffix(_category);

        stack.Children.Add(CreatePrimaryLabel(
            ControlsSampleCategories.GetTitle(_category),
            24,
            $"controls-sample-category-title-{categorySuffix}"));

        stack.Children.Add(CreateSecondaryLabel(
            ControlsSampleCategories.GetDescription(_category),
            14,
            $"controls-sample-category-description-{categorySuffix}"));

        stack.Children.Add(CreateSecondaryLabel(
            ControlsSampleCategories.FormatEntryCount(_entries.Count),
            13,
            $"controls-sample-category-count-{categorySuffix}"));

        var listHost = new VerticalStackLayout
        {
            Spacing = 12,
            AutomationId = $"controls-sample-category-list-{categorySuffix}"
        };

        foreach (var entry in _entries)
        {
            listHost.Children.Add(BuildEntryCard(entry));
        }

        stack.Children.Add(listHost);
        return new ScrollView { Content = stack };
    }

    private View BuildEntryCard(ControlsSampleEntry entry)
    {
        var content = new VerticalStackLayout
        {
            Spacing = 6
        };

        content.Children.Add(CreatePrimaryLabel(entry.Title, 18, $"controls-sample-title-{entry.Id}"));
        content.Children.Add(CreateSecondaryLabel($"Status: {entry.Status}", 13, $"controls-sample-status-{entry.Id}"));
        content.Children.Add(CreateSecondaryLabel(entry.Description, 13, $"controls-sample-description-{entry.Id}"));
        content.Children.Add(CreateAccentButton(
            "Open page",
            $"controls-sample-open-{entry.Id}",
            async () => await OpenEntryAsync(entry)));

        return BuildCard(content);
    }

    private async Task OpenEntryAsync(ControlsSampleEntry entry)
    {
        var page = entry.CreatePage(_services);
        page.Title = entry.Title;
        if (string.IsNullOrWhiteSpace(page.AutomationId))
        {
            page.AutomationId = $"controls-sample-page-{entry.Id}";
        }

        await Navigation.PushAsync(page, animated: false);
    }
}
