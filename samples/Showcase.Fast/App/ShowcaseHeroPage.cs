using Mirage.Web.Runtime;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Showcase.Fast.App;

public partial class ShowcaseHeroPage : ContentPage
{
    private readonly IMiragePageNavigator _pageNavigator;
    private readonly ShowcaseState _state;

    public ShowcaseHeroPage(IMiragePageNavigator pageNavigator)
    {
        _pageNavigator = pageNavigator;
        _state = new ShowcaseState();
        BindingContext = _state;
        LoadXamlFromResource();
    }

    private void OnDemoNavClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("demo");
    }

    private void OnArchitectureNavClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("architecture");
    }

    private void OnProvidersNavClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("providers");
    }

    private void OnDemoCtaClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("demo");
    }

    private void OnArchitectureCtaClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("architecture");
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseHeroPage));
#pragma warning restore IL2026
    }

    private void ScrollTo(string sectionId)
    {
        _ = _pageNavigator.ScrollToAutomationId(sectionId, smooth: true);
    }

    private void OnGenericWidgetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _state.RegisterGenericWidgetClick();
    }
}
