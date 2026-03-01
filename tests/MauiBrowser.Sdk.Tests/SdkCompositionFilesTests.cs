using System.Xml.Linq;

namespace MauiBrowser.Sdk.Tests;

public sealed class SdkCompositionFilesTests
{
    [Fact]
    public void SdkProps_ImportsBlazorWebAssemblyProps()
    {
        var sdkProps = LoadSdkProps();
        var import = sdkProps.Root?
            .Elements("Import")
            .SingleOrDefault(e =>
                string.Equals((string?)e.Attribute("Sdk"), "Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.Ordinal) &&
                string.Equals((string?)e.Attribute("Project"), "Sdk.props", StringComparison.Ordinal));

        Assert.NotNull(import);
    }

    [Fact]
    public void SdkTargets_ImportsBlazorWebAssemblyTargets()
    {
        var sdkTargets = LoadSdkTargets();
        var import = sdkTargets.Root?
            .Elements("Import")
            .SingleOrDefault(e =>
                string.Equals((string?)e.Attribute("Sdk"), "Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.Ordinal) &&
                string.Equals((string?)e.Attribute("Project"), "Sdk.targets", StringComparison.Ordinal));

        Assert.NotNull(import);
    }

    [Theory]
    [InlineData("MauiBrowserRuntimePackageId")]
    [InlineData("MauiBrowserRuntimeVersion")]
    [InlineData("MauiBrowserEnableImplicitRuntimeReference")]
    [InlineData("MauiBrowserRuntimeProjectPath")]
    [InlineData("MauiBrowserValidateTargetFramework")]
    [InlineData("MauiBrowserRequiredTargetFramework")]
    public void SdkProps_DefinesExpectedDefaultProperties(string propertyName)
    {
        var sdkProps = LoadSdkProps();
        var property = sdkProps.Descendants(propertyName).SingleOrDefault();

        Assert.NotNull(property);
        Assert.False(string.IsNullOrWhiteSpace((string?)property!.Attribute("Condition")));
    }

    [Fact]
    public void SdkProps_DerivesSdkVersionFromSdkPathInsteadOfHardcoding()
    {
        var sdkProps = LoadSdkProps();

        var candidate = sdkProps.Descendants("MauiBrowserSdkVersionCandidate").SingleOrDefault();
        Assert.NotNull(candidate);
        Assert.Contains("MauiBrowserSdkPackageRoot", candidate!.Value, StringComparison.Ordinal);

        var sdkVersion = sdkProps.Descendants("MauiBrowserSdkVersion").SingleOrDefault();
        Assert.NotNull(sdkVersion);
        Assert.Contains("Regex", (string?)sdkVersion!.Attribute("Condition"), StringComparison.Ordinal);
        Assert.DoesNotContain("0.0.1", sdkVersion.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void SdkTargets_DefinesImplicitRuntimePackageReference()
    {
        var sdkTargets = LoadSdkTargets();
        var packageReference = sdkTargets.Descendants("PackageReference")
            .SingleOrDefault(e =>
                string.Equals((string?)e.Attribute("Include"), "$(MauiBrowserRuntimePackageId)", StringComparison.Ordinal) &&
                string.Equals((string?)e.Attribute("Version"), "$(MauiBrowserRuntimeVersion)", StringComparison.Ordinal));

        Assert.NotNull(packageReference);

        var itemGroupCondition = packageReference!.Parent?.Attribute("Condition")?.Value;
        Assert.NotNull(itemGroupCondition);
        Assert.Contains("MauiBrowserEnableImplicitRuntimeReference", itemGroupCondition, StringComparison.Ordinal);
    }

    [Fact]
    public void SdkTargets_DefinesRuntimeProjectReferenceOverride()
    {
        var sdkTargets = LoadSdkTargets();
        var projectReference = sdkTargets.Descendants("ProjectReference")
            .SingleOrDefault(e =>
                string.Equals((string?)e.Attribute("Include"), "$(MauiBrowserRuntimeProjectPath)", StringComparison.Ordinal));

        Assert.NotNull(projectReference);

        var itemGroupCondition = projectReference!.Parent?.Attribute("Condition")?.Value;
        Assert.NotNull(itemGroupCondition);
        Assert.Contains("MauiBrowserRuntimeProjectPath", itemGroupCondition, StringComparison.Ordinal);
    }

    [Fact]
    public void SdkTargets_DefinesTargetFrameworkValidationTarget()
    {
        var sdkTargets = LoadSdkTargets();
        var target = sdkTargets.Descendants("Target")
            .SingleOrDefault(e =>
                string.Equals((string?)e.Attribute("Name"), "ValidateMauiBrowserSdkTargetFramework", StringComparison.Ordinal));

        Assert.NotNull(target);
        Assert.Contains(target!.Elements("Error"), e =>
            ((string?)e.Attribute("Condition"))?.Contains("TargetFrameworks", StringComparison.Ordinal) == true);
        Assert.Contains(target.Elements("Error"), e =>
            ((string?)e.Attribute("Condition"))?.Contains("TargetFramework", StringComparison.Ordinal) == true);
    }

    private static XDocument LoadSdkProps()
    {
        var path = Path.Combine(FindRepositoryRoot(), "src", "MauiBrowser.Sdk", "Sdk", "Sdk.props");
        return XDocument.Load(path);
    }

    private static XDocument LoadSdkTargets()
    {
        var path = Path.Combine(FindRepositoryRoot(), "src", "MauiBrowser.Sdk", "Sdk", "Sdk.targets");
        return XDocument.Load(path);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "MauiBrowser.sln");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root from test output directory.");
    }
}
