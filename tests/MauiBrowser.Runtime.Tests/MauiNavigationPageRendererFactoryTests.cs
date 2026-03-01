#pragma warning disable BL0006
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiNavigationPageRendererFactoryTests
{
    [Fact]
    public void CanRender_RecognizesNavigationPageOnly()
    {
        var registry = new RendererRegistry();
        var factory = new MauiNavigationPageRendererFactory(registry);

        Assert.True(factory.CanRender(new NavigationPage()));
        Assert.False(factory.CanRender(new ContentPage()));
    }

    [Fact]
    public async Task Create_RendersCurrentPageFromNavigationStack()
    {
        var registry = new RendererRegistry();
        var labelFactory = new MauiLabelRendererFactory();
        var contentFactory = new MauiContentPageRendererFactory(registry);
        var navigationFactory = new MauiNavigationPageRendererFactory(registry);
        registry.Register<Label>(labelFactory);
        registry.Register<ContentPage>(contentFactory);
        registry.Register<NavigationPage>(navigationFactory);

        var rootPage = new ContentPage
        {
            Content = new Label { Text = "Root page" }
        };
        var detailsPage = new ContentPage
        {
            Content = new Label { Text = "Details page" }
        };

        var navigationPage = new NavigationPage(rootPage);
        await navigationPage.PushAsync(detailsPage, animated: false);

        var fragment = navigationFactory.Create(navigationPage, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var textFrames = FindTextFrames(builder);
        Assert.Contains("Details page", textFrames, StringComparer.Ordinal);
        Assert.DoesNotContain("Root page", textFrames, StringComparer.Ordinal);
    }

    [Fact]
    public void Create_WithNoCurrentPage_RendersEmptyContainer()
    {
        var registry = new RendererRegistry();
        var factory = new MauiNavigationPageRendererFactory(registry);
        var navigationPage = new NavigationPage();

        var fragment = factory.Create(navigationPage, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        var exception = Record.Exception(() => fragment(builder));

        Assert.Null(exception);
        Assert.True(HasElement(builder, "div"));
        Assert.Empty(FindTextFrames(builder));
    }

    [Fact]
    public void Create_ThrowsWhenCurrentPageRendererIsMissing()
    {
        var registry = new RendererRegistry();
        var factory = new MauiNavigationPageRendererFactory(registry);
        registry.Register<NavigationPage>(factory);

        var navigationPage = new NavigationPage(
            new ContentPage
            {
                Content = new Label { Text = "Missing page renderer" }
            });

        var fragment = factory.Create(navigationPage, RenderContext.Empty);
        var builder = new RenderTreeBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => fragment(builder));
        var expectedTypeName = typeof(ContentPage).FullName ?? typeof(ContentPage).Name;
        Assert.Contains("No renderer factory registered for page type", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedTypeName, exception.Message, StringComparison.Ordinal);
    }

    private static bool HasElement(RenderTreeBuilder builder, string elementName)
    {
        var frames = builder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Element
                && string.Equals(frame.ElementName, elementName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> FindTextFrames(RenderTreeBuilder builder)
    {
        var frames = builder.GetFrames();
        var text = new List<string>();

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Text && frame.TextContent is not null)
            {
                text.Add(frame.TextContent);
            }
        }

        return text;
    }
}
#pragma warning restore BL0006
