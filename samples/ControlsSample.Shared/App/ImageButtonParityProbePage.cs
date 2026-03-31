using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class ImageButtonParityProbePage : ContentPage
{
    private readonly ImageButton _imageButton;
    private readonly Label _eventStateLabel;
    private readonly Label _projectionStateLabel;
    private readonly Label _styleStateLabel;

    private int _pressedCount;
    private int _releasedCount;
    private int _clickedCount;
    private bool _useAlternateSource;
    private bool _useAlternateAspect;
    private bool _useAlternateChrome;

    public ImageButtonParityProbePage()
    {
        Title = "ImageButton Parity Probe";
        AutomationId = "controls-sample-page-imagebuttonparitypage";

        _imageButton = new ImageButton
        {
            AutomationId = "controls-sample-imagebutton-parity-host",
            Source = ImageSource.FromFile("cog.png"),
            Aspect = Aspect.AspectFit,
            WidthRequest = 120,
            HeightRequest = 120,
            Padding = new Thickness(16),
            BorderWidth = 2,
            BorderColor = Color.FromArgb("#475569"),
            CornerRadius = 18,
            Background = new SolidColorBrush(Color.FromArgb("#E2E8F0"))
        };
        SemanticProperties.SetDescription(_imageButton, "Image button parity probe");
        SemanticProperties.SetHint(_imageButton, "Activates the parity probe image button");

        _eventStateLabel = new Label
        {
            AutomationId = "controls-sample-imagebutton-parity-events",
            FontAttributes = FontAttributes.Bold
        };
        _projectionStateLabel = new Label
        {
            AutomationId = "controls-sample-imagebutton-parity-projection"
        };
        _styleStateLabel = new Label
        {
            AutomationId = "controls-sample-imagebutton-parity-style"
        };

        _imageButton.Pressed += (_, _) =>
        {
            _pressedCount++;
            UpdateStateLabels();
        };
        _imageButton.Released += (_, _) =>
        {
            _releasedCount++;
            UpdateStateLabels();
        };
        _imageButton.Clicked += (_, _) =>
        {
            _clickedCount++;
            UpdateStateLabels();
        };
        _imageButton.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(ImageButton.Source)
                or nameof(ImageButton.Aspect)
                or nameof(ImageButton.IsEnabled)
                or nameof(ImageButton.Padding)
                or nameof(ImageButton.BorderWidth)
                or nameof(ImageButton.CornerRadius))
            {
                UpdateStateLabels();
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
                        AutomationId = "controls-sample-imagebutton-parity-description",
                        Text = "ImageButton parity probe for source lifecycle, aspect projection, interaction counters, chrome updates, and enabled-state gating."
                    },
                    _imageButton,
                    _eventStateLabel,
                    _projectionStateLabel,
                    _styleStateLabel,
                    BuildControls()
                }
            }
        };

        UpdateStateLabels();
    }

    private View BuildControls()
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Source", "controls-sample-imagebutton-parity-toggle-source", ToggleSource),
                        CreateButton("Toggle Aspect", "controls-sample-imagebutton-parity-toggle-aspect", ToggleAspect),
                        CreateButton("Toggle Style", "controls-sample-imagebutton-parity-toggle-style", ToggleStyle)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Enabled", "controls-sample-imagebutton-parity-toggle-enabled", () =>
                        {
                            _imageButton.IsEnabled = !_imageButton.IsEnabled;
                            UpdateStateLabels();
                        }),
                        CreateButton("Reset Counters", "controls-sample-imagebutton-parity-reset-counters", ResetCounters)
                    }
                }
            }
        };
    }

    private static Button CreateButton(string text, string automationId, Action action)
    {
        return new Button
        {
            Text = text,
            AutomationId = automationId,
            Command = new Command(action)
        };
    }

    private void ToggleSource()
    {
        _useAlternateSource = !_useAlternateSource;
        _imageButton.Source = ImageSource.FromFile(_useAlternateSource ? "dotnet_bot.png" : "cog.png");
        UpdateStateLabels();
    }

    private void ToggleAspect()
    {
        _useAlternateAspect = !_useAlternateAspect;
        _imageButton.Aspect = _useAlternateAspect ? Aspect.AspectFill : Aspect.AspectFit;
        UpdateStateLabels();
    }

    private void ToggleStyle()
    {
        _useAlternateChrome = !_useAlternateChrome;
        _imageButton.Padding = _useAlternateChrome ? new Thickness(6, 10, 14, 18) : new Thickness(16);
        _imageButton.BorderWidth = _useAlternateChrome ? 4 : 2;
        _imageButton.BorderColor = _useAlternateChrome
            ? Color.FromArgb("#7C3AED")
            : Color.FromArgb("#475569");
        _imageButton.CornerRadius = _useAlternateChrome ? 30 : 18;
        _imageButton.Background = _useAlternateChrome
            ? new SolidColorBrush(Color.FromArgb("#EDE9FE"))
            : new SolidColorBrush(Color.FromArgb("#E2E8F0"));
        UpdateStateLabels();
    }

    private void ResetCounters()
    {
        _pressedCount = 0;
        _releasedCount = 0;
        _clickedCount = 0;
        UpdateStateLabels();
    }

    private void UpdateStateLabels()
    {
        _eventStateLabel.Text = $"pressed={_pressedCount} released={_releasedCount} clicked={_clickedCount}";
        _projectionStateLabel.Text = $"source={(_useAlternateSource ? "dotnet_bot.png" : "cog.png")} aspect={_imageButton.Aspect} enabled={_imageButton.IsEnabled}";
        _styleStateLabel.Text = $"padding={_imageButton.Padding.Left},{_imageButton.Padding.Top},{_imageButton.Padding.Right},{_imageButton.Padding.Bottom} borderWidth={_imageButton.BorderWidth} cornerRadius={_imageButton.CornerRadius}";
    }
}
