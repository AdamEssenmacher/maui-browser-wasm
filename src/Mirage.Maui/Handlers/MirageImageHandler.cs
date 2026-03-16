using Mirage.Core.Semantics;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageImageHandler : MirageViewHandler<Image>
{
    private const int ManifestRetryMaxAttempts = 3;
    private IMirageImageSourceResolver? _imageSourceResolver;
    private CancellationTokenSource? _manifestRetryCancellation;
    private int _manifestRetryAttemptCount;

    public static IPropertyMapper<Image, MirageImageHandler> Mapper =
        new PropertyMapper<Image, MirageImageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(Image.Source)] = MapSource,
            [nameof(Image.Aspect)] = MapAspect,
            [nameof(Image.IsOpaque)] = MapIsOpaque,
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
        changed |= UpdateRequestedDimensionProjection(VirtualView);
        changed |= SetStyleIfChanged("boxSizing", "border-box");
        changed |= SetStyleIfChanged("objectPosition", "center center");
        changed |= UpdateSourceProjection(VirtualView);
        changed |= UpdateAspectProjection(VirtualView);
        changed |= UpdateOpaqueProjection(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        CancelManifestRetry();
        base.DisconnectHandler(platformView);
    }

    public static void MapSource(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateSourceProjection(image))
        {
            return;
        }

        handler.RequestRender();
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

    public static void MapRequestedDimensions(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateRequestedDimensionProjection(image))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateSourceProjection(Image image)
    {
        var projection = MirageImageSourceProjection.BuildProjection(image.Source, GetImageSourceResolver(), image);
        var changed = SetPropertyIfChanged("Source", projection.SourceValue);
        changed |= SetPropertyIfChanged("SourceKind", projection.SourceKind);
        changed |= SetPropertyIfChanged("SourceState", projection.InitialState);

        if (projection.IsSupported)
        {
            _manifestRetryAttemptCount = 0;
            CancelManifestRetry();
            return changed;
        }

        if (image.Source is FileImageSource
            && IsManifestUnavailableFailure(projection.FailureReason))
        {
            ScheduleManifestRetry(image);
        }

        return changed;
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
        var width = ResolveDimensionCss(image.WidthRequest);
        var height = ResolveDimensionCss(image.HeightRequest);
        var changed = SetStyleIfChanged("width", width);
        changed |= SetStyleIfChanged("height", height);
        return changed;
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

    private IMirageImageSourceResolver? GetImageSourceResolver()
    {
        if (_imageSourceResolver is not null)
        {
            return _imageSourceResolver;
        }

        _imageSourceResolver = MauiContext?.Services.GetService<IMirageImageSourceResolver>();
        return _imageSourceResolver;
    }

    private void ScheduleManifestRetry(Image image)
    {
        if (_manifestRetryAttemptCount >= ManifestRetryMaxAttempts)
        {
            return;
        }

        CancelManifestRetry();
        _manifestRetryAttemptCount++;
        var attempt = _manifestRetryAttemptCount;
        var cts = new CancellationTokenSource();
        _manifestRetryCancellation = cts;
        _ = RetryManifestResolutionAsync(image, attempt, cts.Token);
    }

    private async Task RetryManifestResolutionAsync(Image image, int attempt, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250 * attempt, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (GetImageSourceResolver() is { } resolver)
            {
                await resolver.EnsureInitializedAsync();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var dispatcher = MauiContext?.Services.GetService<IDispatcher>();
            if (dispatcher is not null && dispatcher.IsDispatchRequired)
            {
                if (!dispatcher.Dispatch(ApplyRetry))
                {
                    ApplyRetry();
                }
            }
            else
            {
                ApplyRetry();
            }
        }
        catch (OperationCanceledException)
        {
        }

        void ApplyRetry()
        {
            if (cancellationToken.IsCancellationRequested || VirtualView != image)
            {
                return;
            }

            if (UpdateSourceProjection(image))
            {
                RequestRender();
            }
        }
    }

    private void CancelManifestRetry()
    {
        try
        {
            _manifestRetryCancellation?.Cancel();
        }
        catch
        {
            // best effort
        }
        finally
        {
            _manifestRetryCancellation?.Dispose();
            _manifestRetryCancellation = null;
        }
    }

    private static bool IsManifestUnavailableFailure(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        return reason.Contains("manifest", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDimensionCss(double requestedValue)
    {
        if (!double.IsFinite(requestedValue) || requestedValue < 0)
        {
            return "100%";
        }

        return MirageViewMapper.ToPx(requestedValue);
    }
}
