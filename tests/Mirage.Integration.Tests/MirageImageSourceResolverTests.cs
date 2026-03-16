using Mirage.Maui.Images;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageImageSourceResolverTests
{
    [Fact]
    public async Task Resolve_ExactIdentityLookup_UsesManifestIdentityMap()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var result = resolver.Resolve("ControlsSample.Shared:dotnet_bot.png");

        Assert.True(result.IsResolved);
        Assert.Equal("/_mirage/assets/ControlsSample.Shared/dotnet_bot.11111111.png", result.BrowserUrl);
    }

    [Fact]
    public async Task Resolve_LogicalPathAliasLookup_UsesAliasWhenUnique()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var result = resolver.Resolve("./Images/hero/banner.png");

        Assert.True(result.IsResolved);
        Assert.Equal("/_mirage/assets/ControlsSample.Shared/Images/hero/banner.22222222.png", result.BrowserUrl);
    }

    [Fact]
    public async Task Resolve_FileNameAliasLookup_UsesAliasWhenLogicalPathIsMissing()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var result = resolver.Resolve("assets/dotnet_bot.png");

        Assert.True(result.IsResolved);
        Assert.Equal("/_mirage/assets/ControlsSample.Shared/dotnet_bot.11111111.png", result.BrowserUrl);
    }

    [Fact]
    public async Task Resolve_FileStemAliasLookup_UsesAliasOnlyWhenUnique()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var result = resolver.Resolve("dotnet_bot");

        Assert.True(result.IsResolved);
        Assert.Equal("/_mirage/assets/ControlsSample.Shared/dotnet_bot.11111111.png", result.BrowserUrl);
    }

    [Fact]
    public async Task Resolve_AmbiguousAliases_FailDeterministically()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var fileNameResult = resolver.Resolve("logo.png");
        var fileStemResult = resolver.Resolve("logo");

        Assert.False(fileNameResult.IsResolved);
        Assert.Contains("ambiguous", fileNameResult.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(fileStemResult.IsResolved);
        Assert.Contains("ambiguous", fileStemResult.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_ExternalUris_ArePassedThrough()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var httpResult = resolver.Resolve("https://example.com/logo.png");
        var rootedResult = resolver.Resolve("/_mirage/assets/ControlsSample.Shared/dotnet_bot.11111111.png");
        var dataUriResult = resolver.Resolve("data:image/png;base64,AAAA");

        Assert.True(httpResult.IsResolved);
        Assert.Equal("https://example.com/logo.png", httpResult.BrowserUrl);
        Assert.True(rootedResult.IsResolved);
        Assert.Equal("/_mirage/assets/ControlsSample.Shared/dotnet_bot.11111111.png", rootedResult.BrowserUrl);
        Assert.True(dataUriResult.IsResolved);
        Assert.Equal("data:image/png;base64,AAAA", dataUriResult.BrowserUrl);
    }

    [Fact]
    public async Task Resolve_ManifestUnavailable_UsesDeterministicDegradedMode()
    {
        var resolver = CreateResolver(content: null, isSuccess: false, statusCode: 404);
        await resolver.EnsureInitializedAsync();

        var bundledResult = resolver.Resolve("dotnet_bot.png");
        var rootedResult = resolver.Resolve("/images/dotnet_bot.png");

        Assert.False(bundledResult.IsResolved);
        Assert.Contains("manifest", bundledResult.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.True(rootedResult.IsResolved);
        Assert.Equal("/images/dotnet_bot.png", rootedResult.BrowserUrl);
    }

    [Fact]
    public async Task Resolve_ManifestMalformed_UsesDeterministicDegradedMode()
    {
        var resolver = CreateResolver("{invalid json");
        await resolver.EnsureInitializedAsync();

        var bundledResult = resolver.Resolve("dotnet_bot.png");

        Assert.False(bundledResult.IsResolved);
        Assert.Contains("manifest", bundledResult.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    private static MirageManifestImageSourceResolver CreateResolver(string? content, bool isSuccess = true, int statusCode = 200)
    {
        return new MirageManifestImageSourceResolver(new StubManifestProvider(content, isSuccess, statusCode));
    }

    private static string CreateManifestJson()
    {
        return """
               {
                 "version": 1,
                 "images": {
                   "ControlsSample.Shared:dotnet_bot.png": "/_mirage/assets/ControlsSample.Shared/dotnet_bot.11111111.png",
                   "ControlsSample.Shared:Images/hero/banner.png": "/_mirage/assets/ControlsSample.Shared/Images/hero/banner.22222222.png",
                   "Showcase.Shared:Images/logo.png": "/_mirage/assets/Showcase.Shared/Images/logo.33333333.png",
                   "Counter.Shared:Images/logo.png": "/_mirage/assets/Counter.Shared/Images/logo.44444444.png"
                 },
                 "logicalPath": {
                   "dotnet_bot.png": ["ControlsSample.Shared:dotnet_bot.png"],
                   "images/hero/banner.png": ["ControlsSample.Shared:Images/hero/banner.png"],
                   "images/logo.png": [
                     "Showcase.Shared:Images/logo.png",
                     "Counter.Shared:Images/logo.png"
                   ]
                 },
                 "fileName": {
                   "dotnet_bot.png": ["ControlsSample.Shared:dotnet_bot.png"],
                   "banner.png": ["ControlsSample.Shared:Images/hero/banner.png"],
                   "logo.png": [
                     "Showcase.Shared:Images/logo.png",
                     "Counter.Shared:Images/logo.png"
                   ]
                 },
                 "fileStem": {
                   "dotnet_bot": ["ControlsSample.Shared:dotnet_bot.png"],
                   "banner": ["ControlsSample.Shared:Images/hero/banner.png"],
                   "logo": [
                     "Showcase.Shared:Images/logo.png",
                     "Counter.Shared:Images/logo.png"
                   ]
                 }
               }
               """;
    }

    private sealed class StubManifestProvider : IMirageImageManifestProvider
    {
        private readonly string? _content;
        private readonly bool _isSuccess;
        private readonly int _statusCode;

        public StubManifestProvider(string? content, bool isSuccess, int statusCode)
        {
            _content = content;
            _isSuccess = isSuccess;
            _statusCode = statusCode;
        }

        public Task<MirageImageManifestLoadResult> LoadAsync(string manifestPath)
        {
            _ = manifestPath;
            return Task.FromResult(new MirageImageManifestLoadResult(_isSuccess, _statusCode, _content));
        }
    }
}
