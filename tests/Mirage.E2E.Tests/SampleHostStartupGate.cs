using System.Threading;

namespace Mirage.E2E.Tests;

internal static class SampleHostStartupGate
{
    private static readonly SemaphoreSlim Gate = new(initialCount: 1, maxCount: 1);

    public static Task WaitAsync() => Gate.WaitAsync();

    public static void Release() => Gate.Release();
}
