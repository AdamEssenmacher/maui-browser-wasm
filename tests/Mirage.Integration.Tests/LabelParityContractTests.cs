using Mirage.Maui.Handlers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class LabelParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void LabelParityContract_MapsToMirageHandlerSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "label-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected Label parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<LabelParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Handlers);

        var coreContract = contract.Handlers!.Single(entry => string.Equals(entry.Name, "LabelHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageLabelHandler.ParityPropertyKeys, coreContract.PropertyMapperKeys);
        AssertSetEquals(MirageLabelHandler.ParityCommandKeys, coreContract.CommandMapperKeys);

        var controlsRemapContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "LabelControlsRemap", StringComparison.Ordinal));
        AssertSetEquals(MirageLabelHandler.ParityControlsRemapPropertyKeys, controlsRemapContract.PropertyMapperKeys);
        AssertSetEquals(MirageLabelHandler.ParityCommandKeys, controlsRemapContract.CommandMapperKeys);
    }

    [Fact]
    public void LabelParityContract_DivergencesAreCompleteAndTestBacked()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "label-parity-contract.json");
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "label-parity-divergences.json");
        Assert.True(File.Exists(contractPath), $"Expected Label parity contract file not found: {contractPath}");
        Assert.True(File.Exists(registryPath), $"Expected Label divergence registry file not found: {registryPath}");

        var contract = JsonSerializer.Deserialize<LabelParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        var registry = JsonSerializer.Deserialize<LabelParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.NotNull(registry);
        Assert.NotNull(registry!.Entries);
        Assert.NotEmpty(registry.Entries!);

        var controlsRemapContract = contract!.Handlers!.Single(entry => string.Equals(entry.Name, "LabelControlsRemap", StringComparison.Ordinal));
        var controlsRemapKeys = new HashSet<string>(controlsRemapContract.PropertyMapperKeys!, StringComparer.Ordinal);
        var requiredDeferredKeys = new HashSet<string>(MirageLabelHandler.ParityDeferredControlsRemapPropertyKeys, StringComparer.Ordinal);
        var testMethodNames = DiscoverTestMethodNames(repositoryRoot);

        var coveredKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in registry.Entries!)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Id));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
            Assert.False(string.IsNullOrWhiteSpace(entry.UpstreamReference));
            Assert.False(string.IsNullOrWhiteSpace(entry.TestReference));
            Assert.Contains(entry.TestReference!, testMethodNames);
            Assert.NotNull(entry.Keys);
            Assert.NotEmpty(entry.Keys!);

            foreach (var key in entry.Keys!)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                Assert.Contains(key, controlsRemapKeys);
                coveredKeys.Add(key);
            }
        }

        Assert.True(
            requiredDeferredKeys.SetEquals(coveredKeys),
            $"Expected deferred key coverage for [{string.Join(", ", requiredDeferredKeys)}] but found [{string.Join(", ", coveredKeys)}].");
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

    private sealed class LabelParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<LabelParityHandlerContract>? Handlers { get; set; }
    }

    private sealed class LabelParityHandlerContract
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? PropertyMapperKeys { get; set; }

        public List<string>? CommandMapperKeys { get; set; }
    }

    private sealed class LabelParityDivergenceRegistry
    {
        public List<LabelParityDivergenceEntry>? Entries { get; set; }
    }

    private sealed class LabelParityDivergenceEntry
    {
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public string? UpstreamReference { get; set; }

        public string? TestReference { get; set; }

        public List<string>? Keys { get; set; }
    }
}
