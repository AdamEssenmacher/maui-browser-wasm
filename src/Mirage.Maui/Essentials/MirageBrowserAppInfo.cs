using System.Globalization;
using System.Reflection;
using Mirage.Web.Dom;
using Microsoft.Maui.ApplicationModel;

namespace Mirage.Maui.Essentials;

internal sealed class MirageBrowserAppInfo : IAppInfo
{
    private readonly AppInfoSnapshot _snapshot;

    public MirageBrowserAppInfo()
    {
        _snapshot = CreateSnapshot(Assembly.GetEntryAssembly() ?? typeof(MirageBrowserAppInfo).Assembly);
    }

    public string PackageName => _snapshot.PackageName;

    public string Name => _snapshot.Name;

    public string VersionString => _snapshot.VersionString;

    public Version Version => _snapshot.Version;

    public string BuildString => _snapshot.BuildString;

    public AppTheme RequestedTheme => DomElement.GetPreferredColorScheme() switch
    {
        "dark" => AppTheme.Dark,
        "light" => AppTheme.Light,
        _ => AppTheme.Unspecified
    };

    public AppPackagingModel PackagingModel => AppPackagingModel.Packaged;

    public LayoutDirection RequestedLayoutDirection =>
        CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
            ? LayoutDirection.RightToLeft
            : LayoutDirection.LeftToRight;

    public void ShowSettingsUI() =>
        throw new FeatureNotSupportedException("AppInfo.ShowSettingsUI is not supported in Mirage browser hosts.");

    private static AppInfoSnapshot CreateSnapshot(Assembly assembly)
    {
        var packageName = assembly.GetName().Name;
        if (string.IsNullOrWhiteSpace(packageName))
        {
            packageName = "Mirage";
        }

        var version = assembly.GetName().Version ?? new Version(1, 0, 0, 0);
        var versionString = ResolveVersionString(assembly, version);
        var buildString = ResolveBuildString(assembly, version);
        var name = ResolveName(assembly, packageName);

        return new AppInfoSnapshot(
            PackageName: packageName,
            Name: name,
            Version: version,
            VersionString: versionString,
            BuildString: buildString);
    }

    private static string ResolveName(Assembly assembly, string fallback)
    {
        var applicationTitle = TryReadApplicationTitle(assembly);
        if (!string.IsNullOrWhiteSpace(applicationTitle))
        {
            return applicationTitle;
        }

        var assemblyTitle = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title?.Trim();
        return string.IsNullOrWhiteSpace(assemblyTitle) ? fallback : assemblyTitle;
    }

    private static string ResolveVersionString(Assembly assembly, Version version)
    {
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Trim();
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return version.ToString();
    }

    private static string ResolveBuildString(Assembly assembly, Version version)
    {
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version?.Trim();
        if (!string.IsNullOrWhiteSpace(fileVersion))
        {
            return fileVersion;
        }

        if (version.Revision >= 0)
        {
            return version.Revision.ToString(CultureInfo.InvariantCulture);
        }

        if (version.Build >= 0)
        {
            return version.Build.ToString(CultureInfo.InvariantCulture);
        }

        return "0";
    }

    private static string? TryReadApplicationTitle(Assembly assembly)
    {
        foreach (var attribute in assembly.GetCustomAttributesData())
        {
            if (!string.Equals(attribute.AttributeType.Name, "ApplicationTitleAttribute", StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Count > 0
                && attribute.ConstructorArguments[0].Value is string ctorValue
                && !string.IsNullOrWhiteSpace(ctorValue))
            {
                return ctorValue.Trim();
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (!string.Equals(argument.MemberName, "Title", StringComparison.Ordinal))
                {
                    continue;
                }

                if (argument.TypedValue.Value is string namedValue
                    && !string.IsNullOrWhiteSpace(namedValue))
                {
                    return namedValue.Trim();
                }
            }
        }

        foreach (var metadata in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (string.Equals(metadata.Key, "ApplicationTitle", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(metadata.Value))
            {
                return metadata.Value.Trim();
            }
        }

        return null;
    }

    private readonly record struct AppInfoSnapshot(
        string PackageName,
        string Name,
        Version Version,
        string VersionString,
        string BuildString);
}
