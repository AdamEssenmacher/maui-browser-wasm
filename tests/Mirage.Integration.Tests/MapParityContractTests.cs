using Mirage.Maui.Handlers;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MapParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void MapParityContract_CoversMirageHandlerSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "map-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected map parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<MapParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Handlers);

        var mapHandlerContract = contract.Handlers!.Single(entry => string.Equals(entry.Name, "MapHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageMapHandler.ParityPropertyKeys, mapHandlerContract.PropertyMapperKeys);
        AssertSetEquals(MirageMapHandler.ParityCommandKeys, mapHandlerContract.CommandMapperKeys);
        AssertSetEquals(MirageMapHandler.ParityRuntimeEvents, mapHandlerContract.RuntimeEvents);
        AssertSetEquals(
            new[] { "Pins", "Elements" },
            mapHandlerContract.CollectionCallbacks);

        var mapPinHandlerContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "MapPinHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageMapPinHandler.ParityPropertyKeys, mapPinHandlerContract.PropertyMapperKeys);

        var mapElementHandlerContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "MapElementHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageMapElementHandler.ParityPropertyKeys, mapElementHandlerContract.PropertyMapperKeys);
        AssertSetEquals(new[] { "Circle", "Polyline", "Polygon" }, mapElementHandlerContract.MapElementTypes);
    }

    [Fact]
    public void MapParityDivergenceRegistry_IsWellFormed_AndBackedByTests()
    {
        var repositoryRoot = FindRepositoryRoot();
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "map-parity-divergences.json");
        Assert.True(File.Exists(registryPath), $"Expected map divergence registry file not found: {registryPath}");

        var registry = JsonSerializer.Deserialize<MapParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);
        Assert.NotNull(registry);
        Assert.NotNull(registry!.Entries);
        Assert.NotEmpty(registry.Entries!);

        var testSourceText = string.Join(
            "\n",
            Directory.GetFiles(Path.Combine(repositoryRoot, "tests"), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        foreach (var entry in registry.Entries!)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Id));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
            Assert.False(string.IsNullOrWhiteSpace(entry.UpstreamReference));
            Assert.False(string.IsNullOrWhiteSpace(entry.TestReference));
            Assert.Contains(entry.TestReference!, testSourceText, StringComparison.Ordinal);
        }
    }

    private static void AssertSetEquals(IReadOnlyCollection<string> expected, IReadOnlyCollection<string>? actual)
    {
        Assert.NotNull(actual);
        var expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);
        var actualSet = new HashSet<string>(actual!, StringComparer.Ordinal);
        Assert.True(
            expectedSet.SetEquals(actualSet),
            $"Expected set [{string.Join(", ", expectedSet)}] but found [{string.Join(", ", actualSet)}].");
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

    private sealed class MapParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<MapParityHandlerContract>? Handlers { get; set; }
    }

    private sealed class MapParityHandlerContract
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? PropertyMapperKeys { get; set; }

        public List<string>? CommandMapperKeys { get; set; }

        public List<string>? CollectionCallbacks { get; set; }

        public List<string>? RuntimeEvents { get; set; }

        public List<string>? MapElementTypes { get; set; }
    }

    private sealed class MapParityDivergenceRegistry
    {
        public List<MapParityDivergenceEntry>? Entries { get; set; }
    }

    private sealed class MapParityDivergenceEntry
    {
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public string? UpstreamReference { get; set; }

        public string? TestReference { get; set; }
    }
}
