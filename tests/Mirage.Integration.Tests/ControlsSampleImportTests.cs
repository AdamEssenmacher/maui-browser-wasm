using System.Diagnostics;
using ControlsSample.Shared.App;
using Maui.Controls.Sample.Pages;
using Mirage.Maui.Hosting;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Dispatching;
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
    public void CatalogEntries_HaveExplicitCategoriesAssigned()
    {
        foreach (var entry in ControlsSampleCatalog.All)
        {
            _ = entry.Category;
        }
    }

    [Fact]
    public void MirageVisibilityPolicy_GroupsVisibleEntriesIntoExpectedCategories()
    {
        var policy = new MirageControlsSampleVisibilityPolicy();
        var visible = ControlsSampleCatalog.All.Where(policy.IsVisible).ToArray();
        var categoryOrder = new[]
        {
            ControlsSampleCategory.Layouts,
            ControlsSampleCategory.Controls,
            ControlsSampleCategory.Core,
            ControlsSampleCategory.ParityProbes,
            ControlsSampleCategory.Compatibility
        };

        var grouped = categoryOrder
            .Select(category => (Category: category, Entries: visible.Where(entry => entry.Category == category).ToArray()))
            .Where(group => group.Entries.Length > 0)
            .ToArray();

        Assert.Equal(categoryOrder, grouped.Select(group => group.Category));
        Assert.Equal(visible.Length, grouped.Sum(group => group.Entries.Length));

        var probeEntries = grouped.Single(group => group.Category == ControlsSampleCategory.ParityProbes).Entries;
        Assert.All(probeEntries, entry => Assert.StartsWith("App/", entry.UpstreamPath, StringComparison.Ordinal));

        Assert.DoesNotContain(
            grouped.Where(group => group.Category != ControlsSampleCategory.ParityProbes).SelectMany(group => group.Entries),
            entry => entry.UpstreamPath.StartsWith("App/", StringComparison.Ordinal));
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

    [Fact]
    public void TemplatedViewCatalogEntry_CanBeInstantiated()
    {
        var originalSynchronizationContext = SynchronizationContext.Current;
        var originalDispatcherProvider = DispatcherProvider.Current;
        Exception? exception = null;

        try
        {
            SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());
            DispatcherProvider.SetCurrent(new BrowserDispatcherProvider());

            var dispatcher = Dispatcher.GetForCurrentThread();
            Assert.NotNull(dispatcher);

            var dispatchSucceeded = dispatcher!.Dispatch(() =>
            {
                _ = new ControlsSampleApp();
#pragma warning disable CS0612 // IFontNamedSizeService is required for upstream sample compatibility controls.
                DependencyService.Register<IFontNamedSizeService, TestFontNamedSizeService>();
#pragma warning restore CS0612
                exception = Record.Exception(() => _ = new TemplatedViewPage());
            });

            Assert.True(dispatchSucceeded);
        }
        finally
        {
            DispatcherProvider.SetCurrent(originalDispatcherProvider);
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
        }

        Assert.Null(exception);
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

#pragma warning disable CS0612
    private sealed class TestFontNamedSizeService : IFontNamedSizeService
    {
        public double GetNamedSize(NamedSize size, Type targetElementType, bool useOldSizes)
        {
            _ = targetElementType;
            _ = useOldSizes;
            return size switch
            {
                NamedSize.Micro => 10,
                NamedSize.Small => 12,
                NamedSize.Medium => 14,
                NamedSize.Large => 18,
                _ => 14
            };
        }
    }
#pragma warning restore CS0612

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            d(state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            d(state);
        }
    }
}
