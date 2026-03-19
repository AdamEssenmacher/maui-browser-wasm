using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace ControlsSample.Shared.App;

public sealed class AbsoluteLayoutParityProbePage : ContentPage
{
    private readonly AbsoluteLayout _absoluteHost;
    private readonly BoxView _primaryBox;
    private readonly BoxView _secondaryBox;
    private bool _useAlternateBounds;
    private bool _useProportionalFlags;
    private bool _promotePrimary;

    public AbsoluteLayoutParityProbePage()
    {
        Title = "AbsoluteLayout Parity Probe";
        AutomationId = "controls-sample-page-absolutelayoutparitypage";

        _absoluteHost = new AbsoluteLayout
        {
            AutomationId = "controls-sample-absolutelayout-parity-host",
            WidthRequest = 360,
            HeightRequest = 240,
            BackgroundColor = Color.FromArgb("#ECEFF4")
        };

        _primaryBox = new BoxView
        {
            AutomationId = "controls-sample-absolutelayout-parity-a",
            Color = Color.FromArgb("#D1495B"),
            ZIndex = 0
        };

        _secondaryBox = new BoxView
        {
            AutomationId = "controls-sample-absolutelayout-parity-b",
            Color = Color.FromArgb("#00798C"),
            ZIndex = 10
        };

        _absoluteHost.Children.Add(_primaryBox);
        _absoluteHost.Children.Add(_secondaryBox);

        ApplyCurrentBounds();

        var controls = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Button
                {
                    Text = "Toggle Bounds",
                    AutomationId = "controls-sample-absolutelayout-parity-toggle-bounds",
                    Command = new Command(ToggleBounds)
                },
                new Button
                {
                    Text = "Toggle Flags",
                    AutomationId = "controls-sample-absolutelayout-parity-toggle-flags",
                    Command = new Command(ToggleFlags)
                },
                new Button
                {
                    Text = "Toggle ZIndex",
                    AutomationId = "controls-sample-absolutelayout-parity-toggle-zindex",
                    Command = new Command(ToggleZIndex)
                }
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "AbsoluteLayout parity probe: mutate bounds, flags, and z-order at runtime.",
                        AutomationId = "controls-sample-absolutelayout-parity-description"
                    },
                    _absoluteHost,
                    controls
                }
            }
        };
    }

    private void ToggleBounds()
    {
        _useAlternateBounds = !_useAlternateBounds;
        ApplyCurrentBounds();
    }

    private void ToggleFlags()
    {
        _useProportionalFlags = !_useProportionalFlags;
        ApplyCurrentBounds();
    }

    private void ToggleZIndex()
    {
        _promotePrimary = !_promotePrimary;
        _primaryBox.ZIndex = _promotePrimary ? 20 : 0;
        _secondaryBox.ZIndex = _promotePrimary ? 0 : 10;
    }

    private void ApplyCurrentBounds()
    {
        var flags = _useProportionalFlags ? AbsoluteLayoutFlags.All : AbsoluteLayoutFlags.None;
        AbsoluteLayout.SetLayoutFlags(_primaryBox, flags);
        AbsoluteLayout.SetLayoutFlags(_secondaryBox, flags);

        var primaryBounds = _useProportionalFlags
            ? (_useAlternateBounds
                ? new Rect(0.28, 0.24, 0.58, 0.52)
                : new Rect(0.10, 0.12, 0.46, 0.36))
            : (_useAlternateBounds
                ? new Rect(148, 102, 170, 112)
                : new Rect(20, 24, 124, 90));

        var secondaryBounds = _useProportionalFlags
            ? (_useAlternateBounds
                ? new Rect(0.08, 0.10, 0.42, 0.44)
                : new Rect(0.30, 0.24, 0.46, 0.38))
            : (_useAlternateBounds
                ? new Rect(32, 36, 130, 94)
                : new Rect(88, 64, 136, 96));

        AbsoluteLayout.SetLayoutBounds(_primaryBox, primaryBounds);
        AbsoluteLayout.SetLayoutBounds(_secondaryBox, secondaryBounds);
        _absoluteHost.InvalidateMeasure();
    }
}
