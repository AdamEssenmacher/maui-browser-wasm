using System.Text.Json;
using System.Text.Json.Serialization;
using Mirage.Web.Dom;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;

namespace Mirage.Maui.Essentials;

internal sealed class MirageBrowserSemanticScreenReader : ISemanticScreenReader
{
    public void Announce(string text) => DomElement.AnnounceSemanticText(text);
}

internal sealed class MirageBrowserBrowser : IBrowser
{
    public Task<bool> OpenAsync(Uri uri, BrowserLaunchOptions options)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!IsWebUri(uri))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(DomElement.OpenExternalUri(uri, "_blank"));
    }

    private static bool IsWebUri(Uri uri) =>
        uri.IsAbsoluteUri
        && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}

internal sealed class MirageBrowserClipboard : IClipboard
{
    public bool HasText => DomElement.HasClipboardText();

    public event EventHandler<EventArgs>? ClipboardContentChanged;

    public async Task SetTextAsync(string text)
    {
        var changed = await DomElement.SetClipboardTextAsync(text);
        if (changed)
        {
            ClipboardContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task<string?> GetTextAsync() => DomElement.GetClipboardTextAsync();
}

internal sealed class MirageBrowserLauncher : ILauncher
{
    private static readonly HashSet<string> SupportedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
        Uri.UriSchemeMailto,
        "tel",
        "sms",
        "geo"
    };

    public Task<bool> CanOpenAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return Task.FromResult(uri.IsAbsoluteUri && SupportedSchemes.Contains(uri.Scheme));
    }

    public async Task<bool> OpenAsync(Uri uri)
    {
        if (!await CanOpenAsync(uri))
        {
            return false;
        }

        return DomElement.OpenExternalUri(uri, "_blank");
    }

    public Task<bool> OpenAsync(OpenFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fullPath = request.File?.FullPath;
        if (string.IsNullOrWhiteSpace(fullPath) || !Uri.TryCreate(fullPath, UriKind.Absolute, out var uri))
        {
            return Task.FromResult(false);
        }

        return OpenAsync(uri);
    }

    public async Task<bool> TryOpenAsync(Uri uri)
    {
        try
        {
            return await OpenAsync(uri);
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class MirageBrowserDeviceDisplay : IDeviceDisplay, IDisposable
{
    private int _resizeSubscriptionId;
    private DisplayInfo _lastDisplayInfo;
    private bool _keepScreenOn;
    private event EventHandler<DisplayInfoChangedEventArgs>? MainDisplayInfoChangedCore;

    public MirageBrowserDeviceDisplay()
    {
        _lastDisplayInfo = ReadDisplayInfo();
    }

    public bool KeepScreenOn
    {
        get => _keepScreenOn;
        set => _keepScreenOn = value;
    }

    public DisplayInfo MainDisplayInfo
    {
        get
        {
            _lastDisplayInfo = ReadDisplayInfo();
            return _lastDisplayInfo;
        }
    }

    public event EventHandler<DisplayInfoChangedEventArgs>? MainDisplayInfoChanged
    {
        add
        {
            MainDisplayInfoChangedCore += value;
            EnsureResizeSubscription();
        }
        remove
        {
            MainDisplayInfoChangedCore -= value;
            if (MainDisplayInfoChangedCore is null)
            {
                ClearResizeSubscription();
            }
        }
    }

    public void Dispose() => ClearResizeSubscription();

    private void EnsureResizeSubscription()
    {
        if (_resizeSubscriptionId != 0)
        {
            return;
        }

        _resizeSubscriptionId = DomElement.SubscribeWindowResize(OnWindowResize);
    }

    private void ClearResizeSubscription()
    {
        if (_resizeSubscriptionId == 0)
        {
            return;
        }

        DomElement.UnsubscribeWindowResize(_resizeSubscriptionId);
        _resizeSubscriptionId = 0;
    }

    private void OnWindowResize()
    {
        var next = ReadDisplayInfo();
        if (DisplayInfoEquals(_lastDisplayInfo, next))
        {
            return;
        }

        _lastDisplayInfo = next;
        MainDisplayInfoChangedCore?.Invoke(this, new DisplayInfoChangedEventArgs(next));
    }

    private static DisplayInfo ReadDisplayInfo()
    {
        var display = DomElement.GetDisplayInfo();
        var width = NormalizeDimension(display.Width, 1024d);
        var height = NormalizeDimension(display.Height, 768d);
        var density = NormalizeDimension(display.Density, 1d);
        var orientation = Enum.IsDefined(typeof(DisplayOrientation), display.Orientation)
            ? (DisplayOrientation)display.Orientation
            : DisplayOrientation.Unknown;
        var rotation = Enum.IsDefined(typeof(DisplayRotation), display.Rotation)
            ? (DisplayRotation)display.Rotation
            : DisplayRotation.Unknown;

        return new DisplayInfo(width, height, density, orientation, rotation);
    }

    private static bool DisplayInfoEquals(DisplayInfo left, DisplayInfo right) =>
        Math.Abs(left.Width - right.Width) < 0.001d
        && Math.Abs(left.Height - right.Height) < 0.001d
        && Math.Abs(left.Density - right.Density) < 0.001d
        && left.Orientation == right.Orientation
        && left.Rotation == right.Rotation;

    private static double NormalizeDimension(double value, double fallback) =>
        double.IsFinite(value) && value > 0d ? value : fallback;
}

internal sealed class MirageBrowserGeolocation : IGeolocation, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private Location? _lastLocation;
    private int _watchSubscriptionId;
    private TimeSpan _minimumWatchInterval;
    private DateTimeOffset _lastWatchLocationRaisedAt;

    public bool IsListeningForeground => _watchSubscriptionId != 0;

    public bool IsEnabled => true;

    public event EventHandler<GeolocationLocationChangedEventArgs>? LocationChanged;

    public event EventHandler<GeolocationListeningFailedEventArgs>? ListeningFailed;

    public Task<Location?> GetLastKnownLocationAsync() => Task.FromResult(_lastLocation);

    public async Task<Location?> GetLocationAsync(GeolocationRequest request, CancellationToken cancelToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancelToken.ThrowIfCancellationRequested();

        var locationTask = DomElement.GetCurrentGeolocationJsonAsync(
            IsHighAccuracy(request.DesiredAccuracy),
            request.Timeout.TotalMilliseconds);
        var completed = await Task.WhenAny(locationTask, Task.Delay(Timeout.InfiniteTimeSpan, cancelToken));
        if (completed != locationTask)
        {
            cancelToken.ThrowIfCancellationRequested();
        }

        var payload = await locationTask;
        if (!TryReadLocation(payload, out var location, out _))
        {
            return null;
        }

        _lastLocation = location;
        return location;
    }

    public Task<bool> StartListeningForegroundAsync(GeolocationListeningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (IsListeningForeground)
        {
            throw new InvalidOperationException("Mirage browser geolocation is already listening.");
        }

        _watchSubscriptionId = DomElement.StartGeolocationWatch(
            IsHighAccuracy(request.DesiredAccuracy),
            request.MinimumTime.TotalMilliseconds,
            OnWatchLocationChanged,
            OnWatchListeningFailed);
        _minimumWatchInterval = NormalizeMinimumTime(request.MinimumTime);
        _lastWatchLocationRaisedAt = default;

        return Task.FromResult(_watchSubscriptionId != 0);
    }

    public void StopListeningForeground()
    {
        if (_watchSubscriptionId == 0)
        {
            return;
        }

        DomElement.UnsubscribeGeolocationWatch(_watchSubscriptionId);
        _watchSubscriptionId = 0;
        _minimumWatchInterval = TimeSpan.Zero;
        _lastWatchLocationRaisedAt = default;
    }

    public void Dispose() => StopListeningForeground();

    private void OnWatchLocationChanged()
    {
        var payload = DomElement.ConsumeGeolocationWatchPayloadJson(_watchSubscriptionId);
        if (!TryReadLocation(payload, out var location, out _))
        {
            return;
        }

        _lastLocation = location;
        if (ShouldThrottleWatchLocation())
        {
            return;
        }

        _lastWatchLocationRaisedAt = DateTimeOffset.UtcNow;
        LocationChanged?.Invoke(this, new GeolocationLocationChangedEventArgs(location));
    }

    private void OnWatchListeningFailed()
    {
        var subscriptionId = _watchSubscriptionId;
        var payload = DomElement.ConsumeGeolocationWatchPayloadJson(subscriptionId);
        _watchSubscriptionId = 0;
        _minimumWatchInterval = TimeSpan.Zero;
        _lastWatchLocationRaisedAt = default;
        if (subscriptionId != 0)
        {
            DomElement.UnsubscribeGeolocationWatch(subscriptionId);
        }

        _ = TryReadLocation(payload, out _, out var error);
        ListeningFailed?.Invoke(this, new GeolocationListeningFailedEventArgs(error));
    }

    private static bool IsHighAccuracy(GeolocationAccuracy accuracy) =>
        accuracy is GeolocationAccuracy.High or GeolocationAccuracy.Best;

    private bool ShouldThrottleWatchLocation()
    {
        if (_minimumWatchInterval <= TimeSpan.Zero || _lastWatchLocationRaisedAt == default)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - _lastWatchLocationRaisedAt < _minimumWatchInterval;
    }

    private static TimeSpan NormalizeMinimumTime(TimeSpan minimumTime) =>
        minimumTime > TimeSpan.Zero ? minimumTime : TimeSpan.Zero;

    private static bool TryReadLocation(string? json, out Location location, out GeolocationError error)
    {
        location = default!;
        error = GeolocationError.PositionUnavailable;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        var payload = JsonSerializer.Deserialize<GeolocationPayload>(json, JsonOptions);
        if (payload is null)
        {
            return false;
        }

        if (!payload.Ok)
        {
            error = payload.Code switch
            {
                1 => GeolocationError.Unauthorized,
                _ => GeolocationError.PositionUnavailable
            };
            return false;
        }

        location = new Location(payload.Latitude, payload.Longitude, DateTimeOffset.FromUnixTimeMilliseconds(payload.Timestamp))
        {
            Altitude = payload.Altitude,
            Accuracy = payload.Accuracy,
            VerticalAccuracy = payload.AltitudeAccuracy,
            Speed = payload.Speed,
            Course = payload.Heading
        };
        return true;
    }

    private sealed record GeolocationPayload(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude,
        [property: JsonPropertyName("altitude")] double? Altitude,
        [property: JsonPropertyName("accuracy")] double? Accuracy,
        [property: JsonPropertyName("altitudeAccuracy")] double? AltitudeAccuracy,
        [property: JsonPropertyName("speed")] double? Speed,
        [property: JsonPropertyName("heading")] double? Heading,
        [property: JsonPropertyName("timestamp")] long Timestamp,
        [property: JsonPropertyName("code")] int Code);
}

internal sealed class MirageBrowserTextToSpeech : ITextToSpeech
{
    public Task<IEnumerable<Locale>> GetLocalesAsync() => Task.FromResult(Enumerable.Empty<Locale>());

    public async Task SpeakAsync(string text, SpeechOptions options, CancellationToken cancelToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancelToken.ThrowIfCancellationRequested();

        if (text.Length == 0)
        {
            return;
        }

        var rate = NormalizeOption(options?.Rate, 1d);
        var pitch = NormalizeOption(options?.Pitch, 1d);
        var volume = NormalizeOption(options?.Volume, 1d);
        var speakTask = DomElement.SpeakTextAsync(text, rate, pitch, volume);
        var completed = await Task.WhenAny(speakTask, Task.Delay(Timeout.InfiniteTimeSpan, cancelToken));
        if (completed != speakTask)
        {
            DomElement.CancelSpeech();
            cancelToken.ThrowIfCancellationRequested();
        }

        if (!await speakTask)
        {
            throw new FeatureNotSupportedException("Text-to-speech is not supported by this browser.");
        }
    }

    private static double NormalizeOption(float? value, double fallback) =>
        value.HasValue && double.IsFinite(value.Value) ? value.Value : fallback;
}
