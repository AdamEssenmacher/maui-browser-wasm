using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Mirage.Core.Diagnostics;

public static class MirageDiagnostics
{
    private static int s_isEnabled = ParseEnabled() ? 1 : 0;
    private static long s_nextRenderSequence;
    private static long s_lastRenderCompletedSequence;
    private static long s_lastRenderCompletedTimestamp;

    public static bool IsEnabled => Volatile.Read(ref s_isEnabled) == 1;

    public static void Enable() => Interlocked.Exchange(ref s_isEnabled, 1);

    public static void Disable() => Interlocked.Exchange(ref s_isEnabled, 0);

    public static void EnableIf(bool enabled)
    {
        if (enabled)
        {
            Enable();
        }
    }

    public static long GetTimestamp() => Stopwatch.GetTimestamp();

    public static double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
    {
        var elapsedTicks = endTimestamp - startTimestamp;
        if (elapsedTicks <= 0)
        {
            return 0;
        }

        return elapsedTicks * 1000d / Stopwatch.Frequency;
    }

    public static long NextRenderSequence()
    {
        return Interlocked.Increment(ref s_nextRenderSequence);
    }

    public static long GetLastRenderScheduledSequence()
    {
        return Interlocked.Read(ref s_nextRenderSequence);
    }

    public static void MarkRenderCompleted(long sequence, long completedTimestamp)
    {
        Interlocked.Exchange(ref s_lastRenderCompletedSequence, sequence);
        Interlocked.Exchange(ref s_lastRenderCompletedTimestamp, completedTimestamp);
    }

    public static long GetLastRenderCompletedSequence()
    {
        return Interlocked.Read(ref s_lastRenderCompletedSequence);
    }

    public static long GetLastRenderCompletedTimestamp()
    {
        return Interlocked.Read(ref s_lastRenderCompletedTimestamp);
    }

    public static void Log(string category, string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"[Mirage.Diagnostics][{category}] {message}"));
    }

    private static bool ParseEnabled()
    {
        var raw = Environment.GetEnvironmentVariable("MIRAGE_DIAGNOSTICS");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return raw.Trim() switch
        {
            "1" => true,
            "true" => true,
            "TRUE" => true,
            "yes" => true,
            "YES" => true,
            "on" => true,
            "ON" => true,
            _ => false
        };
    }
}
