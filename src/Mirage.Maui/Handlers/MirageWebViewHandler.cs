using System.Reflection;
using System.Text.Json;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageWebViewHandler : MirageViewHandler<IWebView>, IWebViewHandler, IMirageRuntimeEventTarget
{
    private static readonly TimeSpan RuntimeConfigureRetryDelay = TimeSpan.FromMilliseconds(16);
    internal static readonly TimeSpan SlowRuntimeConfigureRetryDelay = TimeSpan.FromMilliseconds(250);
    internal const int MaxFastRuntimeConfigureRetryAttempts = 60;
    private const double DefaultWidth = 300;
    private const double DefaultHeight = 150;
    private const string SourceKindNone = "none";
    private const string SourceKindUrl = "url";
    private const string SourceKindHtml = "html";
    private const string SourceKindUnsupported = "unsupported";
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IWebView.Source),
        nameof(IWebView.UserAgent)
    ];

    internal static readonly string[] ParityCommandKeys =
    [
        "GoBack",
        "GoForward",
        "Reload",
        "Eval",
        "EvaluateJavaScriptAsync"
    ];

    public static IPropertyMapper<IWebView, MirageWebViewHandler> Mapper =
        new PropertyMapper<IWebView, MirageWebViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IWebView.Source)] = MapSource,
            [nameof(IWebView.UserAgent)] = MapUserAgent
        };

    public static CommandMapper<IWebView, MirageWebViewHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            ["GoBack"] = MapGoBack,
            ["GoForward"] = MapGoForward,
            ["Reload"] = MapReload,
            ["Eval"] = MapEval,
            ["EvaluateJavaScriptAsync"] = MapEvaluateJavaScriptAsync
        };

    private readonly Dictionary<long, PendingEvaluationRequestBridge> _pendingEvaluationRequests = new();
    private bool _isConnected;
    private bool _committedSourceInitialized;
    private bool _runtimeConfigureRetryQueued;
    private int _consecutiveRuntimeConfigureFailures;
    private long _nextEvaluationRequestId;
    private long _sourceVersion;
    private WebNavigationEvent _pendingNavigationEvent = WebNavigationEvent.NewPage;
    private WebViewRuntimeConfiguration _committedSourceConfiguration = BuildRuntimeConfigurationCore(source: null, userAgent: null, sourceVersion: 0);
    private string _currentUrl = "about:blank";
    private bool _canGoBack;
    private bool _canGoForward;
    private string _evalCapability = "unknown";
    private string _lastNavigationResult = "idle";

    public MirageWebViewHandler()
        : base(ComponentKind.WebView, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    IWebView IWebViewHandler.VirtualView => VirtualView;

    object IWebViewHandler.PlatformView => PlatformView;

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return ResolveMeasuredSizeWithConstraints(VirtualView, widthConstraint, heightConstraint, DefaultWidth, DefaultHeight);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        _isConnected = true;
        EnsureCommittedSourceInitialized();
        platformView.SetStyle("display", DefaultDisplayStyle);
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetStyle("border", "0");

        var changed = ApplyWebViewProjectionCore(VirtualView);

        base.ConnectHandler(platformView);

        if (changed)
        {
            RequestRender();
        }

        ConfigureRuntime();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        _isConnected = false;
        ResetRuntimeConfigureRetryState();
        FailPendingEvaluations(new InvalidOperationException("WebView disconnected before JavaScript evaluation completed."));
        DisableRuntime();
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        switch (eventName)
        {
            case "navigating":
                HandleRuntimeNavigatingEvent(payload);
                break;
            case "navigated":
                HandleNavigated(payload);
                break;
            case "evalresult":
                HandleEvalResult(payload);
                break;
        }
    }

    public static void MapSource(MirageWebViewHandler handler, IWebView webView)
    {
        handler.ApplySourceChange(webView, WebNavigationEvent.NewPage);
    }

    public static void MapUserAgent(MirageWebViewHandler handler, IWebView webView)
    {
        var changed = handler.UpdateUserAgentProjection(webView);
        if (changed)
        {
            handler.RequestRender();
        }
    }

    public static void MapGoBack(MirageWebViewHandler handler, IWebView webView, object? args)
    {
        _ = webView;
        _ = args;
        handler.TryRequestNavigationCommand(WebNavigationEvent.Back, static (coordinator, nodeId) => coordinator.TryRequestNodeWebViewGoBack(nodeId));
    }

    public static void MapGoForward(MirageWebViewHandler handler, IWebView webView, object? args)
    {
        _ = webView;
        _ = args;
        handler.TryRequestNavigationCommand(WebNavigationEvent.Forward, static (coordinator, nodeId) => coordinator.TryRequestNodeWebViewGoForward(nodeId));
    }

    public static void MapReload(MirageWebViewHandler handler, IWebView webView, object? args)
    {
        _ = webView;
        _ = args;
        handler.TryRequestNavigationCommand(WebNavigationEvent.Refresh, static (coordinator, nodeId) => coordinator.TryRequestNodeWebViewReload(nodeId));
    }

    public static void MapEval(MirageWebViewHandler handler, IWebView webView, object? args)
    {
        _ = webView;
        if (!TryExtractScript(args, out var script))
        {
            return;
        }

        handler.TryEvaluateScript(script, captureResult: false, requestBridge: null);
    }

    public static void MapEvaluateJavaScriptAsync(MirageWebViewHandler handler, IWebView webView, object? args)
    {
        _ = webView;
        if (!TryExtractScript(args, out var script))
        {
            PendingEvaluationRequestBridge.Create(args)?.Fail(new InvalidOperationException("WebView EvaluateJavaScriptAsync command did not provide a script."));
            return;
        }

        handler.TryEvaluateScript(script, captureResult: true, requestBridge: PendingEvaluationRequestBridge.Create(args));
    }

    private bool ApplyWebViewProjectionCore(IWebView webView)
    {
        using var _ = BeginRenderBatch();

        var changed = UpdateSourceDiagnostics(webView.Source);
        changed |= UpdateUserAgentProjection(webView);
        changed |= UpdateNavigationStateProjection();
        changed |= UpdateEvaluationStateProjection();
        return changed;
    }

    private void ApplySourceChange(IWebView webView, WebNavigationEvent navigationEvent)
    {
        var changed = UpdateSourceDiagnostics(webView.Source);
        changed |= UpdateNavigationStateProjection();
        changed |= UpdateEvaluationStateProjection();

        var targetUrl = ResolveDiagnosticUrl(webView.Source);
        if (TrySendNavigating(navigationEvent, targetUrl))
        {
            _lastNavigationResult = "cancel";
            changed |= UpdateCommittedSourceDiagnostics();
            changed |= UpdateNavigationStateProjection();
            if (changed)
            {
                RequestRender();
            }

            return;
        }

        _pendingNavigationEvent = navigationEvent;
        CommitSourceConfiguration(webView.Source, advanceSourceVersion: true);
        changed |= ConfigureRuntime();
        if (changed)
        {
            RequestRender();
        }
    }

    private bool ConfigureRuntime()
    {
        if (!_isConnected || Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return false;
        }

        EnsureCommittedSourceInitialized();
        var configuration = BuildCurrentRuntimeConfiguration(VirtualView.UserAgent);
        var configurationJson = JsonSerializer.Serialize(configuration);
        if (!Coordinator.TryConfigureNodeWebView(PlatformView.NodeId, enabled: true, configurationJson))
        {
            ScheduleRuntimeConfigurationRetry();
            return false;
        }

        ResetRuntimeConfigureRetryState();
        return true;
    }

    private void DisableRuntime()
    {
        if (Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return;
        }

        _ = Coordinator.TryConfigureNodeWebView(PlatformView.NodeId, enabled: false, "{}");
    }

    private bool TryRequestNavigationCommand(
        WebNavigationEvent navigationEvent,
        Func<IMirageRenderCoordinator, string, bool> request)
    {
        if (Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return false;
        }

        if (TrySendNavigating(navigationEvent, _currentUrl))
        {
            _lastNavigationResult = "cancel";
            if (UpdateNavigationStateProjection())
            {
                RequestRender();
            }

            return false;
        }

        var accepted = request(Coordinator, PlatformView.NodeId);
        if (!accepted)
        {
            ConfigureRuntime();
            accepted = request(Coordinator, PlatformView.NodeId);
        }

        if (!accepted)
        {
            return false;
        }

        _pendingNavigationEvent = navigationEvent;
        return true;
    }

    private void TryEvaluateScript(string script, bool captureResult, PendingEvaluationRequestBridge? requestBridge)
    {
        if (Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            requestBridge?.Fail(new InvalidOperationException("WebView runtime is unavailable."));
            return;
        }

        var requestId = captureResult ? Interlocked.Increment(ref _nextEvaluationRequestId) : 0;
        if (captureResult && requestBridge is not null)
        {
            _pendingEvaluationRequests[requestId] = requestBridge;
        }

        var accepted = Coordinator.TryEvaluateNodeWebViewScript(PlatformView.NodeId, requestId, script, captureResult);
        if (!accepted)
        {
            ConfigureRuntime();
            accepted = Coordinator.TryEvaluateNodeWebViewScript(PlatformView.NodeId, requestId, script, captureResult);
        }

        if (accepted)
        {
            return;
        }

        if (captureResult && _pendingEvaluationRequests.Remove(requestId, out var pending))
        {
            pending.Fail(new InvalidOperationException("WebView JavaScript evaluation was rejected by the browser runtime."));
        }
    }

    private void HandleNavigated(string? payload)
    {
        if (!TryDeserialize(payload, out WebViewNavigatedPayload? next) || next is null)
        {
            return;
        }

        _currentUrl = string.IsNullOrWhiteSpace(next.Url) ? ResolveDiagnosticUrl(VirtualView.Source) : next.Url!;
        CommitCommittedSourceForNavigatedPayload(next, _currentUrl);
        _canGoBack = next.CanGoBack;
        _canGoForward = next.CanGoForward;
        _evalCapability = next.CanEvaluateJavaScript ? "same-origin" : "restricted";
        _lastNavigationResult = string.IsNullOrWhiteSpace(next.Result) ? "success" : next.Result!;

        var changed = UpdateCommittedSourceDiagnostics();
        changed |= UpdateNavigationStateProjection();
        changed |= UpdateEvaluationStateProjection();
        if (changed)
        {
            RequestRender();
        }

        TrySendNavigated(_pendingNavigationEvent, _currentUrl, ResolveNavigationResult(next.Result));
        _pendingNavigationEvent = WebNavigationEvent.NewPage;
    }

    private void HandleRuntimeNavigatingEvent(string? payload)
    {
        if (!TryDeserialize(payload, out WebViewNavigatingPayload? next) || next is null)
        {
            return;
        }

        var url = string.IsNullOrWhiteSpace(next.Url)
            ? _currentUrl
            : next.Url!;
        var canceled = TrySendNavigating(WebNavigationEvent.NewPage, url);
        if (canceled)
        {
            _lastNavigationResult = "cancel";
            if (UpdateNavigationStateProjection())
            {
                RequestRender();
            }

            return;
        }

        if (Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return;
        }

        var accepted = Coordinator.TryRequestNodeWebViewNavigate(PlatformView.NodeId, url);
        if (!accepted)
        {
            ConfigureRuntime();
            accepted = Coordinator.TryRequestNodeWebViewNavigate(PlatformView.NodeId, url);
        }

        if (!accepted)
        {
            return;
        }

        _pendingNavigationEvent = WebNavigationEvent.NewPage;
    }

    private void HandleEvalResult(string? payload)
    {
        if (!TryDeserialize(payload, out WebViewEvalResultPayload? next) || next is null)
        {
            return;
        }

        if (next.RequestId <= 0 || !_pendingEvaluationRequests.Remove(next.RequestId, out var pending))
        {
            return;
        }

        if (next.Success)
        {
            pending.Succeed(next.Result);
            return;
        }

        pending.Fail(new InvalidOperationException(next.Error ?? "WebView JavaScript evaluation failed."));
    }

    private bool UpdateSourceDiagnostics(IWebViewSource? source)
    {
        var configuration = BuildRuntimeConfigurationCore(source, VirtualView.UserAgent, sourceVersion: 0);
        var changed = SetPropertyIfChanged("SourceKindState", configuration.SourceKind);
        changed |= SetPropertyIfChanged("CurrentUrlState", ResolveDiagnosticUrl(source));
        return changed;
    }

    private bool UpdateCommittedSourceDiagnostics()
    {
        return SetPropertyIfChanged("SourceKindState", _committedSourceConfiguration.SourceKind);
    }

    private bool UpdateUserAgentProjection(IWebView webView)
    {
        return SetPropertyIfChanged("UserAgentState", webView.UserAgent);
    }

    private bool UpdateNavigationStateProjection()
    {
        var changed = SetPropertyIfChanged("CurrentUrlState", _currentUrl);
        changed |= SetPropertyIfChanged("CanGoBackState", _canGoBack ? "true" : "false");
        changed |= SetPropertyIfChanged("CanGoForwardState", _canGoForward ? "true" : "false");
        changed |= SetPropertyIfChanged("LastNavigationResultState", _lastNavigationResult);
        return changed;
    }

    private bool UpdateEvaluationStateProjection()
    {
        return SetPropertyIfChanged("EvalCapabilityState", _evalCapability);
    }

    private void ScheduleRuntimeConfigurationRetry()
    {
        if (_runtimeConfigureRetryQueued || !_isConnected || Coordinator is null)
        {
            return;
        }

        _consecutiveRuntimeConfigureFailures++;
        var delay = _consecutiveRuntimeConfigureFailures <= MaxFastRuntimeConfigureRetryAttempts
            ? RuntimeConfigureRetryDelay
            : SlowRuntimeConfigureRetryDelay;
        if (!Coordinator.DispatchDelayed(delay, FlushRuntimeConfigurationRetry))
        {
            return;
        }

        _runtimeConfigureRetryQueued = true;
    }

    private void FlushRuntimeConfigurationRetry()
    {
        _runtimeConfigureRetryQueued = false;
        if (!_isConnected)
        {
            return;
        }

        ConfigureRuntime();
    }

    private void ResetRuntimeConfigureRetryState()
    {
        _runtimeConfigureRetryQueued = false;
        _consecutiveRuntimeConfigureFailures = 0;
    }

    private void FailPendingEvaluations(Exception exception)
    {
        foreach (var pending in _pendingEvaluationRequests.Values)
        {
            pending.Fail(exception);
        }

        _pendingEvaluationRequests.Clear();
    }

    private bool TrySendNavigating(WebNavigationEvent navigationEvent, string? url)
    {
        try
        {
            return VirtualView.Navigating(navigationEvent, url ?? string.Empty);
        }
        catch (MissingMethodException)
        {
        }

        var method = VirtualView.GetType().GetMethod("SendNavigating", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, [typeof(WebNavigationEvent), typeof(string)]);
        if (method is null)
        {
            return false;
        }

        var result = method.Invoke(VirtualView, [navigationEvent, url ?? string.Empty]);
        return result is bool canceled && canceled;
    }

    private void TrySendNavigated(WebNavigationEvent navigationEvent, string? url, WebNavigationResult navigationResult)
    {
        TrySetNavigationProperty("CanGoBack", _canGoBack);
        TrySetNavigationProperty("CanGoForward", _canGoForward);

        try
        {
            VirtualView.Navigated(navigationEvent, url ?? string.Empty, navigationResult);
            return;
        }
        catch (MissingMethodException)
        {
        }

        var method = VirtualView.GetType().GetMethod("SendNavigated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, [typeof(WebNavigationEvent), typeof(string), typeof(WebNavigationResult)]);
        _ = method?.Invoke(VirtualView, [navigationEvent, url ?? string.Empty, navigationResult]);
    }

    private void TrySetNavigationProperty(string propertyName, bool value)
    {
        switch (propertyName)
        {
            case "CanGoBack":
                VirtualView.CanGoBack = value;
                return;
            case "CanGoForward":
                VirtualView.CanGoForward = value;
                return;
        }

        var property = VirtualView.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var setter = property?.SetMethod ?? property?.GetSetMethod(nonPublic: true);
        if (setter is not null)
        {
            setter.Invoke(VirtualView, [value]);
            return;
        }

        if (VirtualView is BindableObject bindable
            && TryResolveBindableProperty(propertyName, out var bindableProperty))
        {
            bindable.SetValue(bindableProperty!, value);
        }
    }

    private bool TryResolveBindableProperty(string propertyName, out BindableProperty? bindableProperty)
    {
        bindableProperty = null;

        var field = VirtualView.GetType().GetField(
            $"{propertyName}Property",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (field?.GetValue(null) is BindableProperty resolved)
        {
            bindableProperty = resolved;
            return true;
        }

        return false;
    }

    private bool SetPropertyIfChanged(string propertyName, string? value)
    {
        if (string.Equals(PlatformView.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetProperty(propertyName, value);
        return true;
    }

    private void EnsureCommittedSourceInitialized()
    {
        if (_committedSourceInitialized)
        {
            return;
        }

        CommitSourceConfiguration(VirtualView.Source, advanceSourceVersion: false);
    }

    private void CommitSourceConfiguration(IWebViewSource? source, bool advanceSourceVersion)
    {
        CommitRuntimeConfiguration(BuildRuntimeConfigurationCore(source, userAgent: null, sourceVersion: 0), advanceSourceVersion);
    }

    private void CommitRuntimeConfiguration(WebViewRuntimeConfiguration configuration, bool advanceSourceVersion)
    {
        _committedSourceConfiguration = configuration with
        {
            UserAgent = null,
            SourceVersion = 0
        };
        _committedSourceInitialized = true;
        if (advanceSourceVersion)
        {
            _sourceVersion++;
        }
    }

    private void CommitCommittedSourceForNavigatedPayload(WebViewNavigatedPayload payload, string? url)
    {
        if (string.Equals(payload.SourceKind, SourceKindHtml, StringComparison.Ordinal)
            || string.Equals(url, "about:srcdoc", StringComparison.Ordinal))
        {
            CommitSourceConfiguration(
                new HtmlWebViewSource
                {
                    Html = payload.Html ?? string.Empty
                },
                advanceSourceVersion: false);
            return;
        }

        if (string.Equals(payload.SourceKind, SourceKindUnsupported, StringComparison.Ordinal)
            || string.Equals(payload.SourceKind, SourceKindNone, StringComparison.Ordinal))
        {
            CommitRuntimeConfiguration(
                new WebViewRuntimeConfiguration(
                    Enabled: true,
                    SourceKind: payload.SourceKind!,
                    Url: string.IsNullOrWhiteSpace(url) ? "about:blank" : url,
                    Html: null,
                    UserAgent: null,
                    SourceVersion: 0),
                advanceSourceVersion: false);
            return;
        }

        CommitSourceConfiguration(
            string.IsNullOrWhiteSpace(url)
                ? null
                : new UrlWebViewSource { Url = url },
            advanceSourceVersion: false);
    }

    private WebViewRuntimeConfiguration BuildCurrentRuntimeConfiguration(string? userAgent)
    {
        return _committedSourceConfiguration with
        {
            UserAgent = userAgent,
            SourceVersion = _sourceVersion
        };
    }

    private static WebViewRuntimeConfiguration BuildRuntimeConfigurationCore(IWebViewSource? source, string? userAgent, long sourceVersion)
    {
        return source switch
        {
            UrlWebViewSource urlSource => new WebViewRuntimeConfiguration(
                Enabled: true,
                SourceKind: SourceKindUrl,
                Url: string.IsNullOrWhiteSpace(urlSource.Url) ? "about:blank" : urlSource.Url,
                Html: null,
                UserAgent: userAgent,
                SourceVersion: sourceVersion),
            HtmlWebViewSource htmlSource => new WebViewRuntimeConfiguration(
                Enabled: true,
                SourceKind: SourceKindHtml,
                Url: "about:srcdoc",
                Html: htmlSource.Html ?? string.Empty,
                UserAgent: userAgent,
                SourceVersion: sourceVersion),
            null => new WebViewRuntimeConfiguration(
                Enabled: true,
                SourceKind: SourceKindNone,
                Url: "about:blank",
                Html: null,
                UserAgent: userAgent,
                SourceVersion: sourceVersion),
            _ => new WebViewRuntimeConfiguration(
                Enabled: true,
                SourceKind: SourceKindUnsupported,
                Url: "about:blank",
                Html: null,
                UserAgent: userAgent,
                SourceVersion: sourceVersion)
        };
    }

    private static string ResolveDiagnosticUrl(IWebViewSource? source)
    {
        return source switch
        {
            UrlWebViewSource urlSource when !string.IsNullOrWhiteSpace(urlSource.Url) => urlSource.Url!,
            HtmlWebViewSource => "about:srcdoc",
            null => "about:blank",
            _ => "about:blank"
        };
    }

    private static WebNavigationResult ResolveNavigationResult(string? payload)
    {
        if (string.Equals(payload, "cancel", StringComparison.OrdinalIgnoreCase))
        {
            return WebNavigationResult.Cancel;
        }

        if (string.Equals(payload, "failure", StringComparison.OrdinalIgnoreCase))
        {
            return WebNavigationResult.Failure;
        }

        return WebNavigationResult.Success;
    }

    private static bool TryExtractScript(object? args, out string script)
    {
        if (args is string directScript && !string.IsNullOrWhiteSpace(directScript))
        {
            script = directScript;
            return true;
        }

        var scriptValue = GetMemberValue(args, "Script") as string;
        if (!string.IsNullOrWhiteSpace(scriptValue))
        {
            script = scriptValue!;
            return true;
        }

        script = string.Empty;
        return false;
    }

    private static object? GetMemberValue(object? instance, string name)
    {
        if (instance is null)
        {
            return null;
        }

        var type = instance.GetType();
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is not null)
        {
            return property.GetValue(instance);
        }

        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field?.GetValue(instance);
    }

    private static bool TryDeserialize<TPayload>(string? payload, out TPayload? value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<TPayload>(payload, PayloadJsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static Size ResolveMeasuredSizeWithConstraints(
        IView view,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        var (requestedWidth, requestedMinimumWidth, requestedMaximumWidth) = ResolveRequestedDimensionRequests(view, isWidth: true);
        var (requestedHeight, requestedMinimumHeight, requestedMaximumHeight) = ResolveRequestedDimensionRequests(view, isWidth: false);
        return new Size(
            ResolveConstraints(clampedWidth, requestedWidth, requestedMinimumWidth, requestedMaximumWidth),
            ResolveConstraints(clampedHeight, requestedHeight, requestedMinimumHeight, requestedMaximumHeight));
    }

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return Math.Max(0, fallback);
        }

        return Math.Max(0, Math.Min(constraint, fallback));
    }

    private static double ResolveConstraints(double measured, double exact, double min, double max)
    {
        var resolved = measured;
        var resolvedMin = ResolveMinimum(min);

        if (IsExplicitLengthSet(exact))
        {
            resolved = exact;
        }

        if (IsExplicitLengthSet(max) && resolved > max)
        {
            resolved = max;
        }

        if (resolved < resolvedMin)
        {
            resolved = resolvedMin;
        }

        return resolved;
    }

    private static double ResolveMinimum(double value)
    {
        return IsExplicitLengthSet(value)
            ? value
            : 0;
    }

    private static bool IsExplicitLengthSet(double value)
    {
        return double.IsFinite(value) && value >= 0;
    }

    private static (double Exact, double Minimum, double Maximum) ResolveRequestedDimensionRequests(IView view, bool isWidth)
    {
        if (view is not VisualElement element)
        {
            return (-1, -1, -1);
        }

        return isWidth
            ? (element.WidthRequest, element.MinimumWidthRequest, element.MaximumWidthRequest)
            : (element.HeightRequest, element.MinimumHeightRequest, element.MaximumHeightRequest);
    }

    internal sealed record WebViewRuntimeConfiguration(
        bool Enabled,
        string SourceKind,
        string? Url,
        string? Html,
        string? UserAgent,
        long SourceVersion);

    private sealed record WebViewNavigatedPayload(
        string? Url,
        string? SourceKind,
        string? Html,
        bool CanGoBack,
        bool CanGoForward,
        bool CanEvaluateJavaScript,
        string? Result);

    private sealed record WebViewNavigatingPayload(
        string? Url);

    private sealed record WebViewEvalResultPayload(
        long RequestId,
        bool Success,
        string? Result,
        string? Error);

    private sealed class PendingEvaluationRequestBridge
    {
        private readonly object _request;

        private PendingEvaluationRequestBridge(object request)
        {
            _request = request;
        }

        public static PendingEvaluationRequestBridge? Create(object? request)
        {
            return request is null ? null : new PendingEvaluationRequestBridge(request);
        }

        public void Succeed(string? result)
        {
            if (TryInvokeMember(_request, "SetResult", result))
            {
                return;
            }

            var completionSource = GetMemberValue(_request, "TaskCompletionSource")
                ?? GetMemberValue(_request, "CompletionSource");
            if (completionSource is not null && TryInvokeMember(completionSource, "SetResult", result))
            {
                return;
            }

            if (GetMemberValue(_request, "ResultCallback") is Delegate callback)
            {
                callback.DynamicInvoke(result);
            }
        }

        public void Fail(Exception exception)
        {
            if (TryInvokeMember(_request, "SetException", exception))
            {
                return;
            }

            var completionSource = GetMemberValue(_request, "TaskCompletionSource")
                ?? GetMemberValue(_request, "CompletionSource");
            if (completionSource is not null && TryInvokeMember(completionSource, "SetException", exception))
            {
                return;
            }

            if (GetMemberValue(_request, "ExceptionCallback") is Delegate callback)
            {
                callback.DynamicInvoke(exception);
            }
        }

        private static bool TryInvokeMember(object instance, string methodName, object? argument)
        {
            foreach (var method in instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                try
                {
                    method.Invoke(instance, [argument]);
                    return true;
                }
                catch
                {
                    // Try the next overload.
                }
            }

            return false;
        }
    }
}
