using Mirage.Core.Providers;
using Mirage.Provider.Fast;

namespace Mirage.Integration.Tests;

public sealed class FastProviderConformanceTests : ProviderConformanceTestsBase
{
    protected override IMirageComponentProvider CreateProvider()
    {
        return new FastMirageComponentProvider();
    }
}
