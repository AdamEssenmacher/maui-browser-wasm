using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageLayoutHandlerTests
{
    [Fact]
    public void LayoutHandler_Add_InsertsOnlyTargetChildWithoutFullRebuild()
    {
        var layout = new VerticalStackLayout();
        var first = new Label { Text = "First", AutomationId = "layout-test-first" };
        var second = new Label { Text = "Second", AutomationId = "layout-test-second" };
        layout.Children.Add(first);
        layout.Children.Add(second);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(layout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(layout);

        var firstHandler = first.Handler;
        var secondHandler = second.Handler;

        var third = new Label { Text = "Third", AutomationId = "layout-test-third" };
        AttachChildHandler(third, mauiContext);
        handler.Add(third);

        Assert.Equal(3, handler.PlatformView.Children.Count);
        Assert.Same(firstHandler, first.Handler);
        Assert.Same(secondHandler, second.Handler);
        Assert.Equal(
            ["layout-test-first", "layout-test-second", "layout-test-third"],
            handler.PlatformView.Children.Select(static child => child.GetPropertyString("AutomationId")).ToArray());
    }

    [Fact]
    public void LayoutHandler_Remove_RemovesOnlyTargetChildWithoutFullRebuild()
    {
        var layout = new VerticalStackLayout();
        var first = new Label { Text = "First", AutomationId = "layout-test-first" };
        var second = new Label { Text = "Second", AutomationId = "layout-test-second" };
        var third = new Label { Text = "Third", AutomationId = "layout-test-third" };
        layout.Children.Add(first);
        layout.Children.Add(second);
        layout.Children.Add(third);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(layout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(layout);

        var firstHandler = first.Handler;
        var thirdHandler = third.Handler;

        handler.Remove(second);

        Assert.Equal(2, handler.PlatformView.Children.Count);
        Assert.Same(firstHandler, first.Handler);
        Assert.Same(thirdHandler, third.Handler);
        Assert.Null(second.Handler);
        Assert.Equal(
            ["layout-test-first", "layout-test-third"],
            handler.PlatformView.Children.Select(static child => child.GetPropertyString("AutomationId")).ToArray());
    }

    [Fact]
    public void LayoutHandler_Update_RefreshesTargetProjectionWithoutStructureMutation()
    {
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        var first = new Label { Text = "First" };
        var second = new Label { Text = "Second" };
        grid.Children.Add(first);
        grid.Children.Add(second);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(grid, mauiContext);
        ((IElementHandler)handler).SetVirtualView(grid);

        var secondPlatform = GetPlatformView(second);
        Assert.Equal("1", secondPlatform.GetStyleValue("gridRowStart"));

        var beforeRevision = handler.PlatformView.LocalStructureRevision;
        Grid.SetRow(second, 1);
        handler.Update(1, second);

        Assert.Equal(0, handler.PlatformView.LocalStructureRevision - beforeRevision);
        Assert.Equal("2", secondPlatform.GetStyleValue("gridRowStart"));
    }

    [Fact]
    public void LayoutHandler_UpdateZIndex_ReordersChildrenUsingLayoutOrdering()
    {
        var layout = new VerticalStackLayout();
        var first = new Label { Text = "First", AutomationId = "layout-test-first", ZIndex = 0 };
        var second = new Label { Text = "Second", AutomationId = "layout-test-second", ZIndex = 0 };
        var third = new Label { Text = "Third", AutomationId = "layout-test-third", ZIndex = 0 };
        layout.Children.Add(first);
        layout.Children.Add(second);
        layout.Children.Add(third);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(layout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(layout);

        second.SetValue(VisualElement.ZIndexProperty, 10);
        handler.UpdateZIndex(second);

        Assert.Equal(
            ["layout-test-first", "layout-test-third", "layout-test-second"],
            handler.PlatformView.Children.Select(static child => child.GetPropertyString("AutomationId")).ToArray());
        var secondPlatform = GetPlatformView(second);
        Assert.Equal("10", secondPlatform.GetStyleValue("zIndex"));
    }

    [Fact]
    public void LayoutHandler_AbsoluteLayout_BoundsMutationUpdatesGeometryOnArrange()
    {
        var absoluteLayout = new AbsoluteLayout();
        var child = new BoxView { Color = Colors.Red };
        absoluteLayout.Children.Add(child);

        AbsoluteLayout.SetLayoutBounds(child, new Rect(20, 30, 80, 60));
        AbsoluteLayout.SetLayoutFlags(child, AbsoluteLayoutFlags.None);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(absoluteLayout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(absoluteLayout);

        handler.PlatformArrange(new Rect(0, 0, 400, 300));
        var childPlatform = GetPlatformView(child);

        Assert.Equal("20px", childPlatform.GetStyleValue("left"));
        Assert.Equal("30px", childPlatform.GetStyleValue("top"));
        Assert.Equal("80px", childPlatform.GetStyleValue("width"));
        Assert.Equal("60px", childPlatform.GetStyleValue("height"));

        AbsoluteLayout.SetLayoutBounds(child, new Rect(120, 90, 140, 100));
        handler.PlatformArrange(new Rect(0, 0, 400, 300));

        Assert.Equal("120px", childPlatform.GetStyleValue("left"));
        Assert.Equal("90px", childPlatform.GetStyleValue("top"));
        Assert.Equal("140px", childPlatform.GetStyleValue("width"));
        Assert.Equal("100px", childPlatform.GetStyleValue("height"));
    }

    [Fact]
    public void LayoutHandler_AbsoluteLayout_FlagMutationReprojectsGeometryOnArrange()
    {
        var absoluteLayout = new AbsoluteLayout();
        var child = new BoxView { Color = Colors.Red };
        absoluteLayout.Children.Add(child);

        AbsoluteLayout.SetLayoutBounds(child, new Rect(20, 30, 80, 60));
        AbsoluteLayout.SetLayoutFlags(child, AbsoluteLayoutFlags.None);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(absoluteLayout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(absoluteLayout);
        handler.PlatformArrange(new Rect(0, 0, 400, 300));

        var childPlatform = GetPlatformView(child);
        var initialLeft = ParsePx(childPlatform.GetStyleValue("left"));
        var initialTop = ParsePx(childPlatform.GetStyleValue("top"));
        var initialWidth = ParsePx(childPlatform.GetStyleValue("width"));
        var initialHeight = ParsePx(childPlatform.GetStyleValue("height"));

        AbsoluteLayout.SetLayoutBounds(child, new Rect(0.5, 0.5, 0.5, 0.5));
        AbsoluteLayout.SetLayoutFlags(child, AbsoluteLayoutFlags.All);
        handler.PlatformArrange(new Rect(0, 0, 400, 300));

        var updatedLeft = ParsePx(childPlatform.GetStyleValue("left"));
        var updatedTop = ParsePx(childPlatform.GetStyleValue("top"));
        var updatedWidth = ParsePx(childPlatform.GetStyleValue("width"));
        var updatedHeight = ParsePx(childPlatform.GetStyleValue("height"));

        Assert.NotEqual(initialLeft, updatedLeft);
        Assert.NotEqual(initialTop, updatedTop);
        Assert.NotEqual(initialWidth, updatedWidth);
        Assert.NotEqual(initialHeight, updatedHeight);
    }

    [Fact]
    public void LayoutHandler_AbsoluteLayout_AutoSizeUsesResolvedSizeFallback()
    {
        var absoluteLayout = new AbsoluteLayout();
        var boxView = new BoxView
        {
            Color = Colors.Orange,
            WidthRequest = 92,
            HeightRequest = 38
        };

        absoluteLayout.Children.Add(boxView);
        AbsoluteLayout.SetLayoutBounds(boxView, new Rect(12, 16, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(boxView, AbsoluteLayoutFlags.None);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(absoluteLayout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(absoluteLayout);
        handler.PlatformArrange(new Rect(0, 0, 500, 240));

        var boxPlatform = GetPlatformView(boxView);
        var width = ParsePx(boxPlatform.GetStyleValue("width"));
        var height = ParsePx(boxPlatform.GetStyleValue("height"));

        Assert.Equal(92, width, 3);
        Assert.Equal(38, height, 3);
    }

    [Fact]
    public void LayoutHandler_InputTransparent_MapsPointerEventsDeterministically()
    {
        var layout = new VerticalStackLayout
        {
            InputTransparent = false
        };

        var handler = CreateHandler(out _);
        ((IElementHandler)handler).SetVirtualView(layout);

        Assert.Null(handler.PlatformView.GetStyleValue("pointerEvents"));

        layout.InputTransparent = true;
        MirageLayoutHandler.Mapper.UpdateProperty(handler, layout, nameof(IView.InputTransparent));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("pointerEvents"));

        layout.InputTransparent = false;
        MirageLayoutHandler.Mapper.UpdateProperty(handler, layout, nameof(IView.InputTransparent));
        Assert.Null(handler.PlatformView.GetStyleValue("pointerEvents"));
    }

    [Fact]
    public void LayoutHandler_FlexLayout_ProjectsContainerAndAttachedItemStylesIncrementally()
    {
        var layout = new TestFlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            JustifyContent = FlexJustify.SpaceBetween,
            AlignItems = FlexAlignItems.Center,
            AlignContent = FlexAlignContent.SpaceAround,
            WidthRequest = 420,
            HeightRequest = 260
        };

        var first = new Label { Text = "First" };
        var second = new Label { Text = "Second" };
        layout.Children.Add(first);
        layout.Children.Add(second);

        FlexLayout.SetOrder(second, 3);
        FlexLayout.SetGrow(second, 2f);
        FlexLayout.SetShrink(second, 4f);
        FlexLayout.SetBasis(second, new FlexBasis(0.5f, true));
        FlexLayout.SetAlignSelf(second, FlexAlignSelf.End);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(layout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(layout);

        Assert.Equal(ComponentKind.Layout, handler.PlatformView.ControlKind);
        Assert.Equal("flex", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("row", handler.PlatformView.GetStyleValue("flexDirection"));
        Assert.Equal("wrap", handler.PlatformView.GetStyleValue("flexWrap"));
        Assert.Equal("space-between", handler.PlatformView.GetStyleValue("justifyContent"));
        Assert.Equal("center", handler.PlatformView.GetStyleValue("alignItems"));
        Assert.Equal("space-around", handler.PlatformView.GetStyleValue("alignContent"));

        var secondPlatform = GetPlatformView(second);
        Assert.Equal("3", secondPlatform.GetStyleValue("order"));
        Assert.Equal("2", secondPlatform.GetStyleValue("flexGrow"));
        Assert.Equal("4", secondPlatform.GetStyleValue("flexShrink"));
        Assert.Equal("50%", secondPlatform.GetStyleValue("flexBasis"));
        Assert.Equal("flex-end", secondPlatform.GetStyleValue("alignSelf"));

        layout.Direction = FlexDirection.ColumnReverse;
        layout.Wrap = FlexWrap.Reverse;
        layout.JustifyContent = FlexJustify.SpaceEvenly;
        layout.AlignItems = FlexAlignItems.End;
        layout.AlignContent = FlexAlignContent.Start;

        MirageLayoutHandler.Mapper.UpdateProperty(handler, layout, nameof(FlexLayout.Direction));
        MirageLayoutHandler.Mapper.UpdateProperty(handler, layout, nameof(FlexLayout.Wrap));
        MirageLayoutHandler.Mapper.UpdateProperty(handler, layout, nameof(FlexLayout.JustifyContent));
        MirageLayoutHandler.Mapper.UpdateProperty(handler, layout, nameof(FlexLayout.AlignItems));
        MirageLayoutHandler.Mapper.UpdateProperty(handler, layout, nameof(FlexLayout.AlignContent));

        Assert.Equal("column-reverse", handler.PlatformView.GetStyleValue("flexDirection"));
        Assert.Equal("wrap-reverse", handler.PlatformView.GetStyleValue("flexWrap"));
        Assert.Equal("space-evenly", handler.PlatformView.GetStyleValue("justifyContent"));
        Assert.Equal("flex-end", handler.PlatformView.GetStyleValue("alignItems"));
        Assert.Equal("flex-start", handler.PlatformView.GetStyleValue("alignContent"));

        var beforeRevision = handler.PlatformView.LocalStructureRevision;
        FlexLayout.SetOrder(second, 1);
        FlexLayout.SetGrow(second, 0.5f);
        FlexLayout.SetShrink(second, 0.25f);
        FlexLayout.SetBasis(second, FlexBasis.Auto);
        FlexLayout.SetAlignSelf(second, FlexAlignSelf.Auto);

        Assert.Equal(beforeRevision, handler.PlatformView.LocalStructureRevision);
        Assert.Equal("1", secondPlatform.GetStyleValue("order"));
        Assert.Equal("0.5", secondPlatform.GetStyleValue("flexGrow"));
        Assert.Equal("0.25", secondPlatform.GetStyleValue("flexShrink"));
        Assert.Equal("auto", secondPlatform.GetStyleValue("flexBasis"));
        Assert.Null(secondPlatform.GetStyleValue("alignSelf"));
    }

    [Fact]
    public void FlexLayout_Arrange_DoesNotApplyCustomAbsoluteFrames()
    {
        var layout = new TestFlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            JustifyContent = FlexJustify.SpaceBetween,
            AlignItems = FlexAlignItems.Center,
            AlignContent = FlexAlignContent.SpaceAround,
            WidthRequest = 420,
            HeightRequest = 260
        };

        var first = new Label { Text = "First" };
        var second = new Label { Text = "Second" };
        layout.Children.Add(first);
        layout.Children.Add(second);

        FlexLayout.SetOrder(second, 5);
        FlexLayout.SetGrow(second, 2f);
        FlexLayout.SetShrink(second, 1.5f);
        FlexLayout.SetBasis(second, new FlexBasis(120f, false));
        FlexLayout.SetAlignSelf(second, FlexAlignSelf.End);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(layout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(layout);

        var secondPlatform = GetPlatformView(second);
        var preArrangeOrder = secondPlatform.GetStyleValue("order");
        var preArrangeGrow = secondPlatform.GetStyleValue("flexGrow");
        var preArrangeShrink = secondPlatform.GetStyleValue("flexShrink");
        var preArrangeBasis = secondPlatform.GetStyleValue("flexBasis");
        var preArrangeAlignSelf = secondPlatform.GetStyleValue("alignSelf");

        handler.PlatformArrange(new Rect(0, 0, 420, 260));

        var firstPlatform = GetPlatformView(first);
        Assert.Equal("relative", firstPlatform.GetStyleValue("position"));
        Assert.Null(firstPlatform.GetStyleValue("left"));
        Assert.Null(firstPlatform.GetStyleValue("top"));
        Assert.Null(firstPlatform.GetStyleValue("width"));
        Assert.Null(firstPlatform.GetStyleValue("height"));

        Assert.Equal(preArrangeOrder, secondPlatform.GetStyleValue("order"));
        Assert.Equal(preArrangeGrow, secondPlatform.GetStyleValue("flexGrow"));
        Assert.Equal(preArrangeShrink, secondPlatform.GetStyleValue("flexShrink"));
        Assert.Equal(preArrangeBasis, secondPlatform.GetStyleValue("flexBasis"));
        Assert.Equal(preArrangeAlignSelf, secondPlatform.GetStyleValue("alignSelf"));

        Assert.Equal("flex", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("row", handler.PlatformView.GetStyleValue("flexDirection"));
        Assert.Equal("wrap", handler.PlatformView.GetStyleValue("flexWrap"));
        Assert.Equal("space-between", handler.PlatformView.GetStyleValue("justifyContent"));
        Assert.Equal("center", handler.PlatformView.GetStyleValue("alignItems"));
        Assert.Equal("space-around", handler.PlatformView.GetStyleValue("alignContent"));
    }

    [Fact]
    public void FlexLayout_ChildLayoutAlignmentRemaps_ReapplyFlexItemProjection()
    {
        var layout = new TestFlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            JustifyContent = FlexJustify.SpaceBetween,
            AlignItems = FlexAlignItems.Center,
            AlignContent = FlexAlignContent.SpaceAround,
            WidthRequest = 420,
            HeightRequest = 260
        };

        var child = new Label
        {
            Text = "Child",
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            WidthRequest = 80,
            HeightRequest = 40
        };

        layout.Children.Add(child);
        FlexLayout.SetAlignSelf(child, FlexAlignSelf.End);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(layout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(layout);

        var childPlatform = GetPlatformView(child);
        Assert.Equal("flex-end", childPlatform.GetStyleValue("alignSelf"));

        var beforeRevision = handler.PlatformView.LocalStructureRevision;

        child.WidthRequest = 120;
        Assert.Equal("flex-end", childPlatform.GetStyleValue("alignSelf"));

        child.HeightRequest = 64;
        Assert.Equal("flex-end", childPlatform.GetStyleValue("alignSelf"));

        child.HorizontalOptions = LayoutOptions.Center;
        Assert.Equal("flex-end", childPlatform.GetStyleValue("alignSelf"));

        child.VerticalOptions = LayoutOptions.End;
        Assert.Equal("flex-end", childPlatform.GetStyleValue("alignSelf"));

        Assert.Equal(beforeRevision, handler.PlatformView.LocalStructureRevision);
    }

    [Fact]
    public void FlexLayout_ContainerMapperUpdates_PreserveParentAssignedPositioning()
    {
        var parent = new AbsoluteLayout
        {
            WidthRequest = 500,
            HeightRequest = 300
        };

        var flex = new TestFlexLayout
        {
            Direction = FlexDirection.Row,
            WidthRequest = 180,
            HeightRequest = 90
        };
        flex.Children.Add(new Label { Text = "Child" });

        parent.Children.Add(flex);
        AbsoluteLayout.SetLayoutBounds(flex, new Rect(25, 35, 180, 90));
        AbsoluteLayout.SetLayoutFlags(flex, AbsoluteLayoutFlags.None);

        var parentHandler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(parent, mauiContext);
        ((IElementHandler)parentHandler).SetVirtualView(parent);
        parentHandler.PlatformArrange(new Rect(0, 0, 500, 300));

        var flexPlatform = GetPlatformView(flex);
        Assert.Equal("absolute", flexPlatform.GetStyleValue("position"));
        var initialLeft = flexPlatform.GetStyleValue("left");
        var initialTop = flexPlatform.GetStyleValue("top");
        var initialWidth = flexPlatform.GetStyleValue("width");
        var initialHeight = flexPlatform.GetStyleValue("height");
        Assert.NotNull(initialLeft);
        Assert.NotNull(initialTop);
        Assert.NotNull(initialWidth);
        Assert.NotNull(initialHeight);

        flex.Direction = FlexDirection.Column;
        flex.Wrap = FlexWrap.Wrap;
        flex.JustifyContent = FlexJustify.SpaceBetween;

        Assert.Equal("absolute", flexPlatform.GetStyleValue("position"));
        Assert.Equal(initialLeft, flexPlatform.GetStyleValue("left"));
        Assert.Equal(initialTop, flexPlatform.GetStyleValue("top"));
        Assert.Equal(initialWidth, flexPlatform.GetStyleValue("width"));
        Assert.Equal(initialHeight, flexPlatform.GetStyleValue("height"));
    }

    [Fact]
    public void CustomLayout_Arrange_StillAppliesAbsoluteArrangedFrames()
    {
        var layout = new FrameDrivenTestLayout();
        var child = new BoxView { Color = Colors.CadetBlue };
        layout.Children.Add(child);

        var handler = CreateHandler(out var mauiContext);
        AttachLayoutChildHandlers(layout, mauiContext);
        ((IElementHandler)handler).SetVirtualView(layout);

        handler.PlatformArrange(new Rect(0, 0, 300, 200));

        var childPlatform = GetPlatformView(child);
        Assert.Equal("absolute", childPlatform.GetStyleValue("position"));
        Assert.Equal("17px", childPlatform.GetStyleValue("left"));
        Assert.Equal("23px", childPlatform.GetStyleValue("top"));
        Assert.Equal("80px", childPlatform.GetStyleValue("width"));
        Assert.Equal("40px", childPlatform.GetStyleValue("height"));
    }

    [Fact]
    public void LayoutOrderingHelpers_UseStableZIndexTieSemantics()
    {
        var layout = new VerticalStackLayout();
        var first = new Label { Text = "First", ZIndex = 0 };
        var second = new Label { Text = "Second", ZIndex = 1 };
        var third = new Label { Text = "Third", ZIndex = 1 };
        var fourth = new Label { Text = "Fourth", ZIndex = 2 };

        layout.Children.Add(first);
        layout.Children.Add(second);
        layout.Children.Add(third);
        layout.Children.Add(fourth);

        var ordered = MirageLayoutHandlerOrdering.OrderByZIndex(layout).ToArray();
        Assert.Same(first, ordered[0]);
        Assert.Same(second, ordered[1]);
        Assert.Same(third, ordered[2]);
        Assert.Same(fourth, ordered[3]);

        Assert.Equal(0, MirageLayoutHandlerOrdering.GetLayoutHandlerIndex(layout, first));
        Assert.Equal(1, MirageLayoutHandlerOrdering.GetLayoutHandlerIndex(layout, second));
        Assert.Equal(2, MirageLayoutHandlerOrdering.GetLayoutHandlerIndex(layout, third));
        Assert.Equal(3, MirageLayoutHandlerOrdering.GetLayoutHandlerIndex(layout, fourth));
    }

    private static MirageLayoutHandler CreateHandler(out MauiContext mauiContext)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        mauiContext = new MauiContext(services);
        var handler = new MirageLayoutHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        return handler;
    }

    private static void AttachLayoutChildHandlers(Microsoft.Maui.ILayout layout, MauiContext mauiContext)
    {
        foreach (var child in layout)
        {
            AttachChildHandler(child, mauiContext);
        }
    }

    private static void AttachChildHandler(IView child, MauiContext mauiContext)
    {
        if (child is not IElement element || element.Handler is not null)
        {
            return;
        }

        IElementHandler childHandler = child switch
        {
            Label => new MirageLabelHandler(),
            BoxView => new MirageBoxViewHandler(),
            Layout layout => CreateLayoutChildHandler(layout, mauiContext),
            _ => throw new InvalidOperationException($"No test child-handler binding registered for '{child.GetType().Name}'.")
        };

        ((IElementHandler)childHandler).SetMauiContext(mauiContext);
        ((IElementHandler)childHandler).SetVirtualView(element);
    }

    private static MirageLayoutHandler CreateLayoutChildHandler(Layout layout, MauiContext mauiContext)
    {
        var layoutHandler = new MirageLayoutHandler();
        ((IElementHandler)layoutHandler).SetMauiContext(mauiContext);
        AttachLayoutChildHandlers(layout, mauiContext);
        return layoutHandler;
    }

    private static MiragePlatformView GetPlatformView(View view)
    {
        Assert.NotNull(view.Handler);
        return Assert.IsType<MiragePlatformView>(view.Handler!.PlatformView);
    }

    private static double ParsePx(string? value)
    {
        Assert.False(string.IsNullOrWhiteSpace(value));
        Assert.EndsWith("px", value, StringComparison.Ordinal);
        return double.Parse(value.AsSpan(0, value.Length - 2), System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class FrameDrivenTestLayout : Layout
    {
        protected override ILayoutManager CreateLayoutManager()
        {
            return new FrameDrivenTestLayoutManager(this);
        }

        private sealed class FrameDrivenTestLayoutManager : Microsoft.Maui.Layouts.LayoutManager
        {
            private readonly FrameDrivenTestLayout _layout;

            public FrameDrivenTestLayoutManager(FrameDrivenTestLayout layout)
                : base(layout)
            {
                _layout = layout;
            }

            public override Size ArrangeChildren(Rect bounds)
            {
                foreach (View child in _layout)
                {
                    if (!child.IsVisible)
                    {
                        continue;
                    }

                    ((IView)child).Arrange(new Rect(17, 23, 80, 40));
                }

                return bounds.Size;
            }

            public override Size Measure(double widthConstraint, double heightConstraint)
            {
                var width = double.IsFinite(widthConstraint) ? Math.Max(0, widthConstraint) : 320;
                var height = double.IsFinite(heightConstraint) ? Math.Max(0, heightConstraint) : 200;
                return new Size(width, height);
            }
        }
    }

    private sealed class TestFlexLayout : FlexLayout
    {
        protected override ILayoutManager CreateLayoutManager()
        {
            return new TestFlexLayoutManager(this);
        }

        private sealed class TestFlexLayoutManager : Microsoft.Maui.Layouts.LayoutManager
        {
            private readonly TestFlexLayout _layout;

            public TestFlexLayoutManager(TestFlexLayout layout)
                : base(layout)
            {
                _layout = layout;
            }

            public override Size ArrangeChildren(Rect bounds)
            {
                var x = 0d;
                foreach (View child in _layout)
                {
                    if (!child.IsVisible)
                    {
                        continue;
                    }

                    ((IView)child).Arrange(new Rect(x, 0, 80, 40));
                    x += 82;
                }

                return bounds.Size;
            }

            public override Size Measure(double widthConstraint, double heightConstraint)
            {
                var width = double.IsFinite(widthConstraint) ? Math.Max(0, widthConstraint) : 320;
                var height = double.IsFinite(heightConstraint) ? Math.Max(0, heightConstraint) : 200;
                return new Size(width, height);
            }
        }
    }

}
