using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class GestureRecognizersParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void GestureRecognizersParityContract_IsWellFormed()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "gesture-recognizers-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected gesture recognizer parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<GestureRecognizersParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Substrates);

        var substrate = Assert.Single(contract.Substrates!, entry => string.Equals(entry.Name, "View.GestureRecognizers", StringComparison.Ordinal));
        Assert.Equal("MirageGestureRecognizerManager", substrate.MirageType);
        AssertSetEquals(
            ["TapGestureRecognizer", "PanGestureRecognizer", "PinchGestureRecognizer", "PointerGestureRecognizer", "SwipeGestureRecognizer"],
            substrate.Recognizers);
        AssertSetEquals(
            ["click", "contextmenu", "pointerdown", "pointermove", "pointerup", "pointercancel", "pointerenter", "pointerleave"],
            substrate.DomEvents);
    }

    [Fact]
    public void GestureRecognizersParityContract_DivergencesAreWellFormedAndTestBacked()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "gesture-recognizers-parity-contract.json");
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "gesture-recognizers-parity-divergences.json");
        Assert.True(File.Exists(contractPath), $"Expected gesture recognizer parity contract file not found: {contractPath}");
        Assert.True(File.Exists(registryPath), $"Expected gesture recognizer divergence registry file not found: {registryPath}");

        var registry = JsonSerializer.Deserialize<GestureRecognizersParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);
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

    [Fact]
    public void GestureRecognizersParityContract_DocumentsDeferredSpanAndWheelParity()
    {
        var repositoryRoot = FindRepositoryRoot();
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "gesture-recognizers-parity-divergences.json");
        var registry = JsonSerializer.Deserialize<GestureRecognizersParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);
        Assert.NotNull(registry);
        Assert.NotNull(registry!.Entries);

        var ids = registry.Entries!
            .Select(static entry => entry.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("gesture-recognizers-span-hit-regions-deferred", ids);
        Assert.Contains("gesture-recognizers-wheel-trackpad-pinch-deferred", ids);
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

    private sealed class GestureRecognizersParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<GestureRecognizersParitySubstrate>? Substrates { get; set; }
    }

    private sealed class GestureRecognizersParitySubstrate
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? Recognizers { get; set; }

        public List<string>? DomEvents { get; set; }
    }

    private sealed class GestureRecognizersParityDivergenceRegistry
    {
        public List<GestureRecognizersParityDivergenceEntry>? Entries { get; set; }
    }

    private sealed class GestureRecognizersParityDivergenceEntry
    {
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public string? UpstreamReference { get; set; }

        public string? TestReference { get; set; }
    }
}
