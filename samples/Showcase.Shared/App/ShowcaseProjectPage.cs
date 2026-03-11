using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Showcase.Shared.App;

public partial class ShowcaseProjectPage : ContentPage
{
    private string? _projectId;

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
        LoadXamlFromResource();
        ApplyRouteDetails();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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

        if (ProjectIdLabel is not null)
        {
            ProjectIdLabel.Text = $"Project: {projectId}";
        }

        if (ProjectRouteLabel is not null)
        {
            ProjectRouteLabel.Text = $"/projects/{projectId}";
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

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseProjectPage));
#pragma warning restore IL2026
    }
}
