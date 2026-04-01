using Xunit;

namespace Mirage.Integration.Tests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void MirageWeb_DoesNotContainProviderSpecificMappings()
    {
        var webPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web");
        Assert.True(Directory.Exists(webPath), $"Mirage.Web path was not found: {webPath}");

        var sourceFiles = Directory.GetFiles(webPath, "*.*", SearchOption.AllDirectories)
            .Where(static path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(sourceFiles);

        var forbiddenMarkers = new[]
        {
            "Mirage.Provider.Fast",
            "Mirage.Provider.Fluent",
            "fast-button",
            "fast-text-field",
            "fluent-button",
            "fluent-text-field",
            "ui5-"
        };

        foreach (var file in sourceFiles)
        {
            var text = File.ReadAllText(file);
            foreach (var marker in forbiddenMarkers)
            {
                Assert.DoesNotContain(marker, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void BuiltInHandlers_DoNotUseWebNodeFactory_ExceptBorderStrokeRenderer()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");

        var builtInHandlerFiles = new[]
        {
            "MirageButtonHandler.cs",
            "MirageImageButtonHandler.cs",
            "MirageImageHandler.cs",
            "MirageProgressBarHandler.cs",
            "MirageGraphicsViewHandler.cs",
            "MirageMapHandler.cs",
            "MirageLabelHandler.cs",
            "MirageBoxViewHandler.cs",
            "MiragePageHandler.cs",
            "MirageContentViewHandler.cs",
            "MirageContentPageHandler.cs",
            "MirageNavigationPageHandler.cs",
            "MirageLayoutHandler.cs",
            "MirageWindowHandler.cs"
        };

        foreach (var file in builtInHandlerFiles)
        {
            var fullPath = Path.Combine(handlersPath, file);
            Assert.True(File.Exists(fullPath), $"Expected handler file not found: {fullPath}");
            var text = File.ReadAllText(fullPath);
            Assert.DoesNotContain("CreateWeb(", text, StringComparison.Ordinal);
        }

        var borderHandlerPath = Path.Combine(handlersPath, "MirageBorderHandler.cs");
        Assert.True(File.Exists(borderHandlerPath), $"Expected handler file not found: {borderHandlerPath}");
        var borderText = File.ReadAllText(borderHandlerPath);
        Assert.Contains("CreateWeb(", borderText, StringComparison.Ordinal);
    }

    [Fact]
    public void ActiveMirageSurface_DoesNotUseStalePreCutoverTerminology()
    {
        var repositoryRoot = GetRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(repositoryRoot, "src"),
            Path.Combine(repositoryRoot, "tests"),
            Path.Combine(repositoryRoot, "samples"),
            Path.Combine(repositoryRoot, "docs")
        };

        var rootMarkdown = Directory.GetFiles(repositoryRoot, "*.md", SearchOption.TopDirectoryOnly);

        var files = roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            .Concat(rootMarkdown)
            .Where(path =>
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .Where(path =>
                !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.EndsWith($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}DECISIONS.md", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(files);

        var forbiddenMarkers = new[]
        {
            "Typed" + "Node",
            "Element" + "Node",
            "TypedComponent" + "Descriptor",
            "ElementComponent" + "Descriptor",
            "Resolve" + "Typed",
            "Resolve" + "Element",
            "Supports" + "Type" + "dNodes",
            "Supports" + "Element" + "Nodes",
            "PlatformNodeType." + "Type" + "d",
            "PlatformNodeType." + "Element",
            "NodeCreationKind." + "Type" + "d",
            "NodeCreationKind." + "Element"
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var marker in forbiddenMarkers)
            {
                Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void ActiveArchitectureDocs_NarrowProviderBreadthClaims()
    {
        var repositoryRoot = GetRepositoryRoot();
        var readmeText = File.ReadAllText(Path.Combine(repositoryRoot, "README.md"));
        var providerText = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "PROVIDER_AUTHORING.md"));
        var guardrailsText = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "ARCHITECTURE_GUARDRAILS.md"));

        Assert.DoesNotContain("adapt complete web component libraries", readmeText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adapt complete web component libraries", providerText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("raw custom-element hosting seam", readmeText, StringComparison.Ordinal);
        Assert.Contains("not yet a full arbitrary component-library adapter surface", providerText, StringComparison.Ordinal);
        Assert.Contains("current scope is raw DOM/custom-element hosting", guardrailsText, StringComparison.Ordinal);
        Assert.Contains("explicit special-case subsystems such as the map provider seam", guardrailsText, StringComparison.Ordinal);
    }

    [Fact]
    public void DeferredWebNodeSurface_IsDocumentedBeforeRuntimeSupport()
    {
        var repositoryRoot = GetRepositoryRoot();
        var providerText = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "PROVIDER_AUTHORING.md"));
        var guardrailsText = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "ARCHITECTURE_GUARDRAILS.md"));
        var diffText = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Mirage.Core", "Diff", "SemanticDiffEngine.cs"));
        var runtimeText = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Mirage.Web", "Runtime", "MirageRuntime.cs"));
        var platformViewText = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Mirage.Maui", "Handlers", "MiragePlatformView.cs"));

        Assert.Contains("These fields are reserved until Mirage diff/runtime support them end-to-end.", providerText, StringComparison.Ordinal);
        Assert.Contains("LayoutMetadata", guardrailsText, StringComparison.Ordinal);
        Assert.Contains("reserved until Mirage diff/runtime support it end-to-end.", guardrailsText, StringComparison.Ordinal);

        Assert.DoesNotContain("Slots", diffText, StringComparison.Ordinal);
        Assert.DoesNotContain("LayoutMetadata", diffText, StringComparison.Ordinal);
        Assert.DoesNotContain("Metadata", diffText, StringComparison.Ordinal);
        Assert.DoesNotContain("Slots", runtimeText, StringComparison.Ordinal);
        Assert.DoesNotContain("LayoutMetadata", runtimeText, StringComparison.Ordinal);
        Assert.DoesNotContain("Slots", platformViewText, StringComparison.Ordinal);
        Assert.DoesNotContain("LayoutMetadata", platformViewText, StringComparison.Ordinal);
        Assert.DoesNotContain("Metadata", platformViewText, StringComparison.Ordinal);
    }

    [Fact]
    public void PaddingMapping_IncludesLabelParitySurface()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");

        var layoutText = File.ReadAllText(Path.Combine(handlersPath, "MirageLayoutHandler.cs"));
        var contentPageText = File.ReadAllText(Path.Combine(handlersPath, "MirageContentPageHandler.cs"));
        var contentViewText = File.ReadAllText(Path.Combine(handlersPath, "MirageContentViewHandler.cs"));
        var labelText = File.ReadAllText(Path.Combine(handlersPath, "MirageLabelHandler.cs"));
        var entryText = File.ReadAllText(Path.Combine(handlersPath, "MirageEntryHandler.cs"));
        var buttonText = File.ReadAllText(Path.Combine(handlersPath, "MirageButtonHandler.cs"));

        Assert.Contains("MapPadding", layoutText, StringComparison.Ordinal);
        Assert.Contains("MapPadding", contentPageText, StringComparison.Ordinal);
        Assert.Contains("MapPadding", contentViewText, StringComparison.Ordinal);
        Assert.Contains("MapPadding", labelText, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPadding", entryText, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPadding", buttonText, StringComparison.Ordinal);
    }

    [Fact]
    public void LabelHandler_MapsParitySurfaceAndControlsRemapKeys()
    {
        var labelHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageLabelHandler.cs");
        Assert.True(File.Exists(labelHandlerPath), $"Expected handler file not found: {labelHandlerPath}");

        var text = File.ReadAllText(labelHandlerPath);

        Assert.Contains("nameof(ILabel.Text)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(ITextStyle.CharacterSpacing)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(ILabel.TextDecorations)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(ILabel.LineHeight)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(ILabel.Padding)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(Label.LineBreakMode)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(Label.MaxLines)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(Label.TextType)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(Label.TextTransform)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(Label.FormattedText)", text, StringComparison.Ordinal);
        Assert.Contains("ParityCommandKeys = []", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ButtonHandler_MapsStrokeAndCornerStylePrimitives()
    {
        var buttonHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageButtonHandler.cs");
        Assert.True(File.Exists(buttonHandlerPath), $"Expected handler file not found: {buttonHandlerPath}");

        var text = File.ReadAllText(buttonHandlerPath);
        Assert.Contains("nameof(IButtonStroke.StrokeColor)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(IButtonStroke.StrokeThickness)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(IButtonStroke.CornerRadius)", text, StringComparison.Ordinal);
        Assert.Contains("MapStrokeColor", text, StringComparison.Ordinal);
        Assert.Contains("MapStrokeThickness", text, StringComparison.Ordinal);
        Assert.Contains("MapCornerRadius", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VerticalStackContract_UsesExpectedFlexSemantics()
    {
        var layoutHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageLayoutHandler.cs");
        Assert.True(File.Exists(layoutHandlerPath), $"Expected handler file not found: {layoutHandlerPath}");

        var text = File.ReadAllText(layoutHandlerPath);
        Assert.Contains("ComponentKind.VerticalStack", text, StringComparison.Ordinal);
        Assert.Contains("SetStyleIfChanged(\"display\", \"flex\")", text, StringComparison.Ordinal);
        Assert.Contains("SetStyleIfChanged(\"flexDirection\", \"column\")", text, StringComparison.Ordinal);
        Assert.Contains("SetStyleIfChanged(\"alignItems\", \"stretch\")", text, StringComparison.Ordinal);
        Assert.Contains("SetStyleIfChanged(\"gap\", MirageViewMapper.ToPx", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentPageAndWindow_DefaultToNonScrollableOverflowBehavior()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");

        var windowText = File.ReadAllText(Path.Combine(handlersPath, "MirageWindowHandler.cs"));
        var contentContainerText = File.ReadAllText(Path.Combine(handlersPath, "MirageContentViewHandlerBase.cs"));
        var pageText = File.ReadAllText(Path.Combine(handlersPath, "MirageContentPageHandler.cs"));

        Assert.Contains("overflow\", \"hidden\"", windowText, StringComparison.Ordinal);
        Assert.DoesNotContain("overflowY\", \"auto\"", windowText, StringComparison.Ordinal);
        Assert.Contains("overflow\", \"hidden\"", contentContainerText, StringComparison.Ordinal);
        Assert.DoesNotContain("overflowY\", \"auto\"", contentContainerText, StringComparison.Ordinal);
        Assert.Contains("IsBoundedPageSurface => true", pageText, StringComparison.Ordinal);
    }

    [Fact]
    public void ContainerHandlers_ForwardArrangeThroughBaseAndCrossPlatformLayout()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");

        var directContainerHandlerFiles = new[]
        {
            "MirageLayoutHandler.cs",
            "MirageScrollViewHandler.cs",
            "MirageNavigationPageHandler.cs"
        };

        foreach (var file in directContainerHandlerFiles)
        {
            var fullPath = Path.Combine(handlersPath, file);
            Assert.True(File.Exists(fullPath), $"Expected handler file not found: {fullPath}");
            var text = File.ReadAllText(fullPath);
            Assert.Contains("base.PlatformArrange(frame);", text, StringComparison.Ordinal);
        }

        var contentContainerBasePath = Path.Combine(handlersPath, "MirageContentViewHandlerBase.cs");
        Assert.True(File.Exists(contentContainerBasePath), $"Expected handler file not found: {contentContainerBasePath}");
        var contentContainerBaseText = File.ReadAllText(contentContainerBasePath);
        Assert.Contains(": MirageSingleContentHostHandler", contentContainerBaseText, StringComparison.Ordinal);
        Assert.Contains("base.PlatformArrange(frame);", contentContainerBaseText, StringComparison.Ordinal);
        Assert.Contains("VirtualView.CrossPlatformArrange", contentContainerBaseText, StringComparison.Ordinal);
        Assert.Contains("NormalizeHostedContentFlowStyles", contentContainerBaseText, StringComparison.Ordinal);
        Assert.DoesNotContain("SetStyle(\"width\", MirageViewMapper.ToPx", contentContainerBaseText, StringComparison.Ordinal);
        Assert.DoesNotContain("SetStyle(\"height\", MirageViewMapper.ToPx", contentContainerBaseText, StringComparison.Ordinal);

        var singleContentHostBasePath = Path.Combine(handlersPath, "MirageSingleContentHostHandler.cs");
        Assert.True(File.Exists(singleContentHostBasePath), $"Expected handler file not found: {singleContentHostBasePath}");
        var singleContentHostBaseText = File.ReadAllText(singleContentHostBasePath);
        Assert.Contains("SetPresentedContent", singleContentHostBaseText, StringComparison.Ordinal);
        Assert.Contains("DisconnectCurrentPresentedContent", singleContentHostBaseText, StringComparison.Ordinal);
        Assert.Contains("NormalizeHostedContentFlowStyles", singleContentHostBaseText, StringComparison.Ordinal);

        var contentViewHandlerText = File.ReadAllText(Path.Combine(handlersPath, "MirageContentViewHandler.cs"));
        var contentPageHandlerText = File.ReadAllText(Path.Combine(handlersPath, "MirageContentPageHandler.cs"));
        var scrollViewHandlerText = File.ReadAllText(Path.Combine(handlersPath, "MirageScrollViewHandler.cs"));
        Assert.Contains(": MirageContentViewHandlerBase<ContentView>", contentViewHandlerText, StringComparison.Ordinal);
        Assert.Contains(": MirageContentViewHandlerBase<ContentPage>", contentPageHandlerText, StringComparison.Ordinal);
        Assert.Contains("nameof(Page.Title)", contentPageHandlerText, StringComparison.Ordinal);
        Assert.Contains(": MirageSingleContentHostHandler<IScrollView>", scrollViewHandlerText, StringComparison.Ordinal);
        Assert.DoesNotContain("NormalizeContentPosition(", scrollViewHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersScrollViewHandler()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var text = File.ReadAllText(hostingPath);
        Assert.Contains("typeof(ScrollView)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageScrollViewHandler)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersMapHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var mapHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageMapHandler.cs");
        Assert.True(File.Exists(mapHandlerPath), $"Expected map handler file not found: {mapHandlerPath}");

        var hostingText = File.ReadAllText(hostingPath);
        var mapHandlerText = File.ReadAllText(mapHandlerPath);

        Assert.Contains("using MauiMap = Microsoft.Maui.Controls.Maps.Map;", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MauiMap)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageMapHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(Pin)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageMapPinHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MapElement)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageMapElementHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.Map", mapHandlerText, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWeb(", mapHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersIndicatorViewHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var indicatorHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageIndicatorViewHandler.cs");
        Assert.True(File.Exists(indicatorHandlerPath), $"Expected indicator handler file not found: {indicatorHandlerPath}");

        var hostingText = File.ReadAllText(hostingPath);
        var indicatorHandlerText = File.ReadAllText(indicatorHandlerPath);

        Assert.Contains("typeof(IndicatorView)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageIndicatorViewHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.IndicatorView", indicatorHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersShapeHandlersAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var shapeHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageShapeViewHandler.cs");
        Assert.True(File.Exists(shapeHandlerPath), $"Expected shape handler file not found: {shapeHandlerPath}");

        var hostingText = File.ReadAllText(hostingPath);
        var shapeHandlerText = File.ReadAllText(shapeHandlerPath);

        Assert.Contains("typeof(MauiShapes.Ellipse)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageShapeViewHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MauiShapes.Line)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageLineHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MauiShapes.Path)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MiragePathHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MauiShapes.Polygon)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MiragePolygonHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MauiShapes.Polyline)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MiragePolylineHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MauiShapes.Rectangle)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageRectangleHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MauiShapes.RoundRectangle)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageRoundRectangleHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.ShapeView", shapeHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersStackLayoutImageAndBorderHandlers()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var text = File.ReadAllText(hostingPath);
        Assert.Contains("typeof(StackLayout)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(FlexLayout)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageLayoutHandler)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(Image)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageImageHandler)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(Border)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageBorderHandler)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersCollectionViewHandler()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var text = File.ReadAllText(hostingPath);
        Assert.Contains("typeof(CollectionView)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageCollectionViewHandler)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersCarouselViewHandler()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var text = File.ReadAllText(hostingPath);
        Assert.Contains("typeof(CarouselView)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageCarouselViewHandler)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersSearchBarHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var searchBarHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageSearchBarHandler.cs");
        Assert.True(File.Exists(searchBarHandlerPath), $"Expected SearchBar handler file not found: {searchBarHandlerPath}");

        var hostingText = File.ReadAllText(hostingPath);
        var searchBarHandlerText = File.ReadAllText(searchBarHandlerPath);
        var coordinatorPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageRenderCoordinator.cs");
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(coordinatorPath), $"Expected render coordinator file not found: {coordinatorPath}");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");
        var coordinatorText = File.ReadAllText(coordinatorPath);
        var runtimeText = File.ReadAllText(runtimePath);

        Assert.Contains("typeof(SearchBar)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageSearchBarHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.SearchBar", searchBarHandlerText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeFocus(", searchBarHandlerText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeFocus(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeFocus(", runtimeText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersEntryHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var entryHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageEntryHandler.cs");
        Assert.True(File.Exists(entryHandlerPath), $"Expected Entry handler file not found: {entryHandlerPath}");

        var coordinatorPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageRenderCoordinator.cs");
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(coordinatorPath), $"Expected render coordinator file not found: {coordinatorPath}");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var hostingText = File.ReadAllText(hostingPath);
        var entryHandlerText = File.ReadAllText(entryHandlerPath);
        var coordinatorText = File.ReadAllText(coordinatorPath);
        var runtimeText = File.ReadAllText(runtimePath);

        Assert.Contains("typeof(Entry)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageEntryHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.Entry", entryHandlerText, StringComparison.Ordinal);
        Assert.Contains("CreateWeb(\"input\")", entryHandlerText, StringComparison.Ordinal);
        Assert.Contains("CreateWeb(\"button\")", entryHandlerText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeFocus(", entryHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryGetNodeTextSelection(", entryHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryGetNodeTextSelection(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeTextSelection(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TryGetNodeTextSelection(", runtimeText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeTextSelection(", runtimeText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersEditorHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var editorHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageEditorHandler.cs");
        Assert.True(File.Exists(editorHandlerPath), $"Expected Editor handler file not found: {editorHandlerPath}");

        var coordinatorPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageRenderCoordinator.cs");
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(coordinatorPath), $"Expected render coordinator file not found: {coordinatorPath}");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var hostingText = File.ReadAllText(hostingPath);
        var editorHandlerText = File.ReadAllText(editorHandlerPath);
        var coordinatorText = File.ReadAllText(coordinatorPath);
        var runtimeText = File.ReadAllText(runtimePath);

        Assert.Contains("typeof(Editor)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageEditorHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.Editor", editorHandlerText, StringComparison.Ordinal);
        Assert.Contains("CreateWeb(\"textarea\")", editorHandlerText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeFocus(", editorHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryGetNodeTextSelection(", editorHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryGetNodeTextSelection(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeTextSelection(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TryGetNodeTextSelection(", runtimeText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeTextSelection(", runtimeText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersRadioButtonHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var radioButtonHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageRadioButtonHandler.cs");
        Assert.True(File.Exists(radioButtonHandlerPath), $"Expected RadioButton handler file not found: {radioButtonHandlerPath}");

        var hostingText = File.ReadAllText(hostingPath);
        var radioButtonHandlerText = File.ReadAllText(radioButtonHandlerPath);

        Assert.Contains("typeof(RadioButton)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageRadioButtonHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.RadioButton", radioButtonHandlerText, StringComparison.Ordinal);
        Assert.Contains("CreateWeb(\"input\")", radioButtonHandlerText, StringComparison.Ordinal);
        Assert.Contains("CreateWeb(\"div\")", radioButtonHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersImageButtonHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var imageButtonHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageImageButtonHandler.cs");
        Assert.True(File.Exists(imageButtonHandlerPath), $"Expected ImageButton handler file not found: {imageButtonHandlerPath}");

        var hostingText = File.ReadAllText(hostingPath);
        var imageButtonHandlerText = File.ReadAllText(imageButtonHandlerPath);

        Assert.Contains("typeof(ImageButton)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageImageButtonHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.ImageButton", imageButtonHandlerText, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWeb(", imageButtonHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersTimePickerHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var timePickerHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageTimePickerHandler.cs");
        Assert.True(File.Exists(timePickerHandlerPath), $"Expected TimePicker handler file not found: {timePickerHandlerPath}");

        var coordinatorPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageRenderCoordinator.cs");
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(coordinatorPath), $"Expected render coordinator file not found: {coordinatorPath}");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var hostingText = File.ReadAllText(hostingPath);
        var timePickerHandlerText = File.ReadAllText(timePickerHandlerPath);
        var coordinatorText = File.ReadAllText(coordinatorPath);
        var runtimeText = File.ReadAllText(runtimePath);

        Assert.Contains("typeof(TimePicker)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageTimePickerHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.TimePicker", timePickerHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", timePickerHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", runtimeText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersDatePickerHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var datePickerHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageDatePickerHandler.cs");
        Assert.True(File.Exists(datePickerHandlerPath), $"Expected DatePicker handler file not found: {datePickerHandlerPath}");

        var coordinatorPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageRenderCoordinator.cs");
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(coordinatorPath), $"Expected render coordinator file not found: {coordinatorPath}");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var hostingText = File.ReadAllText(hostingPath);
        var datePickerHandlerText = File.ReadAllText(datePickerHandlerPath);
        var coordinatorText = File.ReadAllText(coordinatorPath);
        var runtimeText = File.ReadAllText(runtimePath);

        Assert.Contains("typeof(DatePicker)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageDatePickerHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.DatePicker", datePickerHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", datePickerHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", runtimeText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersPickerHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var pickerHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MiragePickerHandler.cs");
        Assert.True(File.Exists(pickerHandlerPath), $"Expected Picker handler file not found: {pickerHandlerPath}");

        var coordinatorPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageRenderCoordinator.cs");
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(coordinatorPath), $"Expected render coordinator file not found: {coordinatorPath}");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var hostingText = File.ReadAllText(hostingPath);
        var pickerHandlerText = File.ReadAllText(pickerHandlerPath);
        var coordinatorText = File.ReadAllText(coordinatorPath);
        var runtimeText = File.ReadAllText(runtimePath);

        Assert.Contains("typeof(Picker)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MiragePickerHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.Picker", pickerHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", pickerHandlerText, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeFocus(", pickerHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TryRequestNodePickerOpen(", runtimeText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersProgressBarHandlerAsBuiltInControlNode()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var progressBarHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageProgressBarHandler.cs");
        Assert.True(File.Exists(progressBarHandlerPath), $"Expected ProgressBar handler file not found: {progressBarHandlerPath}");

        var hostingText = File.ReadAllText(hostingPath);
        var progressBarHandlerText = File.ReadAllText(progressBarHandlerPath);

        Assert.Contains("typeof(ProgressBar)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageProgressBarHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.ProgressBar", progressBarHandlerText, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWeb(", progressBarHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersGraphicsViewHandlerAsBuiltInCanvasControlNode()
    {
        var repositoryRoot = GetRepositoryRoot();
        var hostingPath = Path.Combine(repositoryRoot, "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var graphicsHandlerPath = Path.Combine(repositoryRoot, "src", "Mirage.Maui", "Handlers", "MirageGraphicsViewHandler.cs");
        Assert.True(File.Exists(graphicsHandlerPath), $"Expected GraphicsView handler file not found: {graphicsHandlerPath}");

        var coordinatorPath = Path.Combine(repositoryRoot, "src", "Mirage.Maui", "Handlers", "MirageRenderCoordinator.cs");
        var runtimePath = Path.Combine(repositoryRoot, "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        var providerPath = Path.Combine(repositoryRoot, "src", "Mirage.Provider.Fluent", "FluentMirageComponentProvider.cs");
        Assert.True(File.Exists(coordinatorPath), $"Expected render coordinator file not found: {coordinatorPath}");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");
        Assert.True(File.Exists(providerPath), $"Expected provider file not found: {providerPath}");

        var hostingText = File.ReadAllText(hostingPath);
        var graphicsHandlerText = File.ReadAllText(graphicsHandlerPath);
        var coordinatorText = File.ReadAllText(coordinatorPath);
        var runtimeText = File.ReadAllText(runtimePath);
        var providerText = File.ReadAllText(providerPath);

        Assert.Contains("typeof(GraphicsView)", hostingText, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageGraphicsViewHandler)", hostingText, StringComparison.Ordinal);
        Assert.Contains("ComponentKind.GraphicsView", graphicsHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryConfigureNodeCanvas(", graphicsHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryRenderNodeCanvas(", graphicsHandlerText, StringComparison.Ordinal);
        Assert.Contains("TryConfigureNodeCanvas(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TryRenderNodeCanvas(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TryConfigureNodeCanvas(", runtimeText, StringComparison.Ordinal);
        Assert.Contains("TryRenderNodeCanvas(", runtimeText, StringComparison.Ordinal);
        Assert.Contains("EventPayloadKind.Pointer", providerText, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWeb(", graphicsHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void CollectionViewHandler_UsesMauiShapedHierarchy_AndGenericScrollSeam()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");
        Assert.True(Directory.Exists(runtimePath), $"Runtime path was not found: {runtimePath}");

        var itemsText = File.ReadAllText(Path.Combine(handlersPath, "MirageItemsViewHandler.cs"));
        var structuredText = File.ReadAllText(Path.Combine(handlersPath, "MirageStructuredItemsViewHandler.cs"));
        var selectableText = File.ReadAllText(Path.Combine(handlersPath, "MirageSelectableItemsViewHandler.cs"));
        var groupableText = File.ReadAllText(Path.Combine(handlersPath, "MirageGroupableItemsViewHandler.cs"));
        var reorderableText = File.ReadAllText(Path.Combine(handlersPath, "MirageReorderableItemsViewHandler.cs"));
        var collectionText = File.ReadAllText(Path.Combine(handlersPath, "MirageCollectionViewHandler.cs"));
        var coordinatorText = File.ReadAllText(Path.Combine(handlersPath, "MirageRenderCoordinator.cs"));
        var runtimeText = File.ReadAllText(Path.Combine(runtimePath, "MirageRuntime.cs"));

        Assert.Contains("class MirageItemsViewHandler", itemsText, StringComparison.Ordinal);
        Assert.Contains(": MirageItemsViewHandler<TItemsView>", structuredText, StringComparison.Ordinal);
        Assert.Contains(": MirageStructuredItemsViewHandler<TItemsView>", selectableText, StringComparison.Ordinal);
        Assert.Contains(": MirageSelectableItemsViewHandler<TItemsView>", groupableText, StringComparison.Ordinal);
        Assert.Contains(": MirageGroupableItemsViewHandler<TItemsView>", reorderableText, StringComparison.Ordinal);
        Assert.Contains(": MirageReorderableItemsViewHandler<CollectionView>", collectionText, StringComparison.Ordinal);

        Assert.Contains("TryScrollNodeChildIntoView(", itemsText, StringComparison.Ordinal);
        Assert.Contains("TryScrollNodeChildIntoView(", coordinatorText, StringComparison.Ordinal);
        Assert.Contains("TryScrollNodeChildIntoView(", runtimeText, StringComparison.Ordinal);
        Assert.DoesNotContain("TryCollectionViewScrollTo", coordinatorText, StringComparison.Ordinal);
        Assert.DoesNotContain("TryCollectionViewScrollTo", runtimeText, StringComparison.Ordinal);
    }

    [Fact]
    public void CarouselViewHandler_UsesDirectItemsViewHierarchy_WithoutCollectionSpecificLayers()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");

        var itemsText = File.ReadAllText(Path.Combine(handlersPath, "MirageItemsViewHandler.cs"));
        var carouselText = File.ReadAllText(Path.Combine(handlersPath, "MirageCarouselViewHandler.cs"));

        Assert.Contains("protected virtual LinearItemsLayout? ResolveLinearItemsLayout()", itemsText, StringComparison.Ordinal);
        Assert.Contains(": MirageItemsViewHandler<CarouselView>", carouselText, StringComparison.Ordinal);
        Assert.DoesNotContain("MirageStructuredItemsViewHandler", carouselText, StringComparison.Ordinal);
        Assert.DoesNotContain("MirageSelectableItemsViewHandler", carouselText, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWeb(", carouselText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersNavigationPageHandler()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var text = File.ReadAllText(hostingPath);
        Assert.Contains("typeof(NavigationPage)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageNavigationPageHandler)", text, StringComparison.Ordinal);
        Assert.Contains("UseMirageNavigation(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersPageHandler()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var text = File.ReadAllText(hostingPath);
        Assert.Contains("typeof(Page)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MiragePageHandler)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PageTitleProjection_UsesSharedDispatcherAcrossContainerHosts()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");

        var windowText = File.ReadAllText(Path.Combine(handlersPath, "MirageWindowHandler.cs"));
        var tabbedText = File.ReadAllText(Path.Combine(handlersPath, "MirageTabbedPageHandler.cs"));

        Assert.Contains("MiragePageHandler.TryProjectActivePageTitle(pageHandler)", windowText, StringComparison.Ordinal);
        Assert.Contains("TryProjectActivePageTitle()", tabbedText, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationProjection_DoesNotOverrideConfiguredHistoryPolicy()
    {
        var handlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageNavigationPageHandler.cs");
        Assert.True(File.Exists(handlerPath), $"Expected handler file not found: {handlerPath}");

        var text = File.ReadAllText(handlerPath);
        Assert.DoesNotContain("writeMode == MirageHistoryWriteMode.Replace", text, StringComparison.Ordinal);
        Assert.DoesNotContain("writeMode = MirageHistoryWriteMode.Push", text, StringComparison.Ordinal);
        Assert.Contains("DefaultRouteParser.ParseLocation(browserHistory.GetCurrentLocation())", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationProjection_PopStatePath_UsesSafeDispatchAndExceptionGuard()
    {
        var handlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageNavigationPageHandler.cs");
        Assert.True(File.Exists(handlerPath), $"Expected handler file not found: {handlerPath}");

        var text = File.ReadAllText(handlerPath);
        Assert.Contains("SafeApplyBrowserPopStateAsync", text, StringComparison.Ordinal);
        Assert.Contains("dispatcher.Dispatch(() => _ = SafeApplyBrowserPopStateAsync())", text, StringComparison.Ordinal);
        Assert.Contains("_pendingPopStateLocation = location;", text, StringComparison.Ordinal);
        Assert.Contains("catch (Exception ex)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationProjection_PopStateDoesNotMutateLastProjectedPathWithoutProjection()
    {
        var handlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageNavigationPageHandler.cs");
        Assert.True(File.Exists(handlerPath), $"Expected handler file not found: {handlerPath}");

        var text = File.ReadAllText(handlerPath);
        Assert.Contains("if (topRouteMatchesTarget)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("_lastProjectedPath = targetMatch.Path;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationPageHandler_ProjectsNavChromeShellAndBarStyleMappings()
    {
        var handlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageNavigationPageHandler.cs");
        Assert.True(File.Exists(handlerPath), $"Expected handler file not found: {handlerPath}");

        var text = File.ReadAllText(handlerPath);
        Assert.Contains("nameof(NavigationPage.BarBackgroundColor)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(NavigationPage.BarTextColor)", text, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.HasNavigationBarProperty.PropertyName", text, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.HasBackButtonProperty.PropertyName", text, StringComparison.Ordinal);
        Assert.Contains("mirage-navigation-bar", text, StringComparison.Ordinal);
        Assert.Contains("mirage-navigation-back-button", text, StringComparison.Ordinal);
        Assert.Contains("mirage-navigation-title", text, StringComparison.Ordinal);
        Assert.Contains("mirage-navigation-toolbar", text, StringComparison.Ordinal);
        Assert.Contains("mirage-navigation-content-host", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserAppHost_InvokesNavigationStartupResolverBeforeHandlerRealization()
    {
        var appHostPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageBrowserAppHost.cs");
        Assert.True(File.Exists(appHostPath), $"Expected browser app host file not found: {appHostPath}");

        var text = File.ReadAllText(appHostPath);
        Assert.Contains("IMirageNavigationStartupResolver", text, StringComparison.Ordinal);
        Assert.Contains("ApplyInitialRouteAsync(window, mauiApp.Services)", text, StringComparison.Ordinal);
        Assert.Contains("IsEnabledForAssembly", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserAppHost_AppliesEssentialsStaticPatchBeforeResolvingApplication()
    {
        var appHostPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageBrowserAppHost.cs");
        Assert.True(File.Exists(appHostPath), $"Expected browser app host file not found: {appHostPath}");

        var text = File.ReadAllText(appHostPath);
        var patchIndex = text.IndexOf("MirageEssentialsStaticPatcher.Apply(mauiApp.Services)", StringComparison.Ordinal);
        var applicationIndex = text.IndexOf("GetRequiredService<IApplication>()", StringComparison.Ordinal);

        Assert.True(patchIndex >= 0, "Expected MirageBrowserAppHost to apply the Essentials static patch.");
        Assert.True(applicationIndex >= 0, "Expected MirageBrowserAppHost to resolve IApplication.");
        Assert.True(patchIndex < applicationIndex, "Expected Essentials static patching to happen before IApplication is resolved.");
    }

    [Fact]
    public void MirageRuntime_BatchesNonStructuralDomMutations_AndLogsBatchDiagnostics()
    {
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        var batchPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "DomMutationBatch.cs");
        var domApiPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Dom", "DomApi.cs");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");
        Assert.True(File.Exists(batchPath), $"Expected batch file not found: {batchPath}");
        Assert.True(File.Exists(domApiPath), $"Expected DomApi file not found: {domApiPath}");

        var runtimeText = File.ReadAllText(runtimePath);
        var batchText = File.ReadAllText(batchPath);
        var domApiText = File.ReadAllText(domApiPath);

        Assert.Contains("var mutationBatch = new DomMutationBatch();", runtimeText, StringComparison.Ordinal);
        Assert.Contains("FlushMutationBatch(mutationBatch);", runtimeText, StringComparison.Ordinal);
        Assert.Contains("DomElement.ApplyMutationBatch(payload.ToJson());", runtimeText, StringComparison.Ordinal);
        Assert.Contains("batchedNodes=", runtimeText, StringComparison.Ordinal);
        Assert.Contains("batchedMutations=", runtimeText, StringComparison.Ordinal);
        Assert.Contains("interopCalls=", runtimeText, StringComparison.Ordinal);
        Assert.Contains("internal sealed class DomMutationBatch", batchText, StringComparison.Ordinal);
        Assert.Contains("[JSImport(\"applyMutationBatch\", Module)]", domApiText, StringComparison.Ordinal);
    }

    [Fact]
    public void MirageRuntime_TracksKeyboardEventSubscriptionInteropInRenderDiagnostics()
    {
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var text = File.ReadAllText(runtimePath);
        Assert.Contains(
            "subscriptionId = InvokeInterop(() => targetElement.On(eventDescriptor.DomEventName, \"key\", payload =>",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MirageRuntimeModule_CoalescesWindowResizeCallbacksToAnimationFrame()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Web",
            "wwwroot",
            "mirage-dom.js");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");

        var text = File.ReadAllText(runtimeModulePath);
        Assert.Contains("export function subscribeWindowResize(callback)", text, StringComparison.Ordinal);
        Assert.Contains("window.requestAnimationFrame(() =>", text, StringComparison.Ordinal);
        Assert.Contains("window.cancelAnimationFrame(frameId);", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MirageRuntimeModule_ReplaysBatchedNodeMutationsInRecordedOrder()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Web",
            "wwwroot",
            "mirage-dom.js");
        var batchPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "DomMutationBatch.cs");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");
        Assert.True(File.Exists(batchPath), $"Expected batch file not found: {batchPath}");

        var runtimeText = File.ReadAllText(runtimeModulePath);
        var batchText = File.ReadAllText(batchPath);
        Assert.Contains("function applyMutationOperation(element, operation)", runtimeText, StringComparison.Ordinal);
        Assert.Contains("if (Array.isArray(mutation.operations))", runtimeText, StringComparison.Ordinal);
        Assert.Contains("applyMutationOperation(element, operation);", runtimeText, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<DomMutationOperation> Operations", batchText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserRuntimeModuleSync_UsesProviderDeclaredModuleItems()
    {
        var targetsPath = Path.Combine(GetRepositoryRoot(), "Directory.Build.targets");
        Assert.True(File.Exists(targetsPath), $"Expected targets file not found: {targetsPath}");

        var text = File.ReadAllText(targetsPath);
        Assert.Contains("MirageCollectBrowserRuntimeModules", text, StringComparison.Ordinal);
        Assert.Contains("MirageBrowserRuntimeModule", text, StringComparison.Ordinal);
        Assert.Contains("MirageBrowserRuntimeModuleMissingSource", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MirageLeafletProviderModuleSourcePath", text, StringComparison.Ordinal);
        Assert.DoesNotContain("src/Mirage.Provider.LeafletMaps/wwwroot/mirage-leaflet-provider.js", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapRuntime_DoesNotUnconditionallyClearFailureStateAfterAdapterApply()
    {
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var text = File.ReadAllText(runtimePath);
        Assert.Contains("adapter.ApplyState(node.MapState);", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ClearMapFailure(node);", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapRuntime_GuardsSecondaryAdapterShowErrorFailures()
    {
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var text = File.ReadAllText(runtimePath);
        Assert.Contains("adapter.ShowError(\"adapter-apply-failed\", message);", text, StringComparison.Ordinal);
        Assert.Contains("catch (Exception showErrorException)", text, StringComparison.Ordinal);
        Assert.Contains("code=adapter-show-error-failed", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapHandler_DiagnosticLogging_UsesPayloadProviderIdentity()
    {
        var handlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageMapHandler.cs");
        Assert.True(File.Exists(handlerPath), $"Expected map handler file not found: {handlerPath}");

        var text = File.ReadAllText(handlerPath);
        Assert.Contains("TryGetString(diagnosticJson, \"provider\") ?? \"unknown\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("provider=leaflet", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapRuntime_ConvertsAsyncProviderInitExceptionsIntoDeterministicErrorState()
    {
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var text = File.ReadAllText(runtimePath);
        Assert.Contains("ConfigureMapNodeWhenProviderReadyAsync", text, StringComparison.Ordinal);
        Assert.Contains("catch (Exception error)", text, StringComparison.Ordinal);
        Assert.Contains("provider-init-exception", text, StringComparison.Ordinal);
        Assert.Contains("ShowMapFailure(currentNode, \"provider-init-exception\", message);", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapRuntime_RetriesProviderInitializationWhenCurrentStateIsNotReady()
    {
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var text = File.ReadAllText(runtimePath);
        Assert.Contains("if (!initialization.IsReady)", text, StringComparison.Ordinal);
        Assert.Contains("InvokeInterop(() => node.Element.Attr(\"data-mirage-map-status\", \"pending\"));", text, StringComparison.Ordinal);
        Assert.Contains("_ = ConfigureMapNodeWhenProviderReadyAsync(nodeId, node);", text, StringComparison.Ordinal);
        Assert.DoesNotContain("string.Equals(initialization.ErrorCode, \"not-initialized\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapRuntime_BatchingKeepsMapStateMutationOnImmediateApplyPath()
    {
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var text = File.ReadAllText(runtimePath);
        Assert.Contains("if (node.IsMapProperty(operation.PropertyName))", text, StringComparison.Ordinal);
        Assert.Contains("public bool IsMapProperty(string propertyName)", text, StringComparison.Ordinal);
        Assert.Contains("&& _mapProjection.IsSupportedProperty(propertyName);", text, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "if (node.ApplyMapProperty(operation.PropertyName, operation.Value))\n        {\n            return false;\n        }",
            text,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "if (node.RemoveMapProperty(operation.PropertyName))\n        {\n            return false;\n        }",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MapRuntime_RetriesRealizedMapNodeAfterNonReadyProviderResults()
    {
        var runtimePath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Web", "Runtime", "MirageRuntime.cs");
        Assert.True(File.Exists(runtimePath), $"Expected runtime file not found: {runtimePath}");

        var text = File.ReadAllText(runtimePath);
        Assert.Contains("ShouldRetryMapProviderInitializationFailure", text, StringComparison.Ordinal);
        Assert.Contains("\"js-timeout\" => true", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"js-load-failed\" => true", text, StringComparison.Ordinal);
        Assert.Contains("TryScheduleMapProviderInitializationRetry(", text, StringComparison.Ordinal);
        Assert.Contains("MapProviderInitializationRetryLimit", text, StringComparison.Ordinal);
        Assert.Contains("ConfigureMapNodeWhenProviderReadyAfterDelayAsync", text, StringComparison.Ordinal);
        Assert.Contains("await ConfigureMapNodeWhenProviderReadyAsync(nodeId, currentNode);", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletHostingExtension_ReplacesMapProviderRegistration()
    {
        var hostingExtensionsPath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "MirageLeafletMapHostingExtensions.cs");
        Assert.True(File.Exists(hostingExtensionsPath), $"Expected hosting extension file not found: {hostingExtensionsPath}");

        var text = File.ReadAllText(hostingExtensionsPath);
        Assert.Contains(
            "Services.Replace(ServiceDescriptor.Singleton<IMirageMapProvider, LeafletMapProvider>())",
            text,
            StringComparison.Ordinal);
        Assert.DoesNotContain("TryAddSingleton<IMirageMapProvider", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletMapProvider_AllowsRetryAfterFailedInitializationAttempts()
    {
        var providerPath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "LeafletMapProvider.cs");
        Assert.True(File.Exists(providerPath), $"Expected provider file not found: {providerPath}");

        var text = File.ReadAllText(providerPath);
        Assert.Contains("ShouldStartNewInitializationTask", text, StringComparison.Ordinal);
        Assert.Contains("_initializationTask = EnsureInitializedCoreAsync();", text, StringComparison.Ordinal);
        Assert.Contains("return !task.Result.IsReady;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletMapInterop_ClearsFailedModuleImportTaskForRetries()
    {
        var interopPath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "LeafletMapInterop.cs");
        Assert.True(File.Exists(interopPath), $"Expected interop file not found: {interopPath}");

        var text = File.ReadAllText(interopPath);
        Assert.Contains("existing.IsFaulted", text, StringComparison.Ordinal);
        Assert.Contains("existing.IsCanceled", text, StringComparison.Ordinal);
        Assert.Contains("Interlocked.CompareExchange(ref s_moduleImportTask, null, existing)", text, StringComparison.Ordinal);
        Assert.Contains("Interlocked.CompareExchange(ref s_moduleImportTask, null, createdTask);", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletRuntimeModule_ClearsReadinessPromiseAfterFailedInitialization()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "wwwroot",
            "mirage-leaflet-provider.js");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");

        var text = File.ReadAllText(runtimeModulePath);
        Assert.Contains("if (!result.ok)", text, StringComparison.Ordinal);
        Assert.Contains("leafletReadinessPromise = null;", text, StringComparison.Ordinal);
        Assert.Contains(".catch((error) =>", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletRuntimeModule_WaitsForExistingStylesheetLoadBeforeReportingReady()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "wwwroot",
            "mirage-leaflet-provider.js");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");

        var text = File.ReadAllText(runtimeModulePath);
        Assert.Contains("const existingStatus = existing.getAttribute(\"data-mirage-leaflet-css-status\");", text, StringComparison.Ordinal);
        Assert.Contains("existing.addEventListener(\"load\", onLoad);", text, StringComparison.Ordinal);
        Assert.Contains("existing.addEventListener(\"error\", onError);", text, StringComparison.Ordinal);
        Assert.Contains("Timed out while waiting for Leaflet CSS", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletRuntimeModule_DetachesOnlyMirageMarkerClickHandlersWhenRewiringPins()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "wwwroot",
            "mirage-leaflet-provider.js");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");

        var text = File.ReadAllText(runtimeModulePath);
        Assert.Contains("const existingPinClickHandler = marker.__miragePinClickHandler;", text, StringComparison.Ordinal);
        Assert.Contains("marker.off(\"click\", existingPinClickHandler);", text, StringComparison.Ordinal);
        Assert.DoesNotContain("marker.off(\"click\");", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletRuntimeModule_DiagnosticPayloadIncludesProviderIdentity()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "wwwroot",
            "mirage-leaflet-provider.js");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");

        var text = File.ReadAllText(runtimeModulePath);
        Assert.Contains("emitMirageEvent(host, \"mirage-map-diagnostic\", {", text, StringComparison.Ordinal);
        Assert.Contains("provider: \"leaflet\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletRuntimeModule_ObservesHostResizesAndInvalidatesMapSize()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "wwwroot",
            "mirage-leaflet-provider.js");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");

        var text = File.ReadAllText(runtimeModulePath);
        Assert.Contains("function subscribeHostResize(entry)", text, StringComparison.Ordinal);
        Assert.Contains("new ResizeObserver(() =>", text, StringComparison.Ordinal);
        Assert.Contains("scheduleInvalidateSize(entry);", text, StringComparison.Ordinal);
        Assert.Contains("function unsubscribeHostResize(entry)", text, StringComparison.Ordinal);
        Assert.Contains("entry.resizeObserver.disconnect();", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletRuntimeModule_ComputesLongitudeSpanAcrossAntimeridian()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "wwwroot",
            "mirage-leaflet-provider.js");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");

        var text = File.ReadAllText(runtimeModulePath);
        Assert.Contains("function getLongitudeSpanFromBounds", text, StringComparison.Ordinal);
        Assert.Contains("let span = northEast - southWest;", text, StringComparison.Ordinal);
        Assert.Contains("while (span < 0)", text, StringComparison.Ordinal);
        Assert.Contains("span += 360;", text, StringComparison.Ordinal);
        Assert.Contains("const longitudeSpan = getLongitudeSpanFromBounds(southWest.lng, northEast.lng);", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LeafletRuntimeModule_ClearsFailedGeolocationWatchIdsToAllowRetry()
    {
        var runtimeModulePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "Mirage.Provider.LeafletMaps",
            "wwwroot",
            "mirage-leaflet-provider.js");
        Assert.True(File.Exists(runtimeModulePath), $"Expected runtime module file not found: {runtimeModulePath}");

        var text = File.ReadAllText(runtimeModulePath);
        Assert.Contains("watchId = navigator.geolocation.watchPosition(", text, StringComparison.Ordinal);
        Assert.Contains("if (entry.userLocationWatchId === watchId)", text, StringComparison.Ordinal);
        Assert.Contains("navigator.geolocation.clearWatch(watchId);", text, StringComparison.Ordinal);
        Assert.Contains("entry.userLocationWatchId = null;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ScrollViewHandler_MapsExplicitRequestScrollToThroughRuntimeBridge()
    {
        var handlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageScrollViewHandler.cs");
        Assert.True(File.Exists(handlerPath), $"Expected handler file not found: {handlerPath}");

        var text = File.ReadAllText(handlerPath);
        Assert.Contains("nameof(IScrollView.RequestScrollTo)", text, StringComparison.Ordinal);
        Assert.Contains("MapRequestScrollTo", text, StringComparison.Ordinal);
        Assert.Contains("ScrollToRequest", text, StringComparison.Ordinal);
        Assert.Contains("ShouldDeferScrollRequest", text, StringComparison.Ordinal);
        Assert.Contains("TryApplyPendingScrollToRequest", text, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeScrollPosition", text, StringComparison.Ordinal);
        Assert.Contains("ScrollFinished", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ScrollViewHandler_DoesNotForceFillSizeStyles()
    {
        var handlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageScrollViewHandler.cs");
        Assert.True(File.Exists(handlerPath), $"Expected handler file not found: {handlerPath}");

        var text = File.ReadAllText(handlerPath);
        Assert.DoesNotContain("SetStyle(\"width\", \"100%\")", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SetStyle(\"height\", \"100%\")", text, StringComparison.Ordinal);
        Assert.Contains("double.PositiveInfinity", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GridContract_UsesCssGridProjectionSemantics()
    {
        var layoutHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageLayoutHandler.cs");
        Assert.True(File.Exists(layoutHandlerPath), $"Expected handler file not found: {layoutHandlerPath}");

        var text = File.ReadAllText(layoutHandlerPath);
        Assert.Contains("ComponentKind.Grid", text, StringComparison.Ordinal);
        Assert.Contains("gridTemplateColumns", text, StringComparison.Ordinal);
        Assert.Contains("gridTemplateRows", text, StringComparison.Ordinal);
        Assert.Contains("rowGap", text, StringComparison.Ordinal);
        Assert.Contains("columnGap", text, StringComparison.Ordinal);
        Assert.Contains("\"justifyContent\", \"start\"", text, StringComparison.Ordinal);
        Assert.Contains("\"alignContent\", \"start\"", text, StringComparison.Ordinal);
        Assert.Contains("TryConvertGridTrack", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LayoutHandler_MapsClipsToBoundsThroughSharedProjection()
    {
        var layoutHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageLayoutHandler.cs");
        var projectionPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageCommonViewProjection.cs");
        Assert.True(File.Exists(layoutHandlerPath), $"Expected handler file not found: {layoutHandlerPath}");
        Assert.True(File.Exists(projectionPath), $"Expected projection file not found: {projectionPath}");

        var layoutText = File.ReadAllText(layoutHandlerPath);
        var projectionText = File.ReadAllText(projectionPath);
        Assert.Contains("nameof(MauiLayout.ClipsToBounds)", layoutText, StringComparison.Ordinal);
        Assert.Contains("MapClipsToBounds", layoutText, StringComparison.Ordinal);
        Assert.Contains("MapLayoutClipsToBounds", projectionText, StringComparison.Ordinal);
        Assert.Contains("\"overflow\"", projectionText, StringComparison.Ordinal);
    }

    [Fact]
    public void CommonViewMapper_RoutesVisualConcernsThroughSharedProjection()
    {
        var mapperPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageViewMapper.cs");
        var projectionPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageCommonViewProjection.cs");
        Assert.True(File.Exists(mapperPath), $"Expected mapper file not found: {mapperPath}");
        Assert.True(File.Exists(projectionPath), $"Expected projection file not found: {projectionPath}");

        var mapperText = File.ReadAllText(mapperPath);
        var projectionText = File.ReadAllText(projectionPath);

        Assert.Contains("nameof(IView.Background)", mapperText, StringComparison.Ordinal);
        Assert.Contains("nameof(IView.Opacity)", mapperText, StringComparison.Ordinal);
        Assert.Contains("nameof(IView.Clip)", mapperText, StringComparison.Ordinal);
        Assert.Contains("nameof(ITransform.ScaleX)", mapperText, StringComparison.Ordinal);
        Assert.Contains("nameof(ITransform.ScaleY)", mapperText, StringComparison.Ordinal);
        Assert.Contains("nameof(ITransform.TranslationX)", mapperText, StringComparison.Ordinal);
        Assert.Contains("nameof(ITextStyle.TextColor)", mapperText, StringComparison.Ordinal);
        Assert.Contains("nameof(ITextStyle.Font)", mapperText, StringComparison.Ordinal);
        Assert.Contains("nameof(ITextStyle.CharacterSpacing)", mapperText, StringComparison.Ordinal);
        Assert.Contains("MapSizeRequests", mapperText, StringComparison.Ordinal);
        Assert.Contains("MapLayoutAlignment", mapperText, StringComparison.Ordinal);
        Assert.Contains("MapTransform", mapperText, StringComparison.Ordinal);

        Assert.Contains("MapBackground", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapOpacity", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapClip", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapTransform", projectionText, StringComparison.Ordinal);
        Assert.Contains("SetLayoutTransformFragment", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapTextColor", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapFont", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapCharacterSpacing", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapSizeRequests", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapLayoutAlignment", projectionText, StringComparison.Ordinal);
        Assert.Contains("ApplyRequestedLength(platformHandler, \"width\"", projectionText, StringComparison.Ordinal);
        Assert.Contains("ApplyRequestedLength(platformHandler, \"height\"", projectionText, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Mirage.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
