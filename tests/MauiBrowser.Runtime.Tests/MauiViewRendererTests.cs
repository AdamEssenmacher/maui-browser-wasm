using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiViewRendererTests
{
    [Fact]
    public void Render_ThrowsWhenVirtualViewIsNull()
    {
        var renderer = new MauiViewRenderer(new RendererRegistry());

        var exception = Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));

        Assert.Equal("virtualView", exception.ParamName);
    }

    [Fact]
    public void Render_ThrowsWhenNoFactoryIsRegistered()
    {
        var renderer = new MauiViewRenderer(new RendererRegistry());

        var exception = Assert.Throws<InvalidOperationException>(() => renderer.Render(new object()));

        Assert.Contains("No renderer factory registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ReturnsFragmentForResolvableView()
    {
        var registry = new RendererRegistry();
        registry.Register<object>(new PassthroughFactory(), priority: 1);
        var renderer = new MauiViewRenderer(registry);

        var fragment = renderer.Render(new object());
        Assert.NotNull(fragment);

        var builder = new RenderTreeBuilder();
        var exception = Record.Exception(() => fragment(builder));

        Assert.Null(exception);
    }

    private sealed class PassthroughFactory : IRendererFactory
    {
        public RenderFragment Create(object virtualView, RenderContext context)
        {
            return builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "ok");
                builder.CloseElement();
            };
        }

        public bool CanRender(object virtualView) => true;
    }
}
