using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageProgressBarHandlerTests
{
    [Fact]
    public void ProgressBarHandler_ProjectsBuiltInControlKind_AndHostStructure()
    {
        var (handler, _, _) = CreateAttachedHandler(new ProgressBar
        {
            Progress = 0.375,
            ProgressColor = Colors.Crimson
        });

        Assert.Equal(ComponentKind.ProgressBar, handler.PlatformView.ControlKind);
        Assert.Equal("inline-block", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("border-box", handler.PlatformView.GetStyleValue("boxSizing"));
        Assert.Empty(handler.PlatformView.Children);
        Assert.Equal("0.375", handler.PlatformView.GetPropertyString("Value"));
        Assert.Equal("rgba(220,20,60,1)", handler.PlatformView.GetStyleValue("accentColor"));
    }

    [Fact]
    public void ProgressBarHandler_GetDesiredSize_UsesLegacyFallbackAndConstraintClamp()
    {
        var (handler, _, _) = CreateAttachedHandler(new ProgressBar
        {
            Progress = 0.2
        });

        var unconstrained = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(220, unconstrained.Width);
        Assert.Equal(18, unconstrained.Height);

        var constrained = handler.GetDesiredSize(120, 12);
        Assert.Equal(120, constrained.Width);
        Assert.Equal(12, constrained.Height);
    }

    [Fact]
    public void ProgressBarHandler_GetDesiredSize_RespectsExplicitMinAndMaxRequests()
    {
        var (handler, _, _) = CreateAttachedHandler(new ProgressBar
        {
            WidthRequest = 260,
            MaximumWidthRequest = 240,
            HeightRequest = 10,
            MinimumHeightRequest = 24
        });

        var size = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(240, size.Width);
        Assert.Equal(24, size.Height);
    }

    [Fact]
    public void ProgressBarHandler_MapsProgressAndColor_Deterministically()
    {
        var progressBar = new ProgressBar
        {
            Progress = 1.25,
            ProgressColor = Colors.Crimson,
            Background = Brush.Bisque
        };

        var (handler, _, _) = CreateAttachedHandler(progressBar);

        Assert.Equal("1", handler.PlatformView.GetPropertyString("Value"));
        Assert.Equal("rgba(220,20,60,1)", handler.PlatformView.GetStyleValue("accentColor"));
        Assert.Equal("rgba(255,228,196,1)", handler.PlatformView.GetStyleValue("backgroundColor"));

        progressBar.Progress = -0.2;
        MirageProgressBarHandler.Mapper.UpdateProperty(handler, progressBar, nameof(IProgress.Progress));
        Assert.Equal("0", handler.PlatformView.GetPropertyString("Value"));

        progressBar.Progress = 0.3333;
        MirageProgressBarHandler.Mapper.UpdateProperty(handler, progressBar, nameof(IProgress.Progress));
        Assert.Equal("0.333", handler.PlatformView.GetPropertyString("Value"));

        progressBar.Progress = double.NaN;
        MirageProgressBarHandler.Mapper.UpdateProperty(handler, progressBar, nameof(IProgress.Progress));
        Assert.Equal("0", handler.PlatformView.GetPropertyString("Value"));
    }

    private static (MirageProgressBarHandler Handler, ProgressBar ProgressBar, IServiceProvider Services) CreateAttachedHandler(ProgressBar progressBar)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageProgressBarHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(progressBar);
        return (handler, progressBar, services);
    }
}
