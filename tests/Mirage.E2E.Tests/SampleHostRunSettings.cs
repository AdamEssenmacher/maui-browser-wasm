using System.Diagnostics;

namespace Mirage.E2E.Tests;

internal static class SampleHostRunSettings
{
    private const string DefaultConfiguration = "Release";
    private const string ConfigurationVariableName = "MIRAGE_E2E_HOST_CONFIGURATION";
    private const string NoBuildVariableName = "MIRAGE_E2E_HOST_NO_BUILD";

    public static void ConfigureDotnetRun(ProcessStartInfo startInfo, string sampleProjectPath)
    {
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(sampleProjectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(GetConfiguration());

        if (UsePrebuiltHost())
        {
            startInfo.ArgumentList.Add("--no-build");
        }

        startInfo.ArgumentList.Add("--no-launch-profile");
    }

    private static string GetConfiguration()
    {
        var configuration = Environment.GetEnvironmentVariable(ConfigurationVariableName);
        return string.IsNullOrWhiteSpace(configuration)
            ? DefaultConfiguration
            : configuration.Trim();
    }

    private static bool UsePrebuiltHost()
    {
        var raw = Environment.GetEnvironmentVariable(NoBuildVariableName);
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
