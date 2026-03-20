using Mirage.Maui.Images;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
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
    public async Task Resolve_ImageSource_FileSource_UsesManifestResolverPath()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var result = resolver.Resolve((ImageSource)new FileImageSource { File = "dotnet_bot.png" });

        Assert.True(result.IsResolved);
        Assert.Equal("file", result.SourceKind);
        Assert.Equal("/_mirage/assets/ControlsSample.Shared/dotnet_bot.11111111.png", result.BrowserUrl);
        Assert.Equal("loading", result.InitialState);
    }

    [Fact]
    public async Task Resolve_ImageSource_FileSource_ExtensionlessAlias_PreservesGifDetectionFromResolvedUrl()
    {
        var resolver = CreateResolver(CreateGifAliasManifestJson());
        await resolver.EnsureInitializedAsync();

        var result = resolver.Resolve((ImageSource)new FileImageSource { File = "animated_heart" });

        Assert.True(result.IsResolved);
        Assert.Equal("file", result.SourceKind);
        Assert.Equal("/_mirage/assets/ControlsSample.Shared/animated_heart.abcd1234.gif", result.BrowserUrl);
        Assert.True(result.IsGifSource);
    }

    [Fact]
    public async Task Resolve_ImageSource_UriSource_AllowsApprovedSchemes_AndRejectsUnsupportedValues()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var httpsResult = resolver.Resolve((ImageSource)new UriImageSource { Uri = new Uri("https://example.com/photo.png") });
        var blobResult = resolver.Resolve((ImageSource)new UriImageSource { Uri = new Uri("blob:https://example.com/123") });
        var ftpResult = resolver.Resolve((ImageSource)new UriImageSource { Uri = new Uri("ftp://example.com/image.png") });

        Assert.True(httpsResult.IsResolved);
        Assert.Equal("uri", httpsResult.SourceKind);
        Assert.Equal("https://example.com/photo.png", httpsResult.BrowserUrl);

        Assert.True(blobResult.IsResolved);
        Assert.Equal("blob:https://example.com/123", blobResult.BrowserUrl);

        Assert.False(ftpResult.IsResolved);
        Assert.Contains("supported", ftpResult.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_ImageSource_FontSource_ReturnsDeterministicDataUri()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var source = new FontImageSource
        {
            Glyph = "★",
            FontFamily = "serif",
            Size = 24,
            Color = Colors.Orange
        };

        var first = resolver.Resolve((ImageSource)source);
        var second = resolver.Resolve((ImageSource)source);

        Assert.True(first.IsResolved);
        Assert.True(second.IsResolved);
        Assert.Equal("font", first.SourceKind);
        Assert.StartsWith("data:image/svg+xml;base64,", first.BrowserUrl, StringComparison.Ordinal);
        Assert.Equal(first.BrowserUrl, second.BrowserUrl);
    }

    [Fact]
    public async Task ResolveAsync_StreamSource_ReturnsBlobUrl_AndReleaseRevokesUrl()
    {
        var objectUrlFactory = new StubObjectUrlFactory();
        var resolver = CreateResolver(CreateManifestJson(), objectUrlFactory);
        await resolver.EnsureInitializedAsync();

        var streamSource = new StreamImageSource
        {
            Stream = _ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00 }))
        };

        var projection = await resolver.ResolveAsync(streamSource);

        Assert.True(projection.IsResolved);
        Assert.Equal("stream", projection.SourceKind);
        Assert.True(projection.IsManagedObjectUrl);
        Assert.True(projection.IsGifSource);
        Assert.Equal("blob:mirage-1", projection.BrowserUrl);

        resolver.Release(projection.BrowserUrl);
        Assert.Equal(new[] { "blob:mirage-1" }, objectUrlFactory.RevokedUrls);
    }

    [Fact]
    public async Task ResolveAsync_StreamSource_WithoutObjectUrlFactory_FailsDeterministically()
    {
        var resolver = CreateResolver(CreateManifestJson());
        await resolver.EnsureInitializedAsync();

        var projection = await resolver.ResolveAsync(new StreamImageSource
        {
            Stream = _ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }))
        });

        Assert.False(projection.IsResolved);
        Assert.Equal("stream", projection.SourceKind);
        Assert.Contains("factory", projection.FailureReason, StringComparison.OrdinalIgnoreCase);
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

    private static MirageManifestImageSourceResolver CreateResolver(
        string? content,
        IMirageObjectUrlFactory? objectUrlFactory = null,
        bool isSuccess = true,
        int statusCode = 200)
    {
        return new MirageManifestImageSourceResolver(
            new StubManifestProvider(content, isSuccess, statusCode),
            objectUrlFactory);
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

    private static string CreateGifAliasManifestJson()
    {
        return """
               {
                 "version": 1,
                 "images": {
                   "ControlsSample.Shared:animated_heart.gif": "/_mirage/assets/ControlsSample.Shared/animated_heart.abcd1234.gif"
                 },
                 "logicalPath": {
                   "animated_heart.gif": ["ControlsSample.Shared:animated_heart.gif"]
                 },
                 "fileName": {
                   "animated_heart.gif": ["ControlsSample.Shared:animated_heart.gif"]
                 },
                 "fileStem": {
                   "animated_heart": ["ControlsSample.Shared:animated_heart.gif"]
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

    private sealed class StubObjectUrlFactory : IMirageObjectUrlFactory
    {
        private int _next;

        public List<string> RevokedUrls { get; } = [];

        public string? CreateObjectUrl(ReadOnlySpan<byte> content, string contentType)
        {
            _ = content;
            _ = contentType;
            _next++;
            return $"blob:mirage-{_next}";
        }

        public void RevokeObjectUrl(string objectUrl)
        {
            RevokedUrls.Add(objectUrl);
        }
    }
}
