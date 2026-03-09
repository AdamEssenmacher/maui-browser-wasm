using Mirage.Core.Providers;
using Mirage.Provider.Fluent;

namespace Mirage.Integration.Tests;

public sealed class FluentProviderConformanceTests : ProviderConformanceTestsBase
{
    protected override IMirageComponentProvider CreateProvider()
    {
        return new FluentMirageComponentProvider();
    }
}
