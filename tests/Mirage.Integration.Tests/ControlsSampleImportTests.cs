using System.Diagnostics;
using ControlsSample.Shared.App;
using Maui.Controls.Sample.Pages;
using Mirage.Core.Providers;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Provider.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
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
    public void AppThemeBindingPage_CatalogEntry_IsRunnableCore()
    {
        var entry = Assert.Single(ControlsSampleCatalog.All, candidate => candidate.Id == "appthemebindingpage");

        Assert.Equal("Pages/Core/AppThemeBindingPage.xaml", entry.UpstreamPath);
        Assert.Equal(ControlsSampleStatus.Runnable, entry.Status);
        Assert.Equal(ControlsSampleCategory.Core, entry.Category);
        Assert.Equal(typeof(AppThemeBindingPage), entry.PageType);
        Assert.NotNull(entry.PageFactory);
    }

    [Fact]
    public void AppThemeBindingPage_PageFactory_AssignsExpectedAutomationIds()
    {
        var originalSynchronizationContext = SynchronizationContext.Current;
        var originalDispatcherProvider = DispatcherProvider.Current;
        Exception? exception = null;
        Page? createdPage = null;

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

                var entry = Assert.Single(ControlsSampleCatalog.All, candidate => candidate.Id == "appthemebindingpage");
                exception = Record.Exception(() => createdPage = entry.CreatePage(NullServiceProvider.Instance));
            });

            Assert.True(dispatchSucceeded);
        }
        finally
        {
            DispatcherProvider.SetCurrent(originalDispatcherProvider);
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
        }

        Assert.Null(exception);
        Assert.NotNull(createdPage);

        var labels = FindDescendants<Label>(createdPage!).ToArray();

        var inlineLabel = Assert.Single(labels, label => label.AutomationId == "controls-sample-appthemebindingpage-inline-label");
        Assert.Equal("This text is green in light mode, and red in dark mode.", inlineLabel.Text);

        var resourceLabel = Assert.Single(labels, label => label.AutomationId == "controls-sample-appthemebindingpage-resource-label");
        Assert.Equal(
            "This text use LightSecondaryColor Resource (Orange Color) in light mode, and DarkSecondaryColor Resource (Teal Color) in dark mode.",
            resourceLabel.Text);
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

    [Fact]
    public void SwipeViewParityProbePage_VisualTreeContainsNoNullChildren()
    {
        var originalSynchronizationContext = SynchronizationContext.Current;
        var originalDispatcherProvider = DispatcherProvider.Current;
        SwipeViewParityProbePage? page = null;

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
                page = new SwipeViewParityProbePage();
            });

            Assert.True(dispatchSucceeded);
        }
        finally
        {
            DispatcherProvider.SetCurrent(originalDispatcherProvider);
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
        }

        Assert.NotNull(page);
        var failures = new List<string>();
        CollectNullVisualChildren(page!, page!.GetType().Name, failures);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void SwipeViewParityProbePage_CanBeRealizedByMirageHandlers()
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
                var builder = MauiApp.CreateBuilder();
                builder.UseMauiApp<ControlsSampleApp>();
                builder.Services.AddSingleton<IControlsSampleVisibilityPolicy, MirageControlsSampleVisibilityPolicy>();
                builder.Services.AddSingleton<IMirageRenderCoordinator>(new MirageRenderCoordinator(new BrowserDispatcher()));
                builder.Services.AddSingleton<IMirageComponentProvider, FluentMirageComponentProvider>();
                builder.ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler(typeof(Page), typeof(MiragePageHandler));
                    handlers.AddHandler(typeof(ContentPage), typeof(MirageContentPageHandler));
                    handlers.AddHandler(typeof(ScrollView), typeof(MirageScrollViewHandler));
                    handlers.AddHandler(typeof(CollectionView), typeof(MirageCollectionViewHandler));
                    handlers.AddHandler(typeof(SwipeView), typeof(MirageSwipeViewHandler));
                    handlers.AddHandler(typeof(SwipeItemView), typeof(MirageSwipeItemViewHandler));
                    handlers.AddHandler(typeof(VerticalStackLayout), typeof(MirageLayoutHandler));
                    handlers.AddHandler(typeof(HorizontalStackLayout), typeof(MirageLayoutHandler));
                    handlers.AddHandler(typeof(Label), typeof(MirageLabelHandler));
                    handlers.AddHandler(typeof(Button), typeof(MirageButtonHandler));
                    handlers.AddHandler(typeof(Border), typeof(MirageBorderHandler));
                });

                var app = builder.Build();
                ControlsSampleApp.BootstrapServices = app.Services;

#pragma warning disable CS0612 // IFontNamedSizeService is required for upstream sample compatibility controls.
                DependencyService.Register<IFontNamedSizeService, TestFontNamedSizeService>();
#pragma warning restore CS0612

                var mauiContext = new MauiContext(app.Services);
                var page = new SwipeViewParityProbePage();
                exception = Record.Exception(() => _ = page.ToHandler(mauiContext));
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

    private static void CollectNullVisualChildren(IVisualTreeElement element, string path, ICollection<string> failures)
    {
        var children = element.GetVisualChildren();
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            if (child is null)
            {
                failures.Add($"{path} -> child[{index}] is null");
                continue;
            }

            var childPath = $"{path}/{child.GetType().Name}[{index}]";
            CollectNullVisualChildren(child, childPath, failures);
        }
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

    private static IEnumerable<TElement> FindDescendants<TElement>(Element root)
        where TElement : Element
    {
        if (root is TElement typedRoot)
        {
            yield return typedRoot;
        }

        if (root is IElementController controller)
        {
            foreach (var child in controller.LogicalChildren)
            {
                if (child is not Element elementChild)
                {
                    continue;
                }

                foreach (var descendant in FindDescendants<TElement>(elementChild))
                {
                    yield return descendant;
                }
            }
        }
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

    private sealed class NullServiceProvider : IServiceProvider
    {
        public static NullServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType)
        {
            _ = serviceType;
            return null;
        }
    }
}
