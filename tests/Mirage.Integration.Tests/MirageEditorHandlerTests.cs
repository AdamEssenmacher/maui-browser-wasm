using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageEditorHandlerTests
{
    [Fact]
    public void EditorHandler_ProjectsBuiltInControlKind_AndHostStructure()
    {
        var (handler, _, _, _) = CreateAttachedHandler(new Editor
        {
            Text = "hello"
        });

        Assert.Equal(ComponentKind.Editor, handler.PlatformView.ControlKind);
        Assert.Equal("flex", handler.PlatformView.GetStyleValue("display"));

        var inputNode = GetInputNode(handler);
        Assert.Equal("textarea", inputNode.ElementName);
        Assert.Contains("mirage-editor-input", inputNode.Classes);
    }

    [Fact]
    public void EditorHandler_MapsCoreTextInputProperties_Deterministically()
    {
        var editor = new Editor
        {
            Text = "alpha\nbeta",
            Placeholder = "Type paragraph",
            PlaceholderColor = Colors.CornflowerBlue,
            TextColor = Colors.Crimson,
            CharacterSpacing = 1.5,
            Keyboard = Keyboard.Email,
            MaxLength = 48,
            IsReadOnly = true,
            IsSpellCheckEnabled = false,
            IsTextPredictionEnabled = false,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.End
        };

        var (handler, _, _, _) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        Assert.Equal("alpha\nbeta", inputNode.GetPropertyString("value"));
        Assert.Equal("Type paragraph", inputNode.Attributes["placeholder"]);
        Assert.Equal("email", inputNode.Attributes["inputmode"]);
        Assert.Equal("48", inputNode.Attributes["maxlength"]);
        Assert.Equal("readonly", inputNode.Attributes["readonly"]);
        Assert.Equal("off", inputNode.Attributes["autocorrect"]);
        Assert.Equal("off", inputNode.Attributes["autocomplete"]);
        Assert.Equal("false", inputNode.Attributes["spellcheck"]);
        Assert.Equal("center", inputNode.GetStyleValue("textAlign"));
        Assert.Equal("8px", inputNode.GetStyleValue("paddingTop"));
        Assert.Equal("4px", inputNode.GetStyleValue("paddingBottom"));
        Assert.Equal("1.5px", inputNode.GetStyleValue("letterSpacing"));
        Assert.Equal("rgba(220,20,60,1)", inputNode.GetStyleValue("color"));
        Assert.Equal("rgba(100,149,237,1)", inputNode.GetStyleValue("--mirage-editor-placeholder-color"));
    }

    [Fact]
    public void EditorHandler_MaxLength_Zero_ProjectsNoInputAllowed()
    {
        var editor = new Editor
        {
            Text = "seed",
            MaxLength = 0
        };

        var (handler, _, _, _) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        Assert.Equal("0", inputNode.Attributes["maxlength"]);
    }

    [Fact]
    public void EditorHandler_TextTransform_ProjectsControlsRemapTextSemantics()
    {
        var editor = new Editor
        {
            Text = "Hello There",
            TextTransform = TextTransform.Uppercase
        };

        var (handler, _, _, _) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        Assert.Equal("HELLO THERE", inputNode.GetPropertyString("value"));
        Assert.Null(inputNode.GetStyleValue("textTransform"));

        editor.TextTransform = TextTransform.Lowercase;
        MirageEditorHandler.Mapper.UpdateProperty(handler, editor, nameof(InputView.TextTransform));
        Assert.Equal("hello there", inputNode.GetPropertyString("value"));
        Assert.Null(inputNode.GetStyleValue("textTransform"));
    }

    [Fact]
    public void EditorHandler_TextTransform_RuntimeInputProjectsTransformedTextChannel()
    {
        var editor = new Editor
        {
            Text = "baseline",
            TextTransform = TextTransform.Uppercase
        };

        var (handler, _, coordinator, _) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);
        coordinator.OnEvent(inputNode.NodeId, "input", "mixedCase text");

        Assert.Equal("MIXEDCASE TEXT", editor.Text);
        Assert.Equal("MIXEDCASE TEXT", inputNode.GetPropertyString("value"));
    }

    [Fact]
    public void EditorHandler_TextTransform_RuntimeInputPreservesSelectionIntent()
    {
        var editor = new Editor
        {
            Text = "baseline",
            TextTransform = TextTransform.Uppercase
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);
        runtime.SetSelection(inputNode.NodeId, 6, 6);
        var expectedTransformed = TextTransformUtilities.GetTransformedText("straße", TextTransform.Uppercase);

        coordinator.OnEvent(inputNode.NodeId, "input", "straße");

        Assert.Equal(expectedTransformed, editor.Text);
        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out var start, out var end));
        Assert.Equal(expectedTransformed.Length, start);
        Assert.Equal(expectedTransformed.Length, end);
    }

    [Fact]
    public void EditorHandler_TextTransform_RuntimeInputPreservesMidStringSelectionOffsets()
    {
        const string runtimeValue = "A\u0130B";
        const int runtimeSelectionStart = 1;
        const int runtimeSelectionEnd = 2;
        const TextTransform transform = TextTransform.Lowercase;
        var expectedStart = TextTransformUtilities.GetTransformedText(
            runtimeValue[..runtimeSelectionStart],
            transform).Length;
        var expectedEnd = TextTransformUtilities.GetTransformedText(
            runtimeValue[..runtimeSelectionEnd],
            transform).Length;
        var editor = new Editor
        {
            Text = "baseline",
            TextTransform = transform
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);
        runtime.SetSelection(inputNode.NodeId, runtimeSelectionStart, runtimeSelectionEnd);
        var expectedTransformed = TextTransformUtilities.GetTransformedText(runtimeValue, transform);

        coordinator.OnEvent(inputNode.NodeId, "input", runtimeValue);

        Assert.Equal(expectedTransformed, editor.Text);
        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out var actualStart, out var actualEnd));
        Assert.Equal(expectedStart, actualStart);
        Assert.Equal(expectedEnd, actualEnd);
    }

    [Fact]
    public void EditorHandler_TextTransform_DoesNotApplyCssTransformToPlaceholderChannel()
    {
        var editor = new Editor
        {
            Text = "alpha",
            Placeholder = "Case Sensitive Placeholder",
            TextTransform = TextTransform.Uppercase
        };

        var (handler, _, _, _) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        Assert.Equal("Case Sensitive Placeholder", inputNode.Attributes["placeholder"]);
        Assert.Null(inputNode.GetStyleValue("textTransform"));
    }

    [Fact]
    public void EditorHandler_DisabledState_DisablesInputInteraction()
    {
        var editor = new Editor
        {
            Text = "active",
            IsReadOnly = false,
            IsEnabled = true
        };
        var (handler, _, coordinator, _) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "input", "updated");
        Assert.Equal("updated", editor.Text);

        editor.IsEnabled = false;
        MirageEditorHandler.Mapper.UpdateProperty(handler, editor, nameof(IView.IsEnabled));
        coordinator.OnEvent(inputNode.NodeId, "input", "blocked");

        Assert.Equal("updated", editor.Text);
        Assert.Equal("disabled", inputNode.Attributes["disabled"]);
    }

    [Fact]
    public void EditorHandler_ReadOnlyState_DisablesInputInteraction()
    {
        var editor = new Editor
        {
            Text = "active",
            IsReadOnly = false,
            IsEnabled = true
        };
        var (handler, _, coordinator, _) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        editor.IsReadOnly = true;
        MirageEditorHandler.Mapper.UpdateProperty(handler, editor, nameof(IEditor.IsReadOnly));
        coordinator.OnEvent(inputNode.NodeId, "input", "blocked-by-readonly");

        Assert.Equal("active", editor.Text);
    }

    [Fact]
    public void EditorHandler_BlurEvent_InvokesCompleted()
    {
        var completedCount = 0;
        var editor = new Editor();
        editor.Completed += (_, _) => completedCount++;
        var (handler, _, coordinator, _) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "focus", null);
        Assert.True(editor.IsFocused);

        coordinator.OnEvent(inputNode.NodeId, "blur", null);
        Assert.False(editor.IsFocused);
        Assert.Equal(1, completedCount);

        coordinator.OnEvent(inputNode.NodeId, "blur", null);
        Assert.Equal(1, completedCount);
    }

    [Fact]
    public void EditorHandler_CursorSelection_MapsVirtualViewAndRuntimeRoundTrip()
    {
        var editor = new Editor
        {
            Text = "abcdef",
            CursorPosition = 2,
            SelectionLength = 3
        };
        var (handler, _, coordinator, runtime) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out var start, out var end));
        Assert.Equal(2, start);
        Assert.Equal(5, end);

        runtime.SetSelection(inputNode.NodeId, 1, 4);
        coordinator.OnEvent(inputNode.NodeId, "select", null);

        Assert.Equal(1, editor.CursorPosition);
        Assert.Equal(3, editor.SelectionLength);

        editor.CursorPosition = 0;
        editor.SelectionLength = 2;
        MirageEditorHandler.Mapper.UpdateProperty(handler, editor, nameof(IEditor.CursorPosition));
        MirageEditorHandler.Mapper.UpdateProperty(handler, editor, nameof(IEditor.SelectionLength));

        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out start, out end));
        Assert.Equal(0, start);
        Assert.Equal(2, end);
    }

    [Fact]
    public void EditorHandler_ProgrammaticSelection_WithTextTransform_UsesTransformedPrefixOffsets()
    {
        var editor = new Editor
        {
            Text = "A\u0130B",
            TextTransform = TextTransform.Lowercase,
            CursorPosition = 1,
            SelectionLength = 1
        };

        var (handler, _, _, runtime) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);
        var expectedStart = TextTransformUtilities.GetTransformedText(
            editor.Text![..editor.CursorPosition],
            editor.TextTransform).Length;
        var expectedEnd = TextTransformUtilities.GetTransformedText(
            editor.Text![..(editor.CursorPosition + editor.SelectionLength)],
            editor.TextTransform).Length;

        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out var start, out var end));
        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);

        editor.CursorPosition = 0;
        editor.SelectionLength = 2;
        MirageEditorHandler.Mapper.UpdateProperty(handler, editor, nameof(IEditor.CursorPosition));
        MirageEditorHandler.Mapper.UpdateProperty(handler, editor, nameof(IEditor.SelectionLength));

        expectedStart = TextTransformUtilities.GetTransformedText(
            editor.Text![..editor.CursorPosition],
            editor.TextTransform).Length;
        expectedEnd = TextTransformUtilities.GetTransformedText(
            editor.Text![..(editor.CursorPosition + editor.SelectionLength)],
            editor.TextTransform).Length;
        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out start, out end));
        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }

    [Fact]
    public void EditorHandler_RuntimeSelection_WithTextTransform_MapsBackToSourceOffsets()
    {
        const string sourceText = "A\u0130B";
        const TextTransform transform = TextTransform.Lowercase;
        const int expectedSourceCursor = 2;
        var editor = new Editor
        {
            Text = sourceText,
            TextTransform = transform
        };

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);
        var mappedRuntimeCursor = TextTransformUtilities.GetTransformedText(
            sourceText[..expectedSourceCursor],
            transform).Length;
        runtime.SetSelection(inputNode.NodeId, mappedRuntimeCursor, mappedRuntimeCursor);

        coordinator.OnEvent(inputNode.NodeId, "select", null);

        Assert.Equal(expectedSourceCursor, editor.CursorPosition);
        Assert.Equal(0, editor.SelectionLength);

        MirageEditorHandler.Mapper.UpdateProperty(handler, editor, nameof(IEditor.CursorPosition));
        Assert.True(runtime.TryGetSelection(inputNode.NodeId, out var projectedStart, out var projectedEnd));
        Assert.Equal(mappedRuntimeCursor, projectedStart);
        Assert.Equal(mappedRuntimeCursor, projectedEnd);
    }

    [Fact]
    public void EditorHandler_FocusCommand_TargetsTextAreaNode()
    {
        var editor = new Editor
        {
            IsEnabled = true
        };
        var (handler, _, _, runtime) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        MirageEditorHandler.MapFocus(handler, editor, args: null);

        Assert.Equal(1, runtime.FocusCallCount);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);
        Assert.True(runtime.LastFocusState);
    }

    [Fact]
    public void EditorHandler_UnfocusCommand_BlursInputNode()
    {
        var editor = new Editor
        {
            IsEnabled = true
        };
        var (handler, _, _, runtime) = CreateAttachedHandler(editor);
        var inputNode = GetInputNode(handler);

        MirageEditorHandler.MapUnfocus(handler, editor, args: null);

        Assert.Equal(1, runtime.FocusCallCount);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);
        Assert.False(runtime.LastFocusState);
    }

    [Fact]
    public void EditorHandler_Connect_RetriesInitialSelectionWhenRuntimeNodeIsNotYetRealized()
    {
        var editor = new Editor
        {
            Text = "abcdef",
            CursorPosition = 2,
            SelectionLength = 3
        };

        var coordinator = new DeferredSelectionCoordinator(failSetSelectionCallCount: 4);
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new MirageEditorHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        ((IElementHandler)handler).SetVirtualView(editor);

        Assert.True(coordinator.SetSelectionCallCount >= 2);
        Assert.True(coordinator.DispatchDelayedCallCount >= 1);
        Assert.True(coordinator.LastSetSelectionResult);
    }

    private static (MirageEditorHandler Handler, Editor Editor, MirageRenderCoordinator Coordinator, EditorRuntimeProbe Runtime) CreateAttachedHandler(Editor editor)
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new EditorRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageEditorHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(editor);
        return (handler, editor, coordinator, runtime);
    }

    private static MiragePlatformView GetInputNode(MirageEditorHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "textarea", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-editor-input", StringComparer.Ordinal));
    }

    private sealed class EditorRuntimeProbe : IMirageRuntime
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

        public bool TryRequestNodePickerOpen(string nodeId)
        {
            _ = nodeId;
            return false;
        }

        public bool TrySetNodeFocus(string nodeId, bool isFocused)
        {
            LastFocusedNodeId = nodeId;
            LastFocusState = isFocused;
            FocusCallCount++;
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

        public void Dispose()
        {
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

        public void SetSelection(string nodeId, int start, int end)
        {
            _selectionByNodeId[nodeId] = (start, end);
        }
    }

    private sealed class DeferredSelectionCoordinator : IMirageRenderCoordinator
    {
        private readonly int _failSetSelectionCallCount;
        private int _setSelectionCallCount;
        private int _nodeIdCounter;

        public DeferredSelectionCoordinator(int failSetSelectionCallCount)
        {
            _failSetSelectionCallCount = failSetSelectionCallCount;
        }

        public int DispatchDelayedCallCount { get; private set; }

        public int LastSelectionStart { get; private set; }

        public int LastSelectionEnd { get; private set; }

        public bool LastSetSelectionResult { get; private set; }

        public int SetSelectionCallCount => _setSelectionCallCount;

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
                platformView.AssignNodeId($"deferred-node-{localId:D4}");
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
            DispatchDelayedCallCount++;
            action();
            return true;
        }

        public IDisposable BeginRenderBatch()
        {
            return NullDisposable.Instance;
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
            LastSelectionStart = selectionStart;
            LastSelectionEnd = selectionEnd;
            _setSelectionCallCount++;
            LastSetSelectionResult = _setSelectionCallCount > _failSetSelectionCallCount;
            return LastSetSelectionResult;
        }

        public void OnEvent(string nodeId, string eventName, string? payload)
        {
            _ = nodeId;
            _ = eventName;
            _ = payload;
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
