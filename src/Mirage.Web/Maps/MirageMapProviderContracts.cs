using Mirage.Core.Semantics;
using Mirage.Web.Dom;

namespace Mirage.Web.Maps;

public readonly record struct MirageMapProviderInitializationResult(
    bool IsReady,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static MirageMapProviderInitializationResult Ready() => new(
        IsReady: true,
        ErrorCode: null,
        ErrorMessage: null);

    public static MirageMapProviderInitializationResult Failed(string errorCode, string? errorMessage) => new(
        IsReady: false,
        ErrorCode: errorCode,
        ErrorMessage: errorMessage);
}

public interface IMirageMapProvider
{
    string ProviderName { get; }

    MirageMapProviderInitializationResult Initialization { get; }

    Task<MirageMapProviderInitializationResult> EnsureInitializedAsync(CancellationToken cancellationToken = default);

    IMirageMapAdapter CreateAdapter(string nodeId, DomElement hostElement);
}

public interface IMirageMapAdapter : IDisposable
{
    void ApplyState(MirageMapSemanticState state);

    void ShowError(string errorCode, string message);
}
