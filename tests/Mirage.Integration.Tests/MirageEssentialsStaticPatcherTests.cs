using Mirage.Maui.Essentials;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageEssentialsStaticPatcherTests
{
    [Fact]
    public void Apply_PatchesPreferencesDefault_FromRegisteredService()
    {
        var preferences = new TestPreferences();
        var services = CreateServices(preferences, new TestAppInfo());

        using var patchScope = MirageEssentialsStaticPatcher.Apply(services);

        Assert.Same(preferences, Preferences.Default);
    }

    [Fact]
    public void Apply_PatchesAppInfoCurrent_FromRegisteredService()
    {
        var appInfo = new TestAppInfo();
        var services = CreateServices(new TestPreferences(), appInfo);

        using var patchScope = MirageEssentialsStaticPatcher.Apply(services);

        Assert.Same(appInfo, AppInfo.Current);
    }

    [Fact]
    public void Apply_AllowsStaticAndInjectedPreferencesToShareState()
    {
        var preferences = new TestPreferences();
        var services = CreateServices(preferences, new TestAppInfo());

        using var patchScope = MirageEssentialsStaticPatcher.Apply(services);

        Preferences.Set("proof-counter", 7);
        Assert.Equal(7, preferences.Get("proof-counter", 0));

        preferences.Set("proof-counter", 11);
        Assert.Equal(11, Preferences.Get("proof-counter", 0));
    }

    [Fact]
    public void Apply_ThrowsWhenFieldDescriptorDrifts()
    {
        var services = CreateServices(new TestPreferences(), new TestAppInfo());

        var error = Assert.Throws<InvalidOperationException>(() =>
            MirageEssentialsStaticPatcher.Apply(
                services,
                new MirageEssentialsStaticPatcher.MirageEssentialsPatchDescriptor(typeof(Preferences), typeof(IPreferences), "missingField")));

        Assert.Contains("missingField", error.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(Preferences).FullName!, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_RollsBackEarlierPatchesWhenALaterDescriptorFails()
    {
        var preferences = new TestPreferences();
        var services = CreateServices(preferences, new TestAppInfo());

        Assert.Throws<InvalidOperationException>(() =>
            MirageEssentialsStaticPatcher.Apply(
                services,
                new MirageEssentialsStaticPatcher.MirageEssentialsPatchDescriptor(typeof(Preferences), typeof(IPreferences), "defaultImplementation"),
                new MirageEssentialsStaticPatcher.MirageEssentialsPatchDescriptor(typeof(AppInfo), typeof(IAppInfo), "missingField")));

        Assert.NotSame(preferences, Preferences.Default);
    }

    [Fact]
    public void RestoreScope_RevertsStaticFieldsAfterDispose()
    {
        var preferences = new TestPreferences();
        var appInfo = new TestAppInfo();
        var services = CreateServices(preferences, appInfo);

        using (MirageEssentialsStaticPatcher.Apply(services))
        {
            Assert.Same(preferences, Preferences.Default);
            Assert.Same(appInfo, AppInfo.Current);
        }

        Assert.NotSame(preferences, Preferences.Default);
        Assert.NotSame(appInfo, AppInfo.Current);
    }

    private static ServiceProvider CreateServices(IPreferences preferences, IAppInfo appInfo)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPreferences>(preferences);
        services.AddSingleton<IAppInfo>(appInfo);
        return services.BuildServiceProvider();
    }

    private sealed class TestPreferences : IPreferences
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

        public bool ContainsKey(string key, string? sharedName = null) => _values.ContainsKey(ComposeKey(key, sharedName));

        public void Remove(string key, string? sharedName = null) => _values.Remove(ComposeKey(key, sharedName));

        public void Clear(string? sharedName = null)
        {
            if (sharedName is null)
            {
                _values.Clear();
                return;
            }

            foreach (var key in _values.Keys.Where(key => key.StartsWith(sharedName + "::", StringComparison.Ordinal)).ToArray())
            {
                _values.Remove(key);
            }
        }

        public void Set<T>(string key, T value, string? sharedName = null) => _values[ComposeKey(key, sharedName)] = value;

        public T Get<T>(string key, T defaultValue, string? sharedName = null)
        {
            return _values.TryGetValue(ComposeKey(key, sharedName), out var value) && value is T typedValue
                ? typedValue
                : defaultValue;
        }

        private static string ComposeKey(string key, string? sharedName) => $"{sharedName ?? "__default"}::{key}";
    }

    private sealed class TestAppInfo : IAppInfo
    {
        public string PackageName => "Mirage.Tests";

        public string Name => "Mirage Tests";

        public string VersionString => "1.2.3";

        public Version Version => new(1, 2, 3, 4);

        public string BuildString => "4";

        public AppTheme RequestedTheme => AppTheme.Dark;

        public AppPackagingModel PackagingModel => AppPackagingModel.Packaged;

        public LayoutDirection RequestedLayoutDirection => LayoutDirection.LeftToRight;

        public void ShowSettingsUI()
        {
        }
    }
}
