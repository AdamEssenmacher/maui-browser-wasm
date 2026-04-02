using Mirage.Maui.Handlers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class SwipeViewParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void SwipeViewParityContract_MapsToMirageHandlerSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "swipeview-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected SwipeView parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<SwipeViewParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Handlers);

        var handlerContract = contract.Handlers!.Single(entry => string.Equals(entry.Name, "SwipeViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageSwipeViewHandler.ParityPropertyKeys, handlerContract.PropertyMapperKeys);
        AssertSetEquals(MirageSwipeViewHandler.ParityCommandKeys, handlerContract.CommandMapperKeys);
        AssertSetEquals(MirageSwipeViewHandler.ParityExtensionPropertyKeys, handlerContract.MirageExtensionPropertyMapperKeys);
    }

    [Fact]
    public void SwipeViewParityContract_DivergencesAreWellFormedAndTestBacked()
    {
        var repositoryRoot = FindRepositoryRoot();
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "swipeview-parity-divergences.json");
        Assert.True(File.Exists(registryPath), $"Expected SwipeView divergence registry file not found: {registryPath}");

        var registry = JsonSerializer.Deserialize<SwipeViewParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);
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
        var expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);
        var actualSet = new HashSet<string>(actual!, StringComparer.Ordinal);
        Assert.True(expectedSet.SetEquals(actualSet), $"Expected set [{string.Join(", ", expectedSet)}] but found [{string.Join(", ", actualSet)}].");
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

    private sealed class SwipeViewParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<SwipeViewParityHandlerContract>? Handlers { get; set; }
    }

    private sealed class SwipeViewParityHandlerContract
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? PropertyMapperKeys { get; set; }

        public List<string>? CommandMapperKeys { get; set; }

        public List<string>? MirageExtensionPropertyMapperKeys { get; set; }
    }

    private sealed class SwipeViewParityDivergenceRegistry
    {
        public List<SwipeViewParityDivergenceEntry>? Entries { get; set; }
    }

    private sealed class SwipeViewParityDivergenceEntry
    {
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public string? UpstreamReference { get; set; }

        public string? TestReference { get; set; }
    }
}
