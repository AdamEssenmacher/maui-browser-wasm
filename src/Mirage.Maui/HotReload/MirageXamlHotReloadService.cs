using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mirage.Maui.Handlers;
using Mirage.Web.Dom;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace Mirage.Maui.HotReload;

internal sealed class MirageXamlHotReloadService : IDisposable
{
    private const string XamlResourceIdAttributeName = "Microsoft.Maui.Controls.Xaml.XamlResourceIdAttribute";

    private static readonly TimeSpan HintPollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan FullScanInterval = TimeSpan.FromSeconds(2);
    private static readonly PropertyInfo? LogicalChildrenProperty = typeof(Element).GetProperty(
        "LogicalChildren",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private readonly Application _application;
    private readonly IDispatcher _dispatcher;
    private readonly IMirageRenderCoordinator _renderCoordinator;
    private readonly Dictionary<string, TrackedXamlDocument> _documentsByPath = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _contentByPath = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _failedHashesByPath = new(StringComparer.Ordinal);

    private CancellationTokenSource? _cancellationSource;
    private DateTime _nextFullScanUtc;
    private int _scanRequested;

    public MirageXamlHotReloadService(
        Application application,
        IDispatcher dispatcher,
        IMirageRenderCoordinator renderCoordinator)
    {
        _application = application;
        _dispatcher = dispatcher;
        _renderCoordinator = renderCoordinator;
    }

    public void Start()
    {
        if (_cancellationSource is not null)
        {
            return;
        }

        DomElement.InstallHotReloadHintBridge();

        _nextFullScanUtc = DateTime.UtcNow;
        _cancellationSource = new CancellationTokenSource();
        _ = RunLoopAsync(_cancellationSource.Token);
    }

    public void RequestImmediateScan() => Interlocked.Exchange(ref _scanRequested, 1);

    public void Stop()
    {
        var cancellationSource = Interlocked.Exchange(ref _cancellationSource, null);
        if (cancellationSource is null)
        {
            return;
        }

        cancellationSource.Cancel();
        cancellationSource.Dispose();
    }

    public void Dispose() => Stop();

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var hints = DrainHotReloadHints();
                    var fullScanDue = DateTime.UtcNow >= _nextFullScanUtc;
                    var forceFullScan = fullScanDue || Interlocked.Exchange(ref _scanRequested, 0) == 1;

                    if (forceFullScan || hints.Count > 0)
                    {
                        await ScanAndApplyAsync(forceFullScan, hints, cancellationToken);
                    }

                    if (fullScanDue)
                    {
                        _nextFullScanUtc = DateTime.UtcNow + FullScanInterval;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // Keep the loop alive for future revisions and valid edits.
                }

                await Task.Delay(HintPollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task ScanAndApplyAsync(bool forceFullScan, IReadOnlyList<string> hints, CancellationToken cancellationToken)
    {
        RefreshTrackedDocuments(forceFullScan || _documentsByPath.Count == 0);

        var candidateDocuments = SelectCandidateDocuments(forceFullScan, hints);
        if (candidateDocuments.Count == 0)
        {
            return;
        }

        var changedDocuments = new List<ChangedXamlDocument>();
        foreach (var document in candidateDocuments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentXaml = await DomElement.FetchTextAsync(document.HotReloadPath);
            if (string.IsNullOrWhiteSpace(currentXaml))
            {
                continue;
            }

            if (!_contentByPath.TryGetValue(document.HotReloadPath, out var previousXaml))
            {
                _contentByPath[document.HotReloadPath] = currentXaml;
                continue;
            }

            if (string.Equals(previousXaml, currentXaml, StringComparison.Ordinal))
            {
                continue;
            }

            _contentByPath[document.HotReloadPath] = currentXaml;
            changedDocuments.Add(new ChangedXamlDocument(document, currentXaml));
        }

        if (changedDocuments.Count == 0)
        {
            return;
        }

        var appliedCount = await InvokeOnDispatcherAsync(() => ApplyXamlChanges(changedDocuments));
        if (appliedCount > 0)
        {
            _renderCoordinator.RequestRender();
        }
    }

    private int ApplyXamlChanges(IReadOnlyList<ChangedXamlDocument> changedDocuments)
    {
        var liveTree = CaptureLiveTree();
        var appliedCount = 0;

        foreach (var changedDocument in changedDocuments)
        {
            var contentHash = ComputeContentHash(changedDocument.XamlText);
            if (_failedHashesByPath.TryGetValue(changedDocument.Document.HotReloadPath, out var failedHash)
                && string.Equals(failedHash, contentHash, StringComparison.Ordinal))
            {
                continue;
            }

            var applyResult = ApplyXamlChange(changedDocument, liveTree);
            if (applyResult.AttemptCount > 0 && applyResult.SuccessCount == 0)
            {
                _failedHashesByPath[changedDocument.Document.HotReloadPath] = contentHash;
                continue;
            }

            if (applyResult.SuccessCount > 0)
            {
                _failedHashesByPath.Remove(changedDocument.Document.HotReloadPath);
                appliedCount += applyResult.SuccessCount;
            }
        }

        return appliedCount;
    }

    private static ApplyResult ApplyXamlChange(ChangedXamlDocument changedDocument, LiveTreeSnapshot liveTree)
    {
        if (typeof(ResourceDictionary).IsAssignableFrom(changedDocument.Document.TargetType))
        {
            var attempts = 0;
            var successes = 0;

            foreach (var dictionary in liveTree.ResourceDictionaries)
            {
                if (!changedDocument.Document.TargetType.IsInstanceOfType(dictionary))
                {
                    continue;
                }

                attempts++;
                if (MirageXamlHotReloadApplicator.TryApplyResourceDictionaryXaml(dictionary, changedDocument.XamlText, out _))
                {
                    successes++;
                }
            }

            return new ApplyResult(attempts, successes);
        }

        {
            var attempts = 0;
            var successes = 0;

            foreach (var element in liveTree.Elements)
            {
                if (!changedDocument.Document.TargetType.IsInstanceOfType(element))
                {
                    continue;
                }

                attempts++;
                if (MirageXamlHotReloadApplicator.TryApplyElementXaml(element, changedDocument.XamlText, out _))
                {
                    successes++;
                }
            }

            return new ApplyResult(attempts, successes);
        }
    }

    private LiveTreeSnapshot CaptureLiveTree()
    {
        var elements = new List<Element>();
        var visitedElements = new HashSet<Element>(ReferenceEqualityComparer.Instance);

        foreach (var window in _application.Windows)
        {
            if (window.Page is not Element rootElement)
            {
                continue;
            }

            TraverseElements(rootElement, visitedElements, elements);
        }

        var dictionaries = new List<ResourceDictionary>();
        var visitedDictionaries = new HashSet<ResourceDictionary>(ReferenceEqualityComparer.Instance);

        AddResourceDictionary(_application.Resources, visitedDictionaries, dictionaries);
        foreach (var element in elements)
        {
            AddResourceDictionary(TryGetResources(element), visitedDictionaries, dictionaries);
        }

        return new LiveTreeSnapshot(elements, dictionaries);
    }

    private static void TraverseElements(Element rootElement, HashSet<Element> visitedElements, List<Element> sink)
    {
        var stack = new Stack<Element>();
        stack.Push(rootElement);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visitedElements.Add(current))
            {
                continue;
            }

            sink.Add(current);
            foreach (var child in EnumerateLogicalChildren(current))
            {
                stack.Push(child);
            }
        }
    }

    private static IEnumerable<Element> EnumerateLogicalChildren(Element element)
    {
        if (LogicalChildrenProperty?.GetValue(element) is not IEnumerable logicalChildren)
        {
            yield break;
        }

        foreach (var child in logicalChildren)
        {
            if (child is Element typedChild)
            {
                yield return typedChild;
            }
        }
    }

    private static void AddResourceDictionary(
        ResourceDictionary? dictionary,
        HashSet<ResourceDictionary> visited,
        List<ResourceDictionary> sink)
    {
        if (dictionary is null || !visited.Add(dictionary))
        {
            return;
        }

        sink.Add(dictionary);

        foreach (var mergedDictionary in dictionary.MergedDictionaries)
        {
            AddResourceDictionary(mergedDictionary, visited, sink);
        }
    }

    private void RefreshTrackedDocuments(bool forceRefresh)
    {
        if (!forceRefresh && _documentsByPath.Count > 0)
        {
            return;
        }

        _documentsByPath.Clear();
        foreach (var document in DiscoverTrackedXamlDocuments())
        {
            _documentsByPath[document.HotReloadPath] = document;
        }

        var activePaths = _documentsByPath.Keys.ToHashSet(StringComparer.Ordinal);
        RemoveStaleEntries(_contentByPath, activePaths);
        RemoveStaleEntries(_failedHashesByPath, activePaths);
    }

    private IReadOnlyList<TrackedXamlDocument> SelectCandidateDocuments(bool forceFullScan, IReadOnlyList<string> hints)
    {
        if (_documentsByPath.Count == 0)
        {
            return Array.Empty<TrackedXamlDocument>();
        }

        if (forceFullScan || hints.Count == 0)
        {
            return _documentsByPath.Values.ToArray();
        }

        var normalizedHints = new HashSet<string>(StringComparer.Ordinal);
        var scanAll = false;

        foreach (var hint in hints)
        {
            var normalizedHint = NormalizeHintPath(hint);
            if (normalizedHint is null)
            {
                continue;
            }

            if (normalizedHint == "*")
            {
                scanAll = true;
                break;
            }

            normalizedHints.Add(normalizedHint);
        }

        if (scanAll || normalizedHints.Count == 0)
        {
            return _documentsByPath.Values.ToArray();
        }

        var matches = _documentsByPath.Values
            .Where(document => normalizedHints.Contains(document.HotReloadPath))
            .ToArray();

        return matches.Length > 0 ? matches : _documentsByPath.Values.ToArray();
    }

    private static IEnumerable<TrackedXamlDocument> DiscoverTrackedXamlDocuments()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                continue;
            }

            IList<CustomAttributeData> attributes;
            try
            {
                attributes = assembly.GetCustomAttributesData();
            }
            catch
            {
                continue;
            }

            foreach (var attribute in attributes)
            {
                if (!string.Equals(attribute.AttributeType.FullName, XamlResourceIdAttributeName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Count < 3)
                {
                    continue;
                }

                var relativePath = attribute.ConstructorArguments[1].Value as string;
                var targetType = attribute.ConstructorArguments[2].Value as Type;

                var normalizedRelativePath = NormalizeRelativePath(relativePath);
                if (normalizedRelativePath is null || targetType is null)
                {
                    continue;
                }

                var hotReloadPath = $"/_hotreload/{assemblyName}/{normalizedRelativePath}";
                yield return new TrackedXamlDocument(targetType, hotReloadPath);
            }
        }
    }

    private static IReadOnlyList<string> DrainHotReloadHints()
    {
        var hintsJson = DomElement.DrainHotReloadHintsJson();
        if (string.IsNullOrWhiteSpace(hintsJson) || string.Equals(hintsJson, "[]", StringComparison.Ordinal))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(hintsJson) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string? NormalizeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalized = path.Replace('\\', '/').Trim();
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = normalized.TrimStart('/');
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeHintPath(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return null;
        }

        var normalized = hint.Trim();
        if (normalized == "*")
        {
            return normalized;
        }

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
        {
            normalized = absoluteUri.AbsolutePath;
        }

        var queryIndex = normalized.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            normalized = normalized[..queryIndex];
        }

        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized;
        }

        return normalized;
    }

    private static string ComputeContentHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void RemoveStaleEntries(Dictionary<string, string> state, HashSet<string> activePaths)
    {
        foreach (var key in state.Keys.ToArray())
        {
            if (!activePaths.Contains(key))
            {
                state.Remove(key);
            }
        }
    }

    private Task<T> InvokeOnDispatcherAsync<T>(Func<T> callback)
    {
        if (!_dispatcher.IsDispatchRequired)
        {
            return Task.FromResult(callback());
        }

        var completionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_dispatcher.Dispatch(() =>
            {
                try
                {
                    completionSource.SetResult(callback());
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
            }))
        {
            completionSource.SetException(new InvalidOperationException("Failed to dispatch hot reload update."));
        }

        return completionSource.Task;
    }

    private static ResourceDictionary? TryGetResources(Element element)
    {
        var resourcesProperty = element.GetType().GetProperty(
            "Resources",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return resourcesProperty?.GetValue(element) as ResourceDictionary;
    }

    private sealed record TrackedXamlDocument(Type TargetType, string HotReloadPath);

    private sealed record ChangedXamlDocument(TrackedXamlDocument Document, string XamlText);

    private sealed record ApplyResult(int AttemptCount, int SuccessCount);

    private sealed record LiveTreeSnapshot(
        IReadOnlyList<Element> Elements,
        IReadOnlyList<ResourceDictionary> ResourceDictionaries);
}
