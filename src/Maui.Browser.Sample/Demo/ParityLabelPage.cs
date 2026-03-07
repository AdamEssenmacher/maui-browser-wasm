using System.ComponentModel;
using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maui.Browser.Sample.Demo;

public partial class ParityLabelPage : ContentPage
{
    private readonly ParityLabelState _state = new();
    private Label? LivePreviewLabelElement => FindByName("LivePreviewLabel") as Label;
    private Label? FormattedTextProbeLabelElement => FindByName("FormattedTextProbeLabel") as Label;
    private Label? HtmlTextProbeLabelElement => FindByName("HtmlTextProbeLabel") as Label;
    private Label? TextTransformProbeLabelElement => FindByName("TextTransformProbeLabel") as Label;

    public ParityLabelPage()
    {
        LoadXamlFromResource();
        BindingContext = _state;
        _state.PropertyChanged += OnStatePropertyChanged;
        var livePreviewLabel = LivePreviewLabelElement;
        if (livePreviewLabel is not null)
        {
            livePreviewLabel.HandlerChanged += OnLivePreviewLabelHandlerChanged;
        }
        ApplyGapProbeState();
        ApplyPreviewVisibilityOverride();
    }

    private async void OnOpenConsolidatedClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new XamlDemoPage(), false);
    }

    private void OnResetDefaultsClicked(object? sender, EventArgs e)
    {
        _state.ResetDefaults();
        ApplyGapProbeState();
    }

    private void OnApplyTruncationPresetClicked(object? sender, EventArgs e)
    {
        _state.ApplyTruncationPreset();
    }

    private void OnToggleFormattedTextProbeClicked(object? sender, EventArgs e)
    {
        _state.FormattedTextProbeEnabled = !_state.FormattedTextProbeEnabled;
        ApplyFormattedTextProbe();
    }

    private void OnToggleHtmlProbeClicked(object? sender, EventArgs e)
    {
        _state.HtmlProbeEnabled = !_state.HtmlProbeEnabled;
        ApplyHtmlProbe();
    }

    private void OnCycleTextTransformProbeClicked(object? sender, EventArgs e)
    {
        _state.TextTransformProbeIndex = (_state.TextTransformProbeIndex + 1) % 3;
        ApplyTextTransformProbe();
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ParityLabelPage));
#pragma warning restore IL2026
    }

    private void ApplyGapProbeState()
    {
        ApplyFormattedTextProbe();
        ApplyHtmlProbe();
        ApplyTextTransformProbe();
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyPreviewVisibilityOverride();
    }

    private void OnLivePreviewLabelHandlerChanged(object? sender, EventArgs e)
    {
        ApplyPreviewVisibilityOverride();
    }

    private void ApplyPreviewVisibilityOverride()
    {
        if (LivePreviewLabelElement?.Handler?.PlatformView is not DomElement platformView)
        {
            return;
        }

        var visibleDisplay = _state.PreviewMaxLines > 1 ? "-webkit-box" : "flex";
        switch (_state.PreviewVisibility)
        {
            case Visibility.Visible:
                platformView.Style("display", visibleDisplay);
                platformView.Style("visibility", "visible");
                break;
            case Visibility.Hidden:
                platformView.Style("display", visibleDisplay);
                platformView.Style("visibility", "hidden");
                break;
            default:
                platformView.Style("display", "none");
                platformView.Style("visibility", "hidden");
                break;
        }
    }

    private void ApplyFormattedTextProbe()
    {
        var probe = FormattedTextProbeLabelElement;
        if (probe is null)
        {
            return;
        }

        if (_state.FormattedTextProbeEnabled)
        {
            var formatted = new FormattedString();
            formatted.Spans.Add(new Span
            {
                Text = "Bold span ",
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Crimson
            });
            formatted.Spans.Add(new Span
            {
                Text = "Italic span",
                FontAttributes = FontAttributes.Italic,
                TextColor = Colors.DodgerBlue
            });

            probe.FormattedText = formatted;
            return;
        }

        probe.FormattedText = null;
        probe.Text = "FormattedText probe baseline";
    }

    private void ApplyHtmlProbe()
    {
        var probe = HtmlTextProbeLabelElement;
        if (probe is null)
        {
            return;
        }

        if (_state.HtmlProbeEnabled)
        {
            probe.TextType = TextType.Html;
            probe.Text = "<b>Bold HTML</b> + <i>italic HTML</i>";
            return;
        }

        probe.TextType = TextType.Text;
        probe.Text = "TextType probe baseline";
    }

    private void ApplyTextTransformProbe()
    {
        var probe = TextTransformProbeLabelElement;
        if (probe is null)
        {
            return;
        }

        probe.TextTransform = _state.SelectedTextTransformProbe;
        probe.Text = "TextTransform probe baseline";
    }
}
