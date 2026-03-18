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
    public void BuiltInHandlers_DoNotUseWebNodeFactory()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");

        var builtInHandlerFiles = new[]
        {
            "MirageButtonHandler.cs",
            "MirageEntryHandler.cs",
            "MirageImageHandler.cs",
            "MirageBorderHandler.cs",
            "MirageMapHandler.cs",
            "MirageLabelHandler.cs",
            "MirageBoxViewHandler.cs",
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
    public void PaddingMapping_RemainsContainerOnly()
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
        Assert.DoesNotContain("MapPadding", labelText, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPadding", entryText, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPadding", buttonText, StringComparison.Ordinal);
    }

    [Fact]
    public void LabelHandler_StaysWithinBasicPhaseOneScope()
    {
        var labelHandlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageLabelHandler.cs");
        Assert.True(File.Exists(labelHandlerPath), $"Expected handler file not found: {labelHandlerPath}");

        var text = File.ReadAllText(labelHandlerPath);

        Assert.Contains("nameof(ILabel.Text)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(ILabel.Font)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("nameof(ILabel.FormattedText)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("nameof(ILabel.LineBreakMode)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("nameof(ILabel.MaxLines)", text, StringComparison.Ordinal);
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
        Assert.Contains("ComponentKind.Map", mapHandlerText, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWeb(", mapHandlerText, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserPlatform_RegistersStackLayoutImageAndBorderHandlers()
    {
        var hostingPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Hosting", "MirageHostingExtensions.cs");
        Assert.True(File.Exists(hostingPath), $"Expected hosting extensions file not found: {hostingPath}");

        var text = File.ReadAllText(hostingPath);
        Assert.Contains("typeof(StackLayout)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageLayoutHandler)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(Image)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageImageHandler)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(Border)", text, StringComparison.Ordinal);
        Assert.Contains("typeof(MirageBorderHandler)", text, StringComparison.Ordinal);
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
    public void ScrollViewHandler_MapsExplicitRequestScrollToThroughRuntimeBridge()
    {
        var handlerPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers", "MirageScrollViewHandler.cs");
        Assert.True(File.Exists(handlerPath), $"Expected handler file not found: {handlerPath}");

        var text = File.ReadAllText(handlerPath);
        Assert.Contains("nameof(IScrollView.RequestScrollTo)", text, StringComparison.Ordinal);
        Assert.Contains("MapRequestScrollTo", text, StringComparison.Ordinal);
        Assert.Contains("TryResolveScrollRequest", text, StringComparison.Ordinal);
        Assert.Contains("TrySetNodeScrollPosition", text, StringComparison.Ordinal);
        Assert.Contains("SendScrollFinished", text, StringComparison.Ordinal);
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
