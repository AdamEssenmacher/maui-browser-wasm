using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Maui.TextInput;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageTextInputSoftInputBridgeTests
{
    [Fact]
    public async Task ShowSoftInputAsync_FocusesEntryHandler()
    {
        var bridge = new MirageTextInputSoftInputBridge();
        var entry = new Entry
        {
            IsEnabled = true
        };

        var (handler, runtime) = CreateAttachedEntryHandler(entry);
        var inputNode = GetEntryInputNode(handler);

        var focused = await bridge.ShowSoftInputAsync(entry, CancellationToken.None);

        Assert.True(focused);
        Assert.Equal(1, runtime.FocusCallCount);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);
        Assert.True(runtime.LastFocusState);
    }

    [Fact]
    public async Task HideSoftInputAsync_UnfocusesEntryHandler()
    {
        var bridge = new MirageTextInputSoftInputBridge();
        var entry = new Entry
        {
            IsEnabled = true
        };

        var (handler, runtime) = CreateAttachedEntryHandler(entry);
        var inputNode = GetEntryInputNode(handler);

        var hidden = await bridge.HideSoftInputAsync(entry, CancellationToken.None);

        Assert.True(hidden);
        Assert.Equal(1, runtime.FocusCallCount);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);
        Assert.False(runtime.LastFocusState);
    }

    [Fact]
    public async Task HideSoftInputAsync_ReturnsFalse_WhenRuntimeCannotBlur()
    {
        var bridge = new MirageTextInputSoftInputBridge();
        var entry = new Entry
        {
            IsEnabled = true
        };

        var (_, runtime) = CreateAttachedEntryHandler(entry);
        runtime.NextUnfocusResult = false;

        var hidden = await bridge.HideSoftInputAsync(entry, CancellationToken.None);

        Assert.False(hidden);
        Assert.Equal(1, runtime.FocusCallCount);
        Assert.False(runtime.LastFocusState);
    }

    [Fact]
    public async Task ShowSoftInputAsync_FocusesEditorHandler()
    {
        var bridge = new MirageTextInputSoftInputBridge();
        var editor = new Editor
        {
            IsEnabled = true
        };

        var (handler, runtime) = CreateAttachedEditorHandler(editor);
        var inputNode = GetEditorInputNode(handler);

        var focused = await bridge.ShowSoftInputAsync(editor, CancellationToken.None);

        Assert.True(focused);
        Assert.Equal(1, runtime.FocusCallCount);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);
        Assert.True(runtime.LastFocusState);
    }

    private static (MirageEntryHandler Handler, TextInputRuntimeProbe Runtime) CreateAttachedEntryHandler(Entry entry)
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new TextInputRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageEntryHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(entry);
        return (handler, runtime);
    }

    private static (MirageEditorHandler Handler, TextInputRuntimeProbe Runtime) CreateAttachedEditorHandler(Editor editor)
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new TextInputRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageEditorHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(editor);
        return (handler, runtime);
    }

    private static MiragePlatformView GetEntryInputNode(MirageEntryHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "input", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-entry-input", StringComparer.Ordinal));
    }

    private static MiragePlatformView GetEditorInputNode(MirageEditorHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "textarea", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-editor-input", StringComparer.Ordinal));
    }

    private sealed class TextInputRuntimeProbe : IMirageRuntime
    {
        public string? LastFocusedNodeId { get; private set; }

        public bool LastFocusState { get; private set; }

        public int FocusCallCount { get; private set; }

        public bool NextUnfocusResult { get; set; } = true;

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
            FocusCallCount++;
            LastFocusedNodeId = nodeId;
            LastFocusState = isFocused;
            return isFocused || NextUnfocusResult;
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
}
