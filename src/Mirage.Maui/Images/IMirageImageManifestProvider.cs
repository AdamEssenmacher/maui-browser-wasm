namespace Mirage.Maui.Images;

internal interface IMirageImageManifestProvider
{
    Task<MirageImageManifestLoadResult> LoadAsync(string manifestPath);
}

internal readonly record struct MirageImageManifestLoadResult(bool IsSuccess, int StatusCode, string? Content);
