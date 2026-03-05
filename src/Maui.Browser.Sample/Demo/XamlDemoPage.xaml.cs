using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

public partial class XamlDemoPage : ContentPage
{
    private readonly DemoState _state = new();
    private VerticalStackLayout? _rootLayout;

    internal static XamlDemoPage? Current { get; private set; }

    public XamlDemoPage()
        : this(isProbePage: false)
    {
        LoadXamlFromResource();
        BindStateAndAttachRootLayout();
    }

    private XamlDemoPage(bool isProbePage)
    {
        if (isProbePage)
        {
            return;
        }

        Current = this;
        HotReloadMetadataUpdateHandler.MetadataDeltaApplied += OnMetadataDeltaApplied;
    }

    private void OnIncrementClicked(object? sender, EventArgs e)
    {
        _state.ClickCount++;
    }

    private void OnToggleVisibilityClicked(object? sender, EventArgs e)
    {
        _state.ToggleProbeVisibility();
    }

    private void OnCycleOpacityClicked(object? sender, EventArgs e)
    {
        _state.CycleProbeOpacity();
    }

    private void OnStressClicked(object? sender, EventArgs e)
    {
        var stressHost = FindByName("StressHost") as VerticalStackLayout;
        if (stressHost is null)
        {
            return;
        }

        RunLeakStress(stressHost);
        _state.RecordStressRun();
    }

    private async void OnForceReloadClicked(object? sender, EventArgs e)
    {
        try
        {
            await BrowserPageInterop.EnsureModuleLoadedAsync();
            BrowserPageInterop.ForceReload();
        }
        catch (Exception ex)
        {
            _state.RecordXamlLiveApplyFailure($"Force reload failed: {ex.Message}");
        }
    }

    internal bool TryApplyLiveXaml(string xaml, out string? error)
    {
        error = null;

        var previousContent = Content;
        var previousTitle = Title;
        var previousRootLayout = _rootLayout;

        try
        {
            ValidateLiveXaml(xaml);

            DetachRootLayout();
#pragma warning disable IL2026
            this.LoadFromXaml(xaml);
#pragma warning restore IL2026
            BindStateAndAttachRootLayout();

            HotReloadStatus.RecordXamlLiveApply();
            _state.RecordXamlLiveApplySuccess();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;

            Content = previousContent;
            Title = previousTitle;
            BindingContext = _state;

            DetachRootLayout();
            _rootLayout = previousRootLayout;
            if (_rootLayout is not null)
            {
                _rootLayout.SizeChanged += OnRootLayoutSizeChanged;
                UpdateViewportFromRoot();
            }

            HotReloadStatus.RecordXamlApplyFailure();
            _state.RecordXamlLiveApplyFailure(error);
            return false;
        }
    }

    private void OnMetadataDeltaApplied()
    {
        var dispatcher = Dispatcher;
        if (dispatcher.IsDispatchRequired)
        {
            dispatcher.Dispatch(() => _state.RecordMetadataDeltaApplied());
            return;
        }

        _state.RecordMetadataDeltaApplied();
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(XamlDemoPage));
#pragma warning restore IL2026
    }

    private void BindStateAndAttachRootLayout()
    {
        BindingContext = _state;
        _state.RefreshHotReloadSummary();

        DetachRootLayout();
        _rootLayout = FindByName("RootLayout") as VerticalStackLayout;
        if (_rootLayout is not null)
        {
            _rootLayout.SizeChanged += OnRootLayoutSizeChanged;
            UpdateViewportFromRoot();
        }
    }

    private void DetachRootLayout()
    {
        if (_rootLayout is null)
        {
            return;
        }

        _rootLayout.SizeChanged -= OnRootLayoutSizeChanged;
        _rootLayout = null;
    }

    private void OnRootLayoutSizeChanged(object? sender, EventArgs e)
    {
        UpdateViewportFromRoot();
    }

    private void UpdateViewportFromRoot()
    {
        if (_rootLayout is null)
        {
            return;
        }

        _state.ViewportText = $"Viewport/layout size: {_rootLayout.Width:0} x {_rootLayout.Height:0}";
    }

    private static void ValidateLiveXaml(string xaml)
    {
        var probe = new XamlDemoPage(isProbePage: true);
#pragma warning disable IL2026
        probe.LoadFromXaml(xaml);
#pragma warning restore IL2026
    }

    private static void RunLeakStress(VerticalStackLayout stressHost)
    {
        const int cycleCount = 12;
        const int controlsPerCycle = 6;

        for (var cycle = 0; cycle < cycleCount; cycle++)
        {
            var cycleLayout = new VerticalStackLayout
            {
                Spacing = 2
            };

            for (var i = 0; i < controlsPerCycle; i++)
            {
                var row = new HorizontalStackLayout
                {
                    Spacing = 4
                };

                var probeLabel = new Label
                {
                    Text = $"Transient {cycle}-{i}",
                    FontSize = 11,
                    TextColor = Colors.DarkGray
                };

                var probeButton = new Button
                {
                    Text = $"Probe {cycle}-{i}",
                    Padding = new Thickness(8, 4),
                    FontSize = 11,
                    TextColor = Colors.Black
                };
                probeButton.Clicked += static (_, _) => { };

                row.Children.Add(probeLabel);
                row.Children.Add(probeButton);
                cycleLayout.Children.Add(row);
            }

            stressHost.Children.Add(cycleLayout);
            stressHost.Children.Remove(cycleLayout);
        }

        stressHost.Clear();
    }
}
