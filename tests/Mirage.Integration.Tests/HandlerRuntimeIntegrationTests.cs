using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Maui.Images;
using Mirage.Web.Runtime;
using Maui.Controls.Sample.Models;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
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
    public void CommonMapper_MapTransform_ProjectsAndClearsDeterministically()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new BoxView
        {
            TranslationX = 12,
            TranslationY = -7,
            Scale = 1.5,
            ScaleX = 0.8,
            ScaleY = 0.5,
            Rotation = 10,
            RotationX = 3,
            RotationY = -2,
            AnchorX = 0.25,
            AnchorY = 0.75
        };

        MirageViewMapper.MapTransform(handler, view);

        Assert.Equal("25% 75%", handler.Node.GetStyleValue("transformOrigin"));
        var projected = handler.Node.GetStyleValue("transform");
        Assert.NotNull(projected);
        Assert.Contains("translate(12px, -7px)", projected, StringComparison.Ordinal);
        Assert.Contains("rotate(10deg)", projected, StringComparison.Ordinal);
        Assert.Contains("rotateX(3deg)", projected, StringComparison.Ordinal);
        Assert.Contains("rotateY(-2deg)", projected, StringComparison.Ordinal);
        Assert.Contains("scale(1.2, 0.75)", projected, StringComparison.Ordinal);

        view.TranslationX = 0;
        view.TranslationY = 0;
        view.Scale = 1;
        view.ScaleX = 1;
        view.ScaleY = 1;
        view.Rotation = 0;
        view.RotationX = 0;
        view.RotationY = 0;
        view.AnchorX = 0.5;
        view.AnchorY = 0.5;

        MirageViewMapper.MapTransform(handler, view);

        Assert.Equal("50% 50%", handler.Node.GetStyleValue("transformOrigin"));
        Assert.Null(handler.Node.GetStyleValue("transform"));
    }

    [Fact]
    public void CommonMapper_MapTransform_PreservesOutOfRangeAnchors()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new BoxView
        {
            AnchorX = -1,
            AnchorY = 2
        };

        MirageViewMapper.MapTransform(handler, view);

        Assert.Equal("-100% 200%", handler.Node.GetStyleValue("transformOrigin"));
        Assert.Null(handler.Node.GetStyleValue("transform"));
    }

    [Fact]
    public void CommonMapper_MapStyle_ReappliesTransformProperties()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageBoxViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var boxView = new BoxView
        {
            ScaleX = 0.6
        };

        ((IElementHandler)handler).SetVirtualView(boxView);
        MirageViewMapper.MapStyle(handler, boxView);

        Assert.Equal("50% 50%", handler.PlatformView.GetStyleValue("transformOrigin"));
        var projected = handler.PlatformView.GetStyleValue("transform");
        Assert.NotNull(projected);
        Assert.Contains("scale(0.6, 1)", projected, StringComparison.Ordinal);
    }

    [Fact]
    public void ButtonHandler_CompatibilityAliases_MapBackgroundColorAndTextColor()
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

        Assert.Equal("rgba(81,43,212,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
        Assert.Equal("rgba(255,255,255,1)", handler.PlatformView.GetStyleValue("color"));
        Assert.Equal("8px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Null(handler.PlatformView.GetStyleValue("--accent-fill-rest"));
        Assert.Null(handler.PlatformView.GetStyleValue("--foreground-on-accent-rest"));
        Assert.Null(handler.PlatformView.GetStyleValue("--control-corner-radius"));
    }

    [Fact]
    public void ButtonHandler_GradientBackground_UsesCommonProjection()
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

        Assert.Equal("rgba(81,43,212,1)", handler.PlatformView.GetStyleValue("backgroundColor"));

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
        Assert.Null(handler.PlatformView.GetStyleValue("backgroundColor"));
    }

    [Fact]
    public void ButtonHandler_ProjectsImageSourceContentLayoutPaddingAndLineBreakModes()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var button = new Button
        {
            Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            ImageSource = "coffee.png",
            ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Top, 6),
            Padding = new Thickness(20, 10),
            WidthRequest = 180,
            LineBreakMode = LineBreakMode.HeadTruncation
        };

        ((IElementHandler)handler).SetVirtualView(button);

        Assert.Equal("/_mirage/assets/test/coffee.png", handler.PlatformView.GetPropertyString("ImageSource"));
        Assert.Equal("block", handler.PlatformView.GetPropertyString("ImageDisplay"));
        Assert.Equal(button.Text, handler.PlatformView.GetPropertyString("ImageAlt"));
        Assert.Equal("column", handler.PlatformView.GetStyleValue("flexDirection"));
        Assert.Equal("6px", handler.PlatformView.GetPropertyString("TextMarginTop"));
        Assert.Equal("10px 20px 10px 20px", handler.PlatformView.GetStyleValue("padding"));

        var headText = handler.PlatformView.GetPropertyString("Text");
        Assert.NotNull(headText);
        Assert.StartsWith("…", headText, StringComparison.Ordinal);

        button.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Right, 4);
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(Button.ContentLayout));
        Assert.Equal("row-reverse", handler.PlatformView.GetStyleValue("flexDirection"));
        Assert.Equal("4px", handler.PlatformView.GetPropertyString("TextMarginRight"));

        button.Padding = new Thickness(double.NaN, double.NaN, 4, double.NaN);
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(Button.Padding));
        Assert.Equal("0px 4px 0px 0px", handler.PlatformView.GetStyleValue("padding"));

        button.LineBreakMode = LineBreakMode.MiddleTruncation;
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(Button.LineBreakMode));
        var middleText = handler.PlatformView.GetPropertyString("Text");
        Assert.NotNull(middleText);
        Assert.Contains('…', middleText);
        Assert.False(middleText.StartsWith("…", StringComparison.Ordinal));
        Assert.False(middleText.EndsWith("…", StringComparison.Ordinal));

        button.LineBreakMode = LineBreakMode.TailTruncation;
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(Button.LineBreakMode));
        var tailText = handler.PlatformView.GetPropertyString("Text");
        Assert.NotNull(tailText);
        Assert.EndsWith("…", tailText, StringComparison.Ordinal);

        button.Text = "Updated accessibility label";
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(Button.Text));
        Assert.Equal("Updated accessibility label", handler.PlatformView.GetPropertyString("ImageAlt"));

        button.ImageSource = null;
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(Button.ImageSource));
        Assert.Null(handler.PlatformView.GetPropertyString("ImageSource"));
        Assert.Equal("none", handler.PlatformView.GetPropertyString("ImageDisplay"));
    }

    [Fact]
    public void ButtonHandler_Truncation_RecomputesOnPlatformArrangeWidthChanges()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        const string longText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt.";
        var button = new Button
        {
            Text = longText,
            WidthRequest = -1,
            LineBreakMode = LineBreakMode.MiddleTruncation
        };

        ((IElementHandler)handler).SetVirtualView(button);

        handler.PlatformView.SetStyle("width", "140px");
        handler.PlatformArrange(new Rect(0, 0, 140, 40));
        var narrowProjected = handler.PlatformView.GetPropertyString("Text");
        Assert.NotNull(narrowProjected);
        Assert.Contains('…', narrowProjected);

        handler.PlatformView.SetStyle("width", "1400px");
        handler.PlatformArrange(new Rect(0, 0, 1400, 40));
        var wideProjected = handler.PlatformView.GetPropertyString("Text");
        Assert.Equal(longText, wideProjected);
    }

    [Fact]
    public void ButtonHandler_IsEnabledToggle_DoesNotOverrideExplicitOpacityProjection()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var button = new Button
        {
            Text = "Opacity",
            Opacity = 0.4
        };

        ((IElementHandler)handler).SetVirtualView(button);
        Assert.Equal("0.4", handler.PlatformView.GetStyleValue("opacity"));

        button.IsEnabled = false;
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(IView.IsEnabled));
        Assert.Equal("0.4", handler.PlatformView.GetStyleValue("opacity"));

        button.IsEnabled = true;
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(IView.IsEnabled));
        Assert.Equal("0.4", handler.PlatformView.GetStyleValue("opacity"));
    }

    [Fact]
    public void ButtonHandler_TruncationBudget_UsesProjectedWidthWhenWidthRequestIsUnset()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        const string longText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt.";
        var button = new Button
        {
            Text = longText,
            WidthRequest = -1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        ((IElementHandler)handler).SetVirtualView(button);

        handler.PlatformView.SetStyle("width", "1400px");
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(VisualElement.WidthRequest));
        var wideProjected = handler.PlatformView.GetPropertyString("Text");
        Assert.Equal(longText, wideProjected);

        handler.PlatformView.SetStyle("width", "140px");
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(Button.LineBreakMode));
        var narrowProjected = handler.PlatformView.GetPropertyString("Text");
        Assert.NotNull(narrowProjected);
        Assert.EndsWith("…", narrowProjected, StringComparison.Ordinal);
    }

    [Fact]
    public void ButtonHandler_UnresolvedImage_DoesNotReduceTruncationBudget()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new RejectingImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var button = new Button
        {
            Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            ImageSource = "missing.png",
            WidthRequest = 140,
            Padding = new Thickness(0),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        ((IElementHandler)handler).SetVirtualView(button);
        Assert.Equal("none", handler.PlatformView.GetPropertyString("ImageDisplay"));
        var unresolvedProjected = handler.PlatformView.GetPropertyString("Text");

        button.ImageSource = null;
        MirageButtonHandler.Mapper.UpdateProperty(handler, button, nameof(Button.ImageSource));
        var noImageProjected = handler.PlatformView.GetPropertyString("Text");

        Assert.Equal(noImageProjected, unresolvedProjected);
    }

    [Fact]
    public void CheckBoxHandler_ProjectsColorAliasesCheckedAndDisabledSemantics()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCheckBoxHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var checkBox = new CheckBox
        {
            IsChecked = true,
            Color = Colors.Purple
        };

        ((IElementHandler)handler).SetVirtualView(checkBox);
        Assert.Equal("true", handler.PlatformView.GetPropertyString("IsChecked"));
        Assert.Equal("rgba(128,0,128,1)", handler.PlatformView.GetStyleValue("accentColor"));

        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, nameof(ICheckBox.Foreground));
        Assert.Equal("rgba(128,0,128,1)", handler.PlatformView.GetStyleValue("accentColor"));

        checkBox.Color = Colors.Green;
        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, "Color");
        Assert.Equal("rgba(0,128,0,1)", handler.PlatformView.GetStyleValue("accentColor"));

        handler.OnRuntimeEvent("change", "false");
        Assert.False(checkBox.IsChecked);
        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, nameof(ICheckBox.IsChecked));
        Assert.Equal("false", handler.PlatformView.GetPropertyString("IsChecked"));

        checkBox.IsEnabled = false;
        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, nameof(IView.IsEnabled));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("pointerEvents"));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));

        handler.OnRuntimeEvent("change", "true");
        Assert.False(checkBox.IsChecked);
    }

    [Fact]
    public void CheckBoxHandler_IsEnabledToggle_DoesNotOverrideExplicitOpacityProjection()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCheckBoxHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var checkBox = new CheckBox
        {
            Opacity = 0.35
        };

        ((IElementHandler)handler).SetVirtualView(checkBox);
        Assert.Equal("0.35", handler.PlatformView.GetStyleValue("opacity"));

        checkBox.IsEnabled = false;
        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, nameof(IView.IsEnabled));
        Assert.Equal("0.35", handler.PlatformView.GetStyleValue("opacity"));

        checkBox.IsEnabled = true;
        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, nameof(IView.IsEnabled));
        Assert.Equal("0.35", handler.PlatformView.GetStyleValue("opacity"));
    }

    [Fact]
    public void SwitchHandler_ProjectsToggleColorAndDisabledSemantics()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageSwitchHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var @switch = new Switch
        {
            IsToggled = false,
            ThumbColor = Colors.White,
            OnColor = Colors.MediumSeaGreen
        };

        ((IElementHandler)handler).SetVirtualView(@switch);

        Assert.Equal("false", handler.PlatformView.GetPropertyString("IsOn"));
        Assert.Equal("rgba(60,179,113,1)", handler.PlatformView.GetStyleValue("--mirage-switch-track-on"));
        Assert.Equal("rgba(255,255,255,1)", handler.PlatformView.GetStyleValue("--mirage-switch-thumb"));
        Assert.Null(handler.PlatformView.GetPropertyString("Disabled"));

        handler.OnRuntimeEvent("change", "true");
        Assert.True(((ISwitch)@switch).IsOn);

        ((ISwitch)@switch).IsOn = false;
        MirageSwitchHandler.Mapper.UpdateProperty(handler, @switch, "IsToggled");
        Assert.Equal("false", handler.PlatformView.GetPropertyString("IsOn"));

        @switch.OnColor = Colors.Teal;
        MirageSwitchHandler.Mapper.UpdateProperty(handler, @switch, "OnColor");
        Assert.Equal("rgba(0,128,128,1)", handler.PlatformView.GetStyleValue("--mirage-switch-track-on"));

        @switch.IsEnabled = false;
        MirageSwitchHandler.Mapper.UpdateProperty(handler, @switch, nameof(IView.IsEnabled));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("pointerEvents"));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));

        handler.OnRuntimeEvent("change", "true");
        Assert.False(((ISwitch)@switch).IsOn);
    }

    [Fact]
    public void ActivityIndicatorHandler_ProjectsColorRunningStateAndStoppedSemantics()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageActivityIndicatorHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var indicator = new ActivityIndicator
        {
            IsRunning = true,
            Color = Colors.Orange
        };

        ((IElementHandler)handler).SetVirtualView(indicator);

        Assert.Null(handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("24px", handler.PlatformView.GetStyleValue("height"));
        Assert.Equal("true", handler.PlatformView.GetPropertyString("IsRunning"));
        Assert.Equal("true", handler.PlatformView.GetPropertyString("BusyState"));
        Assert.Equal("rgba(255,165,0,1)", handler.PlatformView.GetPropertyString("SpinnerColor"));

        indicator.IsRunning = false;
        MirageActivityIndicatorHandler.Mapper.UpdateProperty(handler, indicator, nameof(IActivityIndicator.IsRunning));
        Assert.Equal("false", handler.PlatformView.GetPropertyString("IsRunning"));
        Assert.Equal("false", handler.PlatformView.GetPropertyString("BusyState"));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("hidden", handler.PlatformView.GetStyleValue("visibility"));

        indicator.IsRunning = true;
        MirageActivityIndicatorHandler.Mapper.UpdateProperty(handler, indicator, nameof(IActivityIndicator.IsRunning));
        Assert.Equal("inline-flex", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("visible", handler.PlatformView.GetStyleValue("visibility"));

        indicator.HeightRequest = 80;
        MirageActivityIndicatorHandler.Mapper.UpdateProperty(handler, indicator, nameof(VisualElement.HeightRequest));
        Assert.Equal("80px", handler.PlatformView.GetStyleValue("height"));

        indicator.HeightRequest = -1;
        MirageActivityIndicatorHandler.Mapper.UpdateProperty(handler, indicator, nameof(VisualElement.HeightRequest));
        Assert.Equal("24px", handler.PlatformView.GetStyleValue("height"));

        indicator.Color = null;
        MirageActivityIndicatorHandler.Mapper.UpdateProperty(handler, indicator, nameof(IActivityIndicator.Color));
        Assert.Null(handler.PlatformView.GetPropertyString("SpinnerColor"));
    }

    [Fact]
    public void SliderHandler_ProjectsRangeAndRuntimeDragFlow()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageSliderHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var dragStartedCount = 0;
        var dragCompletedCount = 0;

        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 25,
            MinimumTrackColor = Colors.Red,
            MaximumTrackColor = Colors.LightGray,
            ThumbColor = Colors.LimeGreen,
            ThumbImageSource = "thumb_image.png"
        };
        slider.DragStarted += (_, _) => dragStartedCount++;
        slider.DragCompleted += (_, _) => dragCompletedCount++;

        ((IElementHandler)handler).SetVirtualView(slider);

        Assert.Equal("0", handler.PlatformView.GetPropertyString("Minimum"));
        Assert.Equal("100", handler.PlatformView.GetPropertyString("Maximum"));
        Assert.Equal("25", handler.PlatformView.GetPropertyString("Value"));
        Assert.Null(handler.PlatformView.GetStyleValue("accentColor"));
        Assert.Null(handler.PlatformView.GetStyleValue("background"));
        Assert.Contains("25%", handler.PlatformView.GetPropertyString("TrackGradient") ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("rgba(50,205,50,1)", handler.PlatformView.GetPropertyString("ThumbAccent"));
        Assert.Equal("url(\"/_mirage/assets/test/thumb_image.png\")", handler.PlatformView.GetPropertyString("ThumbImage"));

        handler.OnRuntimeEvent("input", "40");
        Assert.Equal(40d, slider.Value, 3);
        Assert.Equal(1, dragStartedCount);

        handler.OnRuntimeEvent("input", "42");
        Assert.Equal(42d, slider.Value, 3);
        Assert.Equal(1, dragStartedCount);

        handler.OnRuntimeEvent("change", "42");
        Assert.Equal(1, dragCompletedCount);

        slider.ThumbImageSource = null;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, "ThumbImageSource");
        Assert.Null(handler.PlatformView.GetPropertyString("ThumbImage"));

        slider.ThumbImageSource = "coffee.png";
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, "ThumbImageSource");
        Assert.Equal("url(\"/_mirage/assets/test/coffee.png\")", handler.PlatformView.GetPropertyString("ThumbImage"));

        slider.ThumbColor = null;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, nameof(ISlider.ThumbColor));
        Assert.Equal("rgba(255,255,255,1)", handler.PlatformView.GetPropertyString("ThumbAccent"));

        slider.IsEnabled = false;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, nameof(IView.IsEnabled));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("pointerEvents"));
        Assert.Null(handler.PlatformView.GetStyleValue("opacity"));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        var valueBeforeDisabledInput = slider.Value;
        handler.OnRuntimeEvent("input", "88");
        Assert.Equal(valueBeforeDisabledInput, slider.Value, 3);
    }

    [Fact]
    public void SliderHandler_IsEnabledToggle_DoesNotOverrideExplicitOpacityProjection()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageSliderHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var slider = new Slider
        {
            Opacity = 0.45
        };

        ((IElementHandler)handler).SetVirtualView(slider);
        Assert.Equal("0.45", handler.PlatformView.GetStyleValue("opacity"));

        slider.IsEnabled = false;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, nameof(IView.IsEnabled));
        Assert.Equal("0.45", handler.PlatformView.GetStyleValue("opacity"));

        slider.IsEnabled = true;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, nameof(IView.IsEnabled));
        Assert.Equal("0.45", handler.PlatformView.GetStyleValue("opacity"));
    }

    [Fact]
    public void SliderHandler_DisabledMidDrag_CompletesAndResetsDragLifecycle()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageSliderHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var dragStartedCount = 0;
        var dragCompletedCount = 0;
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 10
        };
        slider.DragStarted += (_, _) => dragStartedCount++;
        slider.DragCompleted += (_, _) => dragCompletedCount++;

        ((IElementHandler)handler).SetVirtualView(slider);

        handler.OnRuntimeEvent("input", "20");
        Assert.Equal(1, dragStartedCount);
        Assert.Equal(0, dragCompletedCount);

        slider.IsEnabled = false;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, nameof(IView.IsEnabled));
        Assert.Equal(0, dragCompletedCount);

        slider.IsEnabled = true;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, nameof(IView.IsEnabled));

        handler.OnRuntimeEvent("input", "30");
        Assert.Equal(2, dragStartedCount);
        handler.OnRuntimeEvent("change", "30");
        Assert.Equal(1, dragCompletedCount);
    }

    [Fact]
    public void SliderHandler_WidthRequestUnset_RestoresDefaultFillWidth()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageSliderHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var slider = new Slider
        {
            WidthRequest = -1
        };

        ((IElementHandler)handler).SetVirtualView(slider);
        Assert.Equal("100%", handler.PlatformView.GetStyleValue("width"));

        slider.WidthRequest = 220;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, nameof(VisualElement.WidthRequest));
        Assert.Equal("220px", handler.PlatformView.GetStyleValue("width"));

        slider.WidthRequest = -1;
        MirageSliderHandler.Mapper.UpdateProperty(handler, slider, nameof(VisualElement.WidthRequest));
        Assert.Equal("100%", handler.PlatformView.GetStyleValue("width"));
    }

    [Fact]
    public void PickerHandler_ProjectsItemsSelectionTitleAndEnabledSemantics()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MiragePickerHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var picker = new Picker
        {
            Title = "Choose shape",
            TitleColor = Colors.Silver,
            TextColor = Colors.Black
        };
        picker.Items.Add("Rectangle");
        picker.Items.Add("RoundRectangle");
        picker.SelectedIndex = -1;

        ((IElementHandler)handler).SetVirtualView(picker);

        Assert.Equal("Choose shape", handler.PlatformView.GetPropertyString("Title"));
        Assert.Equal(string.Empty, handler.PlatformView.GetPropertyString("SelectedValue"));
        Assert.Equal("rgba(192,192,192,1)", handler.PlatformView.GetStyleValue("color"));
        Assert.Equal(3, handler.PlatformView.Children.Count);

        var placeholderOption = handler.PlatformView.Children[0];
        Assert.Equal(PlatformNodeType.Web, placeholderOption.NodeType);
        Assert.Equal("option", placeholderOption.ElementName);
        Assert.Equal(string.Empty, placeholderOption.Attributes["value"]);
        Assert.Equal("disabled", placeholderOption.Attributes["disabled"]);
        Assert.Equal("selected", placeholderOption.Attributes["selected"]);

        var firstItemOption = handler.PlatformView.Children[1];
        Assert.Equal("0", firstItemOption.Attributes["value"]);
        Assert.Equal("Rectangle", firstItemOption.GetPropertyString("textContent"));
        Assert.False(firstItemOption.Attributes.ContainsKey("selected"));

        picker.SelectedIndex = 1;
        MiragePickerHandler.Mapper.UpdateProperty(handler, picker, nameof(IPicker.SelectedIndex));

        Assert.Equal("1", handler.PlatformView.GetPropertyString("SelectedValue"));
        Assert.Equal("rgba(0,0,0,1)", handler.PlatformView.GetStyleValue("color"));
        Assert.False(placeholderOption.Attributes.ContainsKey("selected"));
        Assert.False(firstItemOption.Attributes.ContainsKey("selected"));
        Assert.Equal("selected", handler.PlatformView.Children[2].Attributes["selected"]);

        handler.OnRuntimeEvent("change", "0");
        Assert.Equal(0, picker.SelectedIndex);
        MiragePickerHandler.Mapper.UpdateProperty(handler, picker, nameof(IPicker.SelectedIndex));
        Assert.Equal("selected", firstItemOption.Attributes["selected"]);
        Assert.False(handler.PlatformView.Children[2].Attributes.ContainsKey("selected"));

        handler.OnRuntimeEvent("change", string.Empty);
        Assert.Equal(-1, picker.SelectedIndex);
        MiragePickerHandler.Mapper.UpdateProperty(handler, picker, nameof(IPicker.SelectedIndex));
        Assert.Equal("selected", placeholderOption.Attributes["selected"]);
        Assert.False(firstItemOption.Attributes.ContainsKey("selected"));

        picker.IsEnabled = false;
        MiragePickerHandler.Mapper.UpdateProperty(handler, picker, nameof(IView.IsEnabled));

        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        Assert.Equal("not-allowed", handler.PlatformView.GetStyleValue("cursor"));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("pointerEvents"));
    }

    [Fact]
    public void StepperHandler_ProjectsRangeAndButtonRuntimeFlow()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageStepperHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var stepper = new Stepper
        {
            Minimum = 0,
            Maximum = 25,
            Increment = 2,
            Value = 5,
            BackgroundColor = Colors.Red
        };

        ((IElementHandler)handler).SetVirtualView(stepper);

        Assert.Equal("0", handler.PlatformView.GetPropertyString("Minimum"));
        Assert.Equal("25", handler.PlatformView.GetPropertyString("Maximum"));
        Assert.Equal("2", handler.PlatformView.GetPropertyString("Step"));
        Assert.Equal("5", handler.PlatformView.GetPropertyString("Value"));
        Assert.Equal("-", handler.PlatformView.GetPropertyString("DecrementText"));
        Assert.Equal("+", handler.PlatformView.GetPropertyString("IncrementText"));
        Assert.Null(handler.PlatformView.GetPropertyString("DecrementDisabled"));
        Assert.Null(handler.PlatformView.GetPropertyString("IncrementDisabled"));
        Assert.Equal("rgba(255,0,0,1)", handler.PlatformView.GetStyleValue("backgroundColor"));

        stepper.Background = new LinearGradientBrush(
            new GradientStopCollection
            {
                new(Colors.Yellow, 0),
                new(Colors.Green, 1)
            },
            new Point(0, 0),
            new Point(1, 0));
        MirageStepperHandler.Mapper.UpdateProperty(handler, stepper, nameof(IView.Background));
        Assert.NotNull(handler.PlatformView.GetStyleValue("backgroundImage"));

        handler.OnRuntimeEvent("decrement", null);
        Assert.Equal(3d, stepper.Value, 3);

        stepper.Value = 24;
        MirageStepperHandler.Mapper.UpdateProperty(handler, stepper, nameof(IRange.Value));
        handler.OnRuntimeEvent("increment", null);
        Assert.Equal(25d, stepper.Value, 3);
        MirageStepperHandler.Mapper.UpdateProperty(handler, stepper, nameof(IRange.Value));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("IncrementDisabled"));
        Assert.Null(handler.PlatformView.GetPropertyString("DecrementDisabled"));

        handler.OnRuntimeEvent("increment", null);
        Assert.Equal(25d, stepper.Value, 3);

        stepper.IsEnabled = false;
        MirageStepperHandler.Mapper.UpdateProperty(handler, stepper, nameof(IView.IsEnabled));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        Assert.Equal("not-allowed", handler.PlatformView.GetStyleValue("cursor"));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("DecrementDisabled"));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("IncrementDisabled"));

        handler.OnRuntimeEvent("decrement", null);
        Assert.Equal(25d, stepper.Value, 3);
    }

    [Fact]
    public void FrameHandler_ProjectsBorderCornerAndShadowStyles()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageFrameHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var frame = new Frame
        {
            BorderColor = Colors.Red,
            CornerRadius = 16f,
            HasShadow = true
        };

        ((IElementHandler)handler).SetVirtualView(frame);

        Assert.Equal("solid", handler.PlatformView.GetStyleValue("borderStyle"));
        Assert.Equal("1px", handler.PlatformView.GetStyleValue("borderWidth"));
        Assert.Equal("rgba(255,0,0,1)", handler.PlatformView.GetStyleValue("borderColor"));
        Assert.Equal("16px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.NotNull(handler.PlatformView.GetStyleValue("boxShadow"));

        frame.HasShadow = false;
        MirageFrameHandler.Mapper.UpdateProperty(handler, frame, nameof(Frame.HasShadow));
        Assert.Null(handler.PlatformView.GetStyleValue("boxShadow"));

        frame.BorderColor = Colors.Transparent;
        MirageFrameHandler.Mapper.UpdateProperty(handler, frame, nameof(Frame.BorderColor));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("borderStyle"));
    }

    [Fact]
    public void CollectionViewHandler_ItemClickSelectsItem_AndExecutesSelectionCommand()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var firstItem = new SectionModel(typeof(ContentPage), "First", "First item");
        var secondItem = new SectionModel(typeof(ContentPage), "Second", "Second item");
        var commandCount = 0;

        var collectionView = new CollectionView
        {
            ItemsSource = new[] { firstItem, secondItem },
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() => new Label())
        };
        collectionView.SelectionChangedCommand = new Command(() => commandCount++);

        ((IElementHandler)handler).SetVirtualView(collectionView);

        Assert.Equal(2, handler.PlatformView.Children.Count);

        var firstRowNode = handler.PlatformView.Children[0];
        coordinator.OnEvent(firstRowNode.NodeId, "click", null);

        Assert.Same(firstItem, collectionView.SelectedItem);
        Assert.Equal(1, commandCount);
        Assert.Equal("2px solid rgba(86,57,176,0.35)", firstRowNode.GetStyleValue("outline"));
    }

    [Fact]
    public void CollectionViewHandler_ItemClickTogglesSelectedItems_WhenSelectionModeIsMultiple()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var firstItem = new SectionModel(typeof(ContentPage), "First", "First item");
        var secondItem = new SectionModel(typeof(ContentPage), "Second", "Second item");
        var collectionView = new CollectionView
        {
            ItemsSource = new[] { firstItem, secondItem },
            SelectionMode = SelectionMode.Multiple,
            ItemTemplate = new DataTemplate(() => new Label())
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);
        Assert.Equal(2, handler.PlatformView.Children.Count);

        var firstRowNode = handler.PlatformView.Children[0];
        var secondRowNode = handler.PlatformView.Children[1];

        coordinator.OnEvent(firstRowNode.NodeId, "click", null);
        Assert.NotNull(collectionView.SelectedItems);
        Assert.Single(collectionView.SelectedItems!);
        Assert.Same(firstItem, collectionView.SelectedItems![0]);

        coordinator.OnEvent(secondRowNode.NodeId, "click", null);
        Assert.Equal(2, collectionView.SelectedItems!.Count);
        Assert.Same(firstItem, collectionView.SelectedItems[0]);
        Assert.Same(secondItem, collectionView.SelectedItems[1]);

        coordinator.OnEvent(firstRowNode.NodeId, "click", null);
        Assert.Single(collectionView.SelectedItems);
        Assert.Same(secondItem, collectionView.SelectedItems[0]);
        Assert.Equal("2px solid rgba(86,57,176,0.35)", secondRowNode.GetStyleValue("outline"));
        Assert.Null(firstRowNode.GetStyleValue("outline"));
    }

    [Fact]
    public void CollectionViewHandler_SelectionUsesReferenceIdentity_ForValueEqualItems()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var firstItem = new ValueComparableSelectionItem(7, "first");
        var secondItem = new ValueComparableSelectionItem(7, "second");
        var collectionView = new CollectionView
        {
            ItemsSource = new[] { firstItem, secondItem },
            SelectionMode = SelectionMode.Multiple,
            ItemTemplate = new DataTemplate(() => new Label())
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);
        var firstRowNode = handler.PlatformView.Children[0];
        var secondRowNode = handler.PlatformView.Children[1];

        coordinator.OnEvent(secondRowNode.NodeId, "click", null);
        Assert.NotNull(collectionView.SelectedItems);
        Assert.Single(collectionView.SelectedItems!);
        Assert.Same(secondItem, collectionView.SelectedItems![0]);
        Assert.Null(firstRowNode.GetStyleValue("outline"));
        Assert.Equal("2px solid rgba(86,57,176,0.35)", secondRowNode.GetStyleValue("outline"));
    }

    [Fact]
    public void CollectionViewHandler_DuplicateReferenceRows_UseClickedRowIdentity()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var sharedItem = new SectionModel(typeof(ContentPage), "Shared", "Shared item");
        var collectionView = new CollectionView
        {
            ItemsSource = new[] { sharedItem, sharedItem },
            SelectionMode = SelectionMode.Multiple,
            ItemTemplate = new DataTemplate(() => new Label())
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);
        var firstRowNode = handler.PlatformView.Children[0];
        var secondRowNode = handler.PlatformView.Children[1];

        coordinator.OnEvent(secondRowNode.NodeId, "click", null);
        Assert.NotNull(collectionView.SelectedItems);
        Assert.Single(collectionView.SelectedItems!);
        Assert.Same(sharedItem, collectionView.SelectedItems![0]);
        Assert.Null(firstRowNode.GetStyleValue("outline"));
        Assert.Equal("2px solid rgba(86,57,176,0.35)", secondRowNode.GetStyleValue("outline"));

        coordinator.OnEvent(firstRowNode.NodeId, "click", null);
        Assert.Equal(2, collectionView.SelectedItems.Count);
        Assert.Same(sharedItem, collectionView.SelectedItems[0]);
        Assert.Same(sharedItem, collectionView.SelectedItems[1]);
        Assert.Equal("2px solid rgba(86,57,176,0.35)", firstRowNode.GetStyleValue("outline"));
        Assert.Equal("2px solid rgba(86,57,176,0.35)", secondRowNode.GetStyleValue("outline"));

        coordinator.OnEvent(secondRowNode.NodeId, "click", null);
        Assert.Single(collectionView.SelectedItems);
        Assert.Same(sharedItem, collectionView.SelectedItems[0]);
        Assert.Equal("2px solid rgba(86,57,176,0.35)", firstRowNode.GetStyleValue("outline"));
        Assert.Null(secondRowNode.GetStyleValue("outline"));
    }

    [Fact]
    public void CollectionViewHandler_Rebuild_RemovedSelectedItem_ClearsVirtualSelectedItem()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var firstItem = new SectionModel(typeof(ContentPage), "First", "First item");
        var secondItem = new SectionModel(typeof(ContentPage), "Second", "Second item");
        var items = new ObservableCollection<SectionModel> { firstItem, secondItem };

        var collectionView = new CollectionView
        {
            ItemsSource = items,
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() => new Label())
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);
        var secondRowNode = handler.PlatformView.Children[1];
        coordinator.OnEvent(secondRowNode.NodeId, "click", null);

        Assert.Same(secondItem, collectionView.SelectedItem);

        items.Remove(secondItem);

        Assert.Null(collectionView.SelectedItem);
        Assert.Single(handler.PlatformView.Children);
        Assert.Null(handler.PlatformView.Children[0].GetStyleValue("outline"));
    }

    [Fact]
    public void CollectionViewHandler_Rebuild_PreservesDuplicateReferenceRowSelectionIdentity()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var sharedItem = new SectionModel(typeof(ContentPage), "Shared", "Shared item");
        var items = new ObservableCollection<SectionModel> { sharedItem, sharedItem };

        var collectionView = new CollectionView
        {
            ItemsSource = items,
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() => new Label())
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);
        var secondRowNode = handler.PlatformView.Children[1];
        coordinator.OnEvent(secondRowNode.NodeId, "click", null);

        Assert.Same(sharedItem, collectionView.SelectedItem);
        Assert.Null(handler.PlatformView.Children[0].GetStyleValue("outline"));
        Assert.Equal("2px solid rgba(86,57,176,0.35)", handler.PlatformView.Children[1].GetStyleValue("outline"));

        items.Add(new SectionModel(typeof(ContentPage), "Third", "Third item"));

        Assert.Equal(3, handler.PlatformView.Children.Count);
        Assert.Same(sharedItem, collectionView.SelectedItem);
        Assert.Null(handler.PlatformView.Children[0].GetStyleValue("outline"));
        Assert.Equal("2px solid rgba(86,57,176,0.35)", handler.PlatformView.Children[1].GetStyleValue("outline"));
    }

    [Fact]
    public void CollectionViewHandler_RebuildsWhenObservableItemsSourceChanges()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var items = new ObservableCollection<SectionModel>
        {
            new(typeof(ContentPage), "First", "First item")
        };

        var collectionView = new CollectionView
        {
            ItemsSource = items,
            ItemTemplate = new DataTemplate(() => new Label())
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);
        Assert.Single(handler.PlatformView.Children);

        items.Add(new SectionModel(typeof(ContentPage), "Second", "Second item"));
        Assert.Equal(2, handler.PlatformView.Children.Count);
    }

    [Fact]
    public void CollectionViewHandler_RealizesNullItems_AndPreservesRowClickMapping()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var firstItem = new SectionModel(typeof(ContentPage), "First", "First item");
        var secondItem = new SectionModel(typeof(ContentPage), "Second", "Second item");
        var collectionView = new CollectionView
        {
            ItemsSource = new object?[] { firstItem, null, secondItem },
            SelectionMode = SelectionMode.Single
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);
        Assert.Equal(3, handler.PlatformView.Children.Count);

        var thirdRowNode = handler.PlatformView.Children[2];
        coordinator.OnEvent(thirdRowNode.NodeId, "click", null);
        Assert.Same(secondItem, collectionView.SelectedItem);

        var nullRowNode = handler.PlatformView.Children[1];
        coordinator.OnEvent(nullRowNode.NodeId, "click", null);
        Assert.Null(collectionView.SelectedItem);
    }

    [Fact]
    public void CollectionViewHandler_MultipleSelection_PreservesNullSelectedItems()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var secondItem = new SectionModel(typeof(ContentPage), "Second", "Second item");
        var collectionView = new CollectionView
        {
            ItemsSource = new object?[] { new SectionModel(typeof(ContentPage), "First", "First item"), null, secondItem },
            SelectionMode = SelectionMode.Multiple
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);
        Assert.Equal(3, handler.PlatformView.Children.Count);

        var nullRowNode = handler.PlatformView.Children[1];
        coordinator.OnEvent(nullRowNode.NodeId, "click", null);

        Assert.NotNull(collectionView.SelectedItems);
        Assert.Single(collectionView.SelectedItems!);
        Assert.Null(collectionView.SelectedItems[0]);

        var thirdRowNode = handler.PlatformView.Children[2];
        coordinator.OnEvent(thirdRowNode.NodeId, "click", null);

        Assert.Equal(2, collectionView.SelectedItems!.Count);
        Assert.Null(collectionView.SelectedItems[0]);
        Assert.Same(secondItem, collectionView.SelectedItems[1]);
    }

    [Fact]
    public void CollectionViewHandler_MultipleSelection_SyncPreservesNullSelectedItems()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var secondItem = new SectionModel(typeof(ContentPage), "Second", "Second item");
        var selectedItems = new List<object> { null!, secondItem };
        var collectionView = new CollectionView
        {
            ItemsSource = new object?[] { null, secondItem, null },
            SelectionMode = SelectionMode.Multiple,
            SelectedItems = selectedItems
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);

        Assert.NotNull(collectionView.SelectedItems);
        Assert.Equal(2, collectionView.SelectedItems!.Count);
        Assert.Null(collectionView.SelectedItems[0]);
        Assert.Same(secondItem, collectionView.SelectedItems[1]);
        Assert.Equal("2px solid rgba(86,57,176,0.35)", handler.PlatformView.Children[0].GetStyleValue("outline"));
        Assert.Equal("2px solid rgba(86,57,176,0.35)", handler.PlatformView.Children[1].GetStyleValue("outline"));
        Assert.Null(handler.PlatformView.Children[2].GetStyleValue("outline"));
    }

    [Fact]
    public void CollectionViewHandler_UsesDataTemplateSelector_PerItem()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCollectionViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var firstItem = new SectionModel(typeof(ContentPage), "First", "First item");
        var secondItem = new SectionModel(typeof(ContentPage), "Second", "Second item");
        var selector = new RecordingTemplateSelector(secondItem);
        var collectionView = new CollectionView
        {
            ItemsSource = new object?[] { firstItem, secondItem },
            ItemTemplate = selector
        };

        ((IElementHandler)handler).SetVirtualView(collectionView);

        Assert.True(selector.Calls.Count >= 2);
        Assert.Contains(selector.Calls, call => ReferenceEquals(call.Item, firstItem));
        Assert.Contains(selector.Calls, call => ReferenceEquals(call.Item, secondItem));
        Assert.All(selector.Calls, call => Assert.Same(collectionView, call.Container));
        Assert.True(selector.LabelTemplateCreateCount >= 1);
        Assert.True(selector.ButtonTemplateCreateCount >= 1);
    }

    [Fact]
    public void CheckBoxHandler_ProjectsColorAliasCheckedAndDisabledSemantics()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageCheckBoxHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var checkBox = new CheckBox
        {
            IsChecked = true,
            Color = Colors.Purple
        };

        ((IElementHandler)handler).SetVirtualView(checkBox);
        Assert.Equal("true", handler.PlatformView.GetPropertyString("IsChecked"));
        Assert.Equal("rgba(128,0,128,1)", handler.PlatformView.GetStyleValue("accentColor"));

        handler.OnRuntimeEvent("change", "false");
        Assert.False(checkBox.IsChecked);
        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, nameof(ICheckBox.IsChecked));
        Assert.Equal("false", handler.PlatformView.GetPropertyString("IsChecked"));

        checkBox.Color = Colors.Green;
        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, "Color");
        Assert.Equal("rgba(0,128,0,1)", handler.PlatformView.GetStyleValue("accentColor"));

        checkBox.IsEnabled = false;
        MirageCheckBoxHandler.Mapper.UpdateProperty(handler, checkBox, nameof(IView.IsEnabled));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));

        handler.OnRuntimeEvent("change", "true");
        Assert.False(checkBox.IsChecked);
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
    public void EntryHandler_WidthRequestUpdate_RecomputesStackCrossAxisAlignment()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageEntryHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var entry = new Entry
        {
            HorizontalOptions = LayoutOptions.Fill,
            WidthRequest = -1
        };

        var parent = new VerticalStackLayout();
        parent.Children.Add(entry);

        ((IElementHandler)handler).SetVirtualView(entry);

        var projectedParent = MiragePlatformView.CreateControl(ComponentKind.VerticalStack);
        projectedParent.AddChild(handler.PlatformView);

        MirageViewMapper.MapLayoutAlignment(handler, entry);
        Assert.Equal("stretch", handler.PlatformView.GetStyleValue("alignSelf"));

        entry.WidthRequest = 400;
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(VisualElement.WidthRequest));
        Assert.Equal("400px", handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("center", handler.PlatformView.GetStyleValue("alignSelf"));

        entry.WidthRequest = -1;
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(VisualElement.WidthRequest));
        Assert.Equal("stretch", handler.PlatformView.GetStyleValue("alignSelf"));
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
    public void ImageHandler_RespectsRequestedDimensions_AndClearsWhenUnset()
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

        Assert.Null(handler.PlatformView.GetStyleValue("width"));
        Assert.Null(handler.PlatformView.GetStyleValue("height"));

        image.MinimumWidthRequest = 40;
        image.MaximumWidthRequest = 160;
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(VisualElement.MinimumWidthRequest));
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(VisualElement.MaximumWidthRequest));

        Assert.Null(handler.PlatformView.GetStyleValue("width"));
        Assert.Null(handler.PlatformView.GetStyleValue("height"));
        Assert.Equal("40px", handler.PlatformView.GetStyleValue("minWidth"));
        Assert.Equal("160px", handler.PlatformView.GetStyleValue("maxWidth"));
    }

    [Fact]
    public void ImageHandler_WidthRequestUpdate_DoesNotClearParentManagedHeightStyle()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageImageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var image = new Image
        {
            Source = "coffee.png"
        };

        ((IElementHandler)handler).SetVirtualView(image);

        handler.PlatformView.SetStyle("height", "77px");

        image.WidthRequest = 180;
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(VisualElement.WidthRequest));
        Assert.Equal("180px", handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("77px", handler.PlatformView.GetStyleValue("height"));

        image.WidthRequest = -1;
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(VisualElement.WidthRequest));
        Assert.Null(handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("77px", handler.PlatformView.GetStyleValue("height"));
    }

    [Fact]
    public void ImageHandler_HeightRequestUpdate_DoesNotClearParentManagedWidthStyle()
    {
        var services = new ServiceCollection()
            .AddSingleton<IMirageImageSourceResolver>(new StubImageSourceResolver())
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageImageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var image = new Image
        {
            Source = "coffee.png"
        };

        ((IElementHandler)handler).SetVirtualView(image);

        handler.PlatformView.SetStyle("width", "88px");

        image.HeightRequest = 210;
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(VisualElement.HeightRequest));
        Assert.Equal("210px", handler.PlatformView.GetStyleValue("height"));
        Assert.Equal("88px", handler.PlatformView.GetStyleValue("width"));

        image.HeightRequest = -1;
        MirageImageHandler.Mapper.UpdateProperty(handler, image, nameof(VisualElement.HeightRequest));
        Assert.Null(handler.PlatformView.GetStyleValue("height"));
        Assert.Equal("88px", handler.PlatformView.GetStyleValue("width"));
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
    public void BorderHandler_ProjectsGradientStrokeDashShapeAndClipping()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageBorderHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var border = new Border
        {
            Padding = new Thickness(8),
            Stroke = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.CadetBlue, 0),
                    new GradientStop(Colors.Gold, 1)
                },
                new Point(0, 0),
                new Point(1, 0)),
            StrokeThickness = 6,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(12, 6, 10, 4)
            },
            StrokeLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Bevel
        };

        ((IElementHandler)handler).SetVirtualView(border);
        var strokeOverlayNode = handler.PlatformView.Children.FirstOrDefault(child => string.Equals(child.ElementName, "svg", StringComparison.Ordinal));
        Assert.NotNull(strokeOverlayNode);
        Assert.Equal(PlatformNodeType.Web, strokeOverlayNode.NodeType);
        Assert.Equal("svg", strokeOverlayNode.ElementName);
        Assert.Equal(2, strokeOverlayNode.Children.Count);

        var defsNode = strokeOverlayNode.Children[0];
        var pathNode = strokeOverlayNode.Children[1];
        Assert.Equal("defs", defsNode.ElementName);
        Assert.Equal("path", pathNode.ElementName);
        Assert.Equal("none", strokeOverlayNode.GetStyleValue("display"));
        Assert.False(pathNode.Attributes.ContainsKey("d"));
        Assert.Equal("inset(0 round 12px 6px 4px 10px)", handler.PlatformView.GetStyleValue("clipPath"));

        handler.PlatformArrange(new Rect(0, 0, 180, 120));

        Assert.Equal("12px 6px 4px 10px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Equal("inset(0 round 12px 6px 4px 10px)", handler.PlatformView.GetStyleValue("clipPath"));
        Assert.Equal("14px 14px 14px 14px", handler.PlatformView.GetStyleValue("padding"));
        Assert.Equal("hidden", handler.PlatformView.GetStyleValue("overflow"));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("borderStyle"));
        Assert.Equal("block", strokeOverlayNode.GetStyleValue("display"));
        Assert.Equal("0 0 180 120", strokeOverlayNode.Attributes["viewBox"]);
        Assert.Equal("none", strokeOverlayNode.Attributes["preserveAspectRatio"]);
        Assert.Contains("<linearGradient", defsNode.GetPropertyString("innerHTML") ?? string.Empty, StringComparison.Ordinal);
        Assert.StartsWith("url(#border-stroke-", pathNode.Attributes["stroke"], StringComparison.Ordinal);
        Assert.Equal("6", pathNode.Attributes["stroke-width"]);
        Assert.Equal("round", pathNode.Attributes["stroke-linecap"]);
        Assert.Equal("bevel", pathNode.Attributes["stroke-linejoin"]);
        Assert.Equal("none", pathNode.Attributes["fill"]);

        border.StrokeDashArray = new DoubleCollection { 2, 1 };
        MirageBorderHandler.Mapper.UpdateProperty(handler, border, nameof(Border.StrokeDashArray));
        Assert.Equal("12 6", pathNode.Attributes["stroke-dasharray"]);

        border.StrokeDashOffset = 1.5;
        MirageBorderHandler.Mapper.UpdateProperty(handler, border, nameof(Border.StrokeDashOffset));
        Assert.Equal("9", pathNode.Attributes["stroke-dashoffset"]);
        Assert.Equal("1.5", handler.PlatformView.GetPropertyString("StrokeDashOffset"));

        border.StrokeShape = new RoundRectangle
        {
            CornerRadius = new CornerRadius(4, 8, 16, 20)
        };
        MirageBorderHandler.Mapper.UpdateProperty(handler, border, nameof(Border.StrokeShape));
        Assert.Equal("4px 8px 20px 16px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Equal("inset(0 round 4px 8px 20px 16px)", handler.PlatformView.GetStyleValue("clipPath"));

        border.StrokeShape = new Ellipse();
        MirageBorderHandler.Mapper.UpdateProperty(handler, border, nameof(Border.StrokeShape));
        Assert.Equal("ellipse(50% 50% at 50% 50%)", handler.PlatformView.GetStyleValue("clipPath"));
        Assert.False(string.IsNullOrWhiteSpace(pathNode.Attributes["d"]));

        border.Stroke = null;
        MirageBorderHandler.Mapper.UpdateProperty(handler, border, nameof(Border.Stroke));
        Assert.Equal("none", strokeOverlayNode.GetStyleValue("display"));
        Assert.False(pathNode.Attributes.ContainsKey("stroke"));
        Assert.Equal("8px 8px 8px 8px", handler.PlatformView.GetStyleValue("padding"));
    }

    [Fact]
    public void BorderHandler_PolygonShape_ProjectsClipPathAndStrokePath()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageBorderHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var border = new Border
        {
            Stroke = new SolidColorBrush(Colors.Red),
            StrokeThickness = 4,
            StrokeShape = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(40, 10),
                    new Point(70, 80),
                    new Point(10, 50)
                }
            }
        };

        ((IElementHandler)handler).SetVirtualView(border);
        handler.PlatformArrange(new Rect(0, 0, 140, 120));

        Assert.Contains("polygon(", handler.PlatformView.GetStyleValue("clipPath") ?? string.Empty, StringComparison.Ordinal);

        var strokeOverlayNode = handler.PlatformView.Children.Last();
        var pathNode = strokeOverlayNode.Children.Last();
        Assert.Equal("rgba(255,0,0,1)", pathNode.Attributes["stroke"]);
        Assert.False(string.IsNullOrWhiteSpace(pathNode.Attributes["d"]));
    }

    [Fact]
    public void FrameHandler_MapsCompatibilityStyles_AndHostsPresentedContent()
    {
#pragma warning disable CS0618 // Frame is intentionally covered for compatibility scenarios.
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageFrameHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var frame = new Frame
        {
            Padding = new Thickness(6),
            CornerRadius = 12,
            BorderColor = Colors.CadetBlue,
            HasShadow = true
        };

        ((IElementHandler)handler).SetVirtualView(frame);

        Assert.Equal("6px 6px 6px 6px", handler.PlatformView.GetStyleValue("padding"));
        Assert.Equal("12px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Equal("solid", handler.PlatformView.GetStyleValue("borderStyle"));
        Assert.Equal("1px", handler.PlatformView.GetStyleValue("borderWidth"));
        Assert.NotNull(handler.PlatformView.GetStyleValue("borderColor"));
        Assert.NotNull(handler.PlatformView.GetStyleValue("boxShadow"));
        Assert.Empty(handler.PlatformView.Children);

        frame.HasShadow = false;
        MirageFrameHandler.Mapper.UpdateProperty(handler, frame, nameof(Frame.HasShadow));
        Assert.Null(handler.PlatformView.GetStyleValue("boxShadow"));
#pragma warning restore CS0618
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
    public void BoxViewHandler_DoesNotInjectDefaultMinimumSize_WhenSizeRequestsAreUnset()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageBoxViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var boxView = new BoxView();
        ((IElementHandler)handler).SetVirtualView(boxView);

        Assert.Null(handler.PlatformView.GetStyleValue("minWidth"));
        Assert.Null(handler.PlatformView.GetStyleValue("minHeight"));
        Assert.Null(handler.PlatformView.GetStyleValue("width"));
        Assert.Null(handler.PlatformView.GetStyleValue("height"));

        boxView.WidthRequest = 24;
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(VisualElement.WidthRequest));
        Assert.Equal("24px", handler.PlatformView.GetStyleValue("width"));

        boxView.HeightRequest = 16;
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(VisualElement.HeightRequest));
        Assert.Equal("16px", handler.PlatformView.GetStyleValue("height"));

        boxView.WidthRequest = -1;
        boxView.HeightRequest = -1;
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(VisualElement.WidthRequest));
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(VisualElement.HeightRequest));

        Assert.Null(handler.PlatformView.GetStyleValue("width"));
        Assert.Null(handler.PlatformView.GetStyleValue("height"));
        Assert.Null(handler.PlatformView.GetStyleValue("minHeight"));
        Assert.Null(handler.PlatformView.GetStyleValue("minWidth"));
    }

    [Fact]
    public void BoxViewHandler_ExplicitMinimumRequests_ArePreservedWhenSizeRequestsChange()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageBoxViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var boxView = new BoxView
        {
            MinimumWidthRequest = 48,
            MinimumHeightRequest = 26
        };

        ((IElementHandler)handler).SetVirtualView(boxView);
        Assert.Equal("48px", handler.PlatformView.GetStyleValue("minWidth"));
        Assert.Equal("26px", handler.PlatformView.GetStyleValue("minHeight"));

        boxView.WidthRequest = 96;
        boxView.HeightRequest = 64;
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(VisualElement.WidthRequest));
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(VisualElement.HeightRequest));

        Assert.Equal("48px", handler.PlatformView.GetStyleValue("minWidth"));
        Assert.Equal("26px", handler.PlatformView.GetStyleValue("minHeight"));
    }

    [Fact]
    public void BoxViewHandler_MapsCornerRadius_ForUniformAndComplexValues()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageBoxViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var boxView = new BoxView
        {
            CornerRadius = new CornerRadius(10)
        };

        ((IElementHandler)handler).SetVirtualView(boxView);
        Assert.Equal("10px 10px 10px 10px", handler.PlatformView.GetStyleValue("borderRadius"));

        boxView.CornerRadius = new CornerRadius(10, 0, 5, 20);
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(BoxView.CornerRadius));
        Assert.Equal("10px 0px 20px 5px", handler.PlatformView.GetStyleValue("borderRadius"));

        boxView.CornerRadius = new CornerRadius(0);
        MirageBoxViewHandler.Mapper.UpdateProperty(handler, boxView, nameof(BoxView.CornerRadius));
        Assert.Null(handler.PlatformView.GetStyleValue("borderRadius"));
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
    public void LayoutHandler_CustomLayout_UsesGenericLayoutKind_AndProjectsArrangedFrames()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageLayoutHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var customLayout = new FixedFrameLayout();

        ((IElementHandler)handler).SetVirtualView(customLayout);
        Assert.Equal(ComponentKind.Layout, handler.PlatformView.ControlKind);
        
        var applyMethod = typeof(MirageLayoutHandler).GetMethod(
            "ApplyChildProjectionStyles",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(applyMethod);

        var childPlatformView = MiragePlatformView.CreateControl(ComponentKind.Button);
        var child = new Button { Text = "Docked" };
        ((IView)child).Arrange(new Rect(12, 18, 96, 44));

        var changed = (bool)applyMethod!.Invoke(null, [childPlatformView, child, ComponentKind.Layout])!;
        Assert.True(changed);
        Assert.Equal("absolute", childPlatformView.GetStyleValue("position"));
        Assert.Equal("12px", childPlatformView.GetStyleValue("left"));
        Assert.Equal("18px", childPlatformView.GetStyleValue("top"));
        Assert.Equal("96px", childPlatformView.GetStyleValue("width"));
        Assert.Equal("44px", childPlatformView.GetStyleValue("height"));
    }

    [Fact]
    public void LayoutHandler_CustomLayout_ProjectionOffsetsAbsolutePositionByParentPadding()
    {
        var applyMethod = typeof(MirageLayoutHandler).GetMethod(
            "ApplyChildProjectionStyles",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(applyMethod);

        var customLayout = new FixedFrameLayout
        {
            Padding = new Thickness(8, 6, 0, 0)
        };

        var child = new Button { Text = "Docked" };
        customLayout.Children.Add(child);
        ((IView)child).Arrange(new Rect(20, 18, 96, 44));

        var childPlatformView = MiragePlatformView.CreateControl(ComponentKind.Button);
        var changed = (bool)applyMethod!.Invoke(null, [childPlatformView, child, ComponentKind.Layout])!;

        Assert.True(changed);
        Assert.Equal("12px", childPlatformView.GetStyleValue("left"));
        Assert.Equal("12px", childPlatformView.GetStyleValue("top"));
        Assert.Equal("96px", childPlatformView.GetStyleValue("width"));
        Assert.Equal("44px", childPlatformView.GetStyleValue("height"));
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
    public void CommonMapper_WidthRequestUpdate_RecomputesStackCrossAxisAlignment()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var child = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            WidthRequest = -1
        };

        var parent = new VerticalStackLayout();
        parent.Children.Add(child);

        var verticalParent = MiragePlatformView.CreateControl(ComponentKind.VerticalStack);
        verticalParent.AddChild(handler.Node);

        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("stretch", handler.Node.GetStyleValue("alignSelf"));

        child.WidthRequest = 400;
        MirageViewMapper.CommonViewMapper.UpdateProperty(handler, child, nameof(VisualElement.WidthRequest));
        Assert.Equal("400px", handler.Node.GetStyleValue("width"));
        Assert.Equal("center", handler.Node.GetStyleValue("alignSelf"));

        child.WidthRequest = -1;
        MirageViewMapper.CommonViewMapper.UpdateProperty(handler, child, nameof(VisualElement.WidthRequest));
        Assert.Null(handler.Node.GetStyleValue("width"));
        Assert.Equal("stretch", handler.Node.GetStyleValue("alignSelf"));
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
    public void CommonMapper_MapsShadow_ProjectsAndClearsDeterministically()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new BoxView();

        view.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Colors.Red),
            Opacity = 0.5f,
            Radius = 6,
            Offset = new Point(4, 8)
        };

        MirageViewMapper.MapShadow(handler, view);
        Assert.Equal("4px 8px 6px rgba(255,0,0,0.5)", handler.Node.GetStyleValue("boxShadow"));

        view.ClearValue(VisualElement.ShadowProperty);
        MirageViewMapper.MapShadow(handler, view);
        Assert.Null(handler.Node.GetStyleValue("boxShadow"));
    }

    [Fact]
    public void CommonMapper_MapShadow_UnsupportedBrush_ClearsProjection()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new BoxView
        {
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Radius = 4,
                Offset = new Point(1, 1),
                Opacity = 1
            }
        };

        MirageViewMapper.MapShadow(handler, view);
        Assert.Contains("rgba(0,0,0,1)", handler.Node.GetStyleValue("boxShadow"), StringComparison.Ordinal);

        view.Shadow = new Shadow
        {
            Brush = new LinearGradientBrush(
            [
                new GradientStop(Colors.Red, 0),
                new GradientStop(Colors.Blue, 1)
            ], new Point(0, 0), new Point(1, 1)),
            Radius = 6,
            Offset = new Point(2, 3),
            Opacity = 1
        };

        MirageViewMapper.MapShadow(handler, view);
        Assert.Null(handler.Node.GetStyleValue("boxShadow"));
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
    public void CommonMapper_AlignmentProjection_CentersFillWhenCrossAxisRequestIsExplicit()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var child = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            WidthRequest = 400
        };

        var verticalParent = MiragePlatformView.CreateControl(ComponentKind.VerticalStack);
        verticalParent.AddChild(handler.Node);

        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("center", handler.Node.GetStyleValue("alignSelf"));
        Assert.Null(handler.Node.GetStyleValue("justifySelf"));

        child.WidthRequest = -1;
        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("stretch", handler.Node.GetStyleValue("alignSelf"));
        Assert.Null(handler.Node.GetStyleValue("justifySelf"));
    }

    [Fact]
    public void LayoutHandler_ChildProjection_UsesCompatibilityLayoutOptionsSemantics()
    {
        var applyMethod = typeof(MirageLayoutHandler).GetMethod(
            "ApplyChildProjectionStyles",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(applyMethod);

        var childPlatformView = MiragePlatformView.CreateControl(ComponentKind.Grid);
        var child = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill
        };

        var changed = (bool)applyMethod!.Invoke(null, [childPlatformView, child, ComponentKind.VerticalStack])!;
        Assert.True(changed);
        Assert.Equal("stretch", childPlatformView.GetStyleValue("alignSelf"));

        child.WidthRequest = 400;
        changed = (bool)applyMethod.Invoke(null, [childPlatformView, child, ComponentKind.VerticalStack])!;
        Assert.True(changed);
        Assert.Equal("center", childPlatformView.GetStyleValue("alignSelf"));

        child.HorizontalOptions = LayoutOptions.Center;
        child.WidthRequest = -1;
        changed = (bool)applyMethod.Invoke(null, [childPlatformView, child, ComponentKind.VerticalStack])!;
        Assert.False(changed);
        Assert.Equal("center", childPlatformView.GetStyleValue("alignSelf"));
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
    public void LayoutHandler_AbsoluteLayoutTransform_ComposesWithViewTransform_AndClearsOutsideAbsolute()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var childPlatformView = handler.Node;
        var child = new BoxView
        {
            ScaleX = 0.4
        };

        MirageViewMapper.MapTransform(handler, child);
        Assert.Contains("scale(0.4, 1)", childPlatformView.GetStyleValue("transform"), StringComparison.Ordinal);

        AbsoluteLayout.SetLayoutBounds(child, new Rect(0.2, 0.3, 160, 80));
        AbsoluteLayout.SetLayoutFlags(child, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

        var applyAbsoluteMethod = typeof(MirageLayoutHandler).GetMethod(
            "ApplyAbsoluteLayoutChildStyles",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(applyAbsoluteMethod);

        var changed = (bool)applyAbsoluteMethod!.Invoke(null, [childPlatformView, child])!;
        Assert.True(changed);

        var absoluteTransform = childPlatformView.GetStyleValue("transform");
        Assert.NotNull(absoluteTransform);
        Assert.Contains("translate(-20%, -30%)", absoluteTransform, StringComparison.Ordinal);
        Assert.Contains("scale(0.4, 1)", absoluteTransform, StringComparison.Ordinal);

        var applyProjectionMethod = typeof(MirageLayoutHandler).GetMethod(
            "ApplyChildProjectionStyles",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(applyProjectionMethod);

        changed = (bool)applyProjectionMethod!.Invoke(null, [childPlatformView, child, ComponentKind.VerticalStack])!;
        Assert.True(changed);

        var stackedTransform = childPlatformView.GetStyleValue("transform");
        Assert.NotNull(stackedTransform);
        Assert.DoesNotContain("%", stackedTransform, StringComparison.Ordinal);
        Assert.Contains("scale(0.4, 1)", stackedTransform, StringComparison.Ordinal);
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
        await Task.Delay(30);

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
    public void ScrollViewHandler_GetDesiredSize_WithoutContent_ResolvesExplicitMinMaxConstraints()
    {
        var scrollView = new ScrollView
        {
            Padding = new Thickness(12, 8, 6, 4),
            WidthRequest = 80,
            MinimumWidthRequest = 120,
            MaximumWidthRequest = 100,
            HeightRequest = 50,
            MinimumHeightRequest = 70,
            MaximumHeightRequest = 60
        };

        var handler = new MirageScrollViewHandler();
        ((IElementHandler)handler).SetVirtualView(scrollView);

        var desired = handler.GetDesiredSize(40, 30);

        Assert.Equal(120, desired.Width);
        Assert.Equal(70, desired.Height);
    }

    [Fact]
    public void ScrollViewHandler_GetDesiredSize_WithContent_ResolvesExplicitMinMaxConstraints()
    {
        var probe = new MeasureProbeContentView();
        var scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            Content = probe,
            WidthRequest = 280,
            MaximumWidthRequest = 260,
            HeightRequest = 260,
            MinimumHeightRequest = 280
        };

        var handler = new MirageScrollViewHandler();
        ((IElementHandler)handler).SetVirtualView(scrollView);

        var desired = handler.GetDesiredSize(320, 180);

        Assert.True(probe.MeasureCount > 0, "Expected probe content to be measured.");
        Assert.Equal(260, desired.Width);
        Assert.Equal(280, desired.Height);
    }

    [Fact]
    public void ScrollViewHandler_GetDesiredSize_DoesNotTreatArrangedSizeAsExplicitRequest()
    {
        var probe = new MeasureProbeContentView();
        var scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            Content = probe
        };

        var handler = new MirageScrollViewHandler();
        ((IElementHandler)handler).SetVirtualView(scrollView);
        scrollView.Arrange(new Rect(0, 0, 320, 200));
        Assert.True(scrollView.Width > 0, "Expected arrange to populate current layout size.");

        var desired = handler.GetDesiredSize(180, 120);

        Assert.Equal(180, desired.Width);
        Assert.Equal(120, desired.Height);
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

        var content = new Label
        {
            Text = "Scrollable content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var contentHandler = new MirageLabelHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);

        handler.Invoke(nameof(IScrollView.RequestScrollTo), new ScrollToRequest(14, 96, true));
        Assert.False(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out _, out _));
        handler.PlatformArrange(new Rect(0, 0, 320, 200));

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

        var content = new Label
        {
            Text = "Scrollable content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var contentHandler = new MirageLabelHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);
        var pendingScrollTask = scrollView.ScrollToAsync(22, 140, animated: true);
        Assert.False(pendingScrollTask.IsCompleted);
        Assert.False(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out _, out _));

        handler.PlatformArrange(new Rect(0, 0, 320, 200));
        Assert.False(pendingScrollTask.IsCompleted);

        handler.OnRuntimeEvent("scroll", payload: null);
        await pendingScrollTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.Equal(22, left);
        Assert.Equal(140, top);
        Assert.Equal(22, scrollView.ScrollX);
        Assert.Equal(140, scrollView.ScrollY);
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_SupersededAnimatedRequest_WaitsForRuntimeCompletion()
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

        var content = new Label
        {
            Text = "Scrollable content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var contentHandler = new MirageLabelHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);
        handler.PlatformArrange(new Rect(0, 0, 320, 200));
        content.Frame = new Rect(0, 0, 320, 1200);

        var firstTask = scrollView.ScrollToAsync(16, 120, animated: true);
        Assert.False(firstTask.IsCompleted);

        content.Frame = new Rect(0, 0, 320, 1200);
        var secondTask = scrollView.ScrollToAsync(44, 280, animated: true);
        Assert.False(secondTask.IsCompleted);
        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.Equal(44, left);
        Assert.Equal(280, top);

        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 16, 120, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        Assert.False(secondTask.IsCompleted);

        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 44, 280, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        await secondTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(
            firstTask.Status is TaskStatus.Canceled or TaskStatus.WaitingForActivation,
            $"Expected superseded first task to be canceled or pending, but status was {firstTask.Status}.");
        Assert.Equal(44, scrollView.ScrollX);
        Assert.Equal(280, scrollView.ScrollY);
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_SupersededAnimatedRequest_NearTargetStaleEventDoesNotComplete()
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

        var content = new Label
        {
            Text = "Scrollable content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var contentHandler = new MirageLabelHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);
        handler.PlatformArrange(new Rect(0, 0, 320, 200));
        content.Frame = new Rect(0, 0, 320, 1200);

        var firstTask = scrollView.ScrollToAsync(44.00, 280.00, animated: true);
        Assert.False(firstTask.IsCompleted);

        var secondTask = scrollView.ScrollToAsync(44.03, 280.03, animated: true);
        Assert.False(secondTask.IsCompleted);
        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.Equal(44.03, left, 2);
        Assert.Equal(280.03, top, 2);

        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 44.00, 280.00, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        Assert.False(secondTask.IsCompleted);

        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 44.03, 280.03, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        await secondTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(
            firstTask.Status is TaskStatus.Canceled or TaskStatus.WaitingForActivation,
            $"Expected superseded first task to be canceled or pending, but status was {firstTask.Status}.");
        Assert.Equal(44.03, scrollView.ScrollX, 2);
        Assert.Equal(280.03, scrollView.ScrollY, 2);
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_AnimatedRequest_CompletesOnTimeoutWhenTargetIsNeverObserved()
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

        var content = new Label
        {
            Text = "Scrollable content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var contentHandler = new MirageLabelHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);
        handler.PlatformArrange(new Rect(0, 0, 320, 200));
        content.Frame = new Rect(0, 0, 320, 1200);

        var pendingTask = scrollView.ScrollToAsync(48, 300, animated: true);
        Assert.False(pendingTask.IsCompleted);

        // Simulate runtime drift/interruption where scroll events never report the originally requested offsets.
        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 40, 252, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        Assert.False(pendingTask.IsCompleted);

        await pendingTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_AnimatedRequest_RemainsPendingWhileNonTargetScrollEventsContinue()
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

        var content = new Label
        {
            Text = "Scrollable content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var contentHandler = new MirageLabelHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);
        handler.PlatformArrange(new Rect(0, 0, 320, 200));
        content.Frame = new Rect(0, 0, 320, 1200);

        var pendingTask = scrollView.ScrollToAsync(48, 300, animated: true);
        Assert.False(pendingTask.IsCompleted);

        // Keep sending non-target scroll events beyond the initial timeout window;
        // completion should stay pending while movement is still active.
        await Task.Delay(400);
        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 40, 252, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        Assert.False(pendingTask.IsCompleted);

        await Task.Delay(400);
        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 41, 260, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        Assert.False(pendingTask.IsCompleted);

        await Task.Delay(400);
        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 42, 268, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        Assert.False(pendingTask.IsCompleted);

        await Task.Delay(350);
        Assert.False(pendingTask.IsCompleted);

        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 48, 300, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);
        await pendingTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_ElementTarget_RoutesThroughHandlerCommandBridge()
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

        var topSpacer = new BoxView { HeightRequest = 640 };
        var target = new Label
        {
            Text = "Scroll target",
            HeightRequest = 40
        };
        var bottomSpacer = new BoxView { HeightRequest = 640 };

        var content = new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                topSpacer,
                target,
                bottomSpacer
            }
        };

        var topSpacerHandler = new MirageBoxViewHandler();
        ((IElementHandler)topSpacerHandler).SetMauiContext(mauiContext);
        ((IElementHandler)topSpacerHandler).SetVirtualView(topSpacer);

        var targetHandler = new MirageLabelHandler();
        ((IElementHandler)targetHandler).SetMauiContext(mauiContext);
        ((IElementHandler)targetHandler).SetVirtualView(target);

        var bottomSpacerHandler = new MirageBoxViewHandler();
        ((IElementHandler)bottomSpacerHandler).SetMauiContext(mauiContext);
        ((IElementHandler)bottomSpacerHandler).SetVirtualView(bottomSpacer);

        var contentHandler = new MirageLayoutHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);

        handler.PlatformArrange(new Rect(0, 0, 320, 200));

        var pendingScrollTask = scrollView.ScrollToAsync(target, ScrollToPosition.Start, animated: true);
        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.True(left >= 0, $"Expected non-negative left offset for element-targeted scroll. Actual={left}");
        Assert.True(top >= 0, $"Expected non-negative top offset for element-targeted scroll. Actual={top}");

        if (!pendingScrollTask.IsCompleted)
        {
            handler.OnRuntimeEvent("scroll", payload: null);
        }

        await pendingScrollTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(scrollView.ScrollY >= 0, $"Expected non-negative ScrollY after element-targeted scroll. Actual={scrollView.ScrollY}");
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_WithNullContent_CompletesWithoutArrange()
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

        await scrollView.ScrollToAsync(18, 44, animated: true).WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.Equal(18, left);
        Assert.Equal(44, top);
        Assert.Equal(18, scrollView.ScrollX);
        Assert.Equal(44, scrollView.ScrollY);
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_DeferredRequest_CompletesWhenHandlerDisconnects()
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

        var content = new Label
        {
            Text = "Deferred content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var contentHandler = new MirageLabelHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);

        var pendingScrollTask = scrollView.ScrollToAsync(28, 52, animated: true);
        Assert.False(pendingScrollTask.IsCompleted);

        ((IElementHandler)handler).DisconnectHandler();
        await pendingScrollTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ScrollView_ScrollToAsync_DeferredRequest_RemainsPendingAcrossContentRemapUntilArrange()
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

        var firstContent = new Label
        {
            Text = "First content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var firstContentHandler = new MirageLabelHandler();
        ((IElementHandler)firstContentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)firstContentHandler).SetVirtualView(firstContent);

        var scrollView = new ScrollView
        {
            Content = firstContent
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);

        var pendingScrollTask = scrollView.ScrollToAsync(32, 96, animated: true);
        Assert.False(pendingScrollTask.IsCompleted);

        var secondContent = new Label
        {
            Text = "Second content",
            WidthRequest = 800,
            HeightRequest = 1600
        };
        var secondContentHandler = new MirageLabelHandler();
        ((IElementHandler)secondContentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)secondContentHandler).SetVirtualView(secondContent);

        scrollView.Content = secondContent;
        Assert.False(pendingScrollTask.IsCompleted);
        Assert.False(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out _, out _));

        handler.PlatformArrange(new Rect(0, 0, 320, 200));
        Assert.False(pendingScrollTask.IsCompleted);
        handler.OnRuntimeEvent("scroll", payload: null);
        await pendingScrollTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.Equal(32, left);
        Assert.Equal(96, top);
    }

    [Fact]
    public void ScrollViewHandler_RequestScrollTo_DefersUntilArrangeWhenContentIsUnmeasured()
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

        var content = new Label
        {
            Text = "Scrollable content",
            WidthRequest = 640,
            HeightRequest = 1200
        };
        var contentHandler = new MirageLabelHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Content = content
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);

        handler.Invoke(nameof(IScrollView.RequestScrollTo), new ScrollToRequest(18, 220, Instant: true));
        handler.Invoke(nameof(IScrollView.RequestScrollTo), new ScrollToRequest(44, 360, Instant: true));

        Assert.False(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out _, out _));
        Assert.Equal(0, scrollView.ScrollX);
        Assert.Equal(0, scrollView.ScrollY);

        handler.PlatformArrange(new Rect(0, 0, 320, 200));

        Assert.True(runtime.TryGetNodeScrollPosition(handler.PlatformView.NodeId, out var left, out var top));
        Assert.Equal(44, left);
        Assert.Equal(360, top);
        Assert.Equal(44, scrollView.ScrollX);
        Assert.Equal(360, scrollView.ScrollY);
    }

    [Fact]
    public void ScrollViewHandler_RuntimeScrollEvent_UpdatesVirtualOffsets()
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

        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 36, 88, out _, out _);

        handler.OnRuntimeEvent("scroll", payload: null);

        Assert.Equal(36, scrollView.ScrollX);
        Assert.Equal(88, scrollView.ScrollY);
    }

    [Fact]
    public void ScrollViewHandler_RuntimeScrollEvent_RaisesSingleScrolledEventForDualAxisUpdate()
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
        var scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Both
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);

        var scrolledEventCount = 0;
        ScrolledEventArgs? lastArgs = null;
        scrollView.Scrolled += (_, args) =>
        {
            scrolledEventCount++;
            lastArgs = args;
        };

        runtime.TrySetNodeScrollPosition(handler.PlatformView.NodeId, 36, 88, out _, out _);
        handler.OnRuntimeEvent("scroll", payload: null);

        Assert.Equal(1, scrolledEventCount);
        Assert.NotNull(lastArgs);
        Assert.Equal(36, lastArgs!.ScrollX);
        Assert.Equal(88, lastArgs.ScrollY);
        Assert.Equal(36, scrollView.ScrollX);
        Assert.Equal(88, scrollView.ScrollY);
    }

    [Fact]
    public void ScrollViewHandler_PaddingMapper_ProjectsPaddingStyles()
    {
        var handler = new MirageScrollViewHandler();
        var scrollView = new ScrollView
        {
            Padding = new Thickness(12, 8, 6, 4),
            Content = new Label { Text = "Padding test" }
        };
        ((IElementHandler)handler).SetVirtualView(scrollView);

        Assert.Equal("8px 6px 4px 12px", handler.PlatformView.GetStyleValue("padding"));

        scrollView.Padding = new Thickness(2);
        MirageScrollViewHandler.Mapper.UpdateProperty(handler, scrollView, nameof(ScrollView.Padding));

        Assert.Equal("2px 2px 2px 2px", handler.PlatformView.GetStyleValue("padding"));
    }

    [Fact]
    public void ScrollViewHandler_Padding_AdjustsInnerArrangeViewport()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var content = new ArrangeProbeContentView();
        var contentHandler = new MirageContentViewHandler();
        ((IElementHandler)contentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)contentHandler).SetVirtualView(content);

        var scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            Padding = new Thickness(12, 8, 6, 4),
            Content = content
        };

        var handler = new MirageScrollViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(scrollView);

        handler.PlatformArrange(new Rect(0, 0, 320, 200));

        // Arranged width tracks the inner padded viewport for vertical scrolling.
        Assert.True(content.ArrangeCount > 0, "Expected probe content to be arranged.");
        Assert.Equal(302, content.LastArrangeBounds.Width);
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

    private sealed class ValueComparableSelectionItem
    {
        public ValueComparableSelectionItem(int key, string label)
        {
            Key = key;
            Label = label;
        }

        public int Key { get; }

        public string Label { get; }

        public override bool Equals(object? obj)
        {
            return obj is ValueComparableSelectionItem other && other.Key == Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }

    private sealed class FixedFrameLayout : Layout
    {
        protected override Microsoft.Maui.Layouts.ILayoutManager CreateLayoutManager()
        {
            return new FixedFrameLayoutManager(this);
        }

        private sealed class FixedFrameLayoutManager : Microsoft.Maui.Layouts.LayoutManager
        {
            private readonly FixedFrameLayout _layout;

            public FixedFrameLayoutManager(FixedFrameLayout layout)
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

                    ((IView)child).Arrange(new Rect(12, 18, 96, 44));
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

    private sealed class RecordingTemplateSelector : DataTemplateSelector
    {
        private readonly object _buttonTemplateItem;
        private readonly DataTemplate _buttonTemplate;
        private readonly DataTemplate _labelTemplate;

        public RecordingTemplateSelector(object buttonTemplateItem)
        {
            _buttonTemplateItem = buttonTemplateItem;
            _buttonTemplate = new DataTemplate(() =>
            {
                ButtonTemplateCreateCount++;
                return new Button();
            });

            _labelTemplate = new DataTemplate(() =>
            {
                LabelTemplateCreateCount++;
                return new Label();
            });
        }

        public List<TemplateSelectionCall> Calls { get; } = [];

        public int LabelTemplateCreateCount { get; private set; }

        public int ButtonTemplateCreateCount { get; private set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Calls.Add(new TemplateSelectionCall(item, container));
            return ReferenceEquals(item, _buttonTemplateItem) ? _buttonTemplate : _labelTemplate;
        }
    }

    private readonly record struct TemplateSelectionCall(object Item, BindableObject Container);

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

            if (string.Equals(source, "thumb_image.png", StringComparison.Ordinal))
            {
                return MirageImageSourceResolveResult.Resolved("/_mirage/assets/test/thumb_image.png");
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
