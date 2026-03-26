using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Showcase.Shared.App;

public partial class ShowcaseEssentialsLabPage : ContentPage
{
    private const string CounterPreferenceKey = "showcase.essentials.counter";
    private IPreferences? _preferences;
    private IAppInfo? _appInfo;
    private bool _initialized;

    public ShowcaseEssentialsLabPage()
    {
        Title = "Essentials Lab";
        InitializeComponent();
        HandlerChanged += OnPageHandlerChanged;
    }

    private void OnPageHandlerChanged(object? sender, EventArgs e)
    {
        TryInitializeServices();
    }

    private void TryInitializeServices()
    {
        if (_initialized)
        {
            return;
        }

        var services = Handler?.MauiContext?.Services;
        if (services is null)
        {
            return;
        }

        _preferences = services.GetRequiredService<IPreferences>();
        _appInfo = services.GetRequiredService<IAppInfo>();
        _initialized = true;
        RefreshView();
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        _preferences!.Remove(CounterPreferenceKey);
        RefreshView();
    }

    private void OnIncrementClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        EnsureInitialized();
        var nextValue = Preferences.Get(CounterPreferenceKey, 0) + 1;
        Preferences.Set(CounterPreferenceKey, nextValue);
        RefreshView();
    }

    private void EnsureInitialized()
    {
        TryInitializeServices();
        if (!_initialized || _preferences is null || _appInfo is null)
        {
            throw new InvalidOperationException("ShowcaseEssentialsLabPage requires browser-host Essentials services.");
        }
    }

    private void RefreshView()
    {
        EnsureInitialized();

        PreferencesPatchStatusLabel.Text = $"Preferences patched: {ReferenceEquals(_preferences, Preferences.Default).ToString().ToLowerInvariant()}";
        AppInfoPatchStatusLabel.Text = $"AppInfo patched: {ReferenceEquals(_appInfo, AppInfo.Current).ToString().ToLowerInvariant()}";

        StaticCounterLabel.Text = Preferences.Get(CounterPreferenceKey, 0).ToString();
        InjectedCounterLabel.Text = _preferences!.Get(CounterPreferenceKey, 0).ToString();

        StaticAppInfoLabel.Text = RenderAppInfoSummary(AppInfo.Current);
        InjectedAppInfoLabel.Text = RenderAppInfoSummary(_appInfo!);
    }

    private static string RenderAppInfoSummary(IAppInfo appInfo)
    {
        var builder = new StringBuilder();
        builder.Append("Name=").AppendLine(appInfo.Name);
        builder.Append("Package=").AppendLine(appInfo.PackageName);
        builder.Append("Version=").AppendLine(appInfo.VersionString);
        builder.Append("Build=").AppendLine(appInfo.BuildString);
        builder.Append("Theme=").AppendLine(appInfo.RequestedTheme.ToString());
        builder.Append("Direction=").AppendLine(appInfo.RequestedLayoutDirection.ToString());
        builder.Append("Packaging=").Append(appInfo.PackagingModel);
        return builder.ToString();
    }
}
