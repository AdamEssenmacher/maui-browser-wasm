namespace Mirage.Maui.Images;

public interface IMirageImageSourceResolver
{
    ValueTask EnsureInitializedAsync();

    MirageImageSourceResolveResult Resolve(string? source);
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
