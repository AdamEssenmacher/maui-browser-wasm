using Xunit;

namespace Mirage.Integration.Tests;

[CollectionDefinition("Mirage NonParallel", DisableParallelization = true)]
public sealed class NonParallelTestCollection
{
}
