namespace ControlsSample.Shared.App;

public sealed class ControlsSampleHomePage : ControlsSampleCatalogPageBase
{
    private readonly IServiceProvider _services;
    private readonly IReadOnlyList<(ControlsSampleCategory Category, ControlsSampleEntry[] Entries)> _categories;

    public ControlsSampleHomePage(IServiceProvider services)
    {
        _services = services;

        var policy = (services.GetService(typeof(IControlsSampleVisibilityPolicy)) as IControlsSampleVisibilityPolicy)
                     ?? new NativeControlsSampleVisibilityPolicy();

        _categories = ControlsSampleCategories.DisplayOrder
            .Select(category => (
                Category: category,
                Entries: ControlsSampleCatalog.All
                    .Where(entry => entry.Category == category && policy.IsVisible(entry))
                    .ToArray()))
            .Where(group => group.Entries.Length > 0)
            .ToArray();

        Title = "Controls.Sample (Mirage Wrapper)";
        AutomationId = "controls-sample-home";

        Content = BuildContent();
    }

    private View BuildContent()
    {
        var stack = CreateRootStack();

        stack.Children.Add(CreatePrimaryLabel("Upstream Controls.Sample wrapper", 24, "controls-sample-home-title"));
        stack.Children.Add(CreateSecondaryLabel(
            "Browser host shows Mirage-runnable pages only. Native host includes Mirage-unsupported pages unless externally blocked.",
            14,
            "controls-sample-home-subtitle"));

        if (_categories.Count == 0)
        {
            stack.Children.Add(CreateSecondaryLabel(
                "No pages are visible for the active host policy.",
                14,
                "controls-sample-empty-state"));

            return new ScrollView { Content = stack };
        }

        foreach (var category in _categories)
        {
            stack.Children.Add(BuildCategoryCard(category.Category, category.Entries));
        }

        return new ScrollView { Content = stack };
    }

    private View BuildCategoryCard(ControlsSampleCategory category, IReadOnlyList<ControlsSampleEntry> entries)
    {
        var suffix = ControlsSampleCategories.GetAutomationSuffix(category);
        var content = new VerticalStackLayout
        {
            Spacing = 6
        };

        content.Children.Add(CreatePrimaryLabel(
            ControlsSampleCategories.GetTitle(category),
            18,
            $"controls-sample-category-card-title-{suffix}"));
        content.Children.Add(CreateSecondaryLabel(
            ControlsSampleCategories.GetDescription(category),
            13,
            $"controls-sample-category-card-description-{suffix}"));
        content.Children.Add(CreateSecondaryLabel(
            ControlsSampleCategories.FormatEntryCount(entries.Count),
            13,
            $"controls-sample-category-card-count-{suffix}"));
        content.Children.Add(CreateAccentButton(
            "Browse pages",
            $"controls-sample-open-category-{suffix}",
            async () => await OpenCategoryAsync(category, entries)));

        return BuildCard(content);
    }

    private async Task OpenCategoryAsync(ControlsSampleCategory category, IReadOnlyList<ControlsSampleEntry> entries)
    {
        var page = new ControlsSampleCategoryPage(_services, category, entries);
        await Navigation.PushAsync(page, animated: false);
    }
}
