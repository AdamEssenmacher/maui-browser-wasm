namespace Mirage.Maui.Images;

using Microsoft.Maui.Controls;

public interface IMirageImageSourceResolver
{
    ValueTask EnsureInitializedAsync();

    MirageImageSourceResolveResult Resolve(string? source);

    MirageImageSourceProjectionResult Resolve(ImageSource? source);

    ValueTask<MirageImageSourceProjectionResult> ResolveAsync(ImageSource? source, CancellationToken cancellationToken = default);

    void Release(string? browserUrl);
}

public readonly record struct MirageImageSourceResolveResult(bool IsResolved, string? BrowserUrl, string? FailureReason)
{
    public static MirageImageSourceResolveResult Resolved(string browserUrl)
    {
        return new MirageImageSourceResolveResult(true, browserUrl, null);
    }

    public static MirageImageSourceResolveResult Unresolved(string failureReason)
    {
        return new MirageImageSourceResolveResult(false, null, failureReason);
    }
}

public readonly record struct MirageImageSourceProjectionResult(
    bool IsResolved,
    string? BrowserUrl,
    string SourceKind,
    string InitialState,
    string? FailureReason,
    bool IsGifSource,
    bool IsManagedObjectUrl)
{
    public static MirageImageSourceProjectionResult Empty()
    {
        return new MirageImageSourceProjectionResult(
            IsResolved: true,
            BrowserUrl: null,
            SourceKind: "none",
            InitialState: "empty",
            FailureReason: null,
            IsGifSource: false,
            IsManagedObjectUrl: false);
    }

    public static MirageImageSourceProjectionResult Resolved(
        string browserUrl,
        string sourceKind,
        bool isGifSource = false,
        bool isManagedObjectUrl = false)
    {
        return new MirageImageSourceProjectionResult(
            IsResolved: true,
            BrowserUrl: browserUrl,
            SourceKind: sourceKind,
            InitialState: "loading",
            FailureReason: null,
            IsGifSource: isGifSource,
            IsManagedObjectUrl: isManagedObjectUrl);
    }

    public static MirageImageSourceProjectionResult Unresolved(string sourceKind, string failureReason)
    {
        return new MirageImageSourceProjectionResult(
            IsResolved: false,
            BrowserUrl: null,
            SourceKind: sourceKind,
            InitialState: "unresolved",
            FailureReason: failureReason,
            IsGifSource: false,
            IsManagedObjectUrl: false);
    }
}
