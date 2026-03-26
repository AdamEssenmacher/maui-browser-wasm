using Mirage.Maui.Handlers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class EditorParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void EditorParityContract_MapsToMirageHandlerSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "editor-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected Editor parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<EditorParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Handlers);

        var coreContract = contract.Handlers!.Single(entry => string.Equals(entry.Name, "EditorHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageEditorHandler.ParityPropertyKeys, coreContract.PropertyMapperKeys);
        AssertSetEquals(MirageEditorHandler.ParityCommandKeys, coreContract.CommandMapperKeys);
        AssertSetEquals(MirageEditorHandler.ParityExtensionPropertyKeys, coreContract.MirageExtensionPropertyMapperKeys);
        AssertSetEquals(MirageEditorHandler.ParityExtensionCommandKeys, coreContract.MirageExtensionCommandMapperKeys);

        var controlsRemapContract = contract.Handlers.Single(entry => string.Equals(entry.Name, "EditorControlsRemap", StringComparison.Ordinal));
        AssertSetEquals(MirageEditorHandler.ParityControlsRemapPropertyKeys, controlsRemapContract.PropertyMapperKeys);
        AssertSetEquals(Array.Empty<string>(), controlsRemapContract.CommandMapperKeys ?? []);
    }

    [Fact]
    public void EditorParityContract_DivergencesAreWellFormedAndTestBacked()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "editor-parity-contract.json");
        var registryPath = Path.Combine(repositoryRoot, "docs", "parity", "editor-parity-divergences.json");
        Assert.True(File.Exists(contractPath), $"Expected Editor parity contract file not found: {contractPath}");
        Assert.True(File.Exists(registryPath), $"Expected Editor divergence registry file not found: {registryPath}");

        var contract = JsonSerializer.Deserialize<EditorParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        var registry = JsonSerializer.Deserialize<EditorParityDivergenceRegistry>(File.ReadAllText(registryPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.NotNull(registry);
        Assert.NotNull(registry!.Entries);
        Assert.NotEmpty(registry.Entries!);

        var coreContract = contract!.Handlers!.Single(entry => string.Equals(entry.Name, "EditorHandler", StringComparison.Ordinal));
        var requiredExtensionPropertyKeys = new HashSet<string>(coreContract.MirageExtensionPropertyMapperKeys!, StringComparer.Ordinal);
        var requiredExtensionCommandKeys = new HashSet<string>(coreContract.MirageExtensionCommandMapperKeys!, StringComparer.Ordinal);
        var coveredExtensionPropertyKeys = new HashSet<string>(StringComparer.Ordinal);
        var coveredExtensionCommandKeys = new HashSet<string>(StringComparer.Ordinal);
        var testMethodNames = DiscoverTestMethodNames(repositoryRoot);

        foreach (var entry in registry.Entries!)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Id));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
            Assert.False(string.IsNullOrWhiteSpace(entry.UpstreamReference));
            Assert.False(string.IsNullOrWhiteSpace(entry.TestReference));
            Assert.Contains(entry.TestReference!, testMethodNames);

            if (entry.ExtensionPropertyKeys is not null)
            {
                foreach (var extensionKey in entry.ExtensionPropertyKeys)
                {
                    if (!string.IsNullOrWhiteSpace(extensionKey))
                    {
                        coveredExtensionPropertyKeys.Add(extensionKey);
                    }
                }
            }

            if (entry.ExtensionCommandKeys is not null)
            {
                foreach (var extensionKey in entry.ExtensionCommandKeys)
                {
                    if (!string.IsNullOrWhiteSpace(extensionKey))
                    {
                        coveredExtensionCommandKeys.Add(extensionKey);
                    }
                }
            }
        }

        Assert.True(
            requiredExtensionPropertyKeys.SetEquals(coveredExtensionPropertyKeys),
            $"Expected extension property divergence coverage for [{string.Join(", ", requiredExtensionPropertyKeys)}] but found [{string.Join(", ", coveredExtensionPropertyKeys)}].");
        Assert.True(
            requiredExtensionCommandKeys.SetEquals(coveredExtensionCommandKeys),
            $"Expected extension command divergence coverage for [{string.Join(", ", requiredExtensionCommandKeys)}] but found [{string.Join(", ", coveredExtensionCommandKeys)}].");
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

    private sealed class EditorParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<EditorParityHandlerContract>? Handlers { get; set; }
    }

    private sealed class EditorParityHandlerContract
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? PropertyMapperKeys { get; set; }

        public List<string>? CommandMapperKeys { get; set; }

        public List<string>? MirageExtensionPropertyMapperKeys { get; set; }

        public List<string>? MirageExtensionCommandMapperKeys { get; set; }
    }

    private sealed class EditorParityDivergenceRegistry
    {
        public List<EditorParityDivergenceEntry>? Entries { get; set; }
    }

    private sealed class EditorParityDivergenceEntry
    {
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public string? UpstreamReference { get; set; }

        public string? TestReference { get; set; }

        public List<string>? ExtensionPropertyKeys { get; set; }

        public List<string>? ExtensionCommandKeys { get; set; }
    }
}
