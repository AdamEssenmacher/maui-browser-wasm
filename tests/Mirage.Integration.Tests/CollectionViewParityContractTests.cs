using Mirage.Maui.Handlers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class CollectionViewParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void CollectionViewParityContract_MapsToMirageHandlerSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "collectionview-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected CollectionView parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<CollectionViewParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Handlers);

        var itemsContract = contract.Handlers!.Single(entry => string.Equals(entry.Name, "ItemsViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageItemsViewHandler<CollectionView>.ParityPropertyKeys, itemsContract.PropertyMapperKeys);
        AssertSetEquals(MirageItemsViewHandler<CollectionView>.ParityCommandKeys, itemsContract.CommandMapperKeys);
        AssertSetEquals(MirageItemsViewHandler<CollectionView>.ParityExtensionPropertyKeys, itemsContract.MirageExtensionPropertyMapperKeys);

        var structuredContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "StructuredItemsViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageStructuredItemsViewHandler<CollectionView>.ParityPropertyKeys, structuredContract.PropertyMapperKeys);
        AssertSetEquals(MirageStructuredItemsViewHandler<CollectionView>.ParityCommandKeys, structuredContract.CommandMapperKeys);

        var selectableContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "SelectableItemsViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageSelectableItemsViewHandler<CollectionView>.ParityPropertyKeys, selectableContract.PropertyMapperKeys);
        AssertSetEquals(MirageSelectableItemsViewHandler<CollectionView>.ParityCommandKeys, selectableContract.CommandMapperKeys);

        var groupableContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "GroupableItemsViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageGroupableItemsViewHandler<CollectionView>.ParityPropertyKeys, groupableContract.PropertyMapperKeys);
        AssertSetEquals(MirageGroupableItemsViewHandler<CollectionView>.ParityCommandKeys, groupableContract.CommandMapperKeys);

        var reorderableContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "ReorderableItemsViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageReorderableItemsViewHandler<CollectionView>.ParityPropertyKeys, reorderableContract.PropertyMapperKeys);
        AssertSetEquals(MirageReorderableItemsViewHandler<CollectionView>.ParityCommandKeys, reorderableContract.CommandMapperKeys);

        var collectionContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "CollectionViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageCollectionViewHandler.ParityPropertyKeys, collectionContract.PropertyMapperKeys);
        AssertSetEquals(MirageCollectionViewHandler.ParityCommandKeys, collectionContract.CommandMapperKeys);
        AssertSetEquals(MirageCollectionViewHandler.ParityExtensionPropertyKeys, collectionContract.MirageExtensionPropertyMapperKeys);
    }

    [Fact]
    public void CollectionViewParityContract_DivergencesCoverMirageExtensionKeysAndMetadata()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "collectionview-parity-contract.json");
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "collectionview-parity-divergences.json");
        Assert.True(File.Exists(contractPath), $"Expected CollectionView parity contract file not found: {contractPath}");
        Assert.True(File.Exists(registryPath), $"Expected CollectionView divergence registry file not found: {registryPath}");

        var contract = JsonSerializer.Deserialize<CollectionViewParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        var registry = JsonSerializer.Deserialize<CollectionViewParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.NotNull(registry);
        Assert.NotNull(registry!.Entries);
        Assert.NotEmpty(registry.Entries!);

        var collectionContract = contract!.Handlers!.Single(entry => string.Equals(entry.Name, "CollectionViewHandler", StringComparison.Ordinal));
        var requiredExtensionKeys = new HashSet<string>(collectionContract.MirageExtensionPropertyMapperKeys!, StringComparer.Ordinal);

        var testMethodNames = DiscoverTestMethodNames(repositoryRoot);

        var coveredExtensionKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in registry.Entries!)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Id));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
            Assert.False(string.IsNullOrWhiteSpace(entry.UpstreamReference));
            Assert.False(string.IsNullOrWhiteSpace(entry.TestReference));
            Assert.Contains(entry.TestReference!, testMethodNames);

            if (entry.ExtensionKeys is null)
            {
                continue;
            }

            foreach (var extensionKey in entry.ExtensionKeys)
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

    private sealed class CollectionViewParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<CollectionViewParityHandlerContract>? Handlers { get; set; }
    }

    private sealed class CollectionViewParityHandlerContract
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? PropertyMapperKeys { get; set; }

        public List<string>? CommandMapperKeys { get; set; }

        public List<string>? MirageExtensionPropertyMapperKeys { get; set; }
    }

    private sealed class CollectionViewParityDivergenceRegistry
    {
        public List<CollectionViewParityDivergenceEntry>? Entries { get; set; }
    }

    private sealed class CollectionViewParityDivergenceEntry
    {
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public string? UpstreamReference { get; set; }

        public string? TestReference { get; set; }

        public List<string>? ExtensionKeys { get; set; }
    }
}
