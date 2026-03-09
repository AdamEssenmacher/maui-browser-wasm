using Mirage.Maui.HotReload;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class XamlHotReloadApplicatorTests
{
    [Fact]
    public void ReapplyingElementXaml_RebindsXNameFieldsWithoutDuplicateNameScopeErrors()
    {
        var page = new ProbePage();

        const string firstXaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <VerticalStackLayout>
                    <Label x:Name="TitleProbe" Text="First pass" />
                </VerticalStackLayout>
            </ContentPage>
            """;

        const string secondXaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <VerticalStackLayout>
                    <Label x:Name="TitleProbe" Text="Second pass" />
                </VerticalStackLayout>
            </ContentPage>
            """;

        var firstApplied = MirageXamlHotReloadApplicator.TryApplyElementXaml(page, firstXaml, out var firstError);
        Assert.True(firstApplied, firstError?.ToString());
        Assert.Equal("First pass", page.TitleProbeField?.Text);

        var secondApplied = MirageXamlHotReloadApplicator.TryApplyElementXaml(page, secondXaml, out var secondError);
        Assert.True(secondApplied, secondError?.ToString());
        Assert.Equal("Second pass", page.TitleProbeField?.Text);
    }

    [Fact]
    public void ReapplyingElementXaml_WithHandlerMissingPlatformView_UsesDetachedFallback()
    {
        var livePage = new ProbePage
        {
            Handler = new NullPlatformElementHandler()
        };
        var window = new Window(livePage);

        const string xaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <VerticalStackLayout>
                    <Label x:Name="TitleProbe" Text="Fallback pass" />
                </VerticalStackLayout>
            </ContentPage>
            """;

        var applied = MirageXamlHotReloadApplicator.TryApplyElementXaml(livePage, xaml, out var error);
        Assert.True(applied, error?.ToString());

        var updatedPage = Assert.IsType<ProbePage>(window.Page);
        Assert.Equal("Fallback pass", updatedPage.TitleProbeField?.Text);
    }

    [Fact]
    public void ReapplyingElementXaml_WithHandlerThrowingPlatformView_UsesDetachedFallback()
    {
        var livePage = new ProbePage
        {
            Handler = new ThrowingPlatformElementHandler()
        };
        var window = new Window(livePage);

        const string xaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <VerticalStackLayout>
                    <Label x:Name="TitleProbe" Text="Throwing fallback pass" />
                </VerticalStackLayout>
            </ContentPage>
            """;

        var applied = MirageXamlHotReloadApplicator.TryApplyElementXaml(livePage, xaml, out var error);
        Assert.True(applied, error?.ToString());

        var updatedPage = Assert.IsType<ProbePage>(window.Page);
        Assert.Equal("Throwing fallback pass", updatedPage.TitleProbeField?.Text);
    }

    [Fact]
    public void ReapplyingElementXaml_WithCtorInjectedPageAndNullPlatformView_UsesFieldResolvedFallback()
    {
        var livePage = new CtorInjectedProbePage(new ProbeToken("alpha"))
        {
            Handler = new NullPlatformElementHandler()
        };
        var window = new Window(livePage);

        const string xaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <Label Text="Ctor fallback pass" />
            </ContentPage>
            """;

        var applied = MirageXamlHotReloadApplicator.TryApplyElementXaml(livePage, xaml, out var error);
        Assert.True(applied, error?.ToString());

        var updatedPage = Assert.IsType<CtorInjectedProbePage>(window.Page);
        Assert.Equal("alpha", updatedPage.TokenName);
        var updatedLabel = Assert.IsType<Label>(updatedPage.Content);
        Assert.Equal("Ctor fallback pass", updatedLabel.Text);
    }

    [Fact]
    public void ReapplyingElementXaml_WithCtorPreloadedXaml_UsesDetachedFallbackWithoutDuplicateNameScope()
    {
        var livePage = new CtorPreloadedXamlProbePage
        {
            Handler = new NullPlatformElementHandler()
        };
        var window = new Window(livePage);

        const string xaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <VerticalStackLayout>
                    <ContentView x:Name="BacklogTableHost" />
                    <Label Text="Ctor preloaded fallback pass" />
                </VerticalStackLayout>
            </ContentPage>
            """;

        var applied = MirageXamlHotReloadApplicator.TryApplyElementXaml(livePage, xaml, out var error);
        Assert.True(applied, error?.ToString());

        var updatedPage = Assert.IsType<CtorPreloadedXamlProbePage>(window.Page);
        var stack = Assert.IsType<VerticalStackLayout>(updatedPage.Content);
        var label = Assert.IsType<Label>(stack.Children[1]);
        Assert.Equal("Ctor preloaded fallback pass", label.Text);
    }

    [Fact]
    public void ReapplyingElementXaml_WithDetachedPageAndProvidedHostWindow_UsesPageSwapFallback()
    {
        var livePage = new CtorInjectedProbePage(new ProbeToken("detached"))
        {
            Handler = new NullPlatformElementHandler()
        };
        var hostWindow = new Window(new ProbePage());

        const string xaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <Label Text="Host window fallback pass" />
            </ContentPage>
            """;

        var applied = MirageXamlHotReloadApplicator.TryApplyElementXaml(livePage, xaml, out var error, hostWindow);
        Assert.True(applied, error?.ToString());

        var updatedPage = Assert.IsType<CtorInjectedProbePage>(hostWindow.Page);
        Assert.Equal("detached", updatedPage.TokenName);
        var updatedLabel = Assert.IsType<Label>(updatedPage.Content);
        Assert.Equal("Host window fallback pass", updatedLabel.Text);
    }

    [Fact]
    public void ReapplyingResourceDictionaryXaml_ClearsAndReloadsEntries()
    {
        var dictionary = new ResourceDictionary();

        const string firstXaml = """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <Color x:Key="AccentColor">#FF0000</Color>
            </ResourceDictionary>
            """;

        const string secondXaml = """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <Color x:Key="AccentColor">#00FF00</Color>
            </ResourceDictionary>
            """;

        var firstApplied = MirageXamlHotReloadApplicator.TryApplyResourceDictionaryXaml(dictionary, firstXaml, out var firstError);
        Assert.True(firstApplied, firstError?.ToString());
        Assert.True(dictionary.ContainsKey("AccentColor"));

        var firstColor = Assert.IsType<Color>(dictionary["AccentColor"]);
        Assert.InRange(firstColor.Red, 0.99f, 1.0f);
        Assert.InRange(firstColor.Green, 0.0f, 0.01f);

        var secondApplied = MirageXamlHotReloadApplicator.TryApplyResourceDictionaryXaml(dictionary, secondXaml, out var secondError);
        Assert.True(secondApplied, secondError?.ToString());
        Assert.True(dictionary.ContainsKey("AccentColor"));

        var secondColor = Assert.IsType<Color>(dictionary["AccentColor"]);
        Assert.InRange(secondColor.Red, 0.0f, 0.01f);
        Assert.InRange(secondColor.Green, 0.99f, 1.0f);
    }

    private sealed class ProbePage : ContentPage
    {
#pragma warning disable CS0649
        private Label? TitleProbe;
#pragma warning restore CS0649

        public Label? TitleProbeField => TitleProbe;
    }

    private interface IProbeToken
    {
        string Name { get; }
    }

    private sealed class ProbeToken : IProbeToken
    {
        public ProbeToken(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class CtorInjectedProbePage : ContentPage
    {
        private readonly IProbeToken _token;

        public CtorInjectedProbePage(IProbeToken token)
        {
            _token = token;
        }

        public string TokenName => _token.Name;
    }

    private sealed class CtorPreloadedXamlProbePage : ContentPage
    {
        public CtorPreloadedXamlProbePage()
        {
#pragma warning disable IL2026
            this.LoadFromXaml("""
                <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                    <VerticalStackLayout>
                        <ContentView x:Name="BacklogTableHost" />
                    </VerticalStackLayout>
                </ContentPage>
                """);
#pragma warning restore IL2026
        }
    }

    private sealed class NullPlatformElementHandler : IViewHandler
    {
        public object PlatformView => null!;

        public IView VirtualView { get; private set; } = null!;

        IElement IElementHandler.VirtualView => VirtualView;

        public IMauiContext MauiContext { get; private set; } = null!;

        public bool HasContainer { get; set; }

        public object ContainerView => null!;

        public void SetMauiContext(IMauiContext mauiContext)
        {
            MauiContext = mauiContext;
        }

        public void SetVirtualView(IElement view)
        {
            VirtualView = (IView)view;
        }

        public void UpdateValue(string property)
        {
            _ = property;
        }

        public void Invoke(string command, object? args)
        {
            _ = command;
            _ = args;
        }

        public void DisconnectHandler()
        {
        }

        public Size GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            _ = widthConstraint;
            _ = heightConstraint;
            return Size.Zero;
        }

        public void PlatformArrange(Rect frame)
        {
            _ = frame;
        }
    }

    private sealed class ThrowingPlatformElementHandler : IViewHandler
    {
        public object PlatformView => throw new InvalidOperationException("Handler detached unexpectedly");

        public IView VirtualView { get; private set; } = null!;

        IElement IElementHandler.VirtualView => VirtualView;

        public IMauiContext MauiContext { get; private set; } = null!;

        public bool HasContainer { get; set; }

        public object ContainerView => null!;

        public void SetMauiContext(IMauiContext mauiContext)
        {
            MauiContext = mauiContext;
        }

        public void SetVirtualView(IElement view)
        {
            VirtualView = (IView)view;
        }

        public void UpdateValue(string property)
        {
            _ = property;
        }

        public void Invoke(string command, object? args)
        {
            _ = command;
            _ = args;
        }

        public void DisconnectHandler()
        {
        }

        public Size GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            _ = widthConstraint;
            _ = heightConstraint;
            return Size.Zero;
        }

        public void PlatformArrange(Rect frame)
        {
            _ = frame;
        }
    }

}
