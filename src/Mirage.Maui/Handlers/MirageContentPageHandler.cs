using Mirage.Core.Diagnostics;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageContentPageHandler : MirageContentViewHandlerBase<ContentPage>, IMiragePageTitleProjectionHost
{
    private IMirageImageSourceResolver? _imageSourceResolver;
    private CancellationTokenSource? _backgroundImageResolutionCancellation;
    private long _backgroundImageResolutionVersion;
    private string? _ownedBackgroundObjectUrl;

    public static IPropertyMapper<ContentPage, MirageContentPageHandler> Mapper =
        new PropertyMapper<ContentPage, MirageContentPageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ContentPage.Content)] = MapContent,
            [nameof(ContentPage.Padding)] = MapPadding,
            [nameof(Page.Title)] = MapTitle,
            [nameof(IView.Background)] = MapBackground,
            ["BackgroundColor"] = MapBackground,
            [nameof(Page.BackgroundImageSource)] = MapBackgroundImageSource,
            ["Style"] = MapStyle
        };

    public MirageContentPageHandler()
        : base(Mapper)
    {
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        base.ConnectHandler(platformView);
        _ = ResolveBackgroundImageSourceAsync(VirtualView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        CancelPendingBackgroundImageResolution();
        ReleaseOwnedBackgroundObjectUrl();
        base.DisconnectHandler(platformView);
    }

    protected override IPadding PaddingSource => VirtualView;

    protected override bool IsBoundedPageSurface => true;

    public static void MapContent(MirageContentPageHandler handler, ContentPage page)
    {
        handler.SetPresentedContent(((IContentView)page).PresentedContent);
    }

    public static void MapPadding(MirageContentPageHandler handler, ContentPage page)
    {
        MirageViewMapper.MapPadding(handler, page);
    }

    public static void MapTitle(MirageContentPageHandler handler, ContentPage page)
    {
        _ = page;
        handler.TryProjectActivePageTitle();
    }

    public static void MapBackground(MirageContentPageHandler handler, ContentPage page)
    {
        MirageViewMapper.MapBackground(handler, page);
        _ = handler.ResolveBackgroundImageSourceAsync(page);
    }

    public static void MapBackgroundImageSource(MirageContentPageHandler handler, ContentPage page)
    {
        _ = handler.ResolveBackgroundImageSourceAsync(page);
    }

    public static void MapStyle(MirageContentPageHandler handler, ContentPage page)
    {
        MirageViewMapper.MapStyle(handler, page);
        _ = handler.ResolveBackgroundImageSourceAsync(page);
    }

    private async Task ResolveBackgroundImageSourceAsync(ContentPage page)
    {
        CancelPendingBackgroundImageResolution();
        var cancellation = new CancellationTokenSource();
        _backgroundImageResolutionCancellation = cancellation;
        var version = Interlocked.Increment(ref _backgroundImageResolutionVersion);

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
                if (RequiresManifestInitialization(page.BackgroundImageSource))
                {
                    await resolver.EnsureInitializedAsync();
                }

                resolved = await resolver.ResolveAsync(page.BackgroundImageSource, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                resolved = MirageImageSourceProjectionResult.Unresolved(
                    "unsupported",
                    $"ContentPage background image resolution failed: {ex.GetType().Name}.");
            }
        }

        if (cancellation.Token.IsCancellationRequested
            || version != Volatile.Read(ref _backgroundImageResolutionVersion)
            || VirtualView != page)
        {
            ReleaseIfOwnedByResolution(resolved);
            return;
        }

        var dispatcher = MauiContext?.Services.GetService<IDispatcher>();
        if (dispatcher is not null && dispatcher.IsDispatchRequired)
        {
            if (!dispatcher.Dispatch(() => ApplyResolvedBackgroundImage(page, resolved, version, cancellation.Token)))
            {
                ApplyResolvedBackgroundImage(page, resolved, version, cancellation.Token);
            }

            return;
        }

        ApplyResolvedBackgroundImage(page, resolved, version, cancellation.Token);
    }

    private void ApplyResolvedBackgroundImage(
        ContentPage page,
        MirageImageSourceProjectionResult resolved,
        long version,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested
            || version != Volatile.Read(ref _backgroundImageResolutionVersion)
            || VirtualView != page)
        {
            ReleaseIfOwnedByResolution(resolved);
            return;
        }

        if (!resolved.IsResolved || string.IsNullOrWhiteSpace(resolved.BrowserUrl))
        {
            ReleaseOwnedBackgroundObjectUrl();
            var changed = ClearBackgroundImageStyles();
            changed |= MirageCommonViewProjection.MapBackground(this, page);

            if (changed)
            {
                RequestRender();
            }

            if (!resolved.IsResolved && page.BackgroundImageSource is not null)
            {
                MirageDiagnostics.Log(
                    "contentpage.background-image",
                    $"BackgroundImageSource unresolved for '{page.GetType().Name}': {resolved.FailureReason ?? "Unknown failure."}");
            }

            return;
        }

        var nextOwnedUrl = resolved.IsManagedObjectUrl ? resolved.BrowserUrl : null;
        if (!string.Equals(_ownedBackgroundObjectUrl, nextOwnedUrl, StringComparison.Ordinal))
        {
            ReleaseOwnedBackgroundObjectUrl();
            _ownedBackgroundObjectUrl = nextOwnedUrl;
        }

        var changedResolved = SetStyleIfChanged("backgroundImage", BuildCssUrl(resolved.BrowserUrl));
        changedResolved |= SetStyleIfChanged("backgroundSize", "100% 100%");
        changedResolved |= SetStyleIfChanged("backgroundRepeat", "no-repeat");
        changedResolved |= SetStyleIfChanged("backgroundPosition", "0 0");

        if (changedResolved)
        {
            RequestRender();
        }
    }

    private bool ClearBackgroundImageStyles()
    {
        var changed = SetStyleIfChanged("backgroundImage", null);
        changed |= SetStyleIfChanged("backgroundSize", null);
        changed |= SetStyleIfChanged("backgroundRepeat", null);
        changed |= SetStyleIfChanged("backgroundPosition", null);
        return changed;
    }

    private static string BuildCssUrl(string url)
    {
        var escaped = url
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"url(\"{escaped}\")";
    }

    private static bool RequiresManifestInitialization(ImageSource? source)
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

    internal bool TryProjectActivePageTitle()
    {
        return TryGetPlatformView(out var platformView)
            && MiragePageHandler.TryProjectActivePageTitle(MauiContext, platformView, VirtualView);
    }

    bool IMiragePageTitleProjectionHost.TryProjectActivePageTitle() => TryProjectActivePageTitle();

    private void ReleaseOwnedBackgroundObjectUrl()
    {
        if (string.IsNullOrWhiteSpace(_ownedBackgroundObjectUrl))
        {
            return;
        }

        GetImageSourceResolver()?.Release(_ownedBackgroundObjectUrl);
        _ownedBackgroundObjectUrl = null;
    }

    private void CancelPendingBackgroundImageResolution()
    {
        try
        {
            _backgroundImageResolutionCancellation?.Cancel();
        }
        catch
        {
            // best effort cancellation
        }
        finally
        {
            _backgroundImageResolutionCancellation?.Dispose();
            _backgroundImageResolutionCancellation = null;
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

    private bool SetStyleIfChanged(string styleName, string? styleValue)
    {
        var current = PlatformView.GetStyleValue(styleName);
        if (string.Equals(current, styleValue, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, styleValue);
        return true;
    }
}
