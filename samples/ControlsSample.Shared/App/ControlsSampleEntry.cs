using Microsoft.Maui.Controls;

namespace ControlsSample.Shared.App;

public sealed class ControlsSampleEntry
{
    private readonly ControlsSampleCategory? _category;

    public ControlsSampleEntry(
        string id,
        string title,
        string upstreamPath,
        ControlsSampleStatus status,
        Type pageType,
        string description,
        Func<IServiceProvider, Page>? pageFactory = null,
        ControlsSampleCategory? category = null)
    {
        Id = id;
        Title = title;
        UpstreamPath = upstreamPath;
        Status = status;
        PageType = pageType;
        Description = description;
        PageFactory = pageFactory;
        _category = category;
    }

    public string Id { get; }

    public string Title { get; }

    public string UpstreamPath { get; }

    public ControlsSampleCategory Category => _category
        ?? throw new InvalidOperationException($"Controls sample entry '{Id}' has no assigned category.");

    public ControlsSampleStatus Status { get; }

    public Type PageType { get; }

    public string Description { get; }

    public Func<IServiceProvider, Page>? PageFactory { get; }

    public ControlsSampleEntry WithCategory(ControlsSampleCategory category)
    {
        return new ControlsSampleEntry(
            id: Id,
            title: Title,
            upstreamPath: UpstreamPath,
            status: Status,
            pageType: PageType,
            description: Description,
            pageFactory: PageFactory,
            category: category);
    }

    public Page CreatePage(IServiceProvider services)
    {
        if (PageFactory is not null)
        {
            return PageFactory(services);
        }

        if (services.GetService(PageType) is Page pageFromServices)
        {
            return pageFromServices;
        }

        if (Activator.CreateInstance(PageType) is Page page)
        {
            return page;
        }

        throw new InvalidOperationException($"Unable to instantiate page '{PageType.FullName}'.");
    }
}
