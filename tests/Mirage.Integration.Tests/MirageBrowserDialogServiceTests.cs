using System.Reflection;
using Mirage.Core.Semantics;
using Mirage.Maui.Dialogs;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Maui.Navigation;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Maui.Controls.Sample.Pages;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageBrowserDialogServiceTests
{
    [Fact]
    public async Task DisplayAlertAsync_QueuesAndCompletesInOrder()
    {
        using var harness = new DialogHarness();
        var page = new ContentPage();
        harness.Attach(page);
        Assert.True(((IVisualElementController)page).IsPlatformEnabled);

        var firstTask = page.DisplayAlertAsync("First", "First body", "OK");
        var secondTask = page.DisplayAlertAsync("Second", "Second body", "Close");

        await harness.WaitForDialogTitleAsync("First");
        var acceptNode = Assert.IsType<WebNode>(Assert.IsAssignableFrom<NodeBase>(harness.FindByAutomationId("mirage-dialog-accept")));
        Assert.True(acceptNode.Properties.TryGetValue("textContent", out var acceptText));
        Assert.Equal("OK", acceptText.GetStringOrNull());
        Assert.Null(harness.FindByAutomationId("mirage-dialog-cancel"));
        harness.ClickByAutomationId("mirage-dialog-accept");
        await firstTask;

        await harness.WaitForDialogTitleAsync("Second");
        harness.ClickByAutomationId("mirage-dialog-accept");
        await secondTask;

        await harness.WaitForNoDialogAsync();
    }

    [Fact]
    public async Task DisplayPromptAsync_ProjectsInputHintsAndReturnsTypedValue()
    {
        using var harness = new DialogHarness();
        var page = new ContentPage();
        harness.Attach(page);

        var task = page.DisplayPromptAsync(
            "Question 2",
            "What's 5 + 5?",
            accept: "Submit",
            cancel: "Cancel",
            placeholder: "Answer",
            initialValue: "10",
            maxLength: 2,
            keyboard: Keyboard.Numeric);

        await harness.WaitForDialogTitleAsync("Question 2");

        var inputNodeBase = harness.FindByAutomationId("mirage-dialog-input");
        Assert.NotNull(inputNodeBase);
        var inputNode = Assert.IsType<WebNode>(inputNodeBase);
        Assert.Equal("numeric", inputNode.Attributes["inputmode"]);
        Assert.Equal("2", inputNode.Attributes["maxlength"]);
        Assert.Equal("10", inputNode.Properties["value"].GetStringOrNull());

        harness.SetInputValue("mirage-dialog-input", "10");
        harness.ClickByAutomationId("mirage-dialog-accept");

        Assert.Equal("10", await task);
        await harness.WaitForNoDialogAsync();
    }

    [Fact]
    public async Task DisplayPromptAsync_DoesNotDismissOnBackdropOrEscape()
    {
        using var harness = new DialogHarness();
        var page = new ContentPage();
        harness.Attach(page);

        var task = page.DisplayPromptAsync(
            "Question",
            "Type something",
            accept: "OK",
            cancel: "Cancel",
            initialValue: "Ada");

        await harness.WaitForDialogTitleAsync("Question");

        harness.ClickByAutomationId("mirage-dialog-backdrop");
        Assert.False(task.IsCompleted);
        Assert.NotNull(harness.FindByAutomationId("mirage-dialog-overlay"));

        harness.SendEventByAutomationId("mirage-dialog-input", "keydown", "Escape");
        Assert.False(task.IsCompleted);
        Assert.NotNull(harness.FindByAutomationId("mirage-dialog-overlay"));

        harness.ClickByAutomationId("mirage-dialog-cancel");
        Assert.Null(await task);
        await harness.WaitForNoDialogAsync();
    }

    [Fact]
    public async Task DisplayActionSheetAsync_BackdropUsesCancelWhenAvailable()
    {
        using var harness = new DialogHarness();
        var page = new ContentPage();
        harness.Attach(page);

        var task = page.DisplayActionSheetAsync("Pick one", "Cancel", "Delete", FlowDirection.MatchParent, "Email", "Twitter");

        await harness.WaitForDialogTitleAsync("Pick one");
        harness.ClickByAutomationId("mirage-dialog-backdrop");

        Assert.Equal("Cancel", await task);
        await harness.WaitForNoDialogAsync();
    }

    [Fact]
    public async Task DisplayAlertAsync_InitialFocusTargetsInteractiveButtonNotBackdrop()
    {
        using var harness = new DialogHarness();
        var page = new ContentPage();
        harness.Attach(page);

        var task = page.DisplayAlertAsync("Alert", "Body", "OK");

        await harness.WaitForDialogTitleAsync("Alert");
        await harness.WaitForFocusTargetAsync("mirage-dialog-accept");

        Assert.NotEqual(harness.GetNodeIdByAutomationId("mirage-dialog-backdrop"), harness.LastFocusedNodeId);
        Assert.Equal(harness.GetNodeIdByAutomationId("mirage-dialog-accept"), harness.LastFocusedNodeId);

        harness.ClickByAutomationId("mirage-dialog-accept");
        await task;
        await harness.WaitForNoDialogAsync();
    }

    [Fact]
    public async Task DisplayAlertAsync_QueuedDialogs_RestoreOriginalFocusAfterQueueDrains()
    {
        using var harness = new DialogHarness();
        var focusSeed = new Button
        {
            AutomationId = "focus-seed",
            Text = "Seed"
        };
        var page = new ContentPage
        {
            Content = focusSeed
        };
        harness.Attach(page);
        await harness.WaitForAutomationIdAsync("focus-seed");
        harness.FocusByAutomationId("focus-seed");

        var firstTask = page.DisplayAlertAsync("First", "First body", "OK");
        var secondTask = page.DisplayAlertAsync("Second", "Second body", "Close");

        await harness.WaitForDialogTitleAsync("First");
        harness.ClickByAutomationId("mirage-dialog-accept");
        await firstTask;

        await harness.WaitForDialogTitleAsync("Second");
        harness.ClickByAutomationId("mirage-dialog-accept");
        await secondTask;

        await harness.WaitForNoDialogAsync();
        await harness.WaitForFocusTargetAsync("focus-seed");
        Assert.Equal(harness.GetNodeIdByAutomationId("focus-seed"), harness.LastFocusedNodeId);
    }

    [Fact]
    public async Task CancelDialogs_ResolvesPendingDialogsPredictably()
    {
        using var harness = new DialogHarness();
        var page = new ContentPage();
        harness.Attach(page);

        var alertTask = page.DisplayAlertAsync("Confirm", "Proceed?", "Yes", "No");
        var promptTask = page.DisplayPromptAsync("Prompt", "Type", accept: "OK", cancel: "Cancel");

        await harness.WaitForDialogTitleAsync("Confirm");
        harness.CancelDialogs();

        Assert.False(await alertTask);
        Assert.Null(await promptTask);
        await harness.WaitForNoDialogAsync();
    }

    [Fact]
    public async Task InputTransparentPage_UsesSameSharedDialogSeam()
    {
        using var harness = new DialogHarness();
        var page = new InputTransparentPage();
        harness.Attach(page);

        var clickSuccess = typeof(InputTransparentPage).GetMethod("ClickSuccess", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(clickSuccess);

        clickSuccess!.Invoke(page, [null, EventArgs.Empty]);

        await harness.WaitForDialogTitleAsync("Success");
        var messageNode = harness.FindByAutomationId("mirage-dialog-message");
        Assert.NotNull(messageNode);
        var messageVisual = Assert.IsAssignableFrom<VisualNodeBase>(messageNode);
        Assert.Equal("That should have worked, and it did!", messageVisual.Properties["textContent"].GetStringOrNull());

        harness.ClickByAutomationId("mirage-dialog-accept");
        await harness.WaitForNoDialogAsync();
    }

    private sealed class DialogHarness : IDisposable
    {
        private readonly MirageRenderCoordinator _coordinator;
        private readonly DialogRuntimeProbe _runtime;
        private readonly MirageBrowserDialogService _service;
        private readonly MauiContext _mauiContext;
        private IElementHandler? _windowHandler;
        private Window? _window;

        public DialogHarness()
        {
            _coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
            _runtime = new DialogRuntimeProbe();
            _coordinator.AttachRuntime(_runtime);

            var builder = MauiApp.CreateBuilder();
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler(typeof(Window), typeof(MirageWindowHandler));
                handlers.AddHandler(typeof(Page), typeof(MiragePageHandler));
                handlers.AddHandler(typeof(ContentPage), typeof(MirageContentPageHandler));
                handlers.AddHandler(typeof(ScrollView), typeof(MirageScrollViewHandler));
                handlers.AddHandler(typeof(StackLayout), typeof(MirageLayoutHandler));
                handlers.AddHandler(typeof(VerticalStackLayout), typeof(MirageLayoutHandler));
                handlers.AddHandler(typeof(HorizontalStackLayout), typeof(MirageLayoutHandler));
                handlers.AddHandler(typeof(Grid), typeof(MirageLayoutHandler));
                handlers.AddHandler(typeof(Label), typeof(MirageLabelHandler));
                handlers.AddHandler(typeof(Button), typeof(MirageButtonHandler));
                handlers.AddHandler(typeof(Switch), typeof(MirageSwitchHandler));
                handlers.AddHandler(typeof(BoxView), typeof(MirageBoxViewHandler));
            });

            builder.Services.AddSingleton<IMirageRenderCoordinator>(_coordinator);
            builder.Services.AddSingleton<Microsoft.Maui.Dispatching.IDispatcher>(_ => new BrowserDispatcher());
            builder.Services.AddSingleton<IMirageBrowserDialogService, MirageBrowserDialogService>();
            builder.Services.AddSingleton<IMirageBrowserHistory>(new BrowserHistoryProbe("/"));
            builder.Services.AddSingleton(MirageNavigationConfiguration.Empty);
            builder.Services.AddSingleton<INavigationTitlePolicy, DefaultNavigationTitlePolicy>();

            var mauiApp = builder.Build();
            _mauiContext = new MauiContext(mauiApp.Services);
            _service = Assert.IsType<MirageBrowserDialogService>(_mauiContext.Services.GetRequiredService<IMirageBrowserDialogService>());
        }

        public void Attach(Page page)
        {
            _window = new Window(page);
            var windowHandler = new MirageWindowHandler();
            ((IElementHandler)windowHandler).SetMauiContext(_mauiContext);
            ((IElementHandler)windowHandler).SetVirtualView(_window);
            windowHandler.ArrangeWindow(1024, 768);
            _windowHandler = windowHandler;
        }

        public void CancelDialogs()
        {
            Assert.NotNull(_window);
            _service.CancelDialogs(_window!);
        }

        public async Task WaitForDialogTitleAsync(string title)
        {
            await WaitUntilAsync(() =>
            {
                var titleNode = FindByAutomationId("mirage-dialog-title");
                return titleNode is VisualNodeBase visual
                    && string.Equals(visual.Properties["textContent"].GetStringOrNull(), title, StringComparison.Ordinal);
            });
        }

        public async Task WaitForNoDialogAsync()
        {
            await WaitUntilAsync(() => FindByAutomationId("mirage-dialog-overlay") is null);
        }

        public NodeBase? FindByAutomationId(string automationId)
        {
            var rendered = _runtime.LastRendered;
            return rendered is null ? null : FindByAutomationId(rendered, automationId);
        }

        public void ClickByAutomationId(string automationId)
        {
            var node = FindByAutomationId(automationId);
            Assert.NotNull(node);
            var nodeId = Assert.IsAssignableFrom<VisualNodeBase>(node).NodeId;
            _coordinator.OnEvent(nodeId, "click", payload: null);
        }

        public void SendEventByAutomationId(string automationId, string eventName, string? payload)
        {
            var node = FindByAutomationId(automationId);
            Assert.NotNull(node);
            var nodeId = Assert.IsAssignableFrom<VisualNodeBase>(node).NodeId;
            _coordinator.OnEvent(nodeId, eventName, payload);
        }

        public string GetNodeIdByAutomationId(string automationId)
        {
            var node = FindByAutomationId(automationId);
            Assert.NotNull(node);
            return Assert.IsAssignableFrom<VisualNodeBase>(node).NodeId;
        }

        public void SetInputValue(string automationId, string? value)
        {
            var node = FindByAutomationId(automationId);
            Assert.NotNull(node);
            var nodeId = Assert.IsAssignableFrom<VisualNodeBase>(node).NodeId;
            _runtime.InputValues[nodeId] = value;
        }

        public string? LastFocusedNodeId => _runtime.LastFocusedNodeId;

        public void FocusByAutomationId(string automationId)
        {
            var nodeId = GetNodeIdByAutomationId(automationId);
            Assert.True(_coordinator.TrySetNodeFocus(nodeId, isFocused: true));
        }

        public void Dispose()
        {
            if (_windowHandler is not null)
            {
                MirageHandlerDisconnect.SafeDisconnect(_windowHandler);
            }

            _coordinator.DetachRuntime();
            _runtime.Dispose();
        }

        private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 2000)
        {
            var started = Environment.TickCount64;
            while (!predicate())
            {
                if (Environment.TickCount64 - started > timeoutMs)
                {
                    throw new TimeoutException($"Condition did not become true within {timeoutMs}ms.");
                }

                await Task.Delay(10);
            }
        }

        public Task WaitForFocusTargetAsync(string automationId)
        {
            var expectedNodeId = GetNodeIdByAutomationId(automationId);
            return WaitUntilAsync(() => string.Equals(_runtime.LastFocusedNodeId, expectedNodeId, StringComparison.Ordinal));
        }

        public Task WaitForAutomationIdAsync(string automationId)
        {
            return WaitUntilAsync(() => FindByAutomationId(automationId) is not null);
        }

        private static NodeBase? FindByAutomationId(NodeBase root, string automationId)
        {
            if (root is VisualNodeBase visual && string.Equals(visual.AutomationId, automationId, StringComparison.Ordinal))
            {
                return visual;
            }

            foreach (var child in root.Children)
            {
                var match = FindByAutomationId(child, automationId);
                if (match is not null)
                {
                    return match;
                }
            }

            return null;
        }
    }

    private sealed class DialogRuntimeProbe : IMirageRuntime
    {
        public Dictionary<string, string?> InputValues { get; } = new(StringComparer.Ordinal);

        public NodeBase? LastRendered { get; private set; }

        public IMirageEventSink? EventSink { get; private set; }

        public string? ActiveNodeId { get; private set; }

        public string? LastFocusedNodeId { get; private set; }

        public void SetEventSink(IMirageEventSink? eventSink)
        {
            EventSink = eventSink;
        }

        public void Render(NodeBase nextRoot)
        {
            LastRendered = nextRoot;
        }

        public void SetViewport(double width, double height)
        {
            _ = width;
            _ = height;
        }

        public string? TryGetNodeInputValue(string nodeId)
        {
            return InputValues.TryGetValue(nodeId, out var value) ? value : null;
        }

        public bool TryGetActiveNodeId(out string? nodeId)
        {
            nodeId = ActiveNodeId;
            return !string.IsNullOrWhiteSpace(nodeId);
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
            LastFocusedNodeId = nodeId;
            ActiveNodeId = isFocused ? nodeId : null;
            return true;
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

        public void Dispose()
        {
        }
    }

    private sealed class BrowserHistoryProbe(string initialLocation) : IMirageBrowserHistory
    {
        private readonly Dictionary<int, Action<string>> _subscriptions = new();
        private int _nextSubscriptionId = 1;

        public string CurrentLocation { get; private set; } = string.IsNullOrWhiteSpace(initialLocation) ? "/" : initialLocation;

        public string DocumentTitle { get; private set; } = string.Empty;

        public string GetCurrentLocation() => CurrentLocation;

        public void Push(string path) => CurrentLocation = path;

        public void Replace(string path) => CurrentLocation = path;

        public string GetDocumentTitle() => DocumentTitle;

        public void SetDocumentTitle(string? title) => DocumentTitle = title ?? string.Empty;

        public int SubscribePopState(Action<string> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            var id = _nextSubscriptionId++;
            _subscriptions[id] = callback;
            return id;
        }

        public void UnsubscribePopState(int subscriptionId)
        {
            _subscriptions.Remove(subscriptionId);
        }
    }
}
