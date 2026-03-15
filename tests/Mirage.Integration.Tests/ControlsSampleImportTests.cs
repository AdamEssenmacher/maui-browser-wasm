using System.Diagnostics;
using ControlsSample.Shared.App;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class ControlsSampleImportTests
{
    [Fact]
    public void MirageVisibilityPolicy_ShowsRunnableEntriesOnly()
    {
        var policy = new MirageControlsSampleVisibilityPolicy();

        var visible = ControlsSampleCatalog.All.Where(policy.IsVisible).ToArray();

        Assert.NotEmpty(visible);
        Assert.All(visible, entry => Assert.Equal(ControlsSampleStatus.Runnable, entry.Status));
        Assert.DoesNotContain(visible, entry => entry.Status == ControlsSampleStatus.HiddenUnsupported);
    }

    [Fact]
    public void NativeVisibilityPolicy_AllowsMirageUnsupportedButBlocksExternalCategories()
    {
        var policy = new NativeControlsSampleVisibilityPolicy();

        var visible = ControlsSampleCatalog.All.Where(policy.IsVisible).ToArray();

        Assert.Contains(visible, entry => entry.Status == ControlsSampleStatus.HiddenUnsupported);
        Assert.DoesNotContain(visible, entry => entry.Status == ControlsSampleStatus.HiddenRazor);
        Assert.DoesNotContain(visible, entry => entry.Status == ControlsSampleStatus.HiddenPlatformSpecific);
    }

    [Fact]
    public void SyncScript_DryRun_Path_IsOperational()
    {
        var repoRoot = FindRepoRoot();

        var startInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = "tools/vendor/sync-controls-sample.sh --dry-run",
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start sync script process.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        process.WaitForExit(30_000);

        Assert.True(process.HasExited, "Expected dry-run script process to exit within timeout.");
        Assert.Equal(0, process.ExitCode);
        Assert.Contains("[dry-run]", stdout);
        Assert.DoesNotContain("error", stderr, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
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
}
