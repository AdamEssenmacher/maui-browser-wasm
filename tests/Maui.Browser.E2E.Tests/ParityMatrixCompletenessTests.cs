using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed class ParityMatrixCompletenessTests
{
    [Fact]
    public void MatrixContainsAllInventoryControls()
    {
        var repoRoot = FindRepoRoot();
        var inventoryPath = Path.Combine(repoRoot, "tools", "parity", "maui-modern-controls.tsv");
        var matrixPath = Path.Combine(repoRoot, "docs", "PARITY_MATRIX.md");

        Assert.True(File.Exists(inventoryPath), $"Missing inventory file: {inventoryPath}");
        Assert.True(File.Exists(matrixPath), $"Missing parity matrix: {matrixPath}");

        var matrixText = File.ReadAllText(matrixPath);
        var missing = new List<string>();

        foreach (var line in File.ReadLines(inventoryPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            var control = parts[0].Trim();
            var expectedRowPrefix = $"| `{control}` |";
            if (!matrixText.Contains(expectedRowPrefix, StringComparison.Ordinal))
            {
                missing.Add(control);
            }
        }

        Assert.True(
            missing.Count == 0,
            $"PARITY_MATRIX is missing controls: {string.Join(", ", missing)}");
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MauiDomWasm.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root containing MauiDomWasm.sln.");
    }
}
