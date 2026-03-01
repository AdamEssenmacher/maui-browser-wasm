using System.Reflection;

namespace MauiBrowser.SmokeTests;

public sealed class SolutionSmokeTests
{
    [Fact]
    public void RuntimeAssembly_LoadsWithExpectedName()
    {
        var assembly = Assembly.Load("MauiBrowser.Runtime");
        Assert.Equal("MauiBrowser.Runtime", assembly.GetName().Name);
    }

    [Fact]
    public void InteropAssembly_LoadsWithExpectedName()
    {
        var assembly = Assembly.Load("MauiBrowser.Interop");
        Assert.Equal("MauiBrowser.Interop", assembly.GetName().Name);
    }
}
