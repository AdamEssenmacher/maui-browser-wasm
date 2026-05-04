using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace Showcase.Shared.App;

public partial class ShowcaseEssentialsLabPage : ContentPage
{
    private const string CounterPreferenceKey = "showcase.essentials.counter";
    private IPreferences? _preferences;
    private IAppInfo? _appInfo;
    private IDeviceInfo? _deviceInfo;
    private IDeviceDisplay? _deviceDisplay;
    private ISemanticScreenReader? _semanticScreenReader;
    private IBrowser? _browser;
    private IClipboard? _clipboard;
    private ILauncher? _launcher;
    private IGeolocation? _geolocation;
    private ITextToSpeech? _textToSpeech;
    private bool _initialized;

    public ShowcaseEssentialsLabPage()
    {
        Title = "Essentials Lab";
        InitializeComponent();
        HandlerChanged += OnPageHandlerChanged;
    }

    private void OnPageHandlerChanged(object? sender, EventArgs e)
    {
        TryInitializeServices();
    }

    private void TryInitializeServices()
    {
        if (_initialized)
        {
            return;
        }

        var services = Handler?.MauiContext?.Services;
        if (services is null)
        {
            return;
        }

        _preferences = services.GetRequiredService<IPreferences>();
        _appInfo = services.GetRequiredService<IAppInfo>();
        _deviceInfo = services.GetRequiredService<IDeviceInfo>();
        _deviceDisplay = services.GetRequiredService<IDeviceDisplay>();
        _semanticScreenReader = services.GetRequiredService<ISemanticScreenReader>();
        _browser = services.GetRequiredService<IBrowser>();
        _clipboard = services.GetRequiredService<IClipboard>();
        _launcher = services.GetRequiredService<ILauncher>();
        _geolocation = services.GetRequiredService<IGeolocation>();
        _textToSpeech = services.GetRequiredService<ITextToSpeech>();
        _initialized = true;
        RefreshView();
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        _preferences!.Remove(CounterPreferenceKey);
        RefreshView();
    }

    private void OnIncrementClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        var nextValue = Preferences.Get(CounterPreferenceKey, 0) + 1;
        Preferences.Set(CounterPreferenceKey, nextValue);
        RefreshView();
    }

    private void OnAnnounceClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        _semanticScreenReader!.Announce("Mirage browser semantic screen reader announcement.");
        SemanticScreenReaderStatusLabel.Text = "Announced through injected ISemanticScreenReader";
    }

    private async void OnClipboardWriteClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        var value = $"Mirage clipboard {DateTimeOffset.UtcNow:HH:mm:ss}";
        await _clipboard!.SetTextAsync(value);
        ClipboardStatusLabel.Text = $"Wrote: {value}";
    }

    private async void OnClipboardReadClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        ClipboardStatusLabel.Text = $"Read: {await _clipboard!.GetTextAsync() ?? "<empty>"}";
    }

    private async void OnCheckLaunchClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        var browserCanOpen = await _browser!.OpenAsync(
            new Uri("https://example.com/"),
            new BrowserLaunchOptions { LaunchMode = BrowserLaunchMode.External });
        var launcherCanMail = await _launcher!.CanOpenAsync(new Uri("mailto:hello@example.com"));
        LaunchStatusLabel.Text = $"BrowserOpen={browserCanOpen}; MailtoCanOpen={launcherCanMail}";
    }

    private async void OnLocationClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        try
        {
            var location = await _geolocation!.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Low, TimeSpan.FromSeconds(10)),
                CancellationToken.None);
            LocationStatusLabel.Text = location is null
                ? "Location unavailable"
                : $"Location: {location.Latitude.ToString("0.#####", CultureInfo.InvariantCulture)}, {location.Longitude.ToString("0.#####", CultureInfo.InvariantCulture)}";
        }
        catch (Exception error)
        {
            LocationStatusLabel.Text = $"Location error: {error.GetType().Name}";
        }
    }

    private async void OnSpeakClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        try
        {
            await _textToSpeech!.SpeakAsync(
                "Mirage browser text to speech.",
                new SpeechOptions(),
                CancellationToken.None);
            TextToSpeechStatusLabel.Text = "SpeakAsync completed";
        }
        catch (Exception error)
        {
            TextToSpeechStatusLabel.Text = $"SpeakAsync error: {error.GetType().Name}";
        }
    }

    private void EnsureInitialized()
    {
        TryInitializeServices();
        if (!_initialized
            || _preferences is null
            || _appInfo is null
            || _deviceInfo is null
            || _deviceDisplay is null
            || _semanticScreenReader is null
            || _browser is null
            || _clipboard is null
            || _launcher is null
            || _geolocation is null
            || _textToSpeech is null)
        {
            throw new InvalidOperationException("ShowcaseEssentialsLabPage requires browser-host Essentials services.");
        }
    }

    private void RefreshView()
    {
        EnsureInitialized();

        PreferencesPatchStatusLabel.Text = $"Preferences patched: {ReferenceEquals(_preferences, Preferences.Default).ToString().ToLowerInvariant()}";
        AppInfoPatchStatusLabel.Text = $"AppInfo patched: {ReferenceEquals(_appInfo, AppInfo.Current).ToString().ToLowerInvariant()}";
        DeviceInfoPatchStatusLabel.Text = $"DeviceInfo patched: {ReferenceEquals(_deviceInfo, DeviceInfo.Current).ToString().ToLowerInvariant()}";

        StaticCounterLabel.Text = Preferences.Get(CounterPreferenceKey, 0).ToString();
        InjectedCounterLabel.Text = _preferences!.Get(CounterPreferenceKey, 0).ToString();

        StaticAppInfoLabel.Text = RenderAppInfoSummary(AppInfo.Current);
        InjectedAppInfoLabel.Text = RenderAppInfoSummary(_appInfo!);

        StaticDeviceInfoLabel.Text = RenderDeviceInfoSummary(DeviceInfo.Current);
        InjectedDeviceInfoLabel.Text = RenderDeviceInfoSummary(_deviceInfo!);
        DeviceDisplayLabel.Text = RenderDisplayInfoSummary(_deviceDisplay!.MainDisplayInfo);
    }

    private static string RenderAppInfoSummary(IAppInfo appInfo)
    {
        var builder = new StringBuilder();
        builder.Append("Name=").AppendLine(appInfo.Name);
        builder.Append("Package=").AppendLine(appInfo.PackageName);
        builder.Append("Version=").AppendLine(appInfo.VersionString);
        builder.Append("Build=").AppendLine(appInfo.BuildString);
        builder.Append("Theme=").AppendLine(appInfo.RequestedTheme.ToString());
        builder.Append("Direction=").AppendLine(appInfo.RequestedLayoutDirection.ToString());
        builder.Append("Packaging=").Append(appInfo.PackagingModel);
        return builder.ToString();
    }

    private static string RenderDeviceInfoSummary(IDeviceInfo deviceInfo)
    {
        var builder = new StringBuilder();
        builder.Append("Platform=").AppendLine(deviceInfo.Platform.ToString());
        builder.Append("Idiom=").AppendLine(deviceInfo.Idiom.ToString());
        builder.Append("Type=").AppendLine(deviceInfo.DeviceType.ToString());
        builder.Append("Model=").Append(deviceInfo.Model);
        return builder.ToString();
    }

    private static string RenderDisplayInfoSummary(DisplayInfo displayInfo)
    {
        var builder = new StringBuilder();
        builder.Append("Width=").AppendLine(displayInfo.Width.ToString("0.##", CultureInfo.InvariantCulture));
        builder.Append("Height=").AppendLine(displayInfo.Height.ToString("0.##", CultureInfo.InvariantCulture));
        builder.Append("Density=").AppendLine(displayInfo.Density.ToString("0.##", CultureInfo.InvariantCulture));
        builder.Append("Orientation=").Append(displayInfo.Orientation);
        return builder.ToString();
    }
}
