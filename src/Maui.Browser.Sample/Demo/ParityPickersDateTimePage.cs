using Microsoft.Maui.Controls;

namespace Maui.Browser.Sample.Demo;

public partial class ParityPickersDateTimePage : ContentPage
{
    private readonly ParityPickersDateTimeState _state = new();
    private Stepper? StepperProbeElement => FindByName("StepperProbe") as Stepper;

    public ParityPickersDateTimePage()
    {
        InitializeComponent();
        BindingContext = _state;
    }

    private async void OnOpenConsolidatedClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new XamlDemoPage(), false);
    }

    private void OnToggleActivityClicked(object? sender, EventArgs e)
    {
        _state.ToggleActivity();
    }

    private void OnIncrementProgressClicked(object? sender, EventArgs e)
    {
        _state.IncrementProgress();
    }

    private void OnApplyProgrammaticUpdateClicked(object? sender, EventArgs e)
    {
        _state.ApplyProgrammaticUpdate();
        ReapplyStepperVisibility();
    }

    private void ReapplyStepperVisibility()
    {
        var stepper = StepperProbeElement;
        if (stepper is null || !stepper.IsVisible)
        {
            return;
        }

        var originalVisibility = stepper.IsVisible;
        stepper.IsVisible = false;
        stepper.IsVisible = originalVisibility;
    }
}
