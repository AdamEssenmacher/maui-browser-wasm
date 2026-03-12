using Mirage.Maui.Hosting;
using Mirage.Provider.Fluent;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Showcase.Shared.App;
using Showcase.Fluent.App;

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("Showcase.Fluent must run inside browser WebAssembly runtime.");
            Console.Error.WriteLine("Use: dotnet run --project samples/Showcase.Fluent/Showcase.Fluent.csproj -r browser-wasm");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<ShowcaseApp>();
        builder.UseMirageBrowserPlatform();
        builder.UseMirageNavigation(routes =>
        {
            routes.Map<ShowcaseHeroPage>("/");
            routes.Map<ShowcaseNavigationLabPage>("/navigation-lab");
            routes.Map<ShowcaseProjectPage>("/projects/{projectId}");
            routes.SetFallback<ShowcaseHeroPage>();
            routes.SetDefaultTitle("Mirage Showcase");
        });
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(ShowcaseVendorWidget), typeof(ShowcaseVendorWidgetHandler));
        });
        builder.Services.AddSingleton<IShowcaseSectionNavigator, MirageShowcaseSectionNavigator>();
        builder.UseMirageComponentProvider<FluentMirageComponentProvider>();

        var app = builder.Build();
        await MirageBrowserAppHost.RunAsync(app, rootElementId: "app");

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

internal sealed class MirageShowcaseSectionNavigator(IMiragePageNavigator pageNavigator) : IShowcaseSectionNavigator
{
    public bool ScrollToSection(string sectionId, bool smooth = true)
    {
        return pageNavigator.ScrollToAutomationId(sectionId, smooth);
    }
}

internal sealed class ShowcaseApp : Application
{
    public ShowcaseApp()
    {
        Resources ??= new ResourceDictionary();
        Resources.MergedDictionaries.Add(new ShowcaseStyles());
        Resources.Add(new Style(typeof(ShowcaseVendorWidget))
        {
            Setters =
            {
                new Setter { Property = ShowcaseVendorWidget.ChipPaddingProperty, Value = new Thickness(12, 6) },
                new Setter { Property = ShowcaseVendorWidget.ChipCornerRadiusProperty, Value = 999d },
                new Setter { Property = ShowcaseVendorWidget.ChipBorderWidthProperty, Value = 1d },
                new Setter { Property = ShowcaseVendorWidget.ChipBorderColorProperty, Value = Color.FromArgb("#59005F80") },
                new Setter { Property = ShowcaseVendorWidget.ChipBackgroundColorProperty, Value = Color.FromArgb("#14005F80") },
                new Setter { Property = ShowcaseVendorWidget.ChipTextColorProperty, Value = Color.FromArgb("#003B5C") }
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _ = activationState;
        return new Window(new NavigationPage(new ShowcaseHeroPage(CreateMirageGenericWidget))
        {
            AutomationId = "showcase-navigation-root"
        });
    }

    private static View CreateMirageGenericWidget(EventHandler onClicked)
    {
        var widget = new ShowcaseVendorWidget
        {
            AutomationId = "showcase-generic-widget"
        };

        widget.SetBinding(ShowcaseVendorWidget.TextProperty, static (ShowcaseState state) => state.GenericWidgetText);
        widget.Clicked += onClicked;
        return widget;
    }
}
