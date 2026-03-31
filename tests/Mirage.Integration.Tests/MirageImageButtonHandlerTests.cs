using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using MauiImage = Microsoft.Maui.IImage;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageImageButtonHandlerTests
{
    [Fact]
    public void ImageButtonHandler_ProjectsBuiltInControlKind_AndImageChannels()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png"),
            Aspect = Aspect.AspectFit
        };

        var (handler, _, _, _) = CreateAttachedHandler(imageButton);
        Assert.Equal(ComponentKind.ImageButton, handler.PlatformView.ControlKind);

        AssertEventually(() => string.Equals(handler.PlatformView.GetPropertyString("SourceKind"), "file", StringComparison.Ordinal));
        Assert.Equal("inline-flex", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("contain", handler.PlatformView.GetPropertyString("ImageObjectFit"));
        Assert.Equal("100%", handler.PlatformView.GetPropertyString("ImageWidth"));
        Assert.Equal("100%", handler.PlatformView.GetPropertyString("ImageHeight"));
    }

    [Fact]
    public void ImageButtonHandler_GetDesiredSize_UsesLegacyFallbackAndConstraintClamp()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png")
        };

        var (handler, _, _, _) = CreateAttachedHandler(imageButton);

        var unconstrained = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(52, unconstrained.Width);
        Assert.Equal(52, unconstrained.Height);

        var constrained = handler.GetDesiredSize(40, 30);
        Assert.Equal(40, constrained.Width);
        Assert.Equal(30, constrained.Height);
    }

    [Fact]
    public void ImageButtonHandler_GetDesiredSize_RespectsExplicitMinAndMaxRequests()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png"),
            WidthRequest = 80,
            MaximumWidthRequest = 70,
            HeightRequest = 40,
            MinimumHeightRequest = 60
        };

        var (handler, _, _, _) = CreateAttachedHandler(imageButton);

        var size = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(70, size.Width);
        Assert.Equal(60, size.Height);
    }

    [Fact]
    public void ImageButtonHandler_MapsSourceLifecycle_AndIgnoresStaleRuntimeEvents()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png"),
            IsEnabled = true
        };
        var (handler, _, coordinator, _) = CreateAttachedHandler(imageButton);

        AssertEventually(() => !string.IsNullOrWhiteSpace(handler.PlatformView.GetPropertyString("Source")));
        var firstSource = handler.PlatformView.GetPropertyString("Source");
        Assert.NotNull(firstSource);
        Assert.Equal("loading", handler.PlatformView.GetPropertyString("SourceState"));

        coordinator.OnEvent(handler.PlatformView.NodeId, "load", firstSource);
        Assert.Equal("loaded", handler.PlatformView.GetPropertyString("SourceState"));

        imageButton.Source = ImageSource.FromFile("dotnet_bot.png");
        MirageImageButtonHandler.Mapper.UpdateProperty(handler, imageButton, nameof(MauiImage.Source));
        AssertEventually(() => !string.Equals(handler.PlatformView.GetPropertyString("Source"), firstSource, StringComparison.Ordinal));
        var secondSource = handler.PlatformView.GetPropertyString("Source");
        Assert.NotNull(secondSource);

        coordinator.OnEvent(handler.PlatformView.NodeId, "error", firstSource);
        Assert.NotEqual("error", handler.PlatformView.GetPropertyString("SourceState"));

        coordinator.OnEvent(handler.PlatformView.NodeId, "error", secondSource);
        Assert.Equal("error", handler.PlatformView.GetPropertyString("SourceState"));
    }

    [Fact]
    public void ImageButtonHandler_PointerAndKeyboardActivation_RaisesPressedReleasedClicked()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png"),
            IsEnabled = true
        };

        var pressedCount = 0;
        var releasedCount = 0;
        var clickedCount = 0;
        imageButton.Pressed += (_, _) => pressedCount++;
        imageButton.Released += (_, _) => releasedCount++;
        imageButton.Clicked += (_, _) => clickedCount++;

        var (handler, _, coordinator, _) = CreateAttachedHandler(imageButton);

        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerdown", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerup", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "click", null);

        Assert.Equal(1, pressedCount);
        Assert.Equal(1, releasedCount);
        Assert.Equal(1, clickedCount);

        coordinator.OnEvent(handler.PlatformView.NodeId, "keydown", "Enter");
        coordinator.OnEvent(handler.PlatformView.NodeId, "click", null);

        Assert.Equal(2, pressedCount);
        Assert.Equal(2, releasedCount);
        Assert.Equal(2, clickedCount);

        coordinator.OnEvent(handler.PlatformView.NodeId, "keydown", " ");
        coordinator.OnEvent(handler.PlatformView.NodeId, "click", null);

        Assert.Equal(3, pressedCount);
        Assert.Equal(3, releasedCount);
        Assert.Equal(3, clickedCount);
    }

    [Fact]
    public void ImageButtonHandler_StaleKeyboardActivation_DoesNotContaminateLaterPointerClick()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png"),
            IsEnabled = true
        };

        var pressedCount = 0;
        var releasedCount = 0;
        var clickedCount = 0;
        imageButton.Pressed += (_, _) => pressedCount++;
        imageButton.Released += (_, _) => releasedCount++;
        imageButton.Clicked += (_, _) => clickedCount++;

        var (handler, _, coordinator, _) = CreateAttachedHandler(imageButton);

        coordinator.OnEvent(handler.PlatformView.NodeId, "keydown", "Enter");
        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerdown", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerup", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "click", null);

        Assert.Equal(1, pressedCount);
        Assert.Equal(1, releasedCount);
        Assert.Equal(1, clickedCount);
    }

    [Fact]
    public void ImageButtonHandler_DisabledState_BlocksRuntimeInteractionAndMutation()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png"),
            IsEnabled = true
        };

        var pressedCount = 0;
        var releasedCount = 0;
        var clickedCount = 0;
        imageButton.Pressed += (_, _) => pressedCount++;
        imageButton.Released += (_, _) => releasedCount++;
        imageButton.Clicked += (_, _) => clickedCount++;

        var (handler, _, coordinator, _) = CreateAttachedHandler(imageButton);

        imageButton.IsEnabled = false;
        MirageImageButtonHandler.Mapper.UpdateProperty(handler, imageButton, nameof(IView.IsEnabled));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        Assert.Equal("auto", handler.PlatformView.GetStyleValue("pointerEvents"));

        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerdown", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerup", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "keydown", "Enter");
        coordinator.OnEvent(handler.PlatformView.NodeId, "click", null);

        Assert.Equal(0, pressedCount);
        Assert.Equal(0, releasedCount);
        Assert.Equal(0, clickedCount);
    }

    [Fact]
    public void ImageButtonHandler_PointerLeave_DoesNotReleasePressedState()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png"),
            IsEnabled = true
        };

        var pressedCount = 0;
        var releasedCount = 0;
        imageButton.Pressed += (_, _) => pressedCount++;
        imageButton.Released += (_, _) => releasedCount++;

        var (handler, _, coordinator, _) = CreateAttachedHandler(imageButton);

        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerdown", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerleave", null);

        Assert.Equal(1, pressedCount);
        Assert.Equal(0, releasedCount);

        coordinator.OnEvent(handler.PlatformView.NodeId, "pointerup", null);

        Assert.Equal(1, releasedCount);
    }

    [Fact]
    public void ImageButtonHandler_AutomationId_DoesNotBecomeImageAltText()
    {
        var imageButton = new ImageButton
        {
            AutomationId = "controls-sample-imagebutton-parity-host",
            Source = ImageSource.FromFile("cog.png")
        };

        var (handler, _, _, _) = CreateAttachedHandler(imageButton);

        Assert.Equal(string.Empty, handler.PlatformView.GetPropertyString("ImageAlt"));
    }

    [Fact]
    public void ImageButtonHandler_SemanticsDescription_ProjectsAccessibleLabel()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png")
        };
        SemanticProperties.SetDescription(imageButton, "Open settings");
        SemanticProperties.SetHint(imageButton, "Activates the settings image button");

        var (handler, _, _, _) = CreateAttachedHandler(imageButton);

        Assert.Equal("Open settings", handler.PlatformView.GetPropertyString("AriaLabel"));
        Assert.Equal("Activates the settings image button", handler.PlatformView.GetPropertyString("AriaDescription"));
        Assert.Equal(string.Empty, handler.PlatformView.GetPropertyString("ImageAlt"));
    }

    [Fact]
    public void ImageButtonHandler_StrokePaddingCornerAndBackgroundProjection_AreDeterministic()
    {
        var imageButton = new ImageButton
        {
            Source = ImageSource.FromFile("cog.png"),
            BorderWidth = 3,
            BorderColor = Colors.MediumPurple,
            CornerRadius = 9,
            Padding = new Thickness(2, 4, 6, 8),
            Background = Brush.SkyBlue
        };

        var (handler, _, _, _) = CreateAttachedHandler(imageButton);

        Assert.Equal("solid", handler.PlatformView.GetStyleValue("borderStyle"));
        Assert.Equal("3px", handler.PlatformView.GetStyleValue("borderWidth"));
        Assert.Equal("rgba(147,112,216,1)", handler.PlatformView.GetStyleValue("borderColor"));
        Assert.Equal("9px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Equal("4px 6px 8px 2px", handler.PlatformView.GetStyleValue("padding"));
        Assert.Equal("rgba(135,206,235,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
    }

    private static (MirageImageButtonHandler Handler, ImageButton ImageButton, MirageRenderCoordinator Coordinator, TestImageSourceResolver Resolver) CreateAttachedHandler(
        ImageButton imageButton,
        MirageRenderCoordinator? coordinator = null,
        TestImageSourceResolver? resolver = null)
    {
        coordinator ??= new MirageRenderCoordinator(new BrowserDispatcher());
        resolver ??= new TestImageSourceResolver();

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .AddSingleton<IMirageImageSourceResolver>(resolver)
            .BuildServiceProvider();

        var mauiContext = new MauiContext(services);
        var handler = new MirageImageButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(imageButton);
        return (handler, imageButton, coordinator, resolver);
    }

    private static void AssertEventually(Func<bool> condition, int timeoutMs = 1500)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(10);
        }

        Assert.True(condition());
    }

    private sealed class TestImageSourceResolver : IMirageImageSourceResolver
    {
        public ValueTask EnsureInitializedAsync() => ValueTask.CompletedTask;

        public MirageImageSourceResolveResult Resolve(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return MirageImageSourceResolveResult.Unresolved("Source was empty.");
            }

            return MirageImageSourceResolveResult.Resolved($"/_mirage/assets/{source}");
        }

        public MirageImageSourceProjectionResult Resolve(ImageSource? source)
        {
            return BuildProjection(source);
        }

        public ValueTask<MirageImageSourceProjectionResult> ResolveAsync(ImageSource? source, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return ValueTask.FromResult(BuildProjection(source));
        }

        public void Release(string? browserUrl)
        {
            _ = browserUrl;
        }

        private static MirageImageSourceProjectionResult BuildProjection(ImageSource? source)
        {
            if (source is null)
            {
                return MirageImageSourceProjectionResult.Empty();
            }

            var key = ResolveKey(source);
            if (string.IsNullOrWhiteSpace(key))
            {
                return MirageImageSourceProjectionResult.Unresolved("unsupported", "Unsupported image source in test resolver.");
            }

            var isGif = key.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
            return MirageImageSourceProjectionResult.Resolved(
                browserUrl: $"/_mirage/assets/{key}",
                sourceKind: "file",
                isGifSource: isGif);
        }

        private static string? ResolveKey(ImageSource source)
        {
            return source switch
            {
                FileImageSource file => file.File,
                UriImageSource uri => uri.Uri?.ToString(),
                _ => null
            };
        }
    }
}
