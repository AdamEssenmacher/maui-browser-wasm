using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Showcase.Shared.App;

public partial class ShowcaseNavigationLabPage : ContentPage
{
    private int _toolbarCommandCount;
    private int _titleRevision;
    private bool _navigationBarVisible = true;
    private bool _backButtonVisible = true;

    public ShowcaseNavigationLabPage()
    {
        LoadXamlFromResource();
        BuildToolbarItems();
        ApplyChromeProbeState();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyChromeProbeState();
    }

    private async void OnPushProject101Clicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await NavigateToProjectAsync("101");
    }

    private async void OnPushProject202Clicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await NavigateToProjectAsync("202");
    }

    private void OnToggleNavigationBarClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _navigationBarVisible = !_navigationBarVisible;
        ApplyChromeProbeState();
    }

    private void OnToggleBackButtonClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _backButtonVisible = !_backButtonVisible;
        ApplyChromeProbeState();
    }

    private void OnBumpTitleClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _titleRevision++;
        ApplyChromeProbeState();
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

    private async Task NavigateToProjectAsync(string projectId)
    {
        await Navigation.PushAsync(new ShowcaseProjectPage
        {
            ProjectId = projectId
        }, animated: false);
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
                _toolbarCommandCount++;
                ApplyChromeProbeState();
            })
        });
        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Toggle Bar",
            Order = ToolbarItemOrder.Primary,
            Priority = 1,
            Command = new Command(() =>
            {
                _navigationBarVisible = !_navigationBarVisible;
                ApplyChromeProbeState();
            })
        });
        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Toggle Back",
            Order = ToolbarItemOrder.Primary,
            Priority = 2,
            Command = new Command(() =>
            {
                _backButtonVisible = !_backButtonVisible;
                ApplyChromeProbeState();
            })
        });
        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Retitle",
            Order = ToolbarItemOrder.Primary,
            Priority = 3,
            Command = new Command(() =>
            {
                _titleRevision++;
                ApplyChromeProbeState();
            })
        });
    }

    private void ApplyChromeProbeState()
    {
        var resolvedTitle = _titleRevision == 0
            ? "Navigation Lab"
            : $"Navigation Lab #{_titleRevision}";
        Title = resolvedTitle;
        NavigationPage.SetHasNavigationBar(this, _navigationBarVisible);
        NavigationPage.SetHasBackButton(this, _backButtonVisible);

        if (NavigationLabTitleStateLabel is not null)
        {
            NavigationLabTitleStateLabel.Text = $"Visible title: {resolvedTitle}";
        }

        if (NavigationLabBarStateLabel is not null)
        {
            NavigationLabBarStateLabel.Text = $"Nav bar visible: {_navigationBarVisible}";
        }

        if (NavigationLabBackStateLabel is not null)
        {
            NavigationLabBackStateLabel.Text = $"Back button allowed: {_backButtonVisible}";
        }

        if (NavigationLabToolbarCountLabel is not null)
        {
            NavigationLabToolbarCountLabel.Text = $"Toolbar command count: {_toolbarCommandCount}";
        }
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseNavigationLabPage));
#pragma warning restore IL2026
    }
}
