using Mirage.Maui.Images;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Mirage.Maui.Handlers;

internal static class MirageImageSourceProjection
{
    internal readonly record struct SourceProjection(
        string? SourceValue,
        string SourceKind,
        string InitialState,
        bool IsSupported,
        string? FailureReason,
        bool IsGifSource,
        bool IsManagedObjectUrl);

#if DEBUG
    private static readonly HashSet<string> UnsupportedWarnings = new();
#endif

    public static SourceProjection BuildProjection(
        ImageSource? source,
        IMirageImageSourceResolver? imageSourceResolver,
        IView? ownerView = null)
    {
        if (source is null)
        {
            return new SourceProjection(
                SourceValue: null,
                SourceKind: "none",
                InitialState: "empty",
                IsSupported: true,
                FailureReason: null,
                IsGifSource: false,
                IsManagedObjectUrl: false);
        }

        if (imageSourceResolver is null)
        {
            const string resolverMissingReason = "IMirageImageSourceResolver was not available.";
            ReportUnsupported(ownerView, source, resolverMissingReason);
            return new SourceProjection(
                SourceValue: null,
                SourceKind: "unsupported",
                InitialState: "unresolved",
                IsSupported: false,
                FailureReason: resolverMissingReason,
                IsGifSource: false,
                IsManagedObjectUrl: false);
        }

        var resolved = imageSourceResolver.Resolve(source);
        if (resolved.IsResolved)
        {
            return new SourceProjection(
                SourceValue: resolved.BrowserUrl,
                SourceKind: resolved.SourceKind,
                InitialState: resolved.InitialState,
                IsSupported: true,
                FailureReason: null,
                IsGifSource: resolved.IsGifSource,
                IsManagedObjectUrl: resolved.IsManagedObjectUrl);
        }

        var reason = string.IsNullOrWhiteSpace(resolved.FailureReason)
            ? $"{source.GetType().Name} could not be resolved."
            : resolved.FailureReason;
        ReportUnsupported(ownerView, source, reason);
        return new SourceProjection(
            SourceValue: null,
            SourceKind: "unsupported",
            InitialState: "unresolved",
            IsSupported: false,
            FailureReason: reason,
            IsGifSource: false,
            IsManagedObjectUrl: false);
    }

    public static string ResolveObjectFit(Aspect aspect)
    {
        return aspect switch
        {
            Aspect.AspectFill => "cover",
            Aspect.Fill => "fill",
            Aspect.Center => "none",
            _ => "contain"
        };
    }

    private static void ReportUnsupported(IView? ownerView, object? value, string details)
    {
#if DEBUG
        var key = $"{ownerView?.GetType().FullName}|{value?.GetType().FullName}|{details}";
        if (UnsupportedWarnings.Add(key))
        {
            Console.WriteLine(
                $"[Mirage] Unsupported image source mapping for {ownerView?.GetType().Name ?? "unknown view"}: {value?.GetType().Name ?? "<null>"}. {details}");
        }
#endif
    }
}
