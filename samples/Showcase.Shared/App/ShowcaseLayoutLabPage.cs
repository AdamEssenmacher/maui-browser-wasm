using Microsoft.Maui.Controls;

namespace Showcase.Shared.App;

public partial class ShowcaseLayoutLabPage : ContentPage
{
    private const string LayoutLabScrollRootName = "LayoutLabScrollRoot";
    private const string LayoutLabScrollRootAutomationId = "layout-lab-scroll-root";

    private readonly ShowcaseState _state;

    public ShowcaseLayoutLabPage()
    {
        _state = new ShowcaseState();
        BindingContext = _state;
        InitializeComponent();
    }

    private async void OnLayoutLabScrollToTopClicked(object? sender, EventArgs e)
    {
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

    private ScrollView? ResolveScrollRoot()
    {
        return LayoutLabScrollRoot
            ?? this.FindByName<ScrollView>(LayoutLabScrollRootName)
            ?? FindVisualByAutomationId(LayoutLabScrollRootAutomationId) as ScrollView;
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
}
