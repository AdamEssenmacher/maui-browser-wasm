using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageIndicatorViewHandlerTests
{
    [Fact]
    public void IndicatorViewHandler_ProjectsBuiltInControlKind_AndDefaultDots()
    {
        var handler = CreateHandler();
        var indicator = new IndicatorView
        {
            Count = 4,
            Position = 0
        };

        ((IElementHandler)handler).SetVirtualView(indicator);

        Assert.Equal(ComponentKind.IndicatorView, handler.PlatformView.ControlKind);
        Assert.Equal("inline-flex", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal(4, GetDotNodes(handler).Count);
    }

    [Fact]
    public void IndicatorViewHandler_VisibleCountAndSelectedState_FollowMaximumVisibleAndHideSingle()
    {
        var handler = CreateHandler();
        var indicator = new IndicatorView
        {
            Count = 6,
            Position = 5,
            MaximumVisible = 4,
            HideSingle = false
        };

        ((IElementHandler)handler).SetVirtualView(indicator);
        Assert.Equal(4, GetDotNodes(handler).Count);
        Assert.Equal(3, GetSelectedDotIndex(handler));

        indicator.MaximumVisible = 2;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IIndicatorView.MaximumVisible));
        Assert.Equal(2, GetDotNodes(handler).Count);
        Assert.Equal(1, GetSelectedDotIndex(handler));

        indicator.Count = 1;
        indicator.HideSingle = true;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IIndicatorView.Count));
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IIndicatorView.HideSingle));
        Assert.Empty(GetDotNodes(handler));

        indicator.HideSingle = false;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IIndicatorView.HideSingle));
        Assert.Single(GetDotNodes(handler));
        Assert.Equal(0, GetSelectedDotIndex(handler));
    }

    [Fact]
    public void IndicatorViewHandler_MapsSizeShapeAndColorsDeterministically()
    {
        var handler = CreateHandler();
        var indicator = new IndicatorView
        {
            Count = 3,
            Position = 1,
            IndicatorSize = 12,
            IndicatorColor = Colors.DarkGray,
            SelectedIndicatorColor = Colors.Crimson,
            IndicatorsShape = IndicatorShape.Circle
        };

        ((IElementHandler)handler).SetVirtualView(indicator);
        var dots = GetDotNodes(handler);
        Assert.Equal(3, dots.Count);
        Assert.All(dots, dot => Assert.Equal("12px", dot.GetStyleValue("width")));
        Assert.All(dots, dot => Assert.Equal("12px", dot.GetStyleValue("height")));
        Assert.Equal("999px", dots[0].GetStyleValue("borderRadius"));
        Assert.Equal("rgba(220,20,60,1)", dots[1].GetStyleValue("backgroundColor"));
        Assert.Equal("rgba(169,169,169,1)", dots[0].GetStyleValue("backgroundColor"));

        indicator.IndicatorsShape = IndicatorShape.Square;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IIndicatorView.IndicatorsShape));
        Assert.Equal("0px", GetDotNodes(handler)[0].GetStyleValue("borderRadius"));

        indicator.IndicatorSize = -1;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IIndicatorView.IndicatorSize));
        Assert.Equal("8px", GetDotNodes(handler)[0].GetStyleValue("width"));
    }

    [Fact]
    public void IndicatorViewHandler_DotClickDisabled_DoesNotUpdatePosition()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var handler = CreateHandler(coordinator);
        var indicator = new IndicatorView
        {
            Count = 4,
            Position = 0,
            IsEnabled = true
        };

        ((IElementHandler)handler).SetVirtualView(indicator);
        var dots = GetDotNodes(handler);
        Assert.Equal(4, dots.Count);

        coordinator.OnEvent(dots[2].NodeId, "click", null);
        Assert.Equal(2, indicator.Position);

        indicator.IsEnabled = false;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IView.IsEnabled));
        coordinator.OnEvent(dots[1].NodeId, "click", null);
        Assert.Equal(2, indicator.Position);
    }

    [Fact]
    public void IndicatorViewHandler_DoesNotOverrideMappedViewOpacity_OnIndicatorUpdates()
    {
        var handler = CreateHandler();
        var indicator = new IndicatorView
        {
            Count = 4,
            Position = 0,
            Opacity = 0.3
        };

        ((IElementHandler)handler).SetVirtualView(indicator);
        Assert.Equal("0.3", handler.PlatformView.GetStyleValue("opacity"));

        indicator.IsEnabled = false;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IView.IsEnabled));
        Assert.Equal("0.3", handler.PlatformView.GetStyleValue("opacity"));

        indicator.MaximumVisible = 2;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IIndicatorView.MaximumVisible));
        Assert.Equal("0.3", handler.PlatformView.GetStyleValue("opacity"));
    }

    [Fact]
    public void IndicatorViewHandler_DotNodes_DoNotSetInlineOutline()
    {
        var handler = CreateHandler();
        var indicator = new IndicatorView
        {
            Count = 3,
            Position = 0
        };

        ((IElementHandler)handler).SetVirtualView(indicator);
        Assert.All(GetDotNodes(handler), dot => Assert.Null(dot.GetStyleValue("outline")));
    }

    [Fact]
    public void IndicatorViewHandler_HostCursor_IsDefaultWhenNoIndicatorsAreVisible()
    {
        var handler = CreateHandler();
        var indicator = new IndicatorView
        {
            Count = 1,
            MaximumVisible = 1,
            HideSingle = true,
            IsEnabled = true
        };

        ((IElementHandler)handler).SetVirtualView(indicator);
        Assert.Equal("default", handler.PlatformView.GetStyleValue("cursor"));

        indicator.HideSingle = false;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IIndicatorView.HideSingle));
        Assert.Equal("pointer", handler.PlatformView.GetStyleValue("cursor"));
    }

    [Fact]
    public void IndicatorViewHandler_RealIndicatorTemplate_TogglesTemplateHostAndSuppressesDefaultDots()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var indicator = new IndicatorView
        {
            Count = 4,
            Position = 1,
            MaximumVisible = 4
        };

        indicator.IndicatorTemplate = new DataTemplate(() =>
            new Border
            {
                WidthRequest = 10,
                HeightRequest = 10,
                StrokeThickness = 0,
                Background = Brush.Gold,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 3
                }
            });

        var templatedIndicator = Assert.IsAssignableFrom<ITemplatedIndicatorView>(indicator);
        var templateLayout = Assert.IsAssignableFrom<Microsoft.Maui.ILayout>(templatedIndicator.IndicatorsLayoutOverride);

        var templateLayoutHandler = new MirageLayoutHandler();
        ((IElementHandler)templateLayoutHandler).SetMauiContext(mauiContext);
        ((IElementHandler)templateLayoutHandler).SetVirtualView(templateLayout);

        var handler = new MirageIndicatorViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(indicator);

        Assert.Empty(GetDotNodes(handler));
        Assert.Single(handler.PlatformView.Children);
        Assert.Equal(PlatformNodeType.Control, handler.PlatformView.Children[0].NodeType);
        Assert.Equal("default", handler.PlatformView.GetStyleValue("cursor"));

        indicator.IndicatorTemplate = null;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(IndicatorView.IndicatorTemplate));
        Assert.Equal(4, GetDotNodes(handler).Count);
        Assert.Equal("pointer", handler.PlatformView.GetStyleValue("cursor"));
    }

    [Fact]
    public void IndicatorViewHandler_TemplateLayoutOverride_SuppressesDefaultDotsAndHostsLayout()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var templateLayout = new HorizontalStackLayout
        {
            AutomationId = "templated-indicator-layout"
        };

        var templateLayoutHandler = new MirageLayoutHandler();
        ((IElementHandler)templateLayoutHandler).SetMauiContext(mauiContext);
        ((IElementHandler)templateLayoutHandler).SetVirtualView(templateLayout);

        var handler = new MirageIndicatorViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var indicator = new TemplatedIndicatorViewProbe
        {
            Count = 3,
            Position = 0,
            MaximumVisible = 3,
            HideSingle = false,
            IndicatorSize = 10,
            IndicatorsShape = new Rectangle(),
            IndicatorColor = new SolidPaint(Colors.Gray),
            SelectedIndicatorColor = new SolidPaint(Colors.Blue),
            IndicatorsLayoutOverride = templateLayout
        };

        ((IElementHandler)handler).SetVirtualView(indicator);

        Assert.Empty(GetDotNodes(handler));
        Assert.Single(handler.PlatformView.Children);
        Assert.Equal(PlatformNodeType.Control, handler.PlatformView.Children[0].NodeType);

        indicator.IndicatorsLayoutOverride = null;
        MirageIndicatorViewHandler.Mapper.UpdateProperty(handler, indicator, nameof(ITemplatedIndicatorView.IndicatorsLayoutOverride));

        Assert.Equal(3, GetDotNodes(handler).Count);
    }

    [Fact]
    public void IndicatorViewHandler_UnsupportedPaintsUseDeterministicFallback()
    {
        var handler = CreateHandler();
        var indicator = new IndicatorViewPaintProbe
        {
            Count = 2,
            Position = 0,
            IndicatorSize = 10,
            MaximumVisible = 10,
            HideSingle = false,
            IndicatorsShape = new Rectangle(),
            IndicatorColor = new LinearGradientPaint(
                new[]
                {
                    new PaintGradientStop(0, Colors.DodgerBlue),
                    new PaintGradientStop(1, Colors.MediumPurple)
                },
                new Point(0, 0),
                new Point(1, 1)),
            SelectedIndicatorColor = new RadialGradientPaint()
        };

        ((IElementHandler)handler).SetVirtualView(indicator);
        var dots = GetDotNodes(handler);
        Assert.Equal(2, dots.Count);
        Assert.Equal("rgba(37,99,235,1)", dots[0].GetStyleValue("backgroundColor"));
        Assert.Equal("rgba(148,163,184,0.55)", dots[1].GetStyleValue("backgroundColor"));
    }

    private static MirageIndicatorViewHandler CreateHandler(MirageRenderCoordinator? coordinator = null)
    {
        var services = new ServiceCollection();
        if (coordinator is not null)
        {
            services.AddSingleton<IMirageRenderCoordinator>(coordinator);
        }

        var handler = new MirageIndicatorViewHandler();
        ((IElementHandler)handler).SetMauiContext(new MauiContext(services.BuildServiceProvider()));
        return handler;
    }

    private static IReadOnlyList<MiragePlatformView> GetDotNodes(MirageIndicatorViewHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "button", StringComparison.Ordinal))
            .Where(static child => child.Classes.Contains("mirage-indicator-dot", StringComparer.Ordinal))
            .ToArray();
    }

    private static int GetSelectedDotIndex(MirageIndicatorViewHandler handler)
    {
        var dots = GetDotNodes(handler);
        for (var index = 0; index < dots.Count; index++)
        {
            if (dots[index].Attributes.TryGetValue("data-mirage-indicator-selected", out var selected)
                && string.Equals(selected, "true", StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private class IndicatorViewPaintProbe : View, IIndicatorView
    {
        public int Count { get; set; }

        public int Position { get; set; }

        public double IndicatorSize { get; set; }

        public int MaximumVisible { get; set; }

        public bool HideSingle { get; set; }

        public Paint IndicatorColor { get; set; } = new SolidPaint(Colors.Gray);

        public Paint SelectedIndicatorColor { get; set; } = new SolidPaint(Colors.Blue);

        public IShape IndicatorsShape { get; set; } = new Rectangle();
    }

    private sealed class TemplatedIndicatorViewProbe : IndicatorViewPaintProbe, ITemplatedIndicatorView
    {
        public Microsoft.Maui.ILayout? IndicatorsLayoutOverride { get; set; }
    }
}
