using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Controls.Sample.Converters;
using System.Linq;

namespace ControlsSample.Shared.App;

public class ControlsSampleApp : Application
{
    private static readonly Color UpstreamLightBackgroundColor = Color.FromArgb("#F9F8FF");
    private static readonly Color UpstreamDarkBackgroundColor = Color.FromArgb("#181819");
    private static readonly Color UpstreamLightBackgroundSecondaryColor = Color.FromArgb("#F9F9FF");
    private static readonly Color UpstreamDarkBackgroundSecondaryColor = Color.FromArgb("#313133");
    private static readonly Color UpstreamLightTextPrimaryColor = Color.FromArgb("#B0150303");
    private static readonly Color UpstreamDarkTextPrimaryColor = Color.FromArgb("#FCFCFC");
    private static readonly Color UpstreamLightTextSecondaryColor = Color.FromArgb("#B01D1D1D");
    private static readonly Color UpstreamDarkTextSecondaryColor = Color.FromArgb("#F5F5F5");
    private static readonly Color UpstreamLightAccentColor = Color.FromArgb("#5639B0");
    private static readonly Color UpstreamDarkAccentColor = Color.FromArgb("#7E2BEA");
    private static readonly Color UpstreamLightBorderColor = Color.FromArgb("#C8C8C8");
    private static readonly Color UpstreamDarkBorderColor = Color.FromArgb("#FEFEFE");
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
        ApplyThemeAwareStyleValues(Resources, RequestedTheme);
        RequestedThemeChanged += OnRequestedThemeChanged;
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
        EnsureColorResource(resources, "LightBackgroundSecondaryColor", UpstreamLightBackgroundSecondaryColor);
        EnsureColorResource(resources, "DarkBackgroundSecondaryColor", UpstreamDarkBackgroundSecondaryColor);
        EnsureColorResource(resources, "LightTextPrimaryColor", UpstreamLightTextPrimaryColor);
        EnsureColorResource(resources, "DarkTextPrimaryColor", UpstreamDarkTextPrimaryColor);
        EnsureColorResource(resources, "LightTextSecondaryColor", UpstreamLightTextSecondaryColor);
        EnsureColorResource(resources, "DarkTextSecondaryColor", UpstreamDarkTextSecondaryColor);
        EnsureColorResource(resources, "LightAccentColor", UpstreamLightAccentColor);
        EnsureColorResource(resources, "DarkAccentColor", UpstreamDarkAccentColor);
        EnsureColorResource(resources, "LightBorderColor", UpstreamLightBorderColor);
        EnsureColorResource(resources, "DarkBorderColor", UpstreamDarkBorderColor);
        EnsureColorResource(resources, "WhiteColor", UpstreamDarkNavTextColor);
        EnsureColorResource(resources, "BlackColor", UpstreamLightNavTextColor);
    }

    private static void SeedUpstreamStyleResources(ResourceDictionary resources)
    {
        if (!resources.ContainsKey("SearchBorderStyle"))
        {
            var searchBorderStyle = new Style(typeof(Frame));
            searchBorderStyle.Setters.Add(new Setter
            {
                Property = Frame.BackgroundColorProperty,
                Value = UpstreamLightBackgroundSecondaryColor
            });
            searchBorderStyle.Setters.Add(new Setter
            {
                Property = Frame.HasShadowProperty,
                Value = true
            });
            searchBorderStyle.Setters.Add(new Setter
            {
                Property = Frame.PaddingProperty,
                Value = new Thickness(0)
            });
            searchBorderStyle.Setters.Add(new Setter
            {
                Property = Frame.HeightRequestProperty,
                Value = 40d
            });
            searchBorderStyle.Setters.Add(new Setter
            {
                Property = Frame.VerticalOptionsProperty,
                Value = LayoutOptions.End
            });
            searchBorderStyle.Setters.Add(new Setter
            {
                Property = Frame.MarginProperty,
                Value = new Thickness(12, 0)
            });
            resources["SearchBorderStyle"] = searchBorderStyle;
        }

        if (!resources.ContainsKey("GalleryItemContainerStyle"))
        {
            var galleryItemContainerStyle = new Style(typeof(Border));
            galleryItemContainerStyle.Setters.Add(new Setter
            {
                Property = Border.BackgroundColorProperty,
                Value = UpstreamLightBackgroundSecondaryColor
            });
            galleryItemContainerStyle.Setters.Add(new Setter
            {
                Property = Border.StrokeProperty,
                Value = new SolidColorBrush(UpstreamLightBorderColor)
            });
            galleryItemContainerStyle.Setters.Add(new Setter
            {
                Property = Border.PaddingProperty,
                Value = new Thickness(12)
            });
            galleryItemContainerStyle.Setters.Add(new Setter
            {
                Property = Border.MarginProperty,
                Value = new Thickness(0)
            });
            resources["GalleryItemContainerStyle"] = galleryItemContainerStyle;
        }

        if (!resources.ContainsKey("GalleryItemTitleStyle"))
        {
            var galleryItemTitleStyle = new Style(typeof(Label));
            galleryItemTitleStyle.Setters.Add(new Setter
            {
                Property = Label.FontSizeProperty,
                Value = 12d
            });
            galleryItemTitleStyle.Setters.Add(new Setter
            {
                Property = Label.FontFamilyProperty,
                Value = "Segoe UI"
            });
            galleryItemTitleStyle.Setters.Add(new Setter
            {
                Property = Label.FontAttributesProperty,
                Value = FontAttributes.Bold
            });
            galleryItemTitleStyle.Setters.Add(new Setter
            {
                Property = Label.TextColorProperty,
                Value = UpstreamLightAccentColor
            });
            galleryItemTitleStyle.Setters.Add(new Setter
            {
                Property = Label.MarginProperty,
                Value = new Thickness(0, 0, 0, 6)
            });
            resources["GalleryItemTitleStyle"] = galleryItemTitleStyle;
        }

        if (!resources.ContainsKey("GalleryItemDescriptionStyle"))
        {
            var galleryItemDescriptionStyle = new Style(typeof(Label));
            galleryItemDescriptionStyle.Setters.Add(new Setter
            {
                Property = Label.FontSizeProperty,
                Value = 11d
            });
            galleryItemDescriptionStyle.Setters.Add(new Setter
            {
                Property = Label.FontFamilyProperty,
                Value = "Segoe UI"
            });
            galleryItemDescriptionStyle.Setters.Add(new Setter
            {
                Property = Label.TextColorProperty,
                Value = UpstreamLightTextPrimaryColor
            });
            galleryItemDescriptionStyle.Setters.Add(new Setter
            {
                Property = Label.LineBreakModeProperty,
                Value = LineBreakMode.WordWrap
            });
            resources["GalleryItemDescriptionStyle"] = galleryItemDescriptionStyle;
        }

        if (!resources.ContainsKey("Headline"))
        {
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
            headlineStyle.Setters.Add(new Setter
            {
                Property = Label.TextColorProperty,
                Value = UpstreamLightTextPrimaryColor
            });

            resources["Headline"] = headlineStyle;
        }

        if (!resources.ContainsKey("Subhead"))
        {
            var subheadStyle = new Style(typeof(Label));
            subheadStyle.Setters.Add(new Setter
            {
                Property = Label.FontFamilyProperty,
                Value = "Segoe UI"
            });
            subheadStyle.Setters.Add(new Setter
            {
                Property = Label.FontSizeProperty,
                Value = 9d
            });
            subheadStyle.Setters.Add(new Setter
            {
                Property = Label.TextColorProperty,
                Value = UpstreamLightTextPrimaryColor
            });

            resources["Subhead"] = subheadStyle;
        }
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

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        _ = sender;
        if (Resources is null)
        {
            return;
        }

        ApplyThemeAwareStyleValues(Resources, e.RequestedTheme);
    }

    private static void ApplyThemeAwareStyleValues(ResourceDictionary resources, AppTheme theme)
    {
        var useDarkTheme = theme == AppTheme.Dark;
        var backgroundSecondary = useDarkTheme ? UpstreamDarkBackgroundSecondaryColor : UpstreamLightBackgroundSecondaryColor;
        var accent = useDarkTheme ? UpstreamDarkAccentColor : UpstreamLightAccentColor;
        var textPrimary = useDarkTheme ? UpstreamDarkTextPrimaryColor : UpstreamLightTextPrimaryColor;
        var borderBrush = useDarkTheme
            ? (Brush)new SolidColorBrush(UpstreamDarkBorderColor)
            : new SolidColorBrush(UpstreamLightBorderColor);

        SetStyleSetterValue(resources, "SearchBorderStyle", Frame.BackgroundColorProperty, backgroundSecondary);
        SetStyleSetterValue(resources, "GalleryItemContainerStyle", Border.BackgroundColorProperty, backgroundSecondary);
        SetStyleSetterValue(resources, "GalleryItemContainerStyle", Border.StrokeProperty, borderBrush);
        SetStyleSetterValue(resources, "GalleryItemTitleStyle", Label.TextColorProperty, accent);
        SetStyleSetterValue(resources, "GalleryItemDescriptionStyle", Label.TextColorProperty, textPrimary);
        SetStyleSetterValue(resources, "Headline", Label.TextColorProperty, textPrimary);
        SetStyleSetterValue(resources, "Subhead", Label.TextColorProperty, textPrimary);
    }

    private static void SetStyleSetterValue(
        ResourceDictionary resources,
        string styleKey,
        BindableProperty property,
        object value)
    {
        if (!resources.TryGetValue(styleKey, out var styleResource) || styleResource is not Style style)
        {
            return;
        }

        var setter = style.Setters.FirstOrDefault(candidate => candidate.Property == property);
        if (setter is null)
        {
            style.Setters.Add(new Setter { Property = property, Value = value });
            return;
        }

        setter.Value = value;
    }

}
