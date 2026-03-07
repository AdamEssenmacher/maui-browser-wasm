using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Maui.Browser.Sample.Demo;

public partial class ParityContainersLayoutPage : ContentPage
{
    private readonly ParityContainersLayoutState _state = new();
    private FlexDirectionProbeMode _flexDirectionMode = FlexDirectionProbeMode.Row;
    private FlexWrapProbeMode _flexWrapMode = FlexWrapProbeMode.NoWrap;
    private FlexJustifyProbeMode _flexJustifyMode = FlexJustifyProbeMode.Start;
    private FlexAlignProbeMode _flexAlignMode = FlexAlignProbeMode.StretchStart;
    private FlexItemPresetMode _flexItemPresetMode = FlexItemPresetMode.Default;

    public ParityContainersLayoutPage()
    {
        LoadXamlFromResource();
        BindingContext = _state;

        ApplyClipMode(ClipProbeMode.None);
        ApplyShadowMode(ShadowProbeMode.Off);
        ApplyBackgroundMode(BackgroundProbeMode.Solid);
        ApplyFlexDirectionMode(_flexDirectionMode);
        ApplyFlexWrapMode(_flexWrapMode);
        ApplyFlexJustifyMode(_flexJustifyMode);
        ApplyFlexAlignMode(_flexAlignMode);
        ApplyFlexItemPresetMode(_flexItemPresetMode);
    }

    private Grid? Probe => FindByName("CoreSubstrateProbe") as Grid;
    private FlexLayout? FlexProbe => FindByName("FlexSemanticProbe") as FlexLayout;
    private View? FlexItemOneView => FindByName("FlexItemOne") as View;
    private View? FlexItemTwoView => FindByName("FlexItemTwo") as View;
    private View? FlexItemThreeView => FindByName("FlexItemThree") as View;

    private async void OnOpenConsolidatedClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new XamlDemoPage(), false);
    }

    private void OnClipNoneClicked(object? sender, EventArgs e)
    {
        ApplyClipMode(ClipProbeMode.None);
    }

    private void OnClipRectangleClicked(object? sender, EventArgs e)
    {
        ApplyClipMode(ClipProbeMode.Rectangle);
    }

    private void OnClipRoundRectangleClicked(object? sender, EventArgs e)
    {
        ApplyClipMode(ClipProbeMode.RoundRectangle);
    }

    private void OnClipUnsupportedClicked(object? sender, EventArgs e)
    {
        ApplyClipMode(ClipProbeMode.UnsupportedEllipse);
    }

    private void OnShadowOffClicked(object? sender, EventArgs e)
    {
        ApplyShadowMode(ShadowProbeMode.Off);
    }

    private void OnShadowOnClicked(object? sender, EventArgs e)
    {
        ApplyShadowMode(ShadowProbeMode.Solid);
    }

    private void OnShadowUnsupportedClicked(object? sender, EventArgs e)
    {
        ApplyShadowMode(ShadowProbeMode.UnsupportedGradient);
    }

    private void OnBackgroundSolidClicked(object? sender, EventArgs e)
    {
        ApplyBackgroundMode(BackgroundProbeMode.Solid);
    }

    private void OnBackgroundLinearClicked(object? sender, EventArgs e)
    {
        ApplyBackgroundMode(BackgroundProbeMode.LinearGradient);
    }

    private void OnBackgroundUnsupportedClicked(object? sender, EventArgs e)
    {
        ApplyBackgroundMode(BackgroundProbeMode.UnsupportedRadial);
    }

    private void OnFlexToggleDirectionClicked(object? sender, EventArgs e)
    {
        _flexDirectionMode = _flexDirectionMode == FlexDirectionProbeMode.Row
            ? FlexDirectionProbeMode.Column
            : FlexDirectionProbeMode.Row;
        ApplyFlexDirectionMode(_flexDirectionMode);
    }

    private void OnFlexToggleWrapClicked(object? sender, EventArgs e)
    {
        _flexWrapMode = _flexWrapMode == FlexWrapProbeMode.NoWrap
            ? FlexWrapProbeMode.Wrap
            : FlexWrapProbeMode.NoWrap;
        ApplyFlexWrapMode(_flexWrapMode);
    }

    private void OnFlexToggleJustifyClicked(object? sender, EventArgs e)
    {
        _flexJustifyMode = _flexJustifyMode == FlexJustifyProbeMode.Start
            ? FlexJustifyProbeMode.SpaceBetween
            : FlexJustifyProbeMode.Start;
        ApplyFlexJustifyMode(_flexJustifyMode);
    }

    private void OnFlexToggleAlignClicked(object? sender, EventArgs e)
    {
        _flexAlignMode = _flexAlignMode == FlexAlignProbeMode.StretchStart
            ? FlexAlignProbeMode.CenterAround
            : FlexAlignProbeMode.StretchStart;
        ApplyFlexAlignMode(_flexAlignMode);
    }

    private void OnFlexToggleItemPresetClicked(object? sender, EventArgs e)
    {
        _flexItemPresetMode = _flexItemPresetMode == FlexItemPresetMode.Default
            ? FlexItemPresetMode.Emphasis
            : FlexItemPresetMode.Default;
        ApplyFlexItemPresetMode(_flexItemPresetMode);
    }

    private void ApplyClipMode(ClipProbeMode mode)
    {
        var probe = Probe;
        if (probe is null)
        {
            return;
        }

        switch (mode)
        {
            case ClipProbeMode.None:
                probe.Clip = null;
                break;
            case ClipProbeMode.Rectangle:
                probe.Clip = new RectangleGeometry(new Rect(18, 12, 155, 88));
                break;
            case ClipProbeMode.RoundRectangle:
                probe.Clip = new RoundRectangleGeometry(new CornerRadius(18, 10, 24, 6), new Rect(12, 8, 176, 98));
                break;
            case ClipProbeMode.UnsupportedEllipse:
                probe.Clip = new EllipseGeometry(new Point(120, 64), 68, 40);
                break;
        }

        _state.SetClipMode(mode);
    }

    private void ApplyShadowMode(ShadowProbeMode mode)
    {
        var probe = Probe;
        if (probe is null)
        {
            return;
        }

        switch (mode)
        {
            case ShadowProbeMode.Off:
                probe.ClearValue(VisualElement.ShadowProperty);
                break;
            case ShadowProbeMode.Solid:
                probe.Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Colors.Black),
                    Opacity = 0.55f,
                    Radius = 12,
                    Offset = new Point(8, 6)
                };
                break;
            case ShadowProbeMode.UnsupportedGradient:
                probe.Shadow = new Shadow
                {
                    Brush = new LinearGradientBrush(
                    [
                        new GradientStop(Colors.MidnightBlue, 0),
                        new GradientStop(Colors.SkyBlue, 1)
                    ], new Point(0, 0), new Point(1, 1)),
                    Opacity = 0.8f,
                    Radius = 12,
                    Offset = new Point(8, 6)
                };
                break;
        }

        _state.SetShadowMode(mode);
    }

    private void ApplyBackgroundMode(BackgroundProbeMode mode)
    {
        var probe = Probe;
        if (probe is null)
        {
            return;
        }

        switch (mode)
        {
            case BackgroundProbeMode.Solid:
                probe.Background = new SolidColorBrush(Colors.LightSkyBlue);
                break;
            case BackgroundProbeMode.LinearGradient:
                probe.Background = new LinearGradientBrush(
                [
                    new GradientStop(Colors.CornflowerBlue, 0),
                    new GradientStop(Colors.LightGoldenrodYellow, 0.5f),
                    new GradientStop(Colors.PaleGreen, 1)
                ], new Point(0, 0), new Point(1, 1));
                break;
            case BackgroundProbeMode.UnsupportedRadial:
                probe.Background = new RadialGradientBrush(
                [
                    new GradientStop(Colors.MediumPurple, 0),
                    new GradientStop(Colors.LightPink, 1)
                ], new Point(0.5, 0.5), 0.7f);
                break;
        }

        _state.SetBackgroundMode(mode);
    }

    private void ApplyFlexDirectionMode(FlexDirectionProbeMode mode)
    {
        var flexProbe = FlexProbe;
        if (flexProbe is null)
        {
            return;
        }

        flexProbe.Direction = mode switch
        {
            FlexDirectionProbeMode.Column => FlexDirection.Column,
            _ => FlexDirection.Row
        };
        _state.SetFlexDirectionMode(mode);
        RefreshFlexProjection();
    }

    private void ApplyFlexWrapMode(FlexWrapProbeMode mode)
    {
        var flexProbe = FlexProbe;
        if (flexProbe is null)
        {
            return;
        }

        flexProbe.Wrap = mode switch
        {
            FlexWrapProbeMode.Wrap => FlexWrap.Wrap,
            _ => FlexWrap.NoWrap
        };
        _state.SetFlexWrapMode(mode);
        RefreshFlexProjection();
    }

    private void ApplyFlexJustifyMode(FlexJustifyProbeMode mode)
    {
        var flexProbe = FlexProbe;
        if (flexProbe is null)
        {
            return;
        }

        flexProbe.JustifyContent = mode switch
        {
            FlexJustifyProbeMode.SpaceBetween => FlexJustify.SpaceBetween,
            _ => FlexJustify.Start
        };
        _state.SetFlexJustifyMode(mode);
        RefreshFlexProjection();
    }

    private void ApplyFlexAlignMode(FlexAlignProbeMode mode)
    {
        var flexProbe = FlexProbe;
        if (flexProbe is null)
        {
            return;
        }

        switch (mode)
        {
            case FlexAlignProbeMode.CenterAround:
                flexProbe.AlignItems = FlexAlignItems.Center;
                flexProbe.AlignContent = FlexAlignContent.SpaceAround;
                break;
            default:
                flexProbe.AlignItems = FlexAlignItems.Stretch;
                flexProbe.AlignContent = FlexAlignContent.Start;
                break;
        }

        _state.SetFlexAlignMode(mode);
        RefreshFlexProjection();
    }

    private void ApplyFlexItemPresetMode(FlexItemPresetMode mode)
    {
        var itemOne = FlexItemOneView;
        var itemTwo = FlexItemTwoView;
        var itemThree = FlexItemThreeView;
        if (itemOne is null || itemTwo is null || itemThree is null)
        {
            return;
        }

        switch (mode)
        {
            case FlexItemPresetMode.Emphasis:
                FlexLayout.SetOrder(itemOne, 1);
                FlexLayout.SetOrder(itemTwo, 0);
                FlexLayout.SetOrder(itemThree, 2);
                FlexLayout.SetGrow(itemTwo, 1f);
                FlexLayout.SetShrink(itemTwo, 1f);
                FlexLayout.SetBasis(itemTwo, new FlexBasis(0.5f, true));
                FlexLayout.SetAlignSelf(itemTwo, FlexAlignSelf.End);
                break;
            default:
                FlexLayout.SetOrder(itemOne, 0);
                FlexLayout.SetOrder(itemTwo, 1);
                FlexLayout.SetOrder(itemThree, 2);
                FlexLayout.SetGrow(itemTwo, 0f);
                FlexLayout.SetShrink(itemTwo, 1f);
                FlexLayout.SetBasis(itemTwo, FlexBasis.Auto);
                FlexLayout.SetAlignSelf(itemTwo, FlexAlignSelf.Auto);
                break;
        }

        _state.SetFlexItemPresetMode(mode);
        RefreshFlexProjection();
    }

    private void RefreshFlexProjection()
    {
        var flexProbe = FlexProbe;
        if (flexProbe?.Handler is null)
        {
            return;
        }

        flexProbe.Handler.UpdateValue(nameof(FlexLayout.Direction));
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ParityContainersLayoutPage));
#pragma warning restore IL2026
    }
}
