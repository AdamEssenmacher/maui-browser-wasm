using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageBundledImageBuildPipelineTests
{
    [Fact]
    public void BrowserHostBuild_EmitsBundledImageManifestAndPublishedAssets()
    {
        var repoRoot = FindRepoRoot();
        var projectPath = Path.Combine(repoRoot, "samples", "ControlsSample.Fluent", "ControlsSample.Fluent.csproj");

        var build = RunProcess(
            fileName: "dotnet",
            arguments: $"build \"{projectPath}\" --no-restore --disable-build-servers -nologo",
            workingDirectory: repoRoot);
        Assert.Equal(0, build.ExitCode);

        var manifestPath = Path.Combine(repoRoot, "samples", "ControlsSample.Fluent", "wwwroot", "_mirage", "assets", "images.manifest.v1.json");
        Assert.True(File.Exists(manifestPath), $"Expected bundled-image manifest file: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("version").GetInt32());

        var images = root.GetProperty("images");
        Assert.True(
            images.TryGetProperty("ControlsSample.Shared:dotnet_bot.png", out var dotnetBotUrl),
            "Expected transitive shared-project image identity 'ControlsSample.Shared:dotnet_bot.png' in manifest.");

        var imageUrl = dotnetBotUrl.GetString();
        Assert.False(string.IsNullOrWhiteSpace(imageUrl));
        Assert.StartsWith("/_mirage/assets/ControlsSample.Shared/", imageUrl, StringComparison.Ordinal);

        var imageDiskPath = Path.Combine(repoRoot, "samples", "ControlsSample.Fluent", "wwwroot", imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(imageDiskPath), $"Expected mirrored bundled-image asset file: {imageDiskPath}");
    }

    [Fact]
    public void MirageBundledImageTarget_IdentityCollisionWithDifferentSources_FailsDeterministically()
    {
        var fixture = CreateTaskFixture(
        [
            new BundledImageInput(
                SourceFileRelativePath: "ProjectA/Assets/logo.png",
                SourceContent: "project-a",
                OriginAssemblyName: "SharedAssets",
                OriginProjectRelativePath: "ProjectA/ProjectA.csproj",
                LogicalImagePath: "Assets/logo.png"),
            new BundledImageInput(
                SourceFileRelativePath: "ProjectB/Assets/logo.png",
                SourceContent: "project-b",
                OriginAssemblyName: "SharedAssets",
                OriginProjectRelativePath: "ProjectB/ProjectB.csproj",
                LogicalImagePath: "Assets/logo.png")
        ]);

        try
        {
            var result = RunProcess(
                fileName: "dotnet",
                arguments: $"msbuild \"{fixture.ProjectPath}\" -t:Run -nologo -nodeReuse:false",
                workingDirectory: fixture.RootDirectory);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("identity collision", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDeleteDirectory(fixture.RootDirectory);
        }
    }

    [Fact]
    public void MirageBundledImageTarget_OutputPathCollision_FailsDeterministically()
    {
        var fixture = CreateTaskFixture(
        [
            new BundledImageInput(
                SourceFileRelativePath: "ProjectA/logo.png",
                SourceContent: "shared-content",
                OriginAssemblyName: "Alpha/Beta",
                OriginProjectRelativePath: "ProjectA/ProjectA.csproj",
                LogicalImagePath: "logo.png"),
            new BundledImageInput(
                SourceFileRelativePath: "ProjectB/Beta/logo.png",
                SourceContent: "shared-content",
                OriginAssemblyName: "Alpha",
                OriginProjectRelativePath: "ProjectB/ProjectB.csproj",
                LogicalImagePath: "Beta/logo.png")
        ]);

        try
        {
            var result = RunProcess(
                fileName: "dotnet",
                arguments: $"msbuild \"{fixture.ProjectPath}\" -t:Run -nologo -nodeReuse:false",
                workingDirectory: fixture.RootDirectory);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("output-path collision", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDeleteDirectory(fixture.RootDirectory);
        }
    }

    private static TaskFixture CreateTaskFixture(IReadOnlyList<BundledImageInput> inputs)
    {
        var repoRoot = FindRepoRoot();
        var rootDirectory = Path.Combine(Path.GetTempPath(), "mirage-bundled-image-task-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        var projectPath = Path.Combine(rootDirectory, "TaskFixture.proj");
        var targetsPath = Path.Combine(repoRoot, "Directory.Build.targets");
        var itemXml = new List<string>();

        foreach (var input in inputs)
        {
            var sourcePath = Path.Combine(rootDirectory, input.SourceFileRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var sourceDirectory = Path.GetDirectoryName(sourcePath);
            if (!string.IsNullOrWhiteSpace(sourceDirectory))
            {
                Directory.CreateDirectory(sourceDirectory);
            }

            File.WriteAllText(sourcePath, input.SourceContent);

            var originProjectPath = Path.Combine(rootDirectory, input.OriginProjectRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var originProjectDirectory = Path.GetDirectoryName(originProjectPath);
            if (!string.IsNullOrWhiteSpace(originProjectDirectory))
            {
                Directory.CreateDirectory(originProjectDirectory);
            }

            if (!File.Exists(originProjectPath))
            {
                File.WriteAllText(originProjectPath, "<Project />");
            }

            itemXml.Add(
                $$"""
                        <InputImage Include="{{EscapeXml(input.SourceFileRelativePath)}}">
                          <SourceFullPath>{{EscapeXml(sourcePath)}}</SourceFullPath>
                          <OriginAssemblyName>{{EscapeXml(input.OriginAssemblyName)}}</OriginAssemblyName>
                          <OriginProjectPath>{{EscapeXml(originProjectPath)}}</OriginProjectPath>
                          <LogicalImagePath>{{EscapeXml(input.LogicalImagePath)}}</LogicalImagePath>
                        </InputImage>
                """);
        }

        var itemBlock = string.Join(Environment.NewLine, itemXml);
        File.WriteAllText(
            projectPath,
            $$"""
              <Project>
                <Import Project="{{EscapeXml(targetsPath)}}" />
                <ItemGroup>
              {{itemBlock}}
                </ItemGroup>
                <Target Name="Run">
                  <MirageComputeBundledImageMetadata InputImages="@(InputImage)">
                    <Output TaskParameter="BundledImages" ItemName="_Bundled" />
                  </MirageComputeBundledImageMetadata>
                  <Message Importance="high" Text="BundledCount=@(_Bundled->Count())" />
                </Target>
              </Project>
              """);

        return new TaskFixture(rootDirectory, projectPath);
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }

    private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.Environment["DOTNET_CLI_DISABLE_PERSISTENT_BUILD_SERVERS"] = "1";

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(180_000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best effort
            }

            throw new TimeoutException($"Timed out executing: {fileName} {arguments}");
        }

        Task.WaitAll(stdoutTask, stderrTask);
        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;

        return new ProcessResult(process.ExitCode, stdout + Environment.NewLine + stderr);
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

        throw new DirectoryNotFoundException("Unable to find repository root containing Mirage.sln.");
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup for temp fixture directories.
        }
    }

    private sealed record BundledImageInput(
        string SourceFileRelativePath,
        string SourceContent,
        string OriginAssemblyName,
        string OriginProjectRelativePath,
        string LogicalImagePath);

    private sealed record TaskFixture(string RootDirectory, string ProjectPath);

    private sealed record ProcessResult(int ExitCode, string Output);
}
