using System.Reflection;
using System.Text.Json;
using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageWebViewHandlerTests
{
    [Fact]
    public void WebViewHandler_ProjectsUrlAndHtmlSources_AndUserAgentDiagnostics()
    {
        var webView = new WebView
        {
            AutomationId = "controls-sample-webview-parity-host",
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" },
            UserAgent = "MirageWebViewParityProbe/1.0"
        };

        var (handler, _, _, runtime) = CreateAttachedHandler(webView);

        Assert.Equal(ComponentKind.WebView, handler.PlatformView.ControlKind);
        Assert.Equal("block", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("url", handler.PlatformView.GetPropertyString("SourceKindState"));
        Assert.Equal("MirageWebViewParityProbe/1.0", handler.PlatformView.GetPropertyString("UserAgentState"));

        using (var initialConfig = JsonDocument.Parse(runtime.LastConfigurationJson))
        {
            Assert.Equal("url", initialConfig.RootElement.GetProperty("SourceKind").GetString());
            Assert.Equal("/webview-probe/page-a.html", initialConfig.RootElement.GetProperty("Url").GetString());
        }

        webView.Source = new HtmlWebViewSource
        {
            Html = "<!doctype html><html><body data-probe='inline'>Inline Probe</body></html>"
        };
        MirageWebViewHandler.Mapper.UpdateProperty(handler, webView, nameof(IWebView.Source));

        Assert.Equal("html", handler.PlatformView.GetPropertyString("SourceKindState"));
        using var updatedConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        Assert.Equal("html", updatedConfig.RootElement.GetProperty("SourceKind").GetString());
        Assert.Contains("Inline Probe", updatedConfig.RootElement.GetProperty("Html").GetString(), StringComparison.Ordinal);
        Assert.Equal("about:srcdoc", updatedConfig.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void WebViewHandler_UserAgentDiagnostics_DoNotReconfigureIframeRuntime()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };

        var (handler, _, _, runtime) = CreateAttachedHandler(webView);
        var configureCount = runtime.Configurations.Count;

        webView.UserAgent = "MirageWebViewParityProbe/1.0";
        MirageWebViewHandler.Mapper.UpdateProperty(handler, webView, nameof(IWebView.UserAgent));

        Assert.Equal(configureCount, runtime.Configurations.Count);
        Assert.Equal("MirageWebViewParityProbe/1.0", handler.PlatformView.GetPropertyString("UserAgentState"));
    }

    [Fact]
    public void WebViewHandler_ReapplyingSameSource_AdvancesRuntimeSourceVersion()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };

        var (handler, _, _, runtime) = CreateAttachedHandler(webView);
        using var initialConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        var initialSourceVersion = initialConfig.RootElement.GetProperty("SourceVersion").GetInt64();

        webView.Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" };
        MirageWebViewHandler.Mapper.UpdateProperty(handler, webView, nameof(IWebView.Source));

        using var reappliedConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        var reappliedSourceVersion = reappliedConfig.RootElement.GetProperty("SourceVersion").GetInt64();
        Assert.True(reappliedSourceVersion > initialSourceVersion);
        Assert.Equal("/webview-probe/page-a.html", reappliedConfig.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void WebViewHandler_CanceledSourceChange_DoesNotReplayOnLaterRuntimeConfigure()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };
        webView.Navigating += static (_, args) => args.Cancel = true;

        var (handler, _, _, runtime) = CreateAttachedHandler(webView);
        using var initialConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        var initialSourceVersion = initialConfig.RootElement.GetProperty("SourceVersion").GetInt64();

        webView.Source = new UrlWebViewSource { Url = "/webview-probe/page-b.html" };
        MirageWebViewHandler.Mapper.UpdateProperty(handler, webView, nameof(IWebView.Source));

        Assert.Single(runtime.Configurations);
        Assert.Equal("cancel", handler.PlatformView.GetPropertyString("LastNavigationResultState"));

        var configured = InvokeConfigureRuntime(handler);

        Assert.Equal(2, runtime.Configurations.Count);
        Assert.True(configured);

        using var replayConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        Assert.Equal(initialSourceVersion, replayConfig.RootElement.GetProperty("SourceVersion").GetInt64());
        Assert.Equal("/webview-probe/page-a.html", replayConfig.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void WebViewHandler_UserNavigatedPage_IsPreservedAcrossLaterRuntimeConfigure()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(webView);
        using var initialConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        var initialSourceVersion = initialConfig.RootElement.GetProperty("SourceVersion").GetInt64();

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "navigated",
            """{"url":"/webview-probe/page-b.html","canGoBack":true,"canGoForward":false,"canEvaluateJavaScript":true,"result":"success"}""");

        var configured = InvokeConfigureRuntime(handler);

        Assert.True(configured);
        using var replayConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        Assert.Equal(initialSourceVersion, replayConfig.RootElement.GetProperty("SourceVersion").GetInt64());
        Assert.Equal("/webview-probe/page-b.html", replayConfig.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void WebViewHandler_InFrameNavigation_UpdatesSourceKindDiagnosticsToLiveDocument()
    {
        var webView = new WebView
        {
            Source = new HtmlWebViewSource
            {
                Html = "<!doctype html><html><body>Inline Probe</body></html>"
            }
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(webView);

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "navigated",
            """{"url":"/webview-probe/page-a.html","canGoBack":true,"canGoForward":false,"canEvaluateJavaScript":true,"result":"success"}""");

        Assert.Equal("url", handler.PlatformView.GetPropertyString("SourceKindState"));
        Assert.Equal("/webview-probe/page-a.html", handler.PlatformView.GetPropertyString("CurrentUrlState"));

        var configured = InvokeConfigureRuntime(handler);

        Assert.True(configured);
        using var replayConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        Assert.Equal("url", replayConfig.RootElement.GetProperty("SourceKind").GetString());
        Assert.Equal("/webview-probe/page-a.html", replayConfig.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void WebViewHandler_HistoricalInlineEntry_IsPreservedAcrossLaterRuntimeConfigure()
    {
        const string inlineHtml = "<!doctype html><html><body>Inline A</body></html>";
        var webView = new WebView
        {
            Source = new HtmlWebViewSource
            {
                Html = inlineHtml
            }
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(webView);

        webView.Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" };
        MirageWebViewHandler.Mapper.UpdateProperty(handler, webView, nameof(IWebView.Source));

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "navigated",
            $$"""{"url":"about:srcdoc","sourceKind":"html","html":{{JsonSerializer.Serialize(inlineHtml)}},"canGoBack":false,"canGoForward":true,"canEvaluateJavaScript":true,"result":"success"}""");

        Assert.Equal("html", handler.PlatformView.GetPropertyString("SourceKindState"));
        Assert.Equal("about:srcdoc", handler.PlatformView.GetPropertyString("CurrentUrlState"));

        var configured = InvokeConfigureRuntime(handler);

        Assert.True(configured);
        using var replayConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        Assert.Equal("html", replayConfig.RootElement.GetProperty("SourceKind").GetString());
        Assert.Contains("Inline A", replayConfig.RootElement.GetProperty("Html").GetString(), StringComparison.Ordinal);
        Assert.Equal("about:srcdoc", replayConfig.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void WebViewHandler_RuntimeNavigatingRequest_CanCancelInFrameLinkNavigation()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };
        var navigatingCount = 0;
        string? lastNavigatingUrl = null;
        webView.Navigating += (_, args) =>
        {
            navigatingCount++;
            lastNavigatingUrl = args.Url;
            args.Cancel = true;
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(webView);
        var baselineNavigatingCount = navigatingCount;

        coordinator.OnEvent(handler.PlatformView.NodeId, "navigating", """{"url":"/webview-probe/page-b.html"}""");

        Assert.Equal(baselineNavigatingCount + 1, navigatingCount);
        Assert.Equal("/webview-probe/page-b.html", lastNavigatingUrl);
        Assert.Equal("cancel", handler.PlatformView.GetPropertyString("LastNavigationResultState"));
        Assert.Equal(0, runtime.NavigateCallCount);
    }

    [Fact]
    public void WebViewHandler_RuntimeNavigatingEvent_RequestsInFrameNavigationWhenAllowed()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(webView);

        coordinator.OnEvent(handler.PlatformView.NodeId, "navigating", """{"url":"/webview-probe/page-b.html"}""");

        Assert.Equal(1, runtime.NavigateCallCount);
        Assert.Equal("/webview-probe/page-b.html", runtime.LastNavigateUrl);
    }

    [Fact]
    public void WebViewHandler_UnsupportedSourceKind_RemainsUnsupportedAfterNavigatedAndReconfigure()
    {
        var webView = new WebView
        {
            Source = new UnsupportedWebViewSource()
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(webView);
        using (var initialConfig = JsonDocument.Parse(runtime.LastConfigurationJson))
        {
            Assert.Equal("unsupported", initialConfig.RootElement.GetProperty("SourceKind").GetString());
        }

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "navigated",
            """{"url":"about:blank","sourceKind":"unsupported","html":null,"canGoBack":false,"canGoForward":false,"canEvaluateJavaScript":false,"result":"success"}""");

        Assert.Equal("unsupported", handler.PlatformView.GetPropertyString("SourceKindState"));

        var configured = InvokeConfigureRuntime(handler);

        Assert.True(configured);
        using var replayConfig = JsonDocument.Parse(runtime.LastConfigurationJson);
        Assert.Equal("unsupported", replayConfig.RootElement.GetProperty("SourceKind").GetString());
        Assert.Equal("about:blank", replayConfig.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void WebViewHandler_NavigationCommands_AndRuntimeEvents_UpdateManagedState()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(webView);

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "navigated",
            """{"url":"/webview-probe/page-b.html","canGoBack":true,"canGoForward":true,"canEvaluateJavaScript":true,"result":"success"}""");

        MirageWebViewHandler.MapGoBack(handler, webView, args: null);
        MirageWebViewHandler.MapGoForward(handler, webView, args: null);
        MirageWebViewHandler.MapReload(handler, webView, args: null);

        Assert.Equal(1, runtime.GoBackCallCount);
        Assert.Equal(1, runtime.GoForwardCallCount);
        Assert.Equal(1, runtime.ReloadCallCount);

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "navigated",
            """{"url":"/webview-probe/page-b.html","canGoBack":true,"canGoForward":false,"canEvaluateJavaScript":true,"result":"success"}""");

        Assert.Equal("/webview-probe/page-b.html", handler.PlatformView.GetPropertyString("CurrentUrlState"));
        Assert.Equal("true", handler.PlatformView.GetPropertyString("CanGoBackState"));
        Assert.Equal("false", handler.PlatformView.GetPropertyString("CanGoForwardState"));
        Assert.Equal("same-origin", handler.PlatformView.GetPropertyString("EvalCapabilityState"));
        Assert.Equal("success", handler.PlatformView.GetPropertyString("LastNavigationResultState"));
        Assert.True(webView.CanGoBack);
        Assert.False(webView.CanGoForward);
    }

    [Fact]
    public void WebViewHandler_UnavailableBackForwardCommands_DoNotRaiseNavigating()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };
        var navigatingCount = 0;
        webView.Navigating += (_, _) => navigatingCount++;

        var (handler, _, _, runtime) = CreateAttachedHandler(webView);
        var baselineNavigatingCount = navigatingCount;

        MirageWebViewHandler.MapGoBack(handler, webView, args: null);
        MirageWebViewHandler.MapGoForward(handler, webView, args: null);

        Assert.Equal(baselineNavigatingCount, navigatingCount);
        Assert.Equal(0, runtime.GoBackCallCount);
        Assert.Equal(0, runtime.GoForwardCallCount);
    }

    [Fact]
    public void WebViewHandler_NavigatedEvent_SeesUpdatedCanGoBackAndCanGoForwardState()
    {
        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };

        var capturedCanGoBack = false;
        var capturedCanGoForward = true;
        webView.Navigated += (_, _) =>
        {
            capturedCanGoBack = webView.CanGoBack;
            capturedCanGoForward = webView.CanGoForward;
        };

        var (handler, _, coordinator, _) = CreateAttachedHandler(webView);

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "navigated",
            """{"url":"/webview-probe/page-b.html","canGoBack":true,"canGoForward":false,"canEvaluateJavaScript":true,"result":"success"}""");

        Assert.True(capturedCanGoBack);
        Assert.False(capturedCanGoForward);
    }

    [Fact]
    public void WebViewHandler_EvaluateJavaScriptAsync_CompletesResult_AndCrossOriginFailure()
    {
        var webView = new WebView
        {
            Source = new HtmlWebViewSource { Html = "<html><body>Inline Probe</body></html>" }
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(webView);

        var successRequest = new FakeJavaScriptRequest
        {
            Script = "window.webViewProbeToken ?? 'missing'"
        };

        MirageWebViewHandler.MapEvaluateJavaScriptAsync(handler, webView, successRequest);

        Assert.Equal("window.webViewProbeToken ?? 'missing'", runtime.LastEvalScript);
        Assert.True(runtime.LastEvalCaptureResult);
        Assert.True(runtime.LastEvalRequestId > 0);

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "evalresult",
            $$"""{"requestId":{{runtime.LastEvalRequestId}},"success":true,"result":"inline","error":null}""");

        Assert.Equal("inline", successRequest.Result);
        Assert.Null(successRequest.Exception);

        var failureRequest = new FakeJavaScriptRequest
        {
            Script = "window.webViewProbeToken"
        };

        MirageWebViewHandler.MapEvaluateJavaScriptAsync(handler, webView, failureRequest);
        var failureRequestId = runtime.LastEvalRequestId;

        coordinator.OnEvent(
            handler.PlatformView.NodeId,
            "evalresult",
            $$"""{"requestId":{{failureRequestId}},"success":false,"result":null,"error":"Cross-origin or opaque-origin content prevented JavaScript evaluation."}""");

        Assert.Null(failureRequest.Result);
        Assert.NotNull(failureRequest.Exception);
        Assert.Contains("Cross-origin or opaque-origin", failureRequest.Exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WebViewHandler_RetriesRuntimeConfiguration_WhenInitialAttachPrecedesRuntimeNodeAvailability()
    {
        var coordinator = new QueuedWebViewCoordinator();
        coordinator.EnqueueConfigureResult(false);
        coordinator.EnqueueConfigureResult(true);

        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = "/webview-probe/page-a.html" }
        };

        var handler = CreateAttachedHandler(webView, coordinator);
        var initialConfigureCallCount = coordinator.ConfigureWebViewCallCount;

        Assert.True(initialConfigureCallCount >= 1);
        Assert.Equal(1, coordinator.PendingDelayedCount);

        coordinator.FlushNextDelayed();

        Assert.Equal(initialConfigureCallCount + 1, coordinator.ConfigureWebViewCallCount);
        using var config = JsonDocument.Parse(coordinator.LastConfigurationJson);
        Assert.Equal("url", config.RootElement.GetProperty("SourceKind").GetString());
    }

    private static (MirageWebViewHandler Handler, WebView WebView, MirageRenderCoordinator Coordinator, WebViewRuntimeProbe Runtime) CreateAttachedHandler(
        WebView webView,
        MirageRenderCoordinator? coordinator = null,
        WebViewRuntimeProbe? runtime = null)
    {
        coordinator ??= new MirageRenderCoordinator(new BrowserDispatcher());
        runtime ??= new WebViewRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<IMirageRenderCoordinator>(coordinator);
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(WebView), typeof(MirageWebViewHandler));
        });

        var mauiApp = builder.Build();
        var mauiContext = new MauiContext(mauiApp.Services);

        var handler = new MirageWebViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(webView);
        handler.PlatformArrange(new Rect(0, 0, 320, 220));
        return (handler, webView, coordinator, runtime);
    }

    private static MirageWebViewHandler CreateAttachedHandler(
        WebView webView,
        IMirageRenderCoordinator coordinator)
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton(coordinator);
        builder.Services.AddSingleton(typeof(IMirageRenderCoordinator), coordinator);
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(WebView), typeof(MirageWebViewHandler));
        });

        var mauiApp = builder.Build();
        var mauiContext = new MauiContext(mauiApp.Services);

        var handler = new MirageWebViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(webView);
        handler.PlatformArrange(new Rect(0, 0, 320, 220));
        return handler;
    }

    private static bool InvokeConfigureRuntime(MirageWebViewHandler handler)
    {
        var configureRuntime = typeof(MirageWebViewHandler).GetMethod("ConfigureRuntime", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(configureRuntime);
        return Assert.IsType<bool>(configureRuntime!.Invoke(handler, null));
    }

    private sealed class WebViewRuntimeProbe : IMirageRuntime
    {
        public List<WebViewConfigurationCall> Configurations { get; } = [];

        public string LastConfigurationJson => Configurations.LastOrDefault()?.ConfigurationJson ?? "{}";

        public int GoBackCallCount { get; private set; }

        public int GoForwardCallCount { get; private set; }

        public int ReloadCallCount { get; private set; }

        public int NavigateCallCount { get; private set; }

        public long LastEvalRequestId { get; private set; }

        public string LastEvalScript { get; private set; } = string.Empty;

        public bool LastEvalCaptureResult { get; private set; }

        public string LastNavigateUrl { get; private set; } = string.Empty;

        public void Dispose()
        {
        }

        public void SetEventSink(IMirageEventSink? eventSink)
        {
            _ = eventSink;
        }

        public void Render(NodeBase nextRoot)
        {
            _ = nextRoot;
        }

        public void SetViewport(double width, double height)
        {
            _ = width;
            _ = height;
        }

        public string? TryGetNodeInputValue(string nodeId)
        {
            _ = nodeId;
            return null;
        }

        public bool TryGetActiveNodeId(out string? nodeId)
        {
            nodeId = null;
            return false;
        }

        public bool TryGetNodeScrollPosition(string nodeId, out double left, out double top)
        {
            _ = nodeId;
            left = 0;
            top = 0;
            return false;
        }

        public bool TrySetNodeScrollPosition(string nodeId, double left, double top, out double actualLeft, out double actualTop)
        {
            _ = nodeId;
            actualLeft = left;
            actualTop = top;
            return false;
        }

        public bool TryScrollNodeChildIntoView(
            string containerNodeId,
            string childNodeId,
            int scrollToPosition,
            bool isHorizontalOrientation,
            bool isAnimated,
            out double actualLeft,
            out double actualTop)
        {
            _ = containerNodeId;
            _ = childNodeId;
            _ = scrollToPosition;
            _ = isHorizontalOrientation;
            _ = isAnimated;
            actualLeft = 0;
            actualTop = 0;
            return false;
        }

        public bool TryRequestNodePickerOpen(string nodeId)
        {
            _ = nodeId;
            return false;
        }

        public bool TrySetNodeFocus(string nodeId, bool isFocused)
        {
            _ = nodeId;
            _ = isFocused;
            return false;
        }

        public bool TryGetNodeTextSelection(string nodeId, out int selectionStart, out int selectionEnd)
        {
            _ = nodeId;
            selectionStart = 0;
            selectionEnd = 0;
            return false;
        }

        public bool TrySetNodeTextSelection(string nodeId, int selectionStart, int selectionEnd)
        {
            _ = nodeId;
            _ = selectionStart;
            _ = selectionEnd;
            return false;
        }

        public bool TryConfigureNodePullToRefresh(string ownerNodeId, string sourceNodeId, bool enabled, double threshold)
        {
            _ = ownerNodeId;
            _ = sourceNodeId;
            _ = enabled;
            _ = threshold;
            return false;
        }

        public bool TryConfigureNodeSwipeView(string ownerNodeId, bool enabled, string configurationJson)
        {
            _ = ownerNodeId;
            _ = enabled;
            _ = configurationJson;
            return false;
        }

        public bool TryRequestNodeSwipeViewOpen(string ownerNodeId, string side, bool animated, bool emitEndEvent = true)
        {
            _ = ownerNodeId;
            _ = side;
            _ = animated;
            _ = emitEndEvent;
            return false;
        }

        public bool TryRequestNodeSwipeViewClose(string ownerNodeId, bool animated)
        {
            _ = ownerNodeId;
            _ = animated;
            return false;
        }

        public bool TryConfigureNodeWebView(string nodeId, bool enabled, string configurationJson)
        {
            Configurations.Add(new WebViewConfigurationCall(nodeId, enabled, configurationJson));
            return true;
        }

        public bool TryRequestNodeWebViewNavigate(string nodeId, string url)
        {
            _ = nodeId;
            NavigateCallCount++;
            LastNavigateUrl = url;
            return true;
        }

        public bool TryRequestNodeWebViewGoBack(string nodeId)
        {
            _ = nodeId;
            GoBackCallCount++;
            return true;
        }

        public bool TryRequestNodeWebViewGoForward(string nodeId)
        {
            _ = nodeId;
            GoForwardCallCount++;
            return true;
        }

        public bool TryRequestNodeWebViewReload(string nodeId)
        {
            _ = nodeId;
            ReloadCallCount++;
            return true;
        }

        public bool TryEvaluateNodeWebViewScript(string nodeId, long requestId, string script, bool captureResult)
        {
            _ = nodeId;
            LastEvalRequestId = requestId;
            LastEvalScript = script;
            LastEvalCaptureResult = captureResult;
            return true;
        }

        public bool TryConfigureNodeCanvas(string nodeId, double logicalWidth, double logicalHeight, out double displayScale)
        {
            _ = nodeId;
            _ = logicalWidth;
            _ = logicalHeight;
            displayScale = 1;
            return false;
        }

        public bool TryRenderNodeCanvas(string nodeId, string commandsJson)
        {
            _ = nodeId;
            _ = commandsJson;
            return false;
        }

        public bool TryGetNodeTextContentHeight(string nodeId, out double height)
        {
            _ = nodeId;
            height = 0;
            return false;
        }
    }

    private sealed class QueuedWebViewCoordinator : IMirageRenderCoordinator
    {
        private readonly Queue<Action> _pendingDelayedActions = [];
        private readonly Queue<bool> _configureResults = [];
        private int _nodeIdCounter;

        public int PendingDelayedCount => _pendingDelayedActions.Count;

        public int ConfigureWebViewCallCount { get; private set; }

        public string LastConfigurationJson { get; private set; } = "{}";

        public void EnqueueConfigureResult(bool result)
        {
            _configureResults.Enqueue(result);
        }

        public void FlushNextDelayed()
        {
            Assert.True(_pendingDelayedActions.Count > 0, "Expected a pending delayed WebView configuration retry.");
            var action = _pendingDelayedActions.Dequeue();
            action();
        }

        public void AttachRuntime(IMirageRuntime runtime)
        {
            _ = runtime;
        }

        public void DetachRuntime()
        {
        }

        public void Register(MiragePlatformView platformView, IMirageRuntimeEventTarget? eventTarget)
        {
            if (string.IsNullOrWhiteSpace(platformView.NodeId))
            {
                var localId = Interlocked.Increment(ref _nodeIdCounter);
                platformView.AssignNodeId($"queued-webview-node-{localId:D4}");
            }

            _ = eventTarget;
        }

        public void Unregister(MiragePlatformView platformView)
        {
            _ = platformView;
        }

        public void SetRoot(MiragePlatformView? root)
        {
            _ = root;
        }

        public void RequestRender()
        {
        }

        public bool DispatchDelayed(TimeSpan delay, Action action)
        {
            _ = delay;
            _pendingDelayedActions.Enqueue(action);
            return true;
        }

        public IDisposable BeginRenderBatch()
        {
            return NullDisposable.Instance;
        }

        public bool TryGetNodeInputValue(string nodeId, out string? value)
        {
            _ = nodeId;
            value = null;
            return false;
        }

        public bool TryGetActiveNodeId(out string? nodeId)
        {
            nodeId = null;
            return false;
        }

        public bool TryGetNodeScrollPosition(string nodeId, out double left, out double top)
        {
            _ = nodeId;
            left = 0;
            top = 0;
            return false;
        }

        public bool TrySetNodeScrollPosition(string nodeId, double left, double top, out double actualLeft, out double actualTop)
        {
            _ = nodeId;
            _ = left;
            _ = top;
            actualLeft = 0;
            actualTop = 0;
            return false;
        }

        public bool TryScrollNodeChildIntoView(
            string containerNodeId,
            string childNodeId,
            int scrollToPosition,
            bool isHorizontalOrientation,
            bool isAnimated,
            out double actualLeft,
            out double actualTop)
        {
            _ = containerNodeId;
            _ = childNodeId;
            _ = scrollToPosition;
            _ = isHorizontalOrientation;
            _ = isAnimated;
            actualLeft = 0;
            actualTop = 0;
            return false;
        }

        public bool TryRequestNodePickerOpen(string nodeId)
        {
            _ = nodeId;
            return false;
        }

        public bool TrySetNodeFocus(string nodeId, bool isFocused)
        {
            _ = nodeId;
            _ = isFocused;
            return false;
        }

        public bool TryGetNodeTextSelection(string nodeId, out int selectionStart, out int selectionEnd)
        {
            _ = nodeId;
            selectionStart = 0;
            selectionEnd = 0;
            return false;
        }

        public bool TrySetNodeTextSelection(string nodeId, int selectionStart, int selectionEnd)
        {
            _ = nodeId;
            _ = selectionStart;
            _ = selectionEnd;
            return false;
        }

        public bool TryConfigureNodePullToRefresh(string ownerNodeId, string sourceNodeId, bool enabled, double threshold)
        {
            _ = ownerNodeId;
            _ = sourceNodeId;
            _ = enabled;
            _ = threshold;
            return false;
        }

        public bool TryConfigureNodeSwipeView(string ownerNodeId, bool enabled, string configurationJson)
        {
            _ = ownerNodeId;
            _ = enabled;
            _ = configurationJson;
            return false;
        }

        public bool TryRequestNodeSwipeViewOpen(string ownerNodeId, string side, bool animated, bool emitEndEvent = true)
        {
            _ = ownerNodeId;
            _ = side;
            _ = animated;
            _ = emitEndEvent;
            return false;
        }

        public bool TryRequestNodeSwipeViewClose(string ownerNodeId, bool animated)
        {
            _ = ownerNodeId;
            _ = animated;
            return false;
        }

        public bool TryConfigureNodeWebView(string nodeId, bool enabled, string configurationJson)
        {
            _ = nodeId;
            _ = enabled;
            ConfigureWebViewCallCount++;
            LastConfigurationJson = configurationJson;
            return _configureResults.Count > 0 ? _configureResults.Dequeue() : true;
        }

        public bool TryRequestNodeWebViewNavigate(string nodeId, string url)
        {
            _ = nodeId;
            _ = url;
            return false;
        }

        public bool TryRequestNodeWebViewGoBack(string nodeId)
        {
            _ = nodeId;
            return false;
        }

        public bool TryRequestNodeWebViewGoForward(string nodeId)
        {
            _ = nodeId;
            return false;
        }

        public bool TryRequestNodeWebViewReload(string nodeId)
        {
            _ = nodeId;
            return false;
        }

        public bool TryEvaluateNodeWebViewScript(string nodeId, long requestId, string script, bool captureResult)
        {
            _ = nodeId;
            _ = requestId;
            _ = script;
            _ = captureResult;
            return false;
        }

        public bool TryConfigureNodeCanvas(string nodeId, double logicalWidth, double logicalHeight, out double displayScale)
        {
            _ = nodeId;
            _ = logicalWidth;
            _ = logicalHeight;
            displayScale = 1;
            return false;
        }

        public bool TryRenderNodeCanvas(string nodeId, string commandsJson)
        {
            _ = nodeId;
            _ = commandsJson;
            return false;
        }

        public bool TryGetNodeTextContentHeight(string nodeId, out double height)
        {
            _ = nodeId;
            height = 0;
            return false;
        }

        public void OnEvent(string nodeId, string eventName, string? payload)
        {
            _ = nodeId;
            _ = eventName;
            _ = payload;
        }
    }

    private sealed class FakeJavaScriptRequest
    {
        public string Script { get; init; } = string.Empty;

        public string? Result { get; private set; }

        public Exception? Exception { get; private set; }

        public void SetResult(string? result)
        {
            Result = result;
        }

        public void SetException(Exception exception)
        {
            Exception = exception;
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed class UnsupportedWebViewSource : WebViewSource
    {
        public override void Load(IWebViewDelegate renderer)
        {
            _ = renderer;
        }
    }

    private sealed record WebViewConfigurationCall(string NodeId, bool Enabled, string ConfigurationJson);
}
