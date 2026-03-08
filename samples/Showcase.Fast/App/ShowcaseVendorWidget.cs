using Microsoft.Maui.Controls;

namespace Showcase.Fast.App;

public sealed class ShowcaseVendorWidget : View
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ShowcaseVendorWidget),
            default(string));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public event EventHandler? Clicked;

    internal void SendClicked()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}
