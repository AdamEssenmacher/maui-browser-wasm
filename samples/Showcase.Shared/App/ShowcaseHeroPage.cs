using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Showcase.Shared.App;

public partial class ShowcaseHeroPage : ContentPage
{
    private const string GenericWidgetHostName = "GenericWidgetHost";
    private const string ShowcaseScrollRootName = "ShowcaseScrollRoot";
    private const string ShowcaseScrollRootAutomationId = "showcase-scroll-root";
    private const string DemoSectionName = "DemoSection";
    private const string ProvidersSectionName = "ProvidersSection";
    private const string LayoutLabSectionName = "LayoutLabSection";

    private readonly Func<EventHandler, View>? _genericWidgetFactory;
    private readonly ShowcaseState _state;

    public ShowcaseHeroPage(Func<EventHandler, View>? genericWidgetFactory = null)
    {
        _genericWidgetFactory = genericWidgetFactory;
        _state = new ShowcaseState();
        BindingContext = _state;
        Title = "Mirage Showcase";
        LoadXamlFromResource();
        BuildGenericWidget();
    }

    private async void OnDemoCtaClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await ScrollToSectionAsync("demo");
    }

    private async void OnLayoutLabScrollToTopClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (sender is VisualElement visualElement)
        {
            visualElement.Unfocus();
        }

        _state.RegisterLayoutLabScrollCommandAttempt();
        await Task.Yield();
        var scrollRoot = ResolveScrollRoot();
        if (scrollRoot is null)
        {
            return;
        }

        await scrollRoot.ScrollToAsync(0, 0, animated: true);
    }

    private async void OnCommandScrollToLayoutLabClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await ScrollToSectionAsync("layout-lab");
    }

    private async void OnOpenNavigationLabPageClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PushAsync(new ShowcaseNavigationLabPage(), animated: false);
    }

    private async void OnOpenProjectRoutePageClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PushAsync(new ShowcaseProjectPage
        {
            ProjectId = "42"
        }, animated: false);
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseHeroPage));
#pragma warning restore IL2026
    }

    private void OnGenericWidgetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _state.RegisterGenericWidgetClick();
    }

    private async Task ScrollToSectionAsync(string sectionId)
    {
        if (TryHostSectionNavigation(sectionId))
        {
            return;
        }

        var scrollRoot = ResolveScrollRoot();
        var target = ResolveSectionTarget(sectionId);
        if (scrollRoot is null || target is null)
        {
            return;
        }

        if (TryComputeSectionOffsets(scrollRoot, target, out var horizontalOffset, out var verticalOffset))
        {
            await scrollRoot.ScrollToAsync(horizontalOffset, verticalOffset, animated: true);
            return;
        }

        await scrollRoot.ScrollToAsync(target, ScrollToPosition.Start, animated: true);
    }

    private static bool TryComputeSectionOffsets(
        ScrollView scrollRoot,
        VisualElement target,
        out double horizontalOffset,
        out double verticalOffset)
    {
        horizontalOffset = 0;
        verticalOffset = 0;

        if (scrollRoot.Content is not Element contentRoot)
        {
            return false;
        }

        var offsetX = 0d;
        var offsetY = 0d;
        Element? current = target;
        while (current is not null && !ReferenceEquals(current, contentRoot))
        {
            if (current is VisualElement visualElement)
            {
                if (!double.IsFinite(visualElement.X) || !double.IsFinite(visualElement.Y))
                {
                    return false;
                }

                offsetX += visualElement.X;
                offsetY += visualElement.Y;
            }

            current = current.Parent;
        }

        if (!ReferenceEquals(current, contentRoot) || !double.IsFinite(offsetX) || !double.IsFinite(offsetY))
        {
            return false;
        }

        horizontalOffset = Math.Max(0, offsetX);
        verticalOffset = Math.Max(0, offsetY);
        return true;
    }

    private bool TryHostSectionNavigation(string sectionId)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
        {
            return false;
        }

        var services = ResolveServices();
        var navigator = services?.GetService(typeof(IShowcaseSectionNavigator)) as IShowcaseSectionNavigator;
        if (navigator is null)
        {
            return false;
        }

        return navigator.ScrollToSection(sectionId, smooth: true);
    }

    private IServiceProvider? ResolveServices()
    {
        if (Handler?.MauiContext?.Services is IServiceProvider pageServices)
        {
            return pageServices;
        }

        if (Window?.Handler?.MauiContext?.Services is IServiceProvider windowServices)
        {
            return windowServices;
        }

        if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider appServices)
        {
            return appServices;
        }

        var windows = Application.Current?.Windows;
        if (windows is { Count: > 0 }
            && windows[0].Page?.Handler?.MauiContext?.Services is IServiceProvider primaryWindowServices)
        {
            return primaryWindowServices;
        }

        return null;
    }

    private VisualElement? ResolveSectionTarget(string sectionId)
    {
        return sectionId switch
        {
            "demo" => DemoSection
                ?? this.FindByName<VisualElement>(DemoSectionName)
                ?? FindVisualByAutomationId("demo"),
            "providers" => ProvidersSection
                ?? this.FindByName<VisualElement>(ProvidersSectionName)
                ?? FindVisualByAutomationId("providers"),
            "layout-lab" => LayoutLabSection
                ?? this.FindByName<VisualElement>(LayoutLabSectionName)
                ?? FindVisualByAutomationId("layout-lab"),
            _ => null
        };
    }

    private ScrollView? ResolveScrollRoot()
    {
        return ShowcaseScrollRoot
            ?? this.FindByName<ScrollView>(ShowcaseScrollRootName)
            ?? FindVisualByAutomationId(ShowcaseScrollRootAutomationId) as ScrollView;
    }

    private VisualElement? FindVisualByAutomationId(string automationId)
    {
        if (Content is not VisualElement root)
        {
            return null;
        }

        return FindVisualByAutomationId(root, automationId);
    }

    private static VisualElement? FindVisualByAutomationId(VisualElement element, string automationId)
    {
        if (string.Equals(element.AutomationId, automationId, StringComparison.Ordinal))
        {
            return element;
        }

        foreach (var child in EnumerateChildVisualElements(element))
        {
            var match = FindVisualByAutomationId(child, automationId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static IEnumerable<VisualElement> EnumerateChildVisualElements(VisualElement element)
    {
        if (element is ScrollView scrollView && scrollView.Content is VisualElement scrollContent)
        {
            yield return scrollContent;
        }

        if (element is ContentView contentView && contentView.Content is VisualElement content)
        {
            yield return content;
        }

        if (element is Border border && border.Content is VisualElement borderContent)
        {
            yield return borderContent;
        }

        if (element is Layout layout)
        {
            for (var index = 0; index < layout.Children.Count; index++)
            {
                if (layout.Children[index] is VisualElement layoutChild)
                {
                    yield return layoutChild;
                }
            }
        }
    }

    private void BuildGenericWidget()
    {
        var genericWidgetHost = GenericWidgetHost ?? this.FindByName<ContentView>(GenericWidgetHostName);
        if (genericWidgetHost is null)
        {
            return;
        }

        var widget = _genericWidgetFactory?.Invoke(OnGenericWidgetClicked) ?? CreateDefaultGenericWidget();
        if (string.IsNullOrWhiteSpace(widget.AutomationId))
        {
            widget.AutomationId = "showcase-generic-widget";
        }

        genericWidgetHost.Content = widget;
    }

    private View CreateDefaultGenericWidget()
    {
        var button = new Button
        {
            AutomationId = "showcase-generic-widget",
            HorizontalOptions = LayoutOptions.Start,
            Style = ResolveStyle("ShowcaseLabActionButtonStyle")
        };

        button.SetBinding(Button.TextProperty, nameof(ShowcaseState.GenericWidgetText));
        button.Clicked += OnGenericWidgetClicked;
        return button;
    }

    private Style? ResolveStyle(string styleKey)
    {
        if (Resources.TryGetValue(styleKey, out var pageStyle)
            && pageStyle is Style typedPageStyle)
        {
            return typedPageStyle;
        }

        if (Application.Current?.Resources.TryGetValue(styleKey, out var appStyle) == true
            && appStyle is Style typedAppStyle)
        {
            return typedAppStyle;
        }

        return null;
    }
}
