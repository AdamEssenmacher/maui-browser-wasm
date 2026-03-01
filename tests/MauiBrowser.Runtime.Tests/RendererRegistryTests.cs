using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Tests;

public sealed class RendererRegistryTests
{
    [Fact]
    public void TryResolve_SelectsHighestPriorityFactory()
    {
        var registry = new RendererRegistry();
        var lowPriorityFactory = new TestRendererFactory(canRender: true);
        var highPriorityFactory = new TestRendererFactory(canRender: true);

        registry.Register<Label>(lowPriorityFactory, priority: 0);
        registry.Register<Label>(highPriorityFactory, priority: 10);

        var found = registry.TryResolve(new Label(), out var resolvedFactory);

        Assert.True(found);
        Assert.Same(highPriorityFactory, resolvedFactory);
    }

    [Fact]
    public void TryResolve_ReturnsFalseForUnsupportedView()
    {
        var registry = new RendererRegistry();
        registry.Register<Label>(new TestRendererFactory(canRender: true));

        var found = registry.TryResolve(new object(), out _);

        Assert.False(found);
    }

    [Fact]
    public void MauiLabelRendererFactory_CanRenderRecognizesLabelOnly()
    {
        var factory = new MauiLabelRendererFactory();

        Assert.True(factory.CanRender(new Label()));
        Assert.False(factory.CanRender(new object()));
    }

    [Fact]
    public void MauiLabelRendererFactory_CreateReturnsFragmentThatCanRender()
    {
        var factory = new MauiLabelRendererFactory();
        var label = new Label
        {
            Text = "Hello test label."
        };

        var context = new RenderContext
        {
            StyleTokens = StyleTokens.Empty,
            Semantics = new MauiBrowser.Runtime.Rendering.Semantics
            {
                Label = "Greeting"
            },
            Events = EventDispatcher.None
        };

        var fragment = factory.Create(label, context);
        Assert.NotNull(fragment);

        var builder = new RenderTreeBuilder();
        var exception = Record.Exception(() => fragment(builder));

        Assert.Null(exception);
    }

    private sealed class TestRendererFactory : IRendererFactory
    {
        private readonly bool _canRender;

        public TestRendererFactory(bool canRender)
        {
            _canRender = canRender;
        }

        public RenderFragment Create(object virtualView, RenderContext context)
        {
            return builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, virtualView.GetType().Name);
                builder.CloseElement();
            };
        }

        public bool CanRender(object virtualView) => _canRender;
    }
}
