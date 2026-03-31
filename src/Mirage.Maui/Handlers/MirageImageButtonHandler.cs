using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;
using MauiImage = Microsoft.Maui.IImage;

namespace Mirage.Maui.Handlers;

internal sealed class MirageImageButtonHandler : MirageViewHandler<IImageButton>, IMirageRuntimeEventTarget
{
    private const string ClickEventName = "click";
    private const string KeyDownEventName = "keydown";
    private const string PointerDownEventName = "pointerdown";
    private const string PointerUpEventName = "pointerup";
    private const string PointerCancelEventName = "pointercancel";
    private const string LoadEventName = "load";
    private const string ErrorEventName = "error";

    private IMirageImageSourceResolver? _imageSourceResolver;
    private CancellationTokenSource? _sourceResolutionCancellation;
    private long _sourceResolutionVersion;
    private bool _isPressed;
    private bool _pendingKeyboardActivation;

    private string? _resolvedSourceUrl;
    private string? _projectedSourceUrl;
    private string? _ownedObjectUrl;
    private string _resolvedSourceKind = "none";
    private bool _resolvedSourceIsGif;

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(MauiImage.Source),
        nameof(MauiImage.Aspect),
        nameof(MauiImage.IsAnimationPlaying),
        nameof(MauiImage.IsOpaque),
        nameof(IButtonStroke.StrokeThickness),
        nameof(IButtonStroke.StrokeColor),
        nameof(IButtonStroke.CornerRadius),
        nameof(IImageButton.Padding),
        nameof(IImageButton.Background)
    ];

    internal static readonly string[] ParityCommandKeys = [];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    public static IPropertyMapper<MauiImage, MirageImageButtonHandler> ImageMapper =
        new PropertyMapper<MauiImage, MirageImageButtonHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(MauiImage.Source)] = MapSource,
            [nameof(MauiImage.Aspect)] = MapAspect,
            [nameof(MauiImage.IsAnimationPlaying)] = MapIsAnimationPlaying,
            [nameof(MauiImage.IsOpaque)] = MapIsOpaque
        };

    public static IPropertyMapper<IImageButton, MirageImageButtonHandler> Mapper =
        new PropertyMapper<IImageButton, MirageImageButtonHandler>(ImageMapper)
        {
            [nameof(IButtonStroke.StrokeThickness)] = MapStrokeThickness,
            [nameof(IButtonStroke.StrokeColor)] = MapStrokeColor,
            [nameof(IButtonStroke.CornerRadius)] = MapCornerRadius,
            [nameof(IImageButton.Padding)] = MapPadding,
            [nameof(IImageButton.Background)] = MapBackground,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<IImageButton, MirageImageButtonHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    public MirageImageButtonHandler()
        : base(ComponentKind.ImageButton, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 52;
        const double defaultHeight = 52;

        return ResolveMeasuredSizeWithConstraints(
            VirtualView,
            widthConstraint,
            heightConstraint,
            defaultWidth,
            defaultHeight);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        _isPressed = false;
        _pendingKeyboardActivation = false;

        var changed = ApplyImageButtonProjectionCore(VirtualView);
        changed |= SetEventIfChanged(ClickEventName, enabled: true);
        changed |= SetEventIfChanged(KeyDownEventName, enabled: true);
        changed |= SetEventIfChanged(PointerDownEventName, enabled: true);
        changed |= SetEventIfChanged(PointerUpEventName, enabled: true);
        changed |= SetEventIfChanged(PointerCancelEventName, enabled: true);
        changed |= SetEventIfChanged(LoadEventName, enabled: true);
        changed |= SetEventIfChanged(ErrorEventName, enabled: true);

        base.ConnectHandler(platformView);

        _ = ResolveSourceAsync(VirtualView);

        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        _isPressed = false;
        _pendingKeyboardActivation = false;
        CancelPendingSourceResolution();
        ReleaseOwnedObjectUrl();
        _projectedSourceUrl = null;
        _resolvedSourceUrl = null;
        _resolvedSourceKind = "none";
        _resolvedSourceIsGif = false;
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (HandleRuntimeImageEvent(eventName, payload))
        {
            return;
        }

        if (!VirtualView.IsEnabled)
        {
            return;
        }

        switch (eventName)
        {
            case PointerDownEventName:
                _pendingKeyboardActivation = false;
                HandlePressed();
                return;
            case PointerUpEventName:
            case PointerCancelEventName:
                HandleReleased();
                return;
            case KeyDownEventName:
                _pendingKeyboardActivation = IsActivationKey(payload);
                return;
            case ClickEventName:
                HandleClicked();
                return;
        }
    }

    public static void MapSource(MirageImageButtonHandler handler, MauiImage image)
    {
        if (image is not IImageButton imageButton)
        {
            return;
        }

        _ = handler.ResolveSourceAsync(imageButton);
    }

    public static void MapAspect(MirageImageButtonHandler handler, MauiImage image)
    {
        if (image is not IImageButton imageButton)
        {
            return;
        }

        if (!handler.UpdateAspectProjection(imageButton))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsAnimationPlaying(MirageImageButtonHandler handler, MauiImage image)
    {
        if (image is not IImageButton imageButton)
        {
            return;
        }

        if (!handler.UpdateAnimationPlaybackProjection(imageButton))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsOpaque(MirageImageButtonHandler handler, MauiImage image)
    {
        if (image is not IImageButton imageButton)
        {
            return;
        }

        if (!handler.UpdateOpaqueProjection(imageButton))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeThickness(MirageImageButtonHandler handler, IImageButton imageButton) => handler.MapImageButton(imageButton);

    public static void MapStrokeColor(MirageImageButtonHandler handler, IImageButton imageButton) => handler.MapImageButton(imageButton);

    public static void MapCornerRadius(MirageImageButtonHandler handler, IImageButton imageButton) => handler.MapImageButton(imageButton);

    public static void MapPadding(MirageImageButtonHandler handler, IImageButton imageButton) => handler.MapImageButton(imageButton);

    public static void MapBackground(MirageImageButtonHandler handler, IImageButton imageButton) => handler.MapImageButton(imageButton);

    public static void MapIsEnabled(MirageImageButtonHandler handler, IImageButton imageButton) => handler.MapImageButton(imageButton);

    private void MapImageButton(IImageButton imageButton)
    {
        if (!ApplyImageButtonProjectionCore(imageButton))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplyImageButtonProjectionCore(IImageButton imageButton)
    {
        using var _ = BeginRenderBatch();

        var changed = ApplyContainerStyles(imageButton);
        changed |= UpdateBackgroundProjection(imageButton);
        changed |= UpdateStrokeProjection(imageButton);
        changed |= UpdatePaddingProjection(imageButton);
        changed |= UpdateAspectProjection(imageButton);
        changed |= UpdateOpaqueProjection(imageButton);
        changed |= UpdateEnabledProjection(imageButton);
        changed |= UpdateImageProjectionMetadata(imageButton);
        return changed;
    }

    private bool ApplyContainerStyles(IImageButton imageButton)
    {
        _ = imageButton;
        var changed = SetStyleIfChanged("display", "inline-flex");
        changed |= SetStyleIfChanged("alignItems", "center");
        changed |= SetStyleIfChanged("justifyContent", "center");
        changed |= SetStyleIfChanged("position", "relative");
        changed |= SetStyleIfChanged("boxSizing", "border-box");
        changed |= SetStyleIfChanged("overflow", "hidden");
        return changed;
    }

    private bool UpdateBackgroundProjection(IImageButton imageButton)
    {
        return MirageCommonViewProjection.MapBackground(this, imageButton);
    }

    private bool UpdateStrokeProjection(IImageButton imageButton)
    {
        var strokeThickness = imageButton.StrokeThickness;
        var normalizedThickness = double.IsFinite(strokeThickness) && strokeThickness > 0
            ? strokeThickness
            : 0;

        var changed = SetStyleIfChanged("borderStyle", normalizedThickness > 0 ? "solid" : null);
        changed |= SetStyleIfChanged("borderWidth", normalizedThickness > 0 ? MirageViewMapper.ToPx(normalizedThickness) : null);
        var strokeColor = normalizedThickness > 0
            ? MirageCommonViewProjection.ToCssColor(imageButton.StrokeColor) ?? "currentColor"
            : null;
        changed |= SetStyleIfChanged("borderColor", strokeColor);

        var cornerRadius = imageButton.CornerRadius;
        changed |= SetStyleIfChanged("borderRadius", cornerRadius >= 0 ? MirageViewMapper.ToPx(cornerRadius) : null);
        return changed;
    }

    private bool UpdatePaddingProjection(IImageButton imageButton)
    {
        return SetStyleIfChanged("padding", MirageCommonViewProjection.ToPaddingThickness(imageButton.Padding));
    }

    private bool UpdateAspectProjection(IImageButton imageButton)
    {
        var objectFit = MirageImageSourceProjection.ResolveObjectFit(imageButton.Aspect);
        var changed = SetPropertyIfChanged("ImageObjectFit", objectFit);
        changed |= SetPropertyIfChanged("ImageObjectPosition", "center center");
        return changed;
    }

    private bool UpdateOpaqueProjection(IImageButton imageButton)
    {
        return SetPropertyIfChanged("IsOpaque", imageButton.IsOpaque ? "true" : null);
    }

    private bool UpdateEnabledProjection(IImageButton imageButton)
    {
        var isEnabled = imageButton.IsEnabled;
        var changed = SetStyleIfChanged("cursor", isEnabled ? "pointer" : "default");
        changed |= SetStyleIfChanged("pointerEvents", "auto");
        changed |= SetPropertyIfChanged("Disabled", isEnabled ? null : "disabled");

        if (!isEnabled)
        {
            _isPressed = false;
            _pendingKeyboardActivation = false;
        }

        return changed;
    }

    private bool UpdateImageProjectionMetadata(IImageButton imageButton)
    {
        var hasImage = !string.IsNullOrWhiteSpace(_projectedSourceUrl);
        var changed = SetPropertyIfChanged("ImageDisplay", hasImage ? "block" : "none");
        changed |= SetPropertyIfChanged("ImageWidth", "100%");
        changed |= SetPropertyIfChanged("ImageHeight", "100%");
        changed |= SetPropertyIfChanged("ImagePointerEvents", "none");
        changed |= SetPropertyIfChanged("ImageAlt", string.Empty);
        return changed;
    }

    private bool UpdateAnimationPlaybackProjection(IImageButton imageButton)
    {
        if (!_resolvedSourceIsGif)
        {
            return false;
        }

        var projectedSource = ResolveProjectedSourceForPlayback(imageButton, _resolvedSourceUrl, isGif: true);
        var projectedState = ResolveProjectedInitialState(imageButton, projectedSource, "loading", isGif: true);

        var changed = SetPropertyIfChanged("Source", projectedSource);
        changed |= SetPropertyIfChanged("SourceKind", _resolvedSourceKind);
        changed |= SetPropertyIfChanged("SourceState", projectedState);
        _projectedSourceUrl = projectedSource;
        changed |= UpdateImageProjectionMetadata(imageButton);
        return changed;
    }

    private async Task ResolveSourceAsync(IImageButton imageButton)
    {
        CancelPendingSourceResolution();
        var cancellation = new CancellationTokenSource();
        _sourceResolutionCancellation = cancellation;
        var version = Interlocked.Increment(ref _sourceResolutionVersion);
        var source = imageButton.Source as ImageSource;

        var resolver = GetImageSourceResolver();
        MirageImageSourceProjectionResult resolved;
        if (resolver is null)
        {
            resolved = MirageImageSourceProjectionResult.Unresolved(
                "unsupported",
                "IMirageImageSourceResolver was not available.");
        }
        else
        {
            if (imageButton.Source is not null && source is null)
            {
                resolved = MirageImageSourceProjectionResult.Unresolved(
                    "unsupported",
                    $"Unsupported ImageButton source type '{imageButton.Source.GetType().Name}'.");
            }
            else
            {
                try
                {
                    if (RequiresManifestInitialization(source))
                    {
                        await resolver.EnsureInitializedAsync();
                    }

                    resolved = await resolver.ResolveAsync(source, cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    resolved = MirageImageSourceProjectionResult.Unresolved(
                        "unsupported",
                        $"Image source resolution failed: {ex.GetType().Name}.");
                }
            }
        }

        if (cancellation.Token.IsCancellationRequested
            || version != Volatile.Read(ref _sourceResolutionVersion)
            || VirtualView != imageButton)
        {
            ReleaseIfOwnedByResolution(resolved);
            return;
        }

        var dispatcher = MauiContext?.Services.GetService<IDispatcher>();
        if (dispatcher is not null && dispatcher.IsDispatchRequired)
        {
            if (!dispatcher.Dispatch(() => ApplyResolvedSource(imageButton, resolved, version, cancellation.Token)))
            {
                ApplyResolvedSource(imageButton, resolved, version, cancellation.Token);
            }

            return;
        }

        ApplyResolvedSource(imageButton, resolved, version, cancellation.Token);
    }

    private void ApplyResolvedSource(
        IImageButton imageButton,
        MirageImageSourceProjectionResult resolved,
        long version,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested
            || version != Volatile.Read(ref _sourceResolutionVersion)
            || VirtualView != imageButton)
        {
            ReleaseIfOwnedByResolution(resolved);
            return;
        }

        if (!resolved.IsResolved)
        {
            ReleaseOwnedObjectUrl();
            _resolvedSourceUrl = null;
            _projectedSourceUrl = null;
            _resolvedSourceKind = "unsupported";
            _resolvedSourceIsGif = false;

            var changedUnsupported = SetPropertyIfChanged("Source", null);
            changedUnsupported |= SetPropertyIfChanged("SourceKind", "unsupported");
            changedUnsupported |= SetPropertyIfChanged("SourceState", "unresolved");
            changedUnsupported |= UpdateImageProjectionMetadata(imageButton);

            if (changedUnsupported)
            {
                MirageDiagnostics.Log(
                    "imagebutton.handler",
                    $"Image source unresolved for view '{imageButton.GetType().Name}': {resolved.FailureReason ?? "Unknown failure."}");
                RequestRender();
            }

            return;
        }

        var nextResolvedUrl = resolved.BrowserUrl;
        var nextOwnedUrl = resolved.IsManagedObjectUrl ? nextResolvedUrl : null;

        if (!string.Equals(_ownedObjectUrl, nextOwnedUrl, StringComparison.Ordinal))
        {
            ReleaseOwnedObjectUrl();
            _ownedObjectUrl = nextOwnedUrl;
        }

        _resolvedSourceUrl = nextResolvedUrl;
        _resolvedSourceKind = resolved.SourceKind;
        _resolvedSourceIsGif = resolved.IsGifSource;

        var projectedSource = ResolveProjectedSourceForPlayback(imageButton, nextResolvedUrl, _resolvedSourceIsGif);
        var projectedState = ResolveProjectedInitialState(imageButton, projectedSource, resolved.InitialState, _resolvedSourceIsGif);

        var changed = SetPropertyIfChanged("Source", projectedSource);
        changed |= SetPropertyIfChanged("SourceKind", resolved.SourceKind);
        changed |= SetPropertyIfChanged("SourceState", projectedState);
        _projectedSourceUrl = projectedSource;
        changed |= UpdateImageProjectionMetadata(imageButton);

        if (changed)
        {
            RequestRender();
        }
    }

    private bool HandleRuntimeImageEvent(string eventName, string? payload)
    {
        if (!string.Equals(eventName, LoadEventName, StringComparison.Ordinal)
            && !string.Equals(eventName, ErrorEventName, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_projectedSourceUrl))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            // Image runtime events should be keyed by the source active when the event fired.
            // Missing payload makes stale-event filtering unsafe, so ignore deterministically.
            return true;
        }

        if (!string.Equals(payload, _projectedSourceUrl, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(eventName, LoadEventName, StringComparison.Ordinal))
        {
            if (SetPropertyIfChanged("SourceState", "loaded"))
            {
                RequestRender();
            }

            return true;
        }

        if (SetPropertyIfChanged("SourceState", "error"))
        {
            MirageDiagnostics.Log(
                "imagebutton.handler",
                $"Image load failed for source '{_projectedSourceUrl}' on view '{VirtualView.GetType().Name}'.");
            RequestRender();
        }

        return true;
    }

    private void HandlePressed()
    {
        if (_isPressed)
        {
            return;
        }

        _isPressed = true;
        if (VirtualView is IButton buttonView)
        {
            buttonView.Pressed();
        }
    }

    private void HandleReleased()
    {
        if (!_isPressed)
        {
            return;
        }

        _isPressed = false;
        if (VirtualView is IButton buttonView)
        {
            buttonView.Released();
        }
    }

    private void HandleClicked()
    {
        if (_isPressed)
        {
            HandleReleased();
        }

        if (_pendingKeyboardActivation && VirtualView is IButton keyboardButton)
        {
            keyboardButton.Pressed();
            keyboardButton.Released();
        }

        _pendingKeyboardActivation = false;
        if (VirtualView is IButton buttonView)
        {
            buttonView.Clicked();
        }
    }

    private static bool IsActivationKey(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        return string.Equals(payload, "Enter", StringComparison.Ordinal)
               || string.Equals(payload, " ", StringComparison.Ordinal)
               || string.Equals(payload, "Spacebar", StringComparison.Ordinal);
    }

    private static Size ResolveMeasuredSizeWithConstraints(
        IImageButton imageButton,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        var (requestedWidth, requestedMinimumWidth, requestedMaximumWidth) = ResolveRequestedDimensionRequests(imageButton, isWidth: true);
        var (requestedHeight, requestedMinimumHeight, requestedMaximumHeight) = ResolveRequestedDimensionRequests(imageButton, isWidth: false);
        return new Size(
            ResolveConstraints(clampedWidth, requestedWidth, requestedMinimumWidth, requestedMaximumWidth),
            ResolveConstraints(clampedHeight, requestedHeight, requestedMinimumHeight, requestedMaximumHeight));
    }

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return Math.Max(0, fallback);
        }

        return Math.Max(0, Math.Min(constraint, fallback));
    }

    private static double ResolveConstraints(double measured, double exact, double min, double max)
    {
        var resolved = measured;
        var resolvedMin = ResolveMinimum(min);

        if (IsExplicitLengthSet(exact))
        {
            resolved = exact;
        }

        if (IsExplicitLengthSet(max) && resolved > max)
        {
            resolved = max;
        }

        if (resolved < resolvedMin)
        {
            resolved = resolvedMin;
        }

        return resolved;
    }

    private static double ResolveMinimum(double value)
    {
        return IsExplicitLengthSet(value)
            ? value
            : 0;
    }

    private static bool IsExplicitLengthSet(double value)
    {
        return double.IsFinite(value) && value >= 0;
    }

    private static (double Exact, double Minimum, double Maximum) ResolveRequestedDimensionRequests(IImageButton imageButton, bool isWidth)
    {
        if (imageButton is VisualElement visualElement)
        {
            return isWidth
                ? (visualElement.WidthRequest, visualElement.MinimumWidthRequest, visualElement.MaximumWidthRequest)
                : (visualElement.HeightRequest, visualElement.MinimumHeightRequest, visualElement.MaximumHeightRequest);
        }

        return isWidth
            ? (double.NaN, imageButton.MinimumWidth, imageButton.MaximumWidth)
            : (double.NaN, imageButton.MinimumHeight, imageButton.MaximumHeight);
    }

    private static string? ResolveProjectedSourceForPlayback(IImageButton imageButton, string? resolvedSource, bool isGif)
    {
        if (!isGif)
        {
            return resolvedSource;
        }

        return imageButton.IsAnimationPlaying ? resolvedSource : null;
    }

    private static string ResolveProjectedInitialState(
        IImageButton imageButton,
        string? projectedSource,
        string initialState,
        bool isGif)
    {
        if (string.IsNullOrWhiteSpace(projectedSource))
        {
            return isGif && !imageButton.IsAnimationPlaying ? "loaded" : "empty";
        }

        return string.IsNullOrWhiteSpace(initialState) ? "loading" : initialState;
    }

    private static bool RequiresManifestInitialization(IImageSource? source)
    {
        if (source is not FileImageSource fileImageSource)
        {
            return false;
        }

        return RequiresManifestLookup(fileImageSource.File);
    }

    private static bool RequiresManifestLookup(string? fileSource)
    {
        if (string.IsNullOrWhiteSpace(fileSource))
        {
            return false;
        }

        var trimmed = fileSource.Trim();
        return !(trimmed.StartsWith("/", StringComparison.Ordinal)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase));
    }

    private void ReleaseIfOwnedByResolution(MirageImageSourceProjectionResult resolved)
    {
        if (!resolved.IsManagedObjectUrl || string.IsNullOrWhiteSpace(resolved.BrowserUrl))
        {
            return;
        }

        GetImageSourceResolver()?.Release(resolved.BrowserUrl);
    }

    private void ReleaseOwnedObjectUrl()
    {
        if (string.IsNullOrWhiteSpace(_ownedObjectUrl))
        {
            return;
        }

        GetImageSourceResolver()?.Release(_ownedObjectUrl);
        _ownedObjectUrl = null;
    }

    private void CancelPendingSourceResolution()
    {
        try
        {
            _sourceResolutionCancellation?.Cancel();
        }
        catch
        {
            // best effort cancellation
        }
        finally
        {
            _sourceResolutionCancellation?.Dispose();
            _sourceResolutionCancellation = null;
        }
    }

    private IMirageImageSourceResolver? GetImageSourceResolver()
    {
        if (_imageSourceResolver is not null)
        {
            return _imageSourceResolver;
        }

        _imageSourceResolver = MauiContext?.Services.GetService<IMirageImageSourceResolver>();
        return _imageSourceResolver;
    }

    private bool SetPropertyIfChanged(string propertyName, string? value)
    {
        if (string.Equals(PlatformView.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetProperty(propertyName, value);
        return true;
    }

    private bool SetStyleIfChanged(string styleName, string? styleValue)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), styleValue, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, styleValue);
        return true;
    }

    private bool SetEventIfChanged(string eventName, bool enabled)
    {
        var currentlyEnabled = false;
        foreach (var existingEvent in PlatformView.Events)
        {
            if (!string.Equals(existingEvent, eventName, StringComparison.Ordinal))
            {
                continue;
            }

            currentlyEnabled = true;
            break;
        }

        if (currentlyEnabled == enabled)
        {
            return false;
        }

        PlatformView.SetEvent(eventName, enabled);
        return true;
    }
}
