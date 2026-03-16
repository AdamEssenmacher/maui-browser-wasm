using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Maui.Images;
using Mirage.Web.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class HandlerRuntimeIntegrationTests
{
    [Fact]
    public void CommonMapper_MapsAutomationMarginVisibilityAndPaddingThroughHandler()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "flex");
        var view = new Label
        {
            AutomationId = "mapped-id",
            Margin = new Thickness(1, 2, 3, 4),
            IsVisible = false,
            Padding = new Thickness(5, 6, 7, 8)
        };

        MirageViewMapper.MapAutomationId(handler, view);
        MirageViewMapper.MapMargin(handler, view);
        MirageViewMapper.MapVisibility(handler, view);
        MirageViewMapper.MapPadding(handler, view);

        Assert.Equal("mapped-id", handler.Node.GetPropertyString("AutomationId"));
        Assert.Equal("2px 3px 4px 1px", handler.Node.Styles["margin"]);
        Assert.Equal("none", handler.Node.Styles["display"]);
        Assert.Equal("6px 7px 8px 5px", handler.Node.Styles["padding"]);
        Assert.Equal(4, handler.RenderRequestCount);
    }

    [Fact]
    public void CommonMapper_ReapplyingSameValues_IsIdempotent()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new Label
        {
            AutomationId = "stable-id",
            Margin = new Thickness(4, 3, 2, 1),
            IsVisible = true,
            Padding = new Thickness(2)
        };

        MirageViewMapper.MapAutomationId(handler, view);
        MirageViewMapper.MapMargin(handler, view);
        MirageViewMapper.MapVisibility(handler, view);
        MirageViewMapper.MapPadding(handler, view);
        var firstRenderCount = handler.RenderRequestCount;

        MirageViewMapper.MapAutomationId(handler, view);
        MirageViewMapper.MapMargin(handler, view);
        MirageViewMapper.MapVisibility(handler, view);
        MirageViewMapper.MapPadding(handler, view);

        Assert.Equal(firstRenderCount, handler.RenderRequestCount);
    }

    [Fact]
    public void CommonMapper_ThicknessFormatting_IsDeterministic()
    {
        Assert.Equal("0px", MirageViewMapper.ToPx(-15));
        Assert.Equal("0px", MirageViewMapper.ToPx(double.NaN));
        Assert.Equal("12.345px", MirageViewMapper.ToPx(12.3454));
        Assert.Equal("2px 8.5px 0px 1.25px", MirageCommonViewProjection.ToPaddingThickness(new Thickness(1.25, 2, 8.5, -3)));
        Assert.Equal("2px 8.5px -3px 1.25px", MirageCommonViewProjection.ToMarginThickness(new Thickness(1.25, 2, 8.5, -3)));
    }

    [Fact]
    public void CommonMapper_MapsOpacityBackgroundAndSizeRequestConstraints()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new Label
        {
            Opacity = 0.42,
            WidthRequest = 120,
            HeightRequest = 48,
            MinimumWidthRequest = 40,
            MinimumHeightRequest = 24,
            MaximumWidthRequest = 320,
            MaximumHeightRequest = 220,
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.CornflowerBlue, 0),
                    new GradientStop(Colors.MediumPurple, 1)
                },
                new Point(0, 0),
                new Point(1, 1))
        };

        MirageViewMapper.MapOpacity(handler, view);
        MirageViewMapper.MapBackground(handler, view);
        MirageViewMapper.MapSizeRequests(handler, view);

        Assert.Equal("0.42", handler.Node.GetStyleValue("opacity"));
        Assert.Equal("120px", handler.Node.GetStyleValue("width"));
        Assert.Equal("48px", handler.Node.GetStyleValue("height"));
        Assert.Equal("40px", handler.Node.GetStyleValue("minWidth"));
        Assert.Equal("24px", handler.Node.GetStyleValue("minHeight"));
        Assert.Equal("320px", handler.Node.GetStyleValue("maxWidth"));
        Assert.Equal("220px", handler.Node.GetStyleValue("maxHeight"));
        Assert.NotNull(handler.Node.GetStyleValue("backgroundImage"));
    }

    [Fact]
    public void CommonMapper_MapsTextStylePrimitives_AndIsIdempotent()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var label = new Label
        {
            TextColor = Colors.Red,
            CharacterSpacing = 1.5
        };

        label.FontFamily = "Space Grotesk";
        label.FontSize = 22;
        label.FontAttributes = FontAttributes.Bold;

        MirageViewMapper.MapTextColor(handler, label);
        MirageViewMapper.MapFont(handler, label);
        MirageViewMapper.MapCharacterSpacing(handler, label);

        Assert.Equal("rgba(255,0,0,1)", handler.Node.GetStyleValue("color"));
        Assert.Equal("22px", handler.Node.GetStyleValue("fontSize"));
        Assert.Equal("700", handler.Node.GetStyleValue("fontWeight"));
        Assert.Equal("normal", handler.Node.GetStyleValue("fontStyle"));
        Assert.Equal("\"Space Grotesk\", sans-serif", handler.Node.GetStyleValue("fontFamily"));
        Assert.Equal("1.5px", handler.Node.GetStyleValue("letterSpacing"));

        var firstRenderCount = handler.RenderRequestCount;
        MirageViewMapper.MapTextColor(handler, label);
        MirageViewMapper.MapFont(handler, label);
        MirageViewMapper.MapCharacterSpacing(handler, label);
        Assert.Equal(firstRenderCount, handler.RenderRequestCount);

        label.FontSize = -1;
        MirageViewMapper.MapFont(handler, label);
        Assert.Equal("14px", handler.Node.GetStyleValue("fontSize"));
    }

    [Fact]
    public void LabelHandler_DefaultsToSystemTextFallbacks_WhenUnset()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageLabelHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var label = new Label
        {
            Text = "Parity"
        };

        ((IElementHandler)handler).SetVirtualView(label);
        Assert.Equal("CanvasText", handler.PlatformView.GetStyleValue("color"));
        Assert.Equal("system-ui", handler.PlatformView.GetStyleValue("fontFamily"));

        label.TextColor = Colors.Red;
        label.FontFamily = "OpenSansRegular";
        MirageLabelHandler.Mapper.UpdateProperty(handler, label, nameof(ITextStyle.TextColor));
        MirageLabelHandler.Mapper.UpdateProperty(handler, label, "FontFamily");

        Assert.Equal("rgba(255,0,0,1)", handler.PlatformView.GetStyleValue("color"));
        Assert.Equal("\"OpenSansRegular\", system-ui, sans-serif", handler.PlatformView.GetStyleValue("fontFamily"));
    }

    [Fact]
    public void LabelHandler_MapsHorizontalAndVerticalTextAlignment()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageLabelHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var label = new Label
        {
            Text = "Aligned",
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center
        };

        ((IElementHandler)handler).SetVirtualView(label);
        Assert.Equal("left", handler.PlatformView.GetStyleValue("textAlign"));
        Assert.Equal("center", handler.PlatformView.GetStyleValue("justifyContent"));
        Assert.Equal("flex", handler.PlatformView.GetStyleValue("display"));

        label.HorizontalTextAlignment = TextAlignment.End;
        label.VerticalTextAlignment = TextAlignment.End;
        MirageLabelHandler.Mapper.UpdateProperty(handler, label, nameof(ITextAlignment.HorizontalTextAlignment));
        MirageLabelHandler.Mapper.UpdateProperty(handler, label, nameof(ITextAlignment.VerticalTextAlignment));

        Assert.Equal("right", handler.PlatformView.GetStyleValue("textAlign"));
        Assert.Equal("flex-end", handler.PlatformView.GetStyleValue("justifyContent"));
    }

    [Fact]
    public void CommonMapper_CompatibilityAliases_MapBackgroundColorAndFontProperties()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var label = new Label
        {
            BackgroundColor = Color.FromArgb("#512BD4"),
            FontFamily = "OpenSansRegular",
            FontSize = 19
        };

        MirageViewMapper.CommonViewMapper.UpdateProperty(handler, label, "BackgroundColor");
        MirageViewMapper.CommonViewMapper.UpdateProperty(handler, label, "FontFamily");
        MirageViewMapper.CommonViewMapper.UpdateProperty(handler, label, "FontSize");

        Assert.Equal("rgba(81,43,212,1)", handler.Node.GetStyleValue("backgroundColor"));
        Assert.Equal("\"OpenSansRegular\", sans-serif", handler.Node.GetStyleValue("fontFamily"));
        Assert.Equal("19px", handler.Node.GetStyleValue("fontSize"));
    }

    [Fact]
    public void CommonMapper_MapMargin_PreservesNegativeThicknessValues()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var image = new Image
        {
            Margin = new Thickness(-20)
        };

        MirageViewMapper.MapMargin(handler, image);

        Assert.Equal("-20px -20px -20px -20px", handler.Node.GetStyleValue("margin"));
    }

    [Fact]
    public void CommonMapper_MapBackground_UsesVisualElementBackgroundColorFallback()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var page = new ContentPage
        {
            BackgroundColor = Colors.White
        };

        MirageViewMapper.MapBackground(handler, page);

        Assert.Equal("rgba(255,255,255,1)", handler.Node.GetStyleValue("backgroundColor"));
        Assert.Null(handler.Node.GetStyleValue("backgroundImage"));
    }

    [Fact]
    public void CommonMapper_MapStyle_ReappliesCompatibilityAliasProperties()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageLabelHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var label = new Label
        {
            BackgroundColor = Color.FromArgb("#512BD4"),
            TextColor = Colors.White,
            FontFamily = "OpenSansRegular",
            FontSize = 17
        };

        ((IElementHandler)handler).SetVirtualView(label);
        MirageViewMapper.MapStyle(handler, label);

        Assert.Equal("rgba(81,43,212,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
        Assert.Equal("rgba(255,255,255,1)", handler.PlatformView.GetStyleValue("color"));
        Assert.Equal("\"OpenSansRegular\", system-ui, sans-serif", handler.PlatformView.GetStyleValue("fontFamily"));
        Assert.Equal("17px", handler.PlatformView.GetStyleValue("fontSize"));
    }

    [Fact]
    public void ButtonHandler_CompatibilityAliases_MapBackgroundColorAndTextColorToFluentTokens()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var button = new Button
        {
            Text = "Token Button",
            BackgroundColor = Color.FromArgb("#512BD4"),
            TextColor = Colors.White,
            CornerRadius = 8
        };

        ((IElementHandler)handler).SetVirtualView(button);

        MirageButtonHandler.Mapper.UpdateProperty(handler, button, "BackgroundColor");
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, "TextColor");
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, "CornerRadius");

        Assert.Equal("rgba(81,43,212,1)", handler.PlatformView.GetStyleValue("--accent-fill-rest"));
        Assert.Equal("rgba(255,255,255,1)", handler.PlatformView.GetStyleValue("--foreground-on-accent-rest"));
        Assert.Equal("8px", handler.PlatformView.GetStyleValue("--control-corner-radius"));
        Assert.Null(handler.PlatformView.GetStyleValue("backgroundColor"));
        Assert.Null(handler.PlatformView.GetStyleValue("borderRadius"));
    }

    [Fact]
    public void ButtonHandler_GradientBackground_UsesCommonProjectionAndClearsFluentFillTokens()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var button = new Button
        {
            Text = "Gradient Button",
            BackgroundColor = Color.FromArgb("#512BD4")
        };

        ((IElementHandler)handler).SetVirtualView(button);
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, "BackgroundColor");

        Assert.Equal("rgba(81,43,212,1)", handler.PlatformView.GetStyleValue("--accent-fill-rest"));

        button.Background = new LinearGradientBrush(
            new GradientStopCollection
            {
                new(Color.FromArgb("#512BD4"), 0),
                new(Color.FromArgb("#0EA5E9"), 1)
            },
            new Point(0, 0),
            new Point(1, 1));

        MirageButtonHandler.MapBackground(handler, button);

        var backgroundImage = handler.PlatformView.GetStyleValue("backgroundImage");
        Assert.NotNull(backgroundImage);
        Assert.True(
            backgroundImage.Contains("linear-gradient(", StringComparison.Ordinal)
            || backgroundImage.StartsWith("url(", StringComparison.Ordinal),
            $"Expected gradient projection background image, found '{backgroundImage}'.");
        Assert.Equal("100% 100%", handler.PlatformView.GetStyleValue("backgroundSize"));
        Assert.Null(handler.PlatformView.GetStyleValue("--accent-fill-rest"));
        Assert.Null(handler.PlatformView.GetStyleValue("--neutral-fill-rest"));
    }

    [Fact]
    public void EntryHandler_SizeRequestProjection_UpdatesFluentControlHeightToken()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageEntryHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var entry = new Entry
        {
            Text = "Entry"
        };

        ((IElementHandler)handler).SetVirtualView(entry);
        Assert.Null(handler.PlatformView.GetStyleValue("--base-height-multiplier"));

        entry.MinimumHeightRequest = 44;
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(VisualElement.MinimumHeightRequest));

        Assert.Equal("44px", handler.PlatformView.GetStyleValue("minHeight"));
        Assert.Equal("11", handler.PlatformView.GetStyleValue("--base-height-multiplier"));

        entry.MinimumHeightRequest = -1;
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(VisualElement.MinimumHeightRequest));
        Assert.Null(handler.PlatformView.GetStyleValue("--base-height-multiplier"));
    }

    [Fact]
    public void ImageHandler_MapsSourceAspectAndOpaqueState()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageImageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var image = new Image
        {
            Source = "coffee.png",
            Aspect = Aspect.AspectFill,
            IsOpaque = true
        };

        ((IElementHandler)handler).SetVirtualView(image);

        Assert.Equal("/_mirage/assets/test/coffee.png", handler.PlatformView.GetPropertyString("Source"));
        Assert.Equal("file", handler.PlatformView.GetPropertyString("SourceKind"));
        Assert.Equal("loading", handler.PlatformView.GetPropertyString("SourceState"));
        Assert.Equal("true", handler.PlatformView.GetPropertyString("IsOpaque"));
        Assert.Equal("cover", handler.PlatformView.GetStyleValue("objectFit"));

        image.Source = new UriImageSource { Uri = new Uri("https://example.com/photo.png") };
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(Image.Source));

        Assert.Equal("https://example.com/photo.png", handler.PlatformView.GetPropertyString("Source"));
        Assert.Equal("uri", handler.PlatformView.GetPropertyString("SourceKind"));
    }

    [Fact]
    public void ImageHandler_RespectsRequestedDimensions_AndFallsBackToFillWhenUnset()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageImageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var image = new Image
        {
            Source = "coffee.png",
            WidthRequest = 100,
            HeightRequest = 100
        };

        ((IElementHandler)handler).SetVirtualView(image);
        Assert.Equal("100px", handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("100px", handler.PlatformView.GetStyleValue("height"));

        image.WidthRequest = -1;
        image.HeightRequest = -1;
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(VisualElement.WidthRequest));
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(VisualElement.HeightRequest));

        Assert.Equal("100%", handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("100%", handler.PlatformView.GetStyleValue("height"));
    }

    [Fact]
    public void ImageHandler_UnresolvedFileSource_ProjectsUnsupportedState()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new RejectingImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageImageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var image = new Image
        {
            Source = "missing.png"
        };

        ((IElementHandler)handler).SetVirtualView(image);

        Assert.Null(handler.PlatformView.GetPropertyString("Source"));
        Assert.Equal("unsupported", handler.PlatformView.GetPropertyString("SourceKind"));
        Assert.Equal("unsupported", handler.PlatformView.GetPropertyString("SourceState"));
    }

    [Fact]
    public void BorderHandler_MapsStrokeAndHostsPresentedContent()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageBorderHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var border = new Border
        {
            Padding = new Thickness(8),
            Stroke = new SolidColorBrush(Colors.DarkGray),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(5)
            }
        };

        ((IElementHandler)handler).SetVirtualView(border);

        Assert.Equal("solid", handler.PlatformView.GetStyleValue("borderStyle"));
        Assert.Equal("rgba(169,169,169,1)", handler.PlatformView.GetStyleValue("borderColor"));
        Assert.Equal("2px", handler.PlatformView.GetStyleValue("borderWidth"));
        Assert.Equal("5px 5px 5px 5px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Equal("8px 8px 8px 8px", handler.PlatformView.GetStyleValue("padding"));
        Assert.Equal("hidden", handler.PlatformView.GetStyleValue("overflow"));

        border.StrokeDashArray = new DoubleCollection { 2, 1 };
        MirageBorderHandler.Mapper.UpdateProperty(handler, border, nameof(Border.StrokeDashArray));
        Assert.Equal("dashed", handler.PlatformView.GetStyleValue("borderStyle"));

        border.Stroke = null;
        MirageBorderHandler.Mapper.UpdateProperty(handler, border, nameof(Border.Stroke));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("borderStyle"));
        Assert.Null(handler.PlatformView.GetStyleValue("borderColor"));
    }

    [Fact]
    public void BoxViewHandler_UsesBackgroundColorUnlessColorPropertyIsExplicitlySet()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageBoxViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var boxView = new BoxView
        {
            BackgroundColor = Colors.DarkGray
        };

        ((IElementHandler)handler).SetVirtualView(boxView);
        Assert.Equal("rgba(169,169,169,1)", handler.PlatformView.GetStyleValue("backgroundColor"));

        boxView.Color = Colors.Crimson;
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(BoxView.Color));
        Assert.Equal("rgba(220,20,60,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
    }

    [Fact]
    public void LayoutHandler_StackLayoutProjectsOrientationSpacingAndClipAlias()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageLayoutHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var stack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 12
        };

        ((IElementHandler)handler).SetVirtualView(stack);

        Assert.Equal(ComponentKind.HorizontalStack, handler.PlatformView.ControlKind);
        Assert.Equal("12px", handler.PlatformView.GetStyleValue("gap"));
        Assert.Equal("visible", handler.PlatformView.GetStyleValue("overflow"));

        stack.IsClippedToBounds = true;
        MirageLayoutHandler.Mapper.UpdateProperty(handler, stack, "IsClippedToBounds");
        Assert.Equal("hidden", handler.PlatformView.GetStyleValue("overflow"));
    }

    [Fact]
    public void CommonMapper_SizeRequests_ProjectsAndClearsWidthRequestDeterministically()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new Label();

        MirageViewMapper.MapSizeRequests(handler, view);
        Assert.Null(handler.Node.GetStyleValue("width"));

        view.WidthRequest = 132;
        MirageViewMapper.MapSizeRequests(handler, view);
        Assert.Equal("132px", handler.Node.GetStyleValue("width"));

        view.WidthRequest = -1;
        MirageViewMapper.MapSizeRequests(handler, view);
        Assert.Null(handler.Node.GetStyleValue("width"));
    }

    [Fact]
    public void CommonMapper_MapsLayoutClipsToBoundsToOverflowStyle()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "flex");
        var layout = new VerticalStackLayout();

        var changedVisible = MirageCommonViewProjection.MapLayoutClipsToBounds(handler, layout);
        Assert.True(changedVisible);
        Assert.Equal("visible", handler.Node.GetStyleValue("overflow"));

        var unchanged = MirageCommonViewProjection.MapLayoutClipsToBounds(handler, layout);
        Assert.False(unchanged);

        layout.Clip = new RectangleGeometry(new Rect(0, 0, 30, 20));
        var changedHidden = MirageCommonViewProjection.MapLayoutClipsToBounds(handler, layout);
        Assert.True(changedHidden);
        Assert.Equal("hidden", handler.Node.GetStyleValue("overflow"));
    }

    [Fact]
    public void CommonMapper_MapsClipProjection_ForBuiltInGeometryTypes()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new ContentView();

        view.Clip = new RectangleGeometry(new Rect(0, 0, 120, 72));
        MirageViewMapper.MapClip(handler, view);
        Assert.Contains("path('M", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        view.Clip = new RoundRectangleGeometry(new CornerRadius(12, 8, 14, 6), new Rect(4, 2, 110, 66));
        MirageViewMapper.MapClip(handler, view);
        Assert.Contains(" A ", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        view.Clip = new EllipseGeometry(new Point(60, 36), 40, 24);
        MirageViewMapper.MapClip(handler, view);
        Assert.Contains(" A ", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        view.Clip = new LineGeometry
        {
            StartPoint = new Point(2, 3),
            EndPoint = new Point(98, 42)
        };
        MirageViewMapper.MapClip(handler, view);
        Assert.Contains(" L ", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        var path = new PathGeometry
        {
            FillRule = FillRule.EvenOdd
        };
        var figure = new PathFigure
        {
            StartPoint = new Point(4, 4),
            IsClosed = true
        };
        figure.Segments.Add(new LineSegment { Point = new Point(84, 4) });
        figure.Segments.Add(new LineSegment { Point = new Point(84, 44) });
        figure.Segments.Add(new LineSegment { Point = new Point(4, 44) });
        path.Figures.Add(figure);
        view.Clip = path;
        MirageViewMapper.MapClip(handler, view);
        Assert.StartsWith("path(evenodd, '", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        var group = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd
        };
        group.Children.Add(new RectangleGeometry(new Rect(2, 2, 44, 28)));
        group.Children.Add(new EllipseGeometry(new Point(56, 22), 18, 12));
        view.Clip = group;
        MirageViewMapper.MapClip(handler, view);
        Assert.StartsWith("path(evenodd, '", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);
    }

    [Fact]
    public void CommonMapper_MapClip_InvalidOrUnsupportedGeometry_ThrowsDeterministically()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new ContentView();

        view.Clip = new RectangleGeometry(new Rect(0, 0, double.NaN, 20));
        var invalidRect = Assert.Throws<InvalidOperationException>(() => MirageViewMapper.MapClip(handler, view));
        Assert.Contains("Rect.Width", invalidRect.Message, StringComparison.Ordinal);

        var invalidPath = new PathGeometry();
        var invalidFigure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = false };
        var invalidPolyBezier = new PolyBezierSegment();
        invalidPolyBezier.Points.Add(new Point(4, 4));
        invalidPolyBezier.Points.Add(new Point(8, 8));
        invalidFigure.Segments.Add(invalidPolyBezier);
        invalidPath.Figures.Add(invalidFigure);
        view.Clip = invalidPath;
        var invalidPathException = Assert.Throws<InvalidOperationException>(() => MirageViewMapper.MapClip(handler, view));
        Assert.Contains("divisible by 3", invalidPathException.Message, StringComparison.Ordinal);

        view.Clip = new UnsupportedGeometry();
        var unsupported = Assert.Throws<InvalidOperationException>(() => MirageViewMapper.MapClip(handler, view));
        Assert.Contains("does not support clip geometry type", unsupported.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CommonMapper_AlignmentProjection_UsesParentLayoutKind()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var child = new Label
        {
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        var gridParent = MiragePlatformView.CreateControl(ComponentKind.Grid);
        gridParent.AddChild(handler.Node);

        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("end", handler.Node.GetStyleValue("justifySelf"));
        Assert.Equal("center", handler.Node.GetStyleValue("alignSelf"));

        var verticalParent = MiragePlatformView.CreateControl(ComponentKind.VerticalStack);
        verticalParent.AddChild(handler.Node);
        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("flex-end", handler.Node.GetStyleValue("alignSelf"));

        var contentContainerParent = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        contentContainerParent.AddChild(handler.Node);
        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("end", handler.Node.GetStyleValue("justifySelf"));
        Assert.Equal("center", handler.Node.GetStyleValue("alignSelf"));
    }

    [Fact]
    public void CommonMapper_AlignmentProjection_UsesLiveStackOrientationFromMauiParent()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var child = new Label
        {
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        var stack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal
        };
        stack.Children.Add(child);

        // Keep the projected parent kind intentionally stale to verify we trust MAUI parent state.
        var staleProjectedParent = MiragePlatformView.CreateControl(ComponentKind.VerticalStack);
        staleProjectedParent.AddChild(handler.Node);

        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("center", handler.Node.GetStyleValue("alignSelf"));
        Assert.Null(handler.Node.GetStyleValue("justifySelf"));

        stack.Orientation = StackOrientation.Vertical;
        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("flex-end", handler.Node.GetStyleValue("alignSelf"));
        Assert.Null(handler.Node.GetStyleValue("justifySelf"));
    }

    [Fact]
    public void LayoutHandler_AbsoluteLayoutAutoSize_PreservesExplicitSizeRequests()
    {
        var childPlatformView = MiragePlatformView.CreateControl(ComponentKind.BoxView);
        var child = new BoxView
        {
            WidthRequest = 72,
            HeightRequest = 24
        };

        AbsoluteLayout.SetLayoutBounds(child, new Rect(0, 0, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(child, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);

        var applyMethod = typeof(MirageLayoutHandler).GetMethod(
            "ApplyAbsoluteLayoutChildStyles",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(applyMethod);

        var changed = (bool)applyMethod!.Invoke(null, [childPlatformView, child])!;

        Assert.True(changed);
        Assert.Equal("72px", childPlatformView.GetStyleValue("width"));
        Assert.Equal("24px", childPlatformView.GetStyleValue("height"));
    }

    [Fact]
    public async Task RenderCoordinator_RendersTypedSemanticSnapshotAndRoutesEvents()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        root.SetStyle("display", "flex");

        var button = MiragePlatformView.CreateControl(ComponentKind.Button);
        button.SetProperty("Text", "Click");
        button.SetEvent("click", enabled: true);
        root.AddChild(button);

        var target = new EventTargetProbe();
        coordinator.Register(root, eventTarget: null);
        coordinator.Register(button, target);
        coordinator.SetRoot(root);
        await WaitUntilAsync(() => runtime.LastRendered is not null);

        Assert.NotNull(runtime.LastRendered);
        var rootNode = Assert.IsType<ControlNode>(runtime.LastRendered);
        Assert.Equal(ComponentKind.Root, rootNode.Kind);
        var child = Assert.Single(rootNode.Children);
        var controlChild = Assert.IsType<ControlNode>(child);
        Assert.Equal(ComponentKind.Button, controlChild.Kind);
        Assert.Equal("Click", controlChild.Properties["Text"].GetStringOrNull());

        coordinator.OnEvent(button.NodeId, "click", null);
        Assert.Equal("click", target.LastEventName);
    }

    [Fact]
    public async Task RenderCoordinator_RendersGenericWebNode_AndRoutesEventsByNodeId()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        var vendor = MiragePlatformView.CreateWeb("vendor-widget");
        vendor.SetProperty("open", MirageValue.Bool(true));
        vendor.SetProperty("payload", MirageValue.Json(System.Text.Json.Nodes.JsonNode.Parse("""{"mode":"demo"}""")));
        vendor.SetAttribute("aria-label", "Vendor Widget");
        vendor.AddClass("is-ready");
        vendor.SetEvent("widget-click", enabled: true);
        root.AddChild(vendor);

        var target = new EventTargetProbe();
        coordinator.Register(root, eventTarget: null);
        coordinator.Register(vendor, eventTarget: target);
        coordinator.SetRoot(root);
        await WaitUntilAsync(() => runtime.LastRendered is not null);

        var rootNode = Assert.IsType<ControlNode>(runtime.LastRendered);
        var child = Assert.Single(rootNode.Children);
        var webNode = Assert.IsType<WebNode>(child);
        Assert.Equal("vendor-widget", webNode.ElementName);
        Assert.True(webNode.Properties["open"].TryGetBool(out var isOpen) && isOpen);
        Assert.Contains("is-ready", webNode.Classes);
        Assert.Equal("Vendor Widget", webNode.Attributes["aria-label"]);

        coordinator.OnEvent(vendor.NodeId, "widget-click", payload: null);
        Assert.Equal("widget-click", target.LastEventName);
    }

    [Fact]
    public void RenderCoordinator_AssignsStableNodeIds()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        coordinator.AttachRuntime(new RuntimeProbe());

        var first = MiragePlatformView.CreateControl(ComponentKind.Label);
        var second = MiragePlatformView.CreateWeb("vendor-input");

        coordinator.Register(first, null);
        coordinator.Register(second, null);

        Assert.Equal("node-0001", first.NodeId);
        Assert.Equal("node-0002", second.NodeId);
    }

    [Fact]
    public async Task RenderCoordinator_BatchesRenderRequests_DuringScope()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        coordinator.Register(root, eventTarget: null);
        coordinator.SetRoot(root);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);
        var baselineRenderCount = runtime.RenderCount;

        using (coordinator.BeginRenderBatch())
        {
            coordinator.RequestRender();
            coordinator.RequestRender();
            coordinator.RequestRender();
        }

        await WaitUntilAsync(() => runtime.RenderCount >= baselineRenderCount + 1);
        await Task.Delay(30);
        Assert.Equal(baselineRenderCount + 1, runtime.RenderCount);
    }

    [Fact]
    public async Task RenderCoordinator_CoalescesBurstRequests_WithoutManualBatchScope()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        coordinator.Register(root, eventTarget: null);
        coordinator.SetRoot(root);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);

        var baselineRenderCount = runtime.RenderCount;
        coordinator.RequestRender();
        coordinator.RequestRender();
        coordinator.RequestRender();

        await WaitUntilAsync(() => runtime.RenderCount >= baselineRenderCount + 1);
        await Task.Delay(30);
        Assert.Equal(baselineRenderCount + 1, runtime.RenderCount);
    }

    [Fact]
    public async Task RenderCoordinator_ReentrantInvalidation_SchedulesSingleFollowUpPass()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        var reentrantRequestCount = 0;
        runtime.RenderCallback = () =>
        {
            if (Interlocked.Increment(ref reentrantRequestCount) == 1)
            {
                coordinator.RequestRender();
                coordinator.RequestRender();
                coordinator.RequestRender();
            }
        };
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        coordinator.Register(root, eventTarget: null);
        coordinator.SetRoot(root);

        await WaitUntilAsync(() => runtime.RenderCount >= 2);
        await Task.Delay(30);
        Assert.Equal(2, runtime.RenderCount);
    }

    [Fact]
    public async Task RenderCoordinator_ScopedMaterialization_ReusesUnchangedSiblingNodes()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        root.AssignNodeId("root");
        var first = MiragePlatformView.CreateControl(ComponentKind.Label);
        first.AssignNodeId("first");
        first.SetProperty("Text", "Before");
        var second = MiragePlatformView.CreateControl(ComponentKind.Label);
        second.AssignNodeId("second");
        second.SetProperty("Text", "Stable");
        root.AddChild(first);
        root.AddChild(second);

        coordinator.Register(root, eventTarget: null);
        coordinator.Register(first, eventTarget: null);
        coordinator.Register(second, eventTarget: null);
        coordinator.SetRoot(root);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);

        var initialRoot = Assert.IsType<ControlNode>(runtime.LastRendered);
        var initialFirst = Assert.IsType<ControlNode>(initialRoot.Children[0]);
        var initialSecond = Assert.IsType<ControlNode>(initialRoot.Children[1]);

        first.SetProperty("Text", "After");
        coordinator.RequestRender();
        await WaitUntilAsync(() => runtime.RenderCount >= 2);

        var rerenderedRoot = Assert.IsType<ControlNode>(runtime.LastRendered);
        var rerenderedFirst = Assert.IsType<ControlNode>(rerenderedRoot.Children[0]);
        var rerenderedSecond = Assert.IsType<ControlNode>(rerenderedRoot.Children[1]);

        Assert.False(ReferenceEquals(initialFirst, rerenderedFirst));
        Assert.True(ReferenceEquals(initialSecond, rerenderedSecond));
    }

    [Fact]
    public async Task RenderCoordinator_ScopedMaterialization_PreservesSiblingReuse_AfterNoOpPass()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        root.AssignNodeId("root");
        var first = MiragePlatformView.CreateControl(ComponentKind.Label);
        first.AssignNodeId("first");
        first.SetProperty("Text", "First");
        var second = MiragePlatformView.CreateControl(ComponentKind.Label);
        second.AssignNodeId("second");
        second.SetProperty("Text", "Second");
        root.AddChild(first);
        root.AddChild(second);

        coordinator.Register(root, eventTarget: null);
        coordinator.Register(first, eventTarget: null);
        coordinator.Register(second, eventTarget: null);
        coordinator.SetRoot(root);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);

        var initialRoot = Assert.IsType<ControlNode>(runtime.LastRendered);
        var initialSecond = Assert.IsType<ControlNode>(initialRoot.Children[1]);

        // First no-op pass uses scoped reuse and should not drop descendant cache state.
        coordinator.RequestRender();
        await WaitUntilAsync(() => runtime.RenderCount >= 2);

        first.SetProperty("Text", "First Updated");
        coordinator.RequestRender();
        await WaitUntilAsync(() => runtime.RenderCount >= 3);

        var rerenderedRoot = Assert.IsType<ControlNode>(runtime.LastRendered);
        var rerenderedSecond = Assert.IsType<ControlNode>(rerenderedRoot.Children[1]);
        Assert.True(ReferenceEquals(initialSecond, rerenderedSecond));
    }

    [Fact]
    public void RenderCoordinator_ScrollBridge_RoutesToRuntimeDeterministically()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var set = coordinator.TrySetNodeScrollPosition("node-scroll", 18, 72, out var actualLeft, out var actualTop);
        var got = coordinator.TryGetNodeScrollPosition("node-scroll", out var currentLeft, out var currentTop);

        Assert.True(set);
        Assert.True(got);
        Assert.Equal(18, actualLeft);
        Assert.Equal(72, actualTop);
        Assert.Equal(18, currentLeft);
        Assert.Equal(72, currentTop);
    }

    [Theory]
    [InlineData(ScrollOrientation.Vertical, false, true)]
    [InlineData(ScrollOrientation.Horizontal, true, false)]
    [InlineData(ScrollOrientation.Both, true, true)]
    public void ScrollViewHandler_GetDesiredSize_UsesInfiniteConstraintOnScrollAxis(
        ScrollOrientation orientation,
        bool expectInfiniteWidthConstraint,
        bool expectInfiniteHeightConstraint)
    {
        var probe = new MeasureProbeContentView();
        var scrollView = new ScrollView
        {
            Orientation = orientation,
            Content = probe
        };

        var handler = new MirageScrollViewHandler();
        ((IElementHandler)handler).SetVirtualView(scrollView);

        var desired = handler.GetDesiredSize(320, 180);

        Assert.True(probe.MeasureCount > 0, "Expected probe content to be measured.");
        Assert.Equal(expectInfiniteWidthConstraint, double.IsPositiveInfinity(probe.LastWidthConstraint));
        Assert.Equal(expectInfiniteHeightConstraint, double.IsPositiveInfinity(probe.LastHeightConstraint));
        Assert.Equal(320, desired.Width);
        Assert.Equal(180, desired.Height);
    }

    [Fact]
    public void ScrollViewHandler_RequestScrollTo_ExecutesThroughCoordinatorRuntimeBridge()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageScrollViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var scrollView = new ScrollView();
        ((IElementHandler)handler).SetVirtualView(scrollView);

        handler.Invoke(nameof(IScrollView.RequestScrollTo), new ScrollToRequest(14, 96, true));

        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.Equal(14, left);
        Assert.Equal(96, top);
        Assert.Equal(14, scrollView.ScrollX);
        Assert.Equal(96, scrollView.ScrollY);
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_RoutesThroughHandlerCommandBridge()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageScrollViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var scrollView = new ScrollView();
        ((IElementHandler)handler).SetVirtualView(scrollView);

        await scrollView.ScrollToAsync(22, 140, animated: true);

        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.Equal(22, left);
        Assert.Equal(140, top);
        Assert.Equal(22, scrollView.ScrollX);
        Assert.Equal(140, scrollView.ScrollY);
    }

    [Fact]
    public async Task EntryHandler_RuntimeInputEvents_AreCoalescedPerDispatcherFrame()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var inputDispatcher = new QueuedDispatcher();
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .AddSingleton<IDispatcher>(inputDispatcher)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageEntryHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var entry = new Entry { Text = string.Empty };
        ((IElementHandler)handler).SetVirtualView(entry);

        coordinator.Register(handler.PlatformView, handler);
        coordinator.SetRoot(handler.PlatformView);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);
        var baselineRenderCount = runtime.RenderCount;

        handler.OnRuntimeEvent("input", "A");
        handler.OnRuntimeEvent("input", "AB");
        handler.OnRuntimeEvent("input", "ABC");

        Assert.Equal(string.Empty, entry.Text);
        Assert.Equal(1, inputDispatcher.PendingCount);

        inputDispatcher.FlushAll();
        await WaitUntilAsync(() => string.Equals(entry.Text, "ABC", StringComparison.Ordinal));
        await Task.Delay(30);

        Assert.Equal("ABC", entry.Text);
        Assert.Equal("ABC", handler.PlatformView.GetPropertyString("Text"));
        Assert.Equal(baselineRenderCount, runtime.RenderCount);
    }

    [Fact]
    public async Task EntryHandler_StaleBufferedRuntimeInput_DoesNotOverwriteProgrammaticTextUpdate()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var inputDispatcher = new QueuedDispatcher();
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .AddSingleton<IDispatcher>(inputDispatcher)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageEntryHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var entry = new Entry { Text = string.Empty };
        ((IElementHandler)handler).SetVirtualView(entry);

        coordinator.Register(handler.PlatformView, handler);
        coordinator.SetRoot(handler.PlatformView);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);

        handler.OnRuntimeEvent("input", "runtime-value");
        Assert.Equal(1, inputDispatcher.PendingCount);

        entry.Text = "programmatic-value";
        MirageEntryHandler.MapText(handler, entry);

        inputDispatcher.FlushAll();
        await Task.Delay(30);

        Assert.Equal("programmatic-value", entry.Text);
        Assert.Equal("programmatic-value", handler.PlatformView.GetPropertyString("Text"));
    }

    [Fact]
    public void ContentPage_PlatformArrange_ForwardsArrangeToDirectContent()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var probe = new ArrangeProbeContentView();

        var probeHandler = new MirageContentViewHandler();
        ((IElementHandler)probeHandler).SetMauiContext(mauiContext);
        ((IElementHandler)probeHandler).SetVirtualView(probe);

        var page = new ContentPage
        {
            Content = probe
        };

        var pageHandler = new MirageContentPageHandler();
        ((IElementHandler)pageHandler).SetMauiContext(mauiContext);
        ((IElementHandler)pageHandler).SetVirtualView(page);
        pageHandler.PlatformArrange(new Rect(0, 0, 360, 640));

        Assert.True(probe.ArrangeCount > 0, "Expected direct ContentPage child to be arranged at least once.");
        Assert.True(probe.LastArrangeBounds.Width > 0, $"Expected arranged width > 0. Actual={probe.LastArrangeBounds.Width}");
        Assert.True(probe.LastArrangeBounds.Height > 0, $"Expected arranged height > 0. Actual={probe.LastArrangeBounds.Height}");
    }

    [Fact]
    public void ContentView_PlatformArrange_ForwardsArrangeToNestedContent()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var probe = new ArrangeProbeContentView();

        var probeHandler = new MirageContentViewHandler();
        ((IElementHandler)probeHandler).SetMauiContext(mauiContext);
        ((IElementHandler)probeHandler).SetVirtualView(probe);

        var nestedHost = new ContentView
        {
            Content = probe
        };
        var nestedHostHandler = new MirageContentViewHandler();
        ((IElementHandler)nestedHostHandler).SetMauiContext(mauiContext);
        ((IElementHandler)nestedHostHandler).SetVirtualView(nestedHost);

        var page = new ContentPage
        {
            Content = nestedHost
        };

        var pageHandler = new MirageContentPageHandler();
        ((IElementHandler)pageHandler).SetMauiContext(mauiContext);
        ((IElementHandler)pageHandler).SetVirtualView(page);
        pageHandler.PlatformArrange(new Rect(0, 0, 360, 640));

        Assert.True(probe.ArrangeCount > 0, "Expected nested ContentView child to be arranged at least once.");
        Assert.True(probe.LastArrangeBounds.Width > 0, $"Expected arranged width > 0. Actual={probe.LastArrangeBounds.Width}");
        Assert.True(probe.LastArrangeBounds.Height > 0, $"Expected arranged height > 0. Actual={probe.LastArrangeBounds.Height}");
    }

    [Fact]
    public async Task NavigationPageHandler_RealizesBuiltInShellStructureAndTitleProjection()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var homePage = CreateAttachedContentPage(mauiContext, "Home");
        var navigationPage = new NavigationPage(homePage);
        var handler = new MirageNavigationPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(navigationPage);

        coordinator.SetRoot(handler.PlatformView);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);

        var rendered = Assert.IsType<ControlNode>(runtime.LastRendered);
        var navBar = FindVisualByAutomationId(rendered, "mirage-navigation-bar");
        var navTitle = FindVisualByAutomationId(rendered, "mirage-navigation-title");
        var contentHost = FindVisualByAutomationId(rendered, "mirage-navigation-content-host");
        var backButton = FindVisualByAutomationId(rendered, "mirage-navigation-back-button");
        Assert.NotNull(navBar);
        Assert.NotNull(navTitle);
        Assert.NotNull(contentHost);
        Assert.NotNull(backButton);

        Assert.Equal("Home", navTitle!.Properties["Text"].GetStringOrNull());
        Assert.Equal("system-ui", navTitle.Styles["fontFamily"]);
        Assert.Equal("CanvasText", navTitle.Styles["color"]);
        Assert.Equal("grid", navBar!.Styles["display"]);
        Assert.Equal("1", navBar.Styles["gridRowStart"]);
        Assert.Equal("2", navBar.Styles["gridRowEnd"]);
        Assert.Equal("hidden", contentHost!.Styles["overflow"]);
        Assert.Equal("2", contentHost.Styles["gridRowStart"]);
        Assert.Equal("3", contentHost.Styles["gridRowEnd"]);
        Assert.Equal("none", backButton!.Styles["display"]);

        NavigationPage.SetHasNavigationBar(homePage, false);
        await WaitUntilAsync(() =>
        {
            var root = runtime.LastRendered as ControlNode;
            var bar = root is null ? null : FindVisualByAutomationId(root, "mirage-navigation-bar");
            return string.Equals(bar?.Styles["display"], "none", StringComparison.Ordinal);
        });

        var hiddenRender = Assert.IsType<ControlNode>(runtime.LastRendered);
        var hiddenContentHost = FindVisualByAutomationId(hiddenRender, "mirage-navigation-content-host");
        Assert.NotNull(hiddenContentHost);
        Assert.Equal("2", hiddenContentHost!.Styles["gridRowStart"]);
        Assert.Equal("3", hiddenContentHost.Styles["gridRowEnd"]);
    }

    [Fact]
    public void NavigationPageHandler_GetDesiredSize_IncludesVisibleNavigationBarChrome()
    {
        var measurePage = new MeasureProbePage(new Size(240, 180));
        var navigationPage = new NavigationPage(measurePage);
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var measurePageHandler = new MirageContentPageHandler();
        ((IElementHandler)measurePageHandler).SetMauiContext(mauiContext);
        ((IElementHandler)measurePageHandler).SetVirtualView(measurePage);

        var handler = new MirageNavigationPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(navigationPage);

        var withNavigationBar = handler.GetDesiredSize(420, 600);
        Assert.Equal(420, measurePage.LastWidthConstraint);
        Assert.Equal(554, measurePage.LastHeightConstraint);

        NavigationPage.SetHasNavigationBar(measurePage, false);
        var withoutNavigationBar = handler.GetDesiredSize(420, 600);

        Assert.Equal(420, measurePage.LastWidthConstraint);
        Assert.Equal(600, measurePage.LastHeightConstraint);
        Assert.Equal(240, withoutNavigationBar.Width);
        Assert.Equal(180, withoutNavigationBar.Height);
        Assert.Equal(240, withNavigationBar.Width);
        Assert.Equal(226, withNavigationBar.Height);
    }

    [Fact]
    public async Task NavigationPageHandler_BackButtonReflectsStackDepthAndInvokesPop()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var homePage = CreateAttachedContentPage(mauiContext, "Home");
        var detailsPage = CreateAttachedContentPage(mauiContext, "Details");
        var navigationPage = new NavigationPage(homePage);
        var handler = new MirageNavigationPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(navigationPage);

        coordinator.SetRoot(handler.PlatformView);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);

        var firstRender = Assert.IsType<ControlNode>(runtime.LastRendered);
        var firstBackNode = FindVisualByAutomationId(firstRender, "mirage-navigation-back-button");
        Assert.NotNull(firstBackNode);
        Assert.Equal("none", firstBackNode!.Styles["display"]);

        await navigationPage.PushAsync(detailsPage, animated: false);
        await WaitUntilAsync(() => navigationPage.Navigation.NavigationStack.Count == 2);
        await WaitUntilAsync(() =>
        {
            var root = runtime.LastRendered as ControlNode;
            var title = root is null ? null : FindVisualByAutomationId(root, "mirage-navigation-title");
            return string.Equals(title?.Properties["Text"].GetStringOrNull(), "Details", StringComparison.Ordinal);
        });

        var pushedRender = Assert.IsType<ControlNode>(runtime.LastRendered);
        var pushedBackNode = FindVisualByAutomationId(pushedRender, "mirage-navigation-back-button");
        Assert.NotNull(pushedBackNode);
        Assert.NotEqual("none", pushedBackNode!.Styles["display"]);

        Assert.NotNull(runtime.Sink);
        runtime.Sink!.OnEvent(pushedBackNode!.NodeId, "click", null);

        await WaitUntilAsync(() => navigationPage.Navigation.NavigationStack.Count == 1);
        await WaitUntilAsync(() =>
        {
            var root = runtime.LastRendered as ControlNode;
            var title = root is null ? null : FindVisualByAutomationId(root, "mirage-navigation-title");
            return string.Equals(title?.Properties["Text"].GetStringOrNull(), "Home", StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task NavigationPageHandler_ToolbarItems_AreOrderedAndCommandInvocationsRespectEnabledState()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var commandLog = new List<string>();
        var clickedOnlyCount = 0;
        var primaryLowPriority = new ToolbarItem
        {
            Text = "Primary-2",
            Order = ToolbarItemOrder.Primary,
            Priority = 2,
            Command = new Command(() => commandLog.Add("Primary-2"))
        };
        var clickedOnlyItem = new ToolbarItem
        {
            Text = "Clicked-1",
            Order = ToolbarItemOrder.Primary,
            Priority = 1
        };
        clickedOnlyItem.Clicked += (_, _) => clickedOnlyCount++;
        var primaryHighPriority = new ToolbarItem
        {
            Text = "Primary-0",
            Order = ToolbarItemOrder.Primary,
            Priority = 0,
            Command = new Command(() => commandLog.Add("Primary-0"))
        };

        var homePage = CreateAttachedContentPage(mauiContext, "Home");
        homePage.ToolbarItems.Add(primaryLowPriority);
        homePage.ToolbarItems.Add(clickedOnlyItem);
        homePage.ToolbarItems.Add(primaryHighPriority);

        var navigationPage = new NavigationPage(homePage);
        var handler = new MirageNavigationPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(navigationPage);

        coordinator.SetRoot(handler.PlatformView);
        await WaitUntilAsync(() => runtime.RenderCount >= 1);
        await WaitUntilAsync(() =>
        {
            var root = runtime.LastRendered as ControlNode;
            var first = root is null ? null : FindVisualByAutomationId(root, "mirage-navigation-toolbar-item-1");
            return string.Equals(first?.Properties["Text"].GetStringOrNull(), "Primary-0", StringComparison.Ordinal);
        });

        var firstRender = Assert.IsType<ControlNode>(runtime.LastRendered);
        var firstToolbarNode = FindVisualByAutomationId(firstRender, "mirage-navigation-toolbar-item-1");
        var secondToolbarNode = FindVisualByAutomationId(firstRender, "mirage-navigation-toolbar-item-2");
        var thirdToolbarNode = FindVisualByAutomationId(firstRender, "mirage-navigation-toolbar-item-3");
        Assert.NotNull(firstToolbarNode);
        Assert.NotNull(secondToolbarNode);
        Assert.NotNull(thirdToolbarNode);

        Assert.Equal("Primary-0", firstToolbarNode!.Properties["Text"].GetStringOrNull());
        Assert.Equal("Clicked-1", secondToolbarNode!.Properties["Text"].GetStringOrNull());
        Assert.Equal("Primary-2", thirdToolbarNode!.Properties["Text"].GetStringOrNull());

        Assert.NotNull(runtime.Sink);
        runtime.Sink!.OnEvent(firstToolbarNode!.NodeId, "click", null);
        await WaitUntilAsync(() => commandLog.Count == 1);
        Assert.Equal("Primary-0", commandLog[0]);

        primaryHighPriority.IsEnabled = false;
        await WaitUntilAsync(() =>
        {
            var root = runtime.LastRendered as ControlNode;
            var first = root is null ? null : FindVisualByAutomationId(root, "mirage-navigation-toolbar-item-1");
            return string.Equals(first?.Styles["opacity"], "0.55", StringComparison.Ordinal);
        });

        var disabledRender = Assert.IsType<ControlNode>(runtime.LastRendered);
        var disabledFirst = FindVisualByAutomationId(disabledRender, "mirage-navigation-toolbar-item-1");
        Assert.NotNull(disabledFirst);
        runtime.Sink!.OnEvent(disabledFirst!.NodeId, "click", null);
        await Task.Delay(30);
        Assert.Single(commandLog);

        var clickedOnlyRender = Assert.IsType<ControlNode>(runtime.LastRendered);
        var clickedOnlyNode = FindVisualByAutomationId(clickedOnlyRender, "mirage-navigation-toolbar-item-2");
        Assert.NotNull(clickedOnlyNode);
        runtime.Sink!.OnEvent(clickedOnlyNode!.NodeId, "click", null);
        await WaitUntilAsync(() => clickedOnlyCount == 1);
    }

    [Fact]
    public async Task BrowserDispatcher_DispatchDelayed_CapturesSynchronizationContextAtDispatchTime()
    {
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        try
        {
            var dispatcher = new BrowserDispatcher();
            var queuedContext = new QueuedSynchronizationContext();
            var executedCount = 0;

            SynchronizationContext.SetSynchronizationContext(queuedContext);
            Assert.True(dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1), () => executedCount++));

            SynchronizationContext.SetSynchronizationContext(null);
            await Task.Delay(30);

            Assert.Equal(0, executedCount);
            Assert.Equal(1, queuedContext.PendingCount);

            SynchronizationContext.SetSynchronizationContext(queuedContext);
            queuedContext.FlushAll();

            Assert.Equal(1, executedCount);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task BrowserDispatcher_Dispatch_UsesCapturedSynchronizationContextFromPriorDispatch()
    {
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        try
        {
            var dispatcher = new BrowserDispatcher();
            var queuedContext = new QueuedSynchronizationContext();
            var executedCount = 0;

            SynchronizationContext.SetSynchronizationContext(queuedContext);
            Assert.True(dispatcher.Dispatch(() => executedCount++));
            Assert.Equal(1, executedCount);

            SynchronizationContext.SetSynchronizationContext(null);
            await Task.Run(() => dispatcher.Dispatch(() => executedCount++));

            Assert.Equal(1, executedCount);
            Assert.Equal(1, queuedContext.PendingCount);

            SynchronizationContext.SetSynchronizationContext(queuedContext);
            queuedContext.FlushAll();

            Assert.Equal(2, executedCount);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public void PlatformView_ChildOrderRemainsDeterministicAcrossMutations()
    {
        var root = MiragePlatformView.CreateControl(ComponentKind.VerticalStack);
        root.AssignNodeId("root");
        var first = MiragePlatformView.CreateControl(ComponentKind.Label);
        first.AssignNodeId("first");
        var second = MiragePlatformView.CreateControl(ComponentKind.Label);
        second.AssignNodeId("second");
        var third = MiragePlatformView.CreateControl(ComponentKind.Label);
        third.AssignNodeId("third");

        root.AddChild(first);
        root.AddChild(second);
        root.AddChild(third);

        root.RemoveChild(second);
        root.InsertChild(0, second);

        Assert.Equal(["second", "first", "third"], root.Children.Select(static child => child.NodeId).ToArray());
    }

    private sealed class MapperProbeHandler : ViewHandler<IView, object>, IMiragePlatformHandler
    {
        public MapperProbeHandler(string? defaultDisplayStyle)
            : base(new PropertyMapper<IView, MapperProbeHandler>())
        {
            DefaultDisplayStyle = defaultDisplayStyle;
        }

        public MiragePlatformView Node { get; } = MiragePlatformView.CreateControl(ComponentKind.Label);

        public int RenderRequestCount { get; private set; }

        public string? DefaultDisplayStyle { get; }

        MiragePlatformView IMiragePlatformHandler.MiragePlatformView => Node;

        public void RequestRender()
        {
            RenderRequestCount++;
        }

        protected override object CreatePlatformView() => new();
    }

    private sealed class RuntimeProbe : IMirageRuntime
    {
        private readonly Dictionary<string, (double Left, double Top)> _scrollPositions = new(StringComparer.Ordinal);

        public NodeBase? LastRendered { get; private set; }

        public int RenderCount { get; private set; }

        public IMirageEventSink? Sink { get; private set; }

        public Action? RenderCallback { get; set; }

        public void SetEventSink(IMirageEventSink? eventSink)
        {
            Sink = eventSink;
        }

        public void Render(NodeBase nextRoot)
        {
            LastRendered = nextRoot;
            RenderCount++;
            RenderCallback?.Invoke();
        }

        public void SetViewport(double width, double height)
        {
            _ = width;
            _ = height;
        }

        public string? TryGetNodeInputValue(string nodeId)
        {
            _ = nodeId;
            return null;
        }

        public bool TryGetNodeScrollPosition(string nodeId, out double left, out double top)
        {
            if (_scrollPositions.TryGetValue(nodeId, out var position))
            {
                left = position.Left;
                top = position.Top;
                return true;
            }

            left = 0;
            top = 0;
            return false;
        }

        public bool TrySetNodeScrollPosition(string nodeId, double left, double top, out double actualLeft, out double actualTop)
        {
            actualLeft = Math.Max(0, left);
            actualTop = Math.Max(0, top);
            _scrollPositions[nodeId] = (actualLeft, actualTop);
            return true;
        }

        public void Dispose()
        {
        }
    }

    private sealed class QueuedDispatcher : IDispatcher
    {
        private readonly Queue<Action> _pending = new();

        public bool IsDispatchRequired => false;

        public int PendingCount => _pending.Count;

        public bool Dispatch(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            _pending.Enqueue(action);
            return true;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action)
        {
            _ = delay;
            return Dispatch(action);
        }

        public IDispatcherTimer CreateTimer()
        {
            throw new NotSupportedException("QueuedDispatcher does not provide timers.");
        }

        public void FlushAll()
        {
            while (_pending.Count > 0)
            {
                var action = _pending.Dequeue();
                action();
            }
        }
    }

    private sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = new();

        public int PendingCount
        {
            get
            {
                lock (_pending)
                {
                    return _pending.Count;
                }
            }
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_pending)
            {
                _pending.Enqueue((d, state));
            }
        }

        public void FlushAll()
        {
            while (true)
            {
                (SendOrPostCallback Callback, object? State) workItem;
                lock (_pending)
                {
                    if (_pending.Count == 0)
                    {
                        break;
                    }

                    workItem = _pending.Dequeue();
                }

                workItem.Callback(workItem.State);
            }
        }
    }

    private sealed class EventTargetProbe : IMirageRuntimeEventTarget
    {
        public string? LastEventName { get; private set; }

        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _ = payload;
            LastEventName = eventName;
        }
    }

    private sealed class UnsupportedGeometry : Geometry
    {
        public override void AppendPath(PathF path)
        {
            _ = path;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 2000)
    {
        var start = Environment.TickCount64;
        while (!predicate())
        {
            if (Environment.TickCount64 - start > timeoutMs)
            {
                throw new TimeoutException("Timed out waiting for asynchronous condition.");
            }

            await Task.Delay(10);
        }
    }

    private static ContentPage CreateAttachedContentPage(IMauiContext mauiContext, string title)
    {
        var page = new ContentPage
        {
            Title = title
        };

        var handler = new MirageContentPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(page);
        return page;
    }

    private static VisualNodeBase? FindVisualByAutomationId(NodeBase root, string automationId)
    {
        if (root is VisualNodeBase visual
            && string.Equals(visual.AutomationId, automationId, StringComparison.Ordinal))
        {
            return visual;
        }

        for (var index = 0; index < root.Children.Count; index++)
        {
            var match = FindVisualByAutomationId(root.Children[index], automationId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private sealed class ArrangeProbeContentView : ContentView
    {
        public int ArrangeCount { get; private set; }

        public Rect LastArrangeBounds { get; private set; } = Rect.Zero;

        protected override Size ArrangeOverride(Rect bounds)
        {
            ArrangeCount++;
            LastArrangeBounds = bounds;
            return base.ArrangeOverride(bounds);
        }
    }

    private sealed class MeasureProbeContentView : ContentView
    {
        public int MeasureCount { get; private set; }

        public double LastWidthConstraint { get; private set; }

        public double LastHeightConstraint { get; private set; }

        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            MeasureCount++;
            LastWidthConstraint = widthConstraint;
            LastHeightConstraint = heightConstraint;
            return new Size(640, 420);
        }
    }

    private sealed class MeasureProbePage : ContentPage
    {
        private readonly Size _measuredSize;

        public MeasureProbePage(Size measuredSize)
        {
            _measuredSize = measuredSize;
        }

        public double LastWidthConstraint { get; private set; }

        public double LastHeightConstraint { get; private set; }

        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            LastWidthConstraint = widthConstraint;
            LastHeightConstraint = heightConstraint;
            return _measuredSize;
        }
    }

    private sealed class StubImageSourceResolver : IMirageImageSourceResolver
    {
        public ValueTask EnsureInitializedAsync()
        {
            return ValueTask.CompletedTask;
        }

        public MirageImageSourceResolveResult Resolve(string? source)
        {
            if (string.Equals(source, "coffee.png", StringComparison.Ordinal))
            {
                return MirageImageSourceResolveResult.Resolved("/_mirage/assets/test/coffee.png");
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                return MirageImageSourceResolveResult.Unresolved("Empty source.");
            }

            return MirageImageSourceResolveResult.Resolved(source);
        }
    }

    private sealed class RejectingImageSourceResolver : IMirageImageSourceResolver
    {
        public ValueTask EnsureInitializedAsync()
        {
            return ValueTask.CompletedTask;
        }

        public MirageImageSourceResolveResult Resolve(string? source)
        {
            return MirageImageSourceResolveResult.Unresolved($"Could not resolve '{source}'.");
        }
    }
}
