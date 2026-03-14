using Microsoft.Maui.Controls;

namespace Showcase.Shared.App;

public partial class ShowcaseProjectPage : ContentPage
{
    private string? _projectId;
    private int _toolbarClicks;
    private bool _backButtonVisible = true;

    public string? ProjectId
    {
        get => _projectId;
        set
        {
            if (string.Equals(_projectId, value, StringComparison.Ordinal))
            {
                return;
            }

            _projectId = value;
            ApplyRouteDetails();
        }
    }

    public ShowcaseProjectPage()
    {
        InitializeComponent();
        BuildToolbarItems();
        ApplyRouteDetails();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NavigationPage.SetHasBackButton(this, _backButtonVisible);
        ApplyRouteDetails();
    }

    private async void OnPushNextClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        var next = ResolveNextProjectId();
        await Navigation.PushAsync(new ShowcaseProjectPage
        {
            ProjectId = next
        }, animated: false);
    }

    private async void OnOpenNavigationLabClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PushAsync(new ShowcaseNavigationLabPage(), animated: false);
    }

    private async void OnPopClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (Navigation.NavigationStack.Count <= 1)
        {
            return;
        }

        await Navigation.PopAsync(animated: false);
    }

    private async void OnPopToRootClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PopToRootAsync(animated: false);
    }

    private void ApplyRouteDetails()
    {
        var projectId = string.IsNullOrWhiteSpace(ProjectId) ? "unset" : ProjectId.Trim();
        Title = $"Project {projectId}";
        NavigationPage.SetHasBackButton(this, _backButtonVisible);

        if (ProjectIdLabel is not null)
        {
            ProjectIdLabel.Text = $"Project: {projectId}";
        }

        if (ProjectRouteLabel is not null)
        {
            ProjectRouteLabel.Text = $"/projects/{projectId}";
        }

        if (ProjectBackStateLabel is not null)
        {
            ProjectBackStateLabel.Text = $"Back button allowed: {_backButtonVisible}";
        }

        if (ProjectToolbarCountLabel is not null)
        {
            ProjectToolbarCountLabel.Text = $"Toolbar command count: {_toolbarClicks}";
        }
    }

    private string ResolveNextProjectId()
    {
        if (!int.TryParse(ProjectId, out var current))
        {
            return "1";
        }

        return (current + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void BuildToolbarItems()
    {
        ToolbarItems.Clear();
        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Toolbar +1",
            Order = ToolbarItemOrder.Primary,
            Priority = 0,
            Command = new Command(() =>
            {
                _toolbarClicks++;
                ApplyRouteDetails();
            })
        });
        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Toggle Back",
            Order = ToolbarItemOrder.Primary,
            Priority = 1,
            Command = new Command(() =>
            {
                _backButtonVisible = !_backButtonVisible;
                ApplyRouteDetails();
            })
        });
    }
}
