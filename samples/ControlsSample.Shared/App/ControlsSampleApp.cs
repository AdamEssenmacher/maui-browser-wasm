using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Controls.Sample.Converters;

namespace ControlsSample.Shared.App;

public class ControlsSampleApp : Application
{
    private static readonly Color UpstreamLightBackgroundColor = Color.FromArgb("#F9F8FF");
    private static readonly Color UpstreamDarkBackgroundColor = Color.FromArgb("#181819");
    private static readonly Color UpstreamLightTextPrimaryColor = Color.FromArgb("#B0150303");
    private static readonly Color UpstreamDarkTextPrimaryColor = Color.FromArgb("#FCFCFC");
    private static readonly Color UpstreamLightNavTextColor = Color.FromArgb("#150303");
    private static readonly Color UpstreamDarkNavTextColor = Color.FromArgb("#F8F8FF");

    public static IServiceProvider? BootstrapServices { get; set; }

    private readonly IServiceProvider? _appServices;

    public ControlsSampleApp()
    {
        Resources ??= new ResourceDictionary();
        SeedUpstreamThemeResources(Resources);
        SeedUpstreamStyleResources(Resources);
        SeedUpstreamConverterResources(Resources);
    }

    public ControlsSampleApp(IServiceProvider services)
        : this()
    {
        _appServices = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var services = activationState?.Context?.Services
                       ?? _appServices
                       ?? BootstrapServices
                       ?? Current?.Handler?.MauiContext?.Services
                       ?? throw new InvalidOperationException("ControlsSampleApp could not resolve activation services.");

        var root = new NavigationPage(new ControlsSampleHomePage(services))
        {
            AutomationId = "controls-sample-navigation-root"
        };
        root.SetAppThemeColor(VisualElement.BackgroundColorProperty, UpstreamLightBackgroundColor, UpstreamDarkBackgroundColor);
        root.SetAppThemeColor(NavigationPage.BarBackgroundColorProperty, UpstreamLightBackgroundColor, UpstreamDarkBackgroundColor);
        root.SetAppThemeColor(NavigationPage.BarTextColorProperty, UpstreamLightNavTextColor, UpstreamDarkNavTextColor);

        return new Window(root)
        {
            Title = "Controls.Sample Wrapper"
        };
    }

    private static void SeedUpstreamThemeResources(ResourceDictionary resources)
    {
        EnsureColorResource(resources, "LightBackgroundColor", UpstreamLightBackgroundColor);
        EnsureColorResource(resources, "DarkBackgroundColor", UpstreamDarkBackgroundColor);
        EnsureColorResource(resources, "LightTextPrimaryColor", UpstreamLightTextPrimaryColor);
        EnsureColorResource(resources, "DarkTextPrimaryColor", UpstreamDarkTextPrimaryColor);
        EnsureColorResource(resources, "WhiteColor", UpstreamDarkNavTextColor);
        EnsureColorResource(resources, "BlackColor", UpstreamLightNavTextColor);
    }

    private static void SeedUpstreamStyleResources(ResourceDictionary resources)
    {
        if (resources.ContainsKey("Headline"))
        {
            return;
        }

        var headlineStyle = new Style(typeof(Label));
        headlineStyle.Setters.Add(new Setter
        {
            Property = Label.FontFamilyProperty,
            Value = "Segoe UI"
        });
        headlineStyle.Setters.Add(new Setter
        {
            Property = Label.FontSizeProperty,
            Value = 10d
        });

        resources["Headline"] = headlineStyle;
    }

    private static void SeedUpstreamConverterResources(ResourceDictionary resources)
    {
        if (!resources.ContainsKey("ThicknessConverter"))
        {
            resources["ThicknessConverter"] = new ThicknessConverter();
        }
    }

    private static void EnsureColorResource(ResourceDictionary resources, string key, Color value)
    {
        if (resources.ContainsKey(key))
        {
            return;
        }

        resources[key] = value;
    }
}
