using Microsoft.Maui.Controls;

namespace ControlsSample.Shared.App;

public sealed class WebViewParityProbePage : ContentPage
{
    private const string PageAUrl = "/webview-probe/page-a.html";
    private const string PageBUrl = "/webview-probe/page-b.html";
    private const string AlternateUserAgent = "MirageWebViewParityProbe/1.0";

    private readonly WebView _webView;
    private readonly Label _navigationStateLabel;
    private readonly Label _sourceStateLabel;
    private readonly Label _evalStateLabel;
    private readonly Label _userAgentStateLabel;

    private int _navigatingCount;
    private int _navigatedCount;
    private string _sourceMode = "html";
    private string _lastNavigatingUrl = "about:srcdoc";
    private string _lastNavigatedUrl = "about:srcdoc";
    private string _lastEvalResult = "<none>";
    private string _lastEvalError = "<none>";
    private bool _alternateUserAgentEnabled;

    public WebViewParityProbePage()
    {
        Title = "WebView Parity Probe";
        AutomationId = "controls-sample-page-webviewparitypage";

        _webView = new WebView
        {
            AutomationId = "controls-sample-webview-parity-host",
            WidthRequest = 320,
            HeightRequest = 220
        };
        _webView.Navigating += OnWebViewNavigating;
        _webView.Navigated += OnWebViewNavigated;
        _webView.Source = CreateInlineSource();

        _navigationStateLabel = new Label
        {
            AutomationId = "controls-sample-webview-parity-navigation-state",
            FontAttributes = FontAttributes.Bold
        };
        _sourceStateLabel = new Label
        {
            AutomationId = "controls-sample-webview-parity-source-state"
        };
        _evalStateLabel = new Label
        {
            AutomationId = "controls-sample-webview-parity-eval-state"
        };
        _userAgentStateLabel = new Label
        {
            AutomationId = "controls-sample-webview-parity-useragent-state"
        };

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new Label
                {
                    AutomationId = "controls-sample-webview-parity-description",
                    Text = "WebView parity probe for iframe-hosted UrlWebViewSource and HtmlWebViewSource, same-origin navigation commands, same-origin JavaScript evaluation, deterministic cross-origin eval failure, and browser-limited UserAgent diagnostics."
                },
                _webView,
                _navigationStateLabel,
                _sourceStateLabel,
                _evalStateLabel,
                _userAgentStateLabel,
                BuildControls(),
                new Label
                {
                    AutomationId = "controls-sample-webview-parity-flow-sentinel",
                    FontSize = 12,
                    Text = "Flow sentinel: this label should stay below the WebView host."
                }
            }
        };

        UpdateStateLabels();
    }

    private View BuildControls()
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Inline HTML", "controls-sample-webview-parity-load-inline", LoadInline),
                        CreateButton("Page A", "controls-sample-webview-parity-load-page-a", LoadPageA),
                        CreateButton("Page B", "controls-sample-webview-parity-load-page-b", LoadPageB),
                        CreateButton("Data URL", "controls-sample-webview-parity-load-data", LoadDataUrl)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Back", "controls-sample-webview-parity-back", GoBack),
                        CreateButton("Forward", "controls-sample-webview-parity-forward", GoForward),
                        CreateButton("Reload", "controls-sample-webview-parity-reload", Reload),
                        CreateButton("Toggle UA", "controls-sample-webview-parity-toggle-useragent", ToggleUserAgent)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Eval", "controls-sample-webview-parity-eval", Eval),
                        CreateButton("Eval Async", "controls-sample-webview-parity-eval-async", EvaluateAsync)
                    }
                }
            }
        };
    }

    private static Button CreateButton(string text, string automationId, Action action)
    {
        return new Button
        {
            Text = text,
            AutomationId = automationId,
            Command = new Command(action)
        };
    }

    private static Button CreateButton(string text, string automationId, Func<Task> action)
    {
        return new Button
        {
            Text = text,
            AutomationId = automationId,
            Command = new Command(async () => await action())
        };
    }

    private void LoadInline()
    {
        _sourceMode = "html";
        _webView.Source = CreateInlineSource();
        UpdateStateLabels();
    }

    private void LoadPageA()
    {
        _sourceMode = "url";
        _webView.Source = new UrlWebViewSource { Url = PageAUrl };
        UpdateStateLabels();
    }

    private void LoadPageB()
    {
        _sourceMode = "url";
        _webView.Source = new UrlWebViewSource { Url = PageBUrl };
        UpdateStateLabels();
    }

    private void LoadDataUrl()
    {
        _sourceMode = "url";
        _webView.Source = new UrlWebViewSource
        {
            Url = "data:text/html,%3C!doctype%20html%3E%3Chtml%3E%3Cbody%20data-probe%3D%27data-url%27%3EData%20URL%20probe%3Cscript%3Ewindow.webViewProbeToken%3D%27data-url%27%3B%3C/script%3E%3C/body%3E%3C/html%3E"
        };
        UpdateStateLabels();
    }

    private void GoBack()
    {
        _webView.GoBack();
    }

    private void GoForward()
    {
        _webView.GoForward();
    }

    private void Reload()
    {
        _webView.Reload();
    }

    private void ToggleUserAgent()
    {
        _alternateUserAgentEnabled = !_alternateUserAgentEnabled;
        _webView.UserAgent = _alternateUserAgentEnabled ? AlternateUserAgent : string.Empty;
        UpdateStateLabels();
    }

    private void Eval()
    {
        try
        {
            _webView.Eval("window.__mirageSyncEvalResult = (window.webViewProbeToken ?? document.body.getAttribute('data-probe') ?? 'missing') + ':sync';");
            _lastEvalResult = "eval-dispatched";
            _lastEvalError = "<none>";
        }
        catch (Exception ex)
        {
            _lastEvalResult = "<none>";
            _lastEvalError = ex.Message;
        }

        UpdateStateLabels();
    }

    private async Task EvaluateAsync()
    {
        try
        {
            var result = await _webView.EvaluateJavaScriptAsync("window.__mirageSyncEvalResult ?? window.webViewProbeToken ?? document.body.getAttribute('data-probe') ?? 'missing'");
            _lastEvalResult = string.IsNullOrWhiteSpace(result) ? "<null>" : result;
            _lastEvalError = "<none>";
        }
        catch (Exception ex)
        {
            _lastEvalResult = "<none>";
            _lastEvalError = ex.Message;
        }

        UpdateStateLabels();
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        _ = sender;
        _navigatingCount++;
        _lastNavigatingUrl = string.IsNullOrWhiteSpace(e.Url) ? "<none>" : e.Url;
        UpdateStateLabels();
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        _ = sender;
        _navigatedCount++;
        _lastNavigatedUrl = string.IsNullOrWhiteSpace(e.Url) ? "<none>" : e.Url;
        UpdateStateLabels();
    }

    private void UpdateStateLabels()
    {
        var sourceKind = _webView.Source is HtmlWebViewSource ? "html" : "url";
        _navigationStateLabel.Text =
            $"currentUrl={_lastNavigatedUrl} navigating={_navigatingCount} navigated={_navigatedCount} canGoBack={_webView.CanGoBack} canGoForward={_webView.CanGoForward}";
        _sourceStateLabel.Text =
            $"sourceMode={_sourceMode} sourceKind={sourceKind} lastNavigating={_lastNavigatingUrl}";
        _evalStateLabel.Text =
            $"evalResult={_lastEvalResult} evalError={_lastEvalError}";

        var userAgent = _webView.UserAgent;
        _userAgentStateLabel.Text =
            $"userAgent={(_alternateUserAgentEnabled ? "alt" : "default")} value={(string.IsNullOrEmpty(userAgent) ? "<empty>" : userAgent)}";
    }

    private static HtmlWebViewSource CreateInlineSource()
    {
        return new HtmlWebViewSource
        {
            Html =
                """
                <!doctype html>
                <html>
                <head>
                  <meta charset="utf-8" />
                  <title>Inline Probe</title>
                </head>
                <body data-probe="inline">
                  <main>
                    <h1>Inline Probe</h1>
                    <p>Mirage WebView inline HTML mode.</p>
                    <p><a data-automation-id="controls-sample-webview-probe-link-inline-to-page-a" href="/webview-probe/page-a.html">Go to Probe Page A</a></p>
                  </main>
                  <script>
                    window.webViewProbeToken = "inline";
                  </script>
                </body>
                </html>
                """
        };
    }
}
