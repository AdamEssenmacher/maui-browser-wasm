using System.Collections.ObjectModel;
using System.Linq;
using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageCarouselViewHandlerTests
{
    [Fact]
    public void CarouselViewHandler_UsesItemsViewShapeAndCurrentItemWinsInitialReconciliation()
    {
        var items = new object[] { "Alpha", "Beta", "Gamma" };
        var carousel = new CarouselView
        {
            ItemsSource = items,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal),
            Position = 0,
            CurrentItem = items[2],
            Loop = false
        };

        var handler = CreateHandler();
        ((IElementHandler)handler).SetVirtualView(carousel);

        Assert.Equal(ComponentKind.ContentContainer, handler.PlatformView.ControlKind);
        Assert.Equal(2, carousel.Position);
        Assert.Same(items[2], carousel.CurrentItem);
        Assert.Equal("x mandatory", handler.PlatformView.GetStyleValue("scrollSnapType"));
        Assert.Equal("row", GetItemsHost(handler).GetStyleValue("flexDirection"));
    }

    [Fact]
    public void CarouselViewHandler_PeekInsetsAndSwipeStyles_ReprojectWithoutNodeChurn()
    {
        var carousel = new CarouselView
        {
            ItemsSource = new[] { "Alpha", "Beta", "Gamma" },
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal),
            PeekAreaInsets = new Thickness(20, 0, 20, 0),
            IsSwipeEnabled = true,
            IsBounceEnabled = true,
            IsEnabled = true,
            Loop = false
        };

        var handler = CreateHandler();
        ((IElementHandler)handler).SetVirtualView(carousel);
        handler.PlatformArrange(new Rect(0, 0, 320, 180));

        var initialItemNodeIds = GetItemNodes(handler).Select(static node => node.NodeId).ToArray();
        Assert.Equal("auto", handler.PlatformView.GetStyleValue("overflowX"));
        Assert.Equal("hidden", handler.PlatformView.GetStyleValue("overflowY"));
        Assert.Equal("pan-x", handler.PlatformView.GetStyleValue("touchAction"));
        Assert.Equal("20px", handler.PlatformView.GetStyleValue("scrollPaddingLeft"));
        Assert.Equal("20px", handler.PlatformView.GetStyleValue("scrollPaddingRight"));
        Assert.Equal("auto", handler.PlatformView.GetStyleValue("overscrollBehaviorX"));
        Assert.Equal("280px", GetItemNodes(handler)[0].GetStyleValue("width"));
        Assert.Equal("20px", GetItemNodes(handler)[0].GetStyleValue("marginLeft"));
        Assert.Equal("20px", GetItemNodes(handler)[^1].GetStyleValue("marginRight"));

        carousel.PeekAreaInsets = new Thickness(12, 0, 36, 0);
        MirageCarouselViewHandler.Mapper.UpdateProperty(handler, carousel, nameof(CarouselView.PeekAreaInsets));

        Assert.Equal(initialItemNodeIds, GetItemNodes(handler).Select(static node => node.NodeId).ToArray());
        Assert.Equal("12px", handler.PlatformView.GetStyleValue("scrollPaddingLeft"));
        Assert.Equal("36px", handler.PlatformView.GetStyleValue("scrollPaddingRight"));
        Assert.Equal("272px", GetItemNodes(handler)[0].GetStyleValue("width"));
        Assert.Equal("12px", GetItemNodes(handler)[0].GetStyleValue("marginLeft"));
        Assert.Equal("36px", GetItemNodes(handler)[^1].GetStyleValue("marginRight"));

        carousel.IsSwipeEnabled = false;
        MirageCarouselViewHandler.Mapper.UpdateProperty(handler, carousel, nameof(CarouselView.IsSwipeEnabled));
        Assert.Equal("hidden", handler.PlatformView.GetStyleValue("overflowX"));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("touchAction"));

        carousel.IsEnabled = false;
        carousel.IsSwipeEnabled = true;
        MirageCarouselViewHandler.Mapper.UpdateProperty(handler, carousel, nameof(IView.IsEnabled));
        Assert.Equal("hidden", handler.PlatformView.GetStyleValue("overflowX"));
    }

    [Fact]
    public void CarouselViewHandler_VerticalExplicitHeightRequest_DrivesItemExtentProjection()
    {
        var carousel = new CarouselView
        {
            ItemsSource = new[] { "Alpha", "Beta", "Gamma" },
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
            HeightRequest = 220,
            Loop = false
        };

        var handler = CreateHandler();
        ((IElementHandler)handler).SetVirtualView(carousel);
        handler.PlatformArrange(new Rect(0, 0, 320, 94));

        Assert.Equal("220px", GetItemNodes(handler)[0].GetStyleValue("height"));
    }

    [Fact]
    public void CarouselViewHandler_CollectionMutation_ClampsPositionAndCurrentItem()
    {
        var items = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
        var carousel = new CarouselView
        {
            ItemsSource = items,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal),
            Position = 2,
            CurrentItem = items[2],
            Loop = false
        };

        var handler = CreateHandler();
        ((IElementHandler)handler).SetVirtualView(carousel);

        items.RemoveAt(2);

        Assert.Equal(1, carousel.Position);
        Assert.Equal("Beta", carousel.CurrentItem);
        Assert.Equal(2, GetItemNodes(handler).Count);
    }

    [Fact]
    public void CarouselViewHandler_IndicatorViewLink_ProjectsInitialCountAndPosition()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var items = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
        var indicator = new IndicatorView();
        var carousel = new CarouselView
        {
            ItemsSource = items,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal),
            Position = 1,
            IndicatorView = indicator,
            Loop = false
        };

        var carouselHandler = new MirageCarouselViewHandler();
        ((IElementHandler)carouselHandler).SetMauiContext(mauiContext);
        ((IElementHandler)carouselHandler).SetVirtualView(carousel);

        var indicatorHandler = new MirageIndicatorViewHandler();
        ((IElementHandler)indicatorHandler).SetMauiContext(mauiContext);
        ((IElementHandler)indicatorHandler).SetVirtualView(indicator);

        Assert.Equal(3, indicator.Count);
        Assert.Equal(1, indicator.Position);
        Assert.Equal(3, indicatorHandler.PlatformView.Children.Count(child => child.Attributes.ContainsKey("data-mirage-indicator-dot")));
    }

    private static MirageCarouselViewHandler CreateHandler()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCarouselViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        return handler;
    }

    private static MiragePlatformView GetItemsHost(MirageCarouselViewHandler handler)
    {
        return FindHostByRole(handler.PlatformView, "items")
               ?? throw new InvalidOperationException("Unable to locate carousel items host.");
    }

    private static IReadOnlyList<MiragePlatformView> GetItemNodes(MirageCarouselViewHandler handler)
    {
        return GetItemsHost(handler)
            .Children
            .Where(static child => child.GetPropertyString("CarouselCloneRole") is null)
            .ToArray();
    }

    private static MiragePlatformView? FindHostByRole(MiragePlatformView node, string role)
    {
        if (string.Equals(node.GetPropertyString("CollectionViewHostRole"), role, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindHostByRole(child, role);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
