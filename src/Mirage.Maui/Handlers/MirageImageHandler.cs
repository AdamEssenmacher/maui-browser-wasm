using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageImageHandler : MirageViewHandler<Image>, IMirageRuntimeEventTarget
{
    private IMirageImageSourceResolver? _imageSourceResolver;
    private CancellationTokenSource? _sourceResolutionCancellation;
    private long _sourceResolutionVersion;

    private string? _resolvedSourceUrl;
    private string? _projectedSourceUrl;
    private string? _ownedObjectUrl;
    private string _resolvedSourceKind = "none";
    private bool _resolvedSourceIsGif;

    public static IPropertyMapper<Image, MirageImageHandler> Mapper =
        new PropertyMapper<Image, MirageImageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(Image.Source)] = MapSource,
            [nameof(Image.Aspect)] = MapAspect,
            [nameof(Image.IsOpaque)] = MapIsOpaque,
            [nameof(Image.IsAnimationPlaying)] = MapIsAnimationPlaying,
            [nameof(VisualElement.WidthRequest)] = MapRequestedDimensions,
            [nameof(VisualElement.HeightRequest)] = MapRequestedDimensions
        };

    public MirageImageHandler()
        : base(ComponentKind.Image, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", "block");
        changed |= SetStyleIfChanged("boxSizing", "border-box");
        changed |= SetStyleIfChanged("objectPosition", "center center");
        changed |= UpdateRequestedDimensionProjection(VirtualView);
        changed |= UpdateAspectProjection(VirtualView);
        changed |= UpdateOpaqueProjection(VirtualView);

        PlatformView.SetEvent("load", enabled: true);
        PlatformView.SetEvent("error", enabled: true);

        base.ConnectHandler(platformView);

        _ = ResolveSourceAsync(VirtualView);

        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
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
        if (string.IsNullOrWhiteSpace(_projectedSourceUrl))
        {
            return;
        }

        // Ignore stale events emitted for a previously assigned src.
        if (!string.IsNullOrWhiteSpace(payload)
            && !string.Equals(payload, _projectedSourceUrl, StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(eventName, "load", StringComparison.Ordinal))
        {
            if (SetPropertyIfChanged("SourceState", "loaded"))
            {
                RequestRender();
            }

            return;
        }

        if (!string.Equals(eventName, "error", StringComparison.Ordinal))
        {
            return;
        }

        if (SetPropertyIfChanged("SourceState", "error"))
        {
            MirageDiagnostics.Log(
                "image.handler",
                $"Image load failed for source '{_projectedSourceUrl}' on view '{VirtualView.GetType().Name}'.");
            RequestRender();
        }
    }

    public static void MapSource(MirageImageHandler handler, Image image)
    {
        _ = handler.ResolveSourceAsync(image);
    }

    public static void MapAspect(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateAspectProjection(image))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsOpaque(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateOpaqueProjection(image))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsAnimationPlaying(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateAnimationPlaybackProjection(image))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapRequestedDimensions(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateRequestedDimensionProjection(image))
        {
            return;
        }

        handler.RequestRender();
    }

    private async Task ResolveSourceAsync(Image image)
    {
        CancelPendingSourceResolution();
        var cancellation = new CancellationTokenSource();
        _sourceResolutionCancellation = cancellation;
        var version = Interlocked.Increment(ref _sourceResolutionVersion);

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
            try
            {
                if (RequiresManifestInitialization(image.Source))
                {
                    await resolver.EnsureInitializedAsync();
                }

                resolved = await resolver.ResolveAsync(image.Source, cancellation.Token);
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

        if (cancellation.Token.IsCancellationRequested
            || version != Volatile.Read(ref _sourceResolutionVersion)
            || VirtualView != image)
        {
            ReleaseIfOwnedByResolution(resolved);
            return;
        }

        var dispatcher = MauiContext?.Services.GetService<IDispatcher>();
        if (dispatcher is not null && dispatcher.IsDispatchRequired)
        {
            if (!dispatcher.Dispatch(() => ApplyResolvedSource(image, resolved, version, cancellation.Token)))
            {
                ApplyResolvedSource(image, resolved, version, cancellation.Token);
            }

            return;
        }

        ApplyResolvedSource(image, resolved, version, cancellation.Token);
    }

    private void ApplyResolvedSource(
        Image image,
        MirageImageSourceProjectionResult resolved,
        long version,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested
            || version != Volatile.Read(ref _sourceResolutionVersion)
            || VirtualView != image)
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

            if (changedUnsupported)
            {
                MirageDiagnostics.Log(
                    "image.handler",
                    $"Image source unresolved for view '{image.GetType().Name}': {resolved.FailureReason ?? "Unknown failure."}");
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

        var projectedSource = ResolveProjectedSourceForPlayback(image, nextResolvedUrl, _resolvedSourceIsGif);
        var projectedState = ResolveProjectedInitialState(image, projectedSource, resolved.InitialState, _resolvedSourceIsGif);

        var changed = SetPropertyIfChanged("Source", projectedSource);
        changed |= SetPropertyIfChanged("SourceKind", resolved.SourceKind);
        changed |= SetPropertyIfChanged("SourceState", projectedState);
        _projectedSourceUrl = projectedSource;

        if (changed)
        {
            RequestRender();
        }
    }

    private static bool RequiresManifestInitialization(ImageSource? source)
    {
        return source is FileImageSource;
    }

    private bool UpdateAnimationPlaybackProjection(Image image)
    {
        if (!_resolvedSourceIsGif)
        {
            return false;
        }

        var projectedSource = ResolveProjectedSourceForPlayback(image, _resolvedSourceUrl, isGif: true);
        var projectedState = ResolveProjectedInitialState(image, projectedSource, "loading", isGif: true);

        var changed = SetPropertyIfChanged("Source", projectedSource);
        changed |= SetPropertyIfChanged("SourceKind", _resolvedSourceKind);
        changed |= SetPropertyIfChanged("SourceState", projectedState);
        _projectedSourceUrl = projectedSource;
        return changed;
    }

    private static string? ResolveProjectedSourceForPlayback(Image image, string? resolvedSource, bool isGif)
    {
        if (!isGif)
        {
            return resolvedSource;
        }

        return image.IsAnimationPlaying ? resolvedSource : null;
    }

    private static string ResolveProjectedInitialState(Image image, string? projectedSource, string initialState, bool isGif)
    {
        if (string.IsNullOrWhiteSpace(projectedSource))
        {
            return isGif && !image.IsAnimationPlaying ? "loaded" : "empty";
        }

        return string.IsNullOrWhiteSpace(initialState) ? "loading" : initialState;
    }

    private bool UpdateAspectProjection(Image image)
    {
        var objectFit = MirageImageSourceProjection.ResolveObjectFit(image.Aspect);
        var changed = SetStyleIfChanged("objectFit", objectFit);
        changed |= SetStyleIfChanged("objectPosition", "center center");
        return changed;
    }

    private bool UpdateOpaqueProjection(Image image)
    {
        return SetPropertyIfChanged("IsOpaque", image.IsOpaque ? "true" : null);
    }

    private bool UpdateRequestedDimensionProjection(Image image)
    {
        return MirageCommonViewProjection.MapSizeRequests(this, image);
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
}
