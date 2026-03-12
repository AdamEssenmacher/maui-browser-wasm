using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Showcase.Fluent.App;

public sealed class ShowcaseVendorWidget : View
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ShowcaseVendorWidget),
            default(string));

    public static readonly BindableProperty ChipPaddingProperty =
        BindableProperty.Create(
            nameof(ChipPadding),
            typeof(Thickness),
            typeof(ShowcaseVendorWidget),
            new Thickness(12, 6));

    public static readonly BindableProperty ChipCornerRadiusProperty =
        BindableProperty.Create(
            nameof(ChipCornerRadius),
            typeof(double),
            typeof(ShowcaseVendorWidget),
            999d);

    public static readonly BindableProperty ChipBorderWidthProperty =
        BindableProperty.Create(
            nameof(ChipBorderWidth),
            typeof(double),
            typeof(ShowcaseVendorWidget),
            1d);

    public static readonly BindableProperty ChipBorderColorProperty =
        BindableProperty.Create(
            nameof(ChipBorderColor),
            typeof(Color),
            typeof(ShowcaseVendorWidget),
            Color.FromArgb("#59005F80"));

    public static readonly BindableProperty ChipBackgroundColorProperty =
        BindableProperty.Create(
            nameof(ChipBackgroundColor),
            typeof(Color),
            typeof(ShowcaseVendorWidget),
            Color.FromArgb("#14005F80"));

    public static readonly BindableProperty ChipTextColorProperty =
        BindableProperty.Create(
            nameof(ChipTextColor),
            typeof(Color),
            typeof(ShowcaseVendorWidget),
            Color.FromArgb("#003B5C"));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Thickness ChipPadding
    {
        get => (Thickness)GetValue(ChipPaddingProperty);
        set => SetValue(ChipPaddingProperty, value);
    }

    public double ChipCornerRadius
    {
        get => (double)GetValue(ChipCornerRadiusProperty);
        set => SetValue(ChipCornerRadiusProperty, value);
    }

    public double ChipBorderWidth
    {
        get => (double)GetValue(ChipBorderWidthProperty);
        set => SetValue(ChipBorderWidthProperty, value);
    }

    public Color? ChipBorderColor
    {
        get => (Color?)GetValue(ChipBorderColorProperty);
        set => SetValue(ChipBorderColorProperty, value);
    }

    public Color? ChipBackgroundColor
    {
        get => (Color?)GetValue(ChipBackgroundColorProperty);
        set => SetValue(ChipBackgroundColorProperty, value);
    }

    public Color? ChipTextColor
    {
        get => (Color?)GetValue(ChipTextColorProperty);
        set => SetValue(ChipTextColorProperty, value);
    }

    public event EventHandler? Clicked;

    internal void SendClicked()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}
