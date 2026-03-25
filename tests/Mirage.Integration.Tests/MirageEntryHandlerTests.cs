using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageEntryHandlerTests
{
    [Fact]
    public void EntryHandler_ProjectsBuiltInControlKind_AndHostStructure()
    {
        var (handler, _, _, _) = CreateAttachedHandler(new Entry
        {
            Text = "hello"
        });

        Assert.Equal(ComponentKind.Entry, handler.PlatformView.ControlKind);
        Assert.Equal("flex", handler.PlatformView.GetStyleValue("display"));

        var inputNode = GetInputNode(handler);
        var clearNode = GetClearButtonNode(handler);

        Assert.Equal("input", inputNode.ElementName);
        Assert.Equal("button", clearNode.ElementName);
        Assert.Contains("mirage-entry-input", inputNode.Classes);
        Assert.Contains("mirage-entry-clear", clearNode.Classes);
    }

    [Fact]
    public void EntryHandler_MapsCoreTextInputProperties_Deterministically()
    {
        var entry = new Entry
        {
            Text = "alpha",
            Placeholder = "Type here",
            PlaceholderColor = Colors.CornflowerBlue,
            TextColor = Colors.Crimson,
            CharacterSpacing = 1.5,
            Keyboard = Keyboard.Email,
            ReturnType = ReturnType.Search,
            MaxLength = 24,
            IsReadOnly = true,
            IsSpellCheckEnabled = false,
            IsTextPredictionEnabled = false,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.End
        };
        var (handler, _, _, _) = CreateAttachedHandler(entry);
        var inputNode = GetInputNode(handler);

        Assert.Equal("alpha", inputNode.GetPropertyString("value"));
        Assert.Equal("Type here", inputNode.Attributes["placeholder"]);
        Assert.Equal("email", inputNode.Attributes["type"]);
        Assert.Equal("email", inputNode.Attributes["inputmode"]);
        Assert.Equal("search", inputNode.Attributes["enterkeyhint"]);
        Assert.Equal("24", inputNode.Attributes["maxlength"]);
        Assert.Equal("readonly", inputNode.Attributes["readonly"]);
        Assert.Equal("off", inputNode.Attributes["autocorrect"]);
        Assert.Equal("off", inputNode.Attributes["autocomplete"]);
        Assert.Equal("false", inputNode.Attributes["spellcheck"]);
        Assert.Equal("center", inputNode.GetStyleValue("textAlign"));
        Assert.Equal("6px", inputNode.GetStyleValue("paddingTop"));
        Assert.Equal("2px", inputNode.GetStyleValue("paddingBottom"));
        Assert.Equal("1.5px", inputNode.GetStyleValue("letterSpacing"));
        Assert.Equal("rgba(220,20,60,1)", inputNode.GetStyleValue("color"));
        Assert.Equal("rgba(100,149,237,1)", inputNode.GetStyleValue("--mirage-entry-placeholder-color"));
    }

    [Fact]
    public void EntryHandler_DisabledState_DisablesInputAndClearInteraction()
    {
        var entry = new Entry
        {
            Text = "hello",
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
            IsReadOnly = false,
            IsEnabled = true
        };
        var (handler, _, coordinator, _) = CreateAttachedHandler(entry);
        var inputNode = GetInputNode(handler);
        var clearNode = GetClearButtonNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "focus", null);
        Assert.Equal("visible", clearNode.GetStyleValue("visibility"));

        coordinator.OnEvent(clearNode.NodeId, "click", null);
        Assert.Equal(string.Empty, entry.Text);

        entry.Text = "world";
        entry.IsEnabled = false;
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(IEntry.Text));
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(IView.IsEnabled));

        coordinator.OnEvent(clearNode.NodeId, "click", null);
        Assert.Equal("world", entry.Text);
        Assert.Equal("disabled", inputNode.Attributes["disabled"]);
        Assert.Equal("hidden", clearNode.GetStyleValue("visibility"));
    }

    [Fact]
    public void EntryHandler_ClearButtonClick_RestoresInputFocus()
    {
        var entry = new Entry
        {
            Text = "hello",
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
            IsReadOnly = false,
            IsEnabled = true
        };
        var (handler, _, coordinator, runtime) = CreateAttachedHandler(entry);
        var inputNode = GetInputNode(handler);
        var clearNode = GetClearButtonNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "focus", null);
        coordinator.OnEvent(inputNode.NodeId, "blur", null);
        Assert.False(entry.IsFocused);

        coordinator.OnEvent(clearNode.NodeId, "click", null);

        Assert.Equal(string.Empty, entry.Text);
        Assert.True(entry.IsFocused);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);
        Assert.True(runtime.LastFocusState);
    }

    [Fact]
    public void EntryHandler_KeyboardSubmit_UsesDeterministicEnterKeyPath()
    {
        var completedCount = 0;
        var entry = new Entry { IsEnabled = true };
        entry.Completed += (_, _) => completedCount++;
        var (handler, _, coordinator, _) = CreateAttachedHandler(entry);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "keydown", "Escape");
        coordinator.OnEvent(inputNode.NodeId, "keydown", "Enter");
        Assert.Equal(1, completedCount);

        entry.IsEnabled = false;
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(IView.IsEnabled));
        coordinator.OnEvent(inputNode.NodeId, "keydown", "Enter");
        Assert.Equal(1, completedCount);
    }

    [Fact]
    public void EntryHandler_KeyboardSubmit_DoesNotCompleteWhileComposing()
    {
        var completedCount = 0;
        var entry = new Entry { IsEnabled = true };
        entry.Completed += (_, _) => completedCount++;
        var (handler, _, coordinator, _) = CreateAttachedHandler(entry);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "compositionstart", null);
        coordinator.OnEvent(inputNode.NodeId, "keydown", "Enter");
        Assert.Equal(0, completedCount);

        coordinator.OnEvent(inputNode.NodeId, "compositionend", null);
        coordinator.OnEvent(inputNode.NodeId, "keydown", "Enter");
        Assert.Equal(1, completedCount);
    }

    [Fact]
    public void EntryHandler_DateAndTimeKeyboard_PreserveTextInputSemantics()
    {
        var entry = new Entry
        {
            Keyboard = Keyboard.Date
        };
        var (handler, _, _, _) = CreateAttachedHandler(entry);
        var inputNode = GetInputNode(handler);

        Assert.Equal("text", inputNode.Attributes["type"]);
        Assert.Equal("numeric", inputNode.Attributes["inputmode"]);

        entry.Keyboard = Keyboard.Time;
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(IEntry.Keyboard));

        Assert.Equal("text", inputNode.Attributes["type"]);
        Assert.Equal("numeric", inputNode.Attributes["inputmode"]);
    }

    [Fact]
    public void EntryHandler_CursorSelection_MapsVirtualViewAndRuntimeRoundTrip()
    {
        var entry = new Entry
        {
            Text = "abcdef",
            CursorPosition = 2,
            SelectionLength = 3
        };
        var (handler, _, coordinator, runtime) = CreateAttachedHandler(entry);
        var inputNode = GetInputNode(handler);

        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out var start, out var end));
        Assert.Equal(2, start);
        Assert.Equal(5, end);

        runtime.SetSelection(inputNode.NodeId, 1, 4);
        coordinator.OnEvent(inputNode.NodeId, "select", null);

        Assert.Equal(1, entry.CursorPosition);
        Assert.Equal(3, entry.SelectionLength);

        entry.CursorPosition = 0;
        entry.SelectionLength = 2;
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(IEntry.CursorPosition));
        MirageEntryHandler.Mapper.UpdateProperty(handler, entry, nameof(IEntry.SelectionLength));

        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out start, out end));
        Assert.Equal(0, start);
        Assert.Equal(2, end);
    }

    [Fact]
    public void EntryHandler_FocusAndUnfocusCommands_TargetInputNode()
    {
        var entry = new Entry { IsEnabled = true };
        var (handler, _, _, runtime) = CreateAttachedHandler(entry);
        var inputNode = GetInputNode(handler);

        MirageEntryHandler.MapFocus(handler, entry, args: null);
        MirageEntryHandler.MapUnfocus(handler, entry, args: null);

        Assert.Equal(2, runtime.FocusCallCount);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);
        Assert.False(runtime.LastFocusState);
    }

    private static (MirageEntryHandler Handler, Entry Entry, MirageRenderCoordinator Coordinator, EntryRuntimeProbe Runtime) CreateAttachedHandler(Entry entry)
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new EntryRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageEntryHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(entry);
        return (handler, entry, coordinator, runtime);
    }

    private static MiragePlatformView GetInputNode(MirageEntryHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "input", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-entry-input", StringComparer.Ordinal));
    }

    private static MiragePlatformView GetClearButtonNode(MirageEntryHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "button", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-entry-clear", StringComparer.Ordinal));
    }

    private sealed class EntryRuntimeProbe : IMirageRuntime
    {
        private readonly Dictionary<string, (int Start, int End)> _selectionByNodeId = new(StringComparer.Ordinal);

        public string? LastFocusedNodeId { get; private set; }

        public bool LastFocusState { get; private set; }

        public int FocusCallCount { get; private set; }

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

        public bool TrySetNodeFocus(string nodeId, bool isFocused)
        {
            FocusCallCount++;
            LastFocusedNodeId = nodeId;
            LastFocusState = isFocused;
            return true;
        }

        public bool TryGetNodeTextSelection(string nodeId, out int selectionStart, out int selectionEnd)
        {
            if (_selectionByNodeId.TryGetValue(nodeId, out var selection))
            {
                selectionStart = selection.Start;
                selectionEnd = selection.End;
                return true;
            }

            selectionStart = 0;
            selectionEnd = 0;
            return false;
        }

        public bool TrySetNodeTextSelection(string nodeId, int selectionStart, int selectionEnd)
        {
            _selectionByNodeId[nodeId] = (selectionStart, selectionEnd);
            return true;
        }

        public void SetSelection(string nodeId, int start, int end)
        {
            _selectionByNodeId[nodeId] = (start, end);
        }

        public bool TryGetSelection(string nodeId, out int start, out int end)
        {
            if (_selectionByNodeId.TryGetValue(nodeId, out var selection))
            {
                start = selection.Start;
                end = selection.End;
                return true;
            }

            start = 0;
            end = 0;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
