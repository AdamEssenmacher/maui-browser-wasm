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
            "fast-button",
            "fast-text-field",
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
    public void BuiltInHandlers_DoNotUseGenericElementFactory()
    {
        var handlersPath = Path.Combine(GetRepositoryRoot(), "src", "Mirage.Maui", "Handlers");
        Assert.True(Directory.Exists(handlersPath), $"Handlers path was not found: {handlersPath}");

        var builtInHandlerFiles = new[]
        {
            "MirageButtonHandler.cs",
            "MirageEntryHandler.cs",
            "MirageLabelHandler.cs",
            "MirageContentViewHandler.cs",
            "MirageContentPageHandler.cs",
            "MirageLayoutHandler.cs",
            "MirageWindowHandler.cs"
        };

        foreach (var file in builtInHandlerFiles)
        {
            var fullPath = Path.Combine(handlersPath, file);
            Assert.True(File.Exists(fullPath), $"Expected handler file not found: {fullPath}");
            var text = File.ReadAllText(fullPath);
            Assert.DoesNotContain("CreateElement(", text, StringComparison.Ordinal);
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
        Assert.DoesNotContain("nameof(ILabel.TextColor)", text, StringComparison.Ordinal);
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
        Assert.Contains("TryConvertGridTrack", text, StringComparison.Ordinal);
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
        Assert.Contains("MapSizeRequests", mapperText, StringComparison.Ordinal);
        Assert.Contains("MapLayoutAlignment", mapperText, StringComparison.Ordinal);

        Assert.Contains("MapBackground", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapOpacity", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapSizeRequests", projectionText, StringComparison.Ordinal);
        Assert.Contains("MapLayoutAlignment", projectionText, StringComparison.Ordinal);
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
