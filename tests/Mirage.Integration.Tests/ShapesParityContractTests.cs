using Mirage.Maui.Handlers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class ShapesParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ShapesParityContract_MapsToMirageHandlerSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "shapes-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected shapes parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<ShapesParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Handlers);

        var shapeViewContract = contract.Handlers!.Single(entry => string.Equals(entry.Name, "ShapeViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageShapeViewHandler.ParityPropertyKeys, shapeViewContract.PropertyMapperKeys);
        AssertSetEquals(MirageShapeViewHandler.ParityCommandKeys, shapeViewContract.CommandMapperKeys);

        var lineContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "LineHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageLineHandler.ParityPropertyKeys, lineContract.PropertyMapperKeys);
        AssertSetEquals(MirageShapeViewHandler.ParityCommandKeys, lineContract.CommandMapperKeys);

        var pathContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "PathHandler", StringComparison.Ordinal));
        AssertSetEquals(MiragePathHandler.ParityPropertyKeys, pathContract.PropertyMapperKeys);
        AssertSetEquals(MirageShapeViewHandler.ParityCommandKeys, pathContract.CommandMapperKeys);

        var polygonContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "PolygonHandler", StringComparison.Ordinal));
        AssertSetEquals(MiragePolygonHandler.ParityPropertyKeys, polygonContract.PropertyMapperKeys);
        AssertSetEquals(MirageShapeViewHandler.ParityCommandKeys, polygonContract.CommandMapperKeys);

        var polylineContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "PolylineHandler", StringComparison.Ordinal));
        AssertSetEquals(MiragePolylineHandler.ParityPropertyKeys, polylineContract.PropertyMapperKeys);
        AssertSetEquals(MirageShapeViewHandler.ParityCommandKeys, polylineContract.CommandMapperKeys);

        var rectangleContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "RectangleHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageRectangleHandler.ParityPropertyKeys, rectangleContract.PropertyMapperKeys);
        AssertSetEquals(MirageShapeViewHandler.ParityCommandKeys, rectangleContract.CommandMapperKeys);

        var roundRectangleContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "RoundRectangleHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageRoundRectangleHandler.ParityPropertyKeys, roundRectangleContract.PropertyMapperKeys);
        AssertSetEquals(MirageShapeViewHandler.ParityCommandKeys, roundRectangleContract.CommandMapperKeys);
    }

    [Fact]
    public void ShapesParityContract_DivergencesAreCompleteAndTestBacked()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "shapes-parity-contract.json");
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "shapes-parity-divergences.json");
        Assert.True(File.Exists(contractPath), $"Expected shapes parity contract file not found: {contractPath}");
        Assert.True(File.Exists(registryPath), $"Expected shapes divergence registry file not found: {registryPath}");

        var contract = JsonSerializer.Deserialize<ShapesParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        var registry = JsonSerializer.Deserialize<ShapesParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.NotNull(registry);
        Assert.NotNull(registry!.Entries);
        Assert.NotEmpty(registry.Entries!);

        var testMethodNames = DiscoverTestMethodNames(repositoryRoot);

        foreach (var entry in registry.Entries!)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Id));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
            Assert.False(string.IsNullOrWhiteSpace(entry.UpstreamReference));
            Assert.False(string.IsNullOrWhiteSpace(entry.TestReference));
            Assert.Contains(entry.TestReference!, testMethodNames);
        }
    }

    private static HashSet<string> DiscoverTestMethodNames(string repositoryRoot)
    {
        var testFiles = Directory.GetFiles(Path.Combine(repositoryRoot, "tests"), "*.cs", SearchOption.AllDirectories);
        var methodNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in testFiles)
        {
            var text = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(
                         text,
                         @"\[(Fact|Theory)(?:\s*\(.*?\))?\]\s*public\s+(?:async\s+)?(?:void|Task)\s+([A-Za-z0-9_]+)\s*\(",
                         RegexOptions.Singleline))
            {
                if (match.Groups.Count > 2)
                {
                    methodNames.Add(match.Groups[2].Value);
                }
            }
        }

        return methodNames;
    }

    private static void AssertSetEquals(IReadOnlyCollection<string> expected, IReadOnlyCollection<string>? actual)
    {
        Assert.NotNull(actual);
        AssertNoDuplicates(expected, nameof(expected));
        AssertNoDuplicates(actual!, nameof(actual));

        var expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);
        var actualSet = new HashSet<string>(actual!, StringComparer.Ordinal);
        Assert.True(
            expectedSet.SetEquals(actualSet),
            $"Expected set [{string.Join(", ", expectedSet)}] but found [{string.Join(", ", actualSet)}].");
    }

    private static void AssertNoDuplicates(IEnumerable<string> values, string label)
    {
        var duplicateValues = values
            .GroupBy(static value => value, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();
        Assert.True(
            duplicateValues.Length == 0,
            $"{label} must not contain duplicate keys. Duplicates: [{string.Join(", ", duplicateValues)}].");
    }

    private static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException("Unable to locate repository root containing Mirage.sln.");
    }

    private sealed class ShapesParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<ShapesParityHandlerContract>? Handlers { get; set; }
    }

    private sealed class ShapesParityHandlerContract
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? PropertyMapperKeys { get; set; }

        public List<string>? CommandMapperKeys { get; set; }
    }

    private sealed class ShapesParityDivergenceRegistry
    {
        public List<ShapesParityDivergenceEntry>? Entries { get; set; }
    }

    private sealed class ShapesParityDivergenceEntry
    {
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public string? UpstreamReference { get; set; }

        public string? TestReference { get; set; }
    }
}
