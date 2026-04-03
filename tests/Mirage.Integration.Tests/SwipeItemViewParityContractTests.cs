using Mirage.Maui.Handlers;
using System.Text.Json;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class SwipeItemViewParityContractTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void SwipeItemViewParityContract_MapsToMirageHandlerSurface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractPath = Path.Combine(repositoryRoot, "docs", "parity", "swipeitemview-parity-contract.json");
        Assert.True(File.Exists(contractPath), $"Expected SwipeItemView parity contract file not found: {contractPath}");

        var contract = JsonSerializer.Deserialize<SwipeItemViewParityContract>(File.ReadAllText(contractPath), ContractJsonOptions);
        Assert.NotNull(contract);
        Assert.False(string.IsNullOrWhiteSpace(contract!.MauiBaselineCommit));
        Assert.NotNull(contract.Handlers);

        var handlerContract = contract.Handlers!.Single(entry => string.Equals(entry.Name, "SwipeItemViewHandler", StringComparison.Ordinal));
        AssertSetEquals(MirageSwipeItemViewHandler.ParityPropertyKeys, handlerContract.PropertyMapperKeys);
        AssertSetEquals(MirageSwipeItemViewHandler.ParityCommandKeys, handlerContract.CommandMapperKeys);
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

    private sealed class SwipeItemViewParityContract
    {
        public string? MauiBaselineCommit { get; set; }

        public List<SwipeItemViewParityHandlerContract>? Handlers { get; set; }
    }

    private sealed class SwipeItemViewParityHandlerContract
    {
        public string? Name { get; set; }

        public string? MirageType { get; set; }

        public List<string>? PropertyMapperKeys { get; set; }

        public List<string>? CommandMapperKeys { get; set; }
    }
}
