using ControlsSample.Shared.App;
using Mirage.Maui.Essentials;
using Mirage.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageBrowserDeviceInfoTests
{
    [Theory]
    [InlineData(480, 800, nameof(DeviceIdiom.Phone))]
    [InlineData(768, 1024, nameof(DeviceIdiom.Tablet))]
    [InlineData(1280, 900, nameof(DeviceIdiom.Desktop))]
    [InlineData(1600, 1200, nameof(DeviceIdiom.Desktop))]
    public void MirageBrowserDeviceInfo_ClassifiesViewportBuckets(double viewportWidth, double viewportHeight, string expectedIdiom)
    {
        var deviceInfo = new MirageBrowserDeviceInfo(viewportWidth, viewportHeight);

        Assert.Equal(expectedIdiom, deviceInfo.Idiom.ToString());
    }

    [Fact]
    public void MirageBrowserDeviceInfo_ReturnsDeterministicBrowserIdentity()
    {
        var deviceInfo = new MirageBrowserDeviceInfo(1280, 900);

        Assert.Equal(DevicePlatform.Create("Browser"), deviceInfo.Platform);
        Assert.Equal(DeviceType.Physical, deviceInfo.DeviceType);
        Assert.Equal("Browser", deviceInfo.Model);
        Assert.Equal("Browser", deviceInfo.Manufacturer);
        Assert.Equal("Browser", deviceInfo.Name);
        Assert.Equal("0.0", deviceInfo.VersionString);
        Assert.Equal(new Version(0, 0), deviceInfo.Version);
    }

    [Fact]
    public void DevicePage_UsesDefaultPlatformBranch_AndBrowserIdiom()
    {
        var originalSynchronizationContext = SynchronizationContext.Current;
        var originalDispatcherProvider = DispatcherProvider.Current;

        try
        {
            SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());
            DispatcherProvider.SetCurrent(new BrowserDispatcherProvider());

            var dispatcher = Dispatcher.GetForCurrentThread();
            Assert.NotNull(dispatcher);

            Exception? exception = null;
            string? platformText = null;
            string? idiomText = null;

            var dispatchSucceeded = dispatcher!.Dispatch(() =>
            {
                _ = new ControlsSampleApp();
#pragma warning disable CS0612
                DependencyService.Register<IFontNamedSizeService, TestFontNamedSizeService>();
#pragma warning restore CS0612

                var services = new ServiceCollection();
                services.AddSingleton<IPreferences>(new TestPreferences());
                services.AddSingleton<IAppInfo>(new TestAppInfo());
                services.AddSingleton<IDeviceInfo>(new MirageBrowserDeviceInfo(1280, 900));

                using var provider = services.BuildServiceProvider();
                using var patchScope = MirageEssentialsStaticPatcher.Apply(provider);

                exception = Record.Exception(() =>
                {
                    var page = new global::Maui.Controls.Sample.Pages.DevicePage();
                    var labels = FindDescendants<Label>(page).ToArray();
                    platformText = labels.ElementAtOrDefault(0)?.Text;
                    idiomText = labels.ElementAtOrDefault(1)?.Text;
                });
            });

            Assert.True(dispatchSucceeded);
            Assert.Null(exception);
            Assert.Equal("Default Platform", platformText);
            Assert.Equal("Desktop", idiomText);
        }
        finally
        {
            DispatcherProvider.SetCurrent(originalDispatcherProvider);
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
        }
    }

    private static IEnumerable<TElement> FindDescendants<TElement>(Element root)
        where TElement : Element
    {
        if (root is TElement match)
        {
            yield return match;
        }

        if (root is not IElementController controller)
        {
            yield break;
        }

        foreach (var child in controller.LogicalChildren)
        {
            foreach (var descendant in FindDescendants<TElement>(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class TestPreferences : IPreferences
    {
        public bool ContainsKey(string key, string? sharedName = null) => false;

        public void Remove(string key, string? sharedName = null)
        {
        }

        public void Clear(string? sharedName = null)
        {
        }

        public void Set<T>(string key, T value, string? sharedName = null)
        {
        }

        public T Get<T>(string key, T defaultValue, string? sharedName = null) => defaultValue;
    }

    private sealed class TestAppInfo : IAppInfo
    {
        public string PackageName => "Mirage.Tests";

        public string Name => "Mirage Tests";

        public string VersionString => "1.2.3";

        public Version Version => new(1, 2, 3, 4);

        public string BuildString => "4";

        public AppTheme RequestedTheme => AppTheme.Light;

        public AppPackagingModel PackagingModel => AppPackagingModel.Packaged;

        public LayoutDirection RequestedLayoutDirection => LayoutDirection.LeftToRight;

        public void ShowSettingsUI()
        {
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
}
