using Mirage.Maui.Handlers;
using System.Text.Json;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class ScrollViewParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ScrollViewParityContract_MapsToMirageScrollViewHandlerSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "scrollview-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected ScrollView parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<ScrollViewParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Handlers);

        var scrollViewHandlerContract = contract.Handlers!.Single(entry => string.Equals(entry.Name, "ScrollViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageScrollViewHandler.ParityPropertyKeys, scrollViewHandlerContract.PropertyMapperKeys);
        AssertSetEquals(MirageScrollViewHandler.ParityCommandKeys, scrollViewHandlerContract.CommandMapperKeys);
        AssertSetEquals(MirageScrollViewHandler.ParityExtensionPropertyKeys, scrollViewHandlerContract.MirageExtensionPropertyMapperKeys);
    }

    [Fact]
    public void ScrollViewParityContract_DivergencesCoverMirageExtensionKeys()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "scrollview-parity-contract.json");
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "scrollview-parity-divergences.json");

        Assert.True(File.Exists(contractPath), $"Expected ScrollView parity contract file not found: {contractPath}");
        Assert.True(File.Exists(registryPath), $"Expected ScrollView divergence registry file not found: {registryPath}");

        var contract = JsonSerializer.Deserialize<ScrollViewParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        var registry = JsonSerializer.Deserialize<ScrollViewParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);

        Assert.NotNull(contract);
        Assert.NotNull(registry);
        Assert.NotNull(registry!.Entries);
        Assert.NotEmpty(registry.Entries!);

        var scrollViewHandlerContract = contract!.Handlers!.Single(entry => string.Equals(entry.Name, "ScrollViewHandler", StringComparison.Ordinal));
        var requiredExtensionKeys = new HashSet<string>(scrollViewHandlerContract.MirageExtensionPropertyMapperKeys!, StringComparer.Ordinal);

        var testSourceText = string.Join(
            "\n",
            Directory.GetFiles(Path.Combine(repositoryRoot, "tests"), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        var coveredExtensionKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in registry.Entries!)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Id));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
            Assert.False(string.IsNullOrWhiteSpace(entry.UpstreamReference));
            Assert.False(string.IsNullOrWhiteSpace(entry.TestReference));
            Assert.NotNull(entry.ExtensionKeys);
            Assert.NotEmpty(entry.ExtensionKeys!);
            Assert.Contains(entry.TestReference!, testSourceText, StringComparison.Ordinal);

            foreach (var extensionKey in entry.ExtensionKeys!)
            {
                if (!string.IsNullOrWhiteSpace(extensionKey))
                {
                    coveredExtensionKeys.Add(extensionKey);
                }
            }
        }

        Assert.True(
            requiredExtensionKeys.SetEquals(coveredExtensionKeys),
            $"Expected divergence coverage for [{string.Join(", ", requiredExtensionKeys)}] but found [{string.Join(", ", coveredExtensionKeys)}].");
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

    private sealed class ScrollViewParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<ScrollViewParityHandlerContract>? Handlers { get; set; }
    }

    private sealed class ScrollViewParityHandlerContract
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? PropertyMapperKeys { get; set; }

        public List<string>? CommandMapperKeys { get; set; }

        public List<string>? MirageExtensionPropertyMapperKeys { get; set; }
    }

    private sealed class ScrollViewParityDivergenceRegistry
    {
        public List<ScrollViewParityDivergenceEntry>? Entries { get; set; }
    }

    private sealed class ScrollViewParityDivergenceEntry
    {
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public string? UpstreamReference { get; set; }

        public string? TestReference { get; set; }

        public List<string>? ExtensionKeys { get; set; }
    }
}
