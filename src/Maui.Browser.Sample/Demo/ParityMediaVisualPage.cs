using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

public partial class ParityMediaVisualPage : ContentPage
{
    private readonly ParityMediaVisualState _state = new();
    private Image? ImageProbeElement => FindByName("ImageProbe") as Image;
    private ImageButton? ImageButtonProbeElement => FindByName("ImageButtonProbe") as ImageButton;
    private Border? BorderProbeElement => FindByName("BorderProbe") as Border;

    public ParityMediaVisualPage()
    {
        InitializeComponent();
        BindingContext = _state;
        ApplyCurrentImageSource();
        ApplyCurrentAspect();
        ApplyCurrentBorderMode();
    }

    private async void OnOpenConsolidatedClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new XamlDemoPage(), false);
    }

    private void OnCycleSourceClicked(object? sender, EventArgs e)
    {
        _state.CycleSourceMode();
        ApplyCurrentImageSource();
    }

    private void OnUseUnsupportedSourceClicked(object? sender, EventArgs e)
    {
        _state.UseUnsupportedSourceMode();
        ApplyCurrentImageSource();
    }

    private void OnToggleAspectClicked(object? sender, EventArgs e)
    {
        _state.ToggleAspect();
        ApplyCurrentAspect();
    }

    private void OnImageButtonClicked(object? sender, EventArgs e)
    {
        _state.RecordImageButtonClicked();
    }

    private void OnImageButtonPressed(object? sender, EventArgs e)
    {
        _state.RecordImageButtonPressed();
    }

    private void OnImageButtonReleased(object? sender, EventArgs e)
    {
        _state.RecordImageButtonReleased();
    }

    private void OnToggleBorderClicked(object? sender, EventArgs e)
    {
        _state.ToggleBorderMode();
        ApplyCurrentBorderMode();
    }

    private void ApplyCurrentImageSource()
    {
        ImageSource source = _state.CurrentSourceMode switch
        {
            ParityMediaVisualState.MediaSourceMode.Uri => new UriImageSource
            {
                Uri = new Uri(
                    "data:image/svg+xml;utf8," + Uri.EscapeDataString(
                        "<svg xmlns='http://www.w3.org/2000/svg' width='220' height='160' viewBox='0 0 220 160'><rect width='220' height='160' fill='#0284C7'/><text x='110' y='88' text-anchor='middle' fill='white' font-size='28' font-family='sans-serif'>URI</text></svg>"),
                    UriKind.Absolute)
            },
            ParityMediaVisualState.MediaSourceMode.File => new FileImageSource
            {
                File = "assets/media-file.svg"
            },
            ParityMediaVisualState.MediaSourceMode.Font => new FontImageSource
            {
                Glyph = "★",
                FontFamily = "serif",
                Color = Color.FromArgb("#D97706"),
                Size = 24
            },
            _ => new StreamImageSource
            {
                Stream = _ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(
                    "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><rect width='16' height='16' fill='red' /></svg>")))
            }
        };

        if (ImageProbeElement is not null)
        {
            ImageProbeElement.Source = source;
        }

        if (ImageButtonProbeElement is not null)
        {
            ImageButtonProbeElement.Source = source;
        }
    }

    private void ApplyCurrentAspect()
    {
        if (ImageProbeElement is not null)
        {
            ImageProbeElement.Aspect = _state.CurrentAspect;
        }

        if (ImageButtonProbeElement is not null)
        {
            ImageButtonProbeElement.Aspect = _state.CurrentAspect;
        }
    }

    private void ApplyCurrentBorderMode()
    {
        var border = BorderProbeElement;
        if (border is null)
        {
            return;
        }

        switch (_state.CurrentBorderMode)
        {
            case ParityMediaVisualState.MediaBorderMode.SolidRound:
                border.Stroke = new SolidColorBrush(Color.FromArgb("#2563EB"));
                border.StrokeThickness = 3;
                border.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(14)
                };
                border.StrokeDashArray = null;
                border.StrokeDashOffset = 0;
                break;
            case ParityMediaVisualState.MediaBorderMode.DashedRectangle:
                border.Stroke = new SolidColorBrush(Color.FromArgb("#059669"));
                border.StrokeThickness = 4;
                border.StrokeShape = new Rectangle();
                border.StrokeDashArray = new DoubleCollection
                {
                    6,
                    4
                };
                border.StrokeDashOffset = 1;
                break;
            default:
                border.Stroke = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new(Color.FromArgb("#7C3AED"), 0),
                        new(Color.FromArgb("#EC4899"), 1)
                    },
                    new Point(0, 0),
                    new Point(1, 1));
                border.StrokeThickness = 3;
                border.StrokeShape = new Ellipse();
                border.StrokeDashArray = null;
                border.StrokeDashOffset = 0;
                break;
        }
    }
}
