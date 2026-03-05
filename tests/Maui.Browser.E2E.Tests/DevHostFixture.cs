using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed class DevHostFixture : IAsyncLifetime
{
    private readonly ConcurrentQueue<string> _hostLogs = new();
    private Process? _devHostProcess;

    public IPlaywright? Playwright { get; private set; }

    public IBrowser? Browser { get; private set; }

    public Uri? HttpAppUrl { get; private set; }

    public async Task InitializeAsync()
    {
        HttpAppUrl = await StartDevHostAsync();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--ignore-certificate-errors"]
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
            Browser = null;
        }

        Playwright?.Dispose();
        Playwright = null;

        if (_devHostProcess is not null && !_devHostProcess.HasExited)
        {
            _devHostProcess.Kill(entireProcessTree: true);
            await _devHostProcess.WaitForExitAsync();
        }

        _devHostProcess?.Dispose();
        _devHostProcess = null;
    }

    public async Task<IBrowserContext> NewContextAsync(int width = 1280, int height = 900)
    {
        if (Browser is null)
        {
            throw new InvalidOperationException("Playwright browser is not initialized.");
        }

        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize
            {
                Width = width,
                Height = height
            }
        });
    }

    public async Task<string> GetXamlAsync(string relativePath)
    {
        var appUrl = HttpAppUrl ?? throw new InvalidOperationException("Fixture app URL is not initialized.");
        using var client = new HttpClient { BaseAddress = appUrl };
        var encodedPath = Uri.EscapeDataString(relativePath);
        var response = await client.GetAsync($"/_dev/xaml?path={encodedPath}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task EmitSyntheticXamlChangeAsync(string relativePath, string? xamlOverride)
    {
        var appUrl = HttpAppUrl ?? throw new InvalidOperationException("Fixture app URL is not initialized.");
        using var client = new HttpClient { BaseAddress = appUrl };

        var response = await client.PostAsJsonAsync("/_dev/emit-xaml-change", new
        {
            relativePath,
            xamlOverride
        });

        response.EnsureSuccessStatusCode();
    }

    private async Task<Uri> StartDevHostAsync()
    {
        var repoRoot = FindRepoRoot();
        var projectPath = Path.Combine(repoRoot, "src", "Maui.Browser.Sample.DevHost", "Maui.Browser.Sample.DevHost.csproj");

        var urlSource = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --no-launch-profile",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = repoRoot
        };

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, args) => ProcessHostLine(args.Data, urlSource);
        process.ErrorDataReceived += (_, args) => ProcessHostLine(args.Data, urlSource);
        process.Exited += (_, _) =>
        {
            if (urlSource.Task.IsCompleted)
            {
                return;
            }

            urlSource.TrySetException(new InvalidOperationException(
                $"Dev host exited before publishing a URL. ExitCode={process.ExitCode}.\n{RenderHostLogs()}"));
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start dev host process.");
        }

        _devHostProcess = process;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        using var _ = timeout.Token.Register(() =>
            urlSource.TrySetException(new TimeoutException($"Timed out waiting for dev host URL.\n{RenderHostLogs()}")));

        return await urlSource.Task;
    }

    private void ProcessHostLine(string? line, TaskCompletionSource<Uri> urlSource)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _hostLogs.Enqueue(line);

        const string listeningPrefix = "Now listening on:";
        var markerIndex = line.IndexOf(listeningPrefix, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return;
        }

        var urlText = line[(markerIndex + listeningPrefix.Length)..].Trim();
        if (Uri.TryCreate(urlText, UriKind.Absolute, out var uri))
        {
            urlSource.TrySetResult(uri);
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "MauiDomWasm.sln");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root containing MauiDomWasm.sln.");
    }

    private string RenderHostLogs()
    {
        var sb = new StringBuilder();
        foreach (var line in _hostLogs.TakeLast(200))
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
