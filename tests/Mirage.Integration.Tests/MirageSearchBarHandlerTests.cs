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

public sealed class MirageSearchBarHandlerTests
{
    [Fact]
    public void SearchBarHandler_ProjectsBuiltInControlKind_AndDefaultStructure()
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar
        {
            Text = "query",
            Placeholder = "Search"
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);

        Assert.Equal(ComponentKind.SearchBar, handler.PlatformView.ControlKind);
        Assert.Equal("flex", handler.PlatformView.GetStyleValue("display"));
        Assert.NotNull(GetInputNode(handler));
        Assert.NotNull(GetClearButtonNode(handler));
        Assert.NotNull(GetSubmitButtonNode(handler));
    }

    [Fact]
    public void SearchBarHandler_TextAndPlaceholderProjection_AreDeterministic()
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar
        {
            Text = "first",
            Placeholder = "Find value"
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);
        Assert.Equal("first", inputNode.GetPropertyString("value"));
        Assert.Equal("Find value", inputNode.Attributes["placeholder"]);

        searchBar.Text = "second";
        searchBar.Placeholder = "New placeholder";
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(ISearchBar.Text));
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(ISearchBar.Placeholder));

        Assert.Equal("second", inputNode.GetPropertyString("value"));
        Assert.Equal("New placeholder", inputNode.Attributes["placeholder"]);
    }

    [Fact]
    public void SearchBarHandler_AutomationIdProjection_UpdatesHostAndChildNodes()
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar();

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);
        var clearNode = GetClearButtonNode(handler);
        var submitNode = GetSubmitButtonNode(handler);

        Assert.Null(handler.PlatformView.GetPropertyString("AutomationId"));
        Assert.False(inputNode.Attributes.ContainsKey("data-automation-id"));
        Assert.False(clearNode.Attributes.ContainsKey("data-automation-id"));
        Assert.False(submitNode.Attributes.ContainsKey("data-automation-id"));

        searchBar.AutomationId = "search-a";
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(IView.AutomationId));

        Assert.Equal("search-a", handler.PlatformView.GetPropertyString("AutomationId"));
        Assert.Equal("search-a-input", inputNode.Attributes["data-automation-id"]);
        Assert.Equal("search-a-clear-button", clearNode.Attributes["data-automation-id"]);
        Assert.Equal("search-a-search-button", submitNode.Attributes["data-automation-id"]);
    }

    [Fact]
    public void SearchBarHandler_RuntimeInputAndSearchEvents_UpdateVirtualView()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var handler = CreateHandler(coordinator);
        var searchCount = 0;
        var searchBar = new SearchBar
        {
            Text = string.Empty,
            Placeholder = "Type"
        };
        searchBar.SearchButtonPressed += (_, _) => searchCount++;

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "input", "alpha");
        Assert.Equal("alpha", searchBar.Text);

        coordinator.OnEvent(inputNode.NodeId, "search", "alpha");
        Assert.Equal(1, searchCount);
    }

    [Fact]
    public void SearchBarHandler_SearchEventFromClearTransition_DoesNotInvokeSearch()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var handler = CreateHandler(coordinator);
        var searchCount = 0;
        var searchBar = new SearchBar
        {
            Text = "alpha"
        };
        searchBar.SearchButtonPressed += (_, _) => searchCount++;

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "search", string.Empty);

        Assert.Equal(string.Empty, searchBar.Text);
        Assert.Equal(0, searchCount);
    }

    [Fact]
    public void SearchBarHandler_InputThenSearchClearSequence_DoesNotInvokeSearch()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var handler = CreateHandler(coordinator);
        var searchCount = 0;
        var searchBar = new SearchBar
        {
            Text = "alpha"
        };
        searchBar.SearchButtonPressed += (_, _) => searchCount++;

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "input", string.Empty);
        coordinator.OnEvent(inputNode.NodeId, "search", string.Empty);

        Assert.Equal(string.Empty, searchBar.Text);
        Assert.Equal(0, searchCount);
    }

    [Fact]
    public void SearchBarHandler_InputThenKeyboardSubmitOnEmpty_InvokesSearch()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var handler = CreateHandler(coordinator);
        var searchCount = 0;
        var searchBar = new SearchBar
        {
            Text = "alpha"
        };
        searchBar.SearchButtonPressed += (_, _) => searchCount++;

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "input", string.Empty);
        coordinator.OnEvent(inputNode.NodeId, "keydown", "Enter");
        coordinator.OnEvent(inputNode.NodeId, "search", string.Empty);

        Assert.Equal(string.Empty, searchBar.Text);
        Assert.Equal(1, searchCount);
    }

    [Fact]
    public void SearchBarHandler_DisabledState_PreventsSearchAndClearActions()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var handler = CreateHandler(coordinator);
        var searchCount = 0;
        var searchBar = new SearchBar
        {
            Text = "active",
            IsReadOnly = false,
            IsEnabled = true
        };
        searchBar.SearchButtonPressed += (_, _) => searchCount++;

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var clearButton = GetClearButtonNode(handler);
        var submitButton = GetSubmitButtonNode(handler);

        coordinator.OnEvent(clearButton.NodeId, "click", null);
        Assert.Equal(string.Empty, searchBar.Text);

        searchBar.Text = "active";
        searchBar.IsReadOnly = true;
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(ISearchBar.Text));
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(ISearchBar.IsReadOnly));
        coordinator.OnEvent(clearButton.NodeId, "click", null);
        Assert.Equal("active", searchBar.Text);

        searchBar.IsReadOnly = false;
        searchBar.IsEnabled = false;
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(IView.IsEnabled));
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(ISearchBar.IsReadOnly));

        coordinator.OnEvent(clearButton.NodeId, "click", null);
        coordinator.OnEvent(submitButton.NodeId, "click", null);
        Assert.Equal("active", searchBar.Text);
        Assert.Equal(0, searchCount);
    }

    [Fact]
    public void SearchBarHandler_ColorChannels_ProjectToDeterministicButtonStyles()
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar
        {
            Text = "value",
            CancelButtonColor = Colors.Crimson,
            SearchIconColor = Colors.CornflowerBlue
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var clearButton = GetClearButtonNode(handler);
        var submitButton = GetSubmitButtonNode(handler);

        Assert.Equal("rgba(220,20,60,1)", clearButton.GetStyleValue("color"));
        Assert.Equal("rgba(100,149,237,1)", submitButton.GetStyleValue("color"));
    }

    [Fact]
    public void SearchBarHandler_MapsKeyboardReturnPredictionSpellcheckAndAlignment()
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar
        {
            Keyboard = Keyboard.Email,
            ReturnType = ReturnType.Search,
            IsTextPredictionEnabled = false,
            IsSpellCheckEnabled = false,
            MaxLength = 24,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.End
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);

        Assert.Equal("search", inputNode.Attributes["type"]);
        Assert.Equal("email", inputNode.Attributes["inputmode"]);
        Assert.Equal("search", inputNode.Attributes["enterkeyhint"]);
        Assert.Equal("off", inputNode.Attributes["autocorrect"]);
        Assert.Equal("off", inputNode.Attributes["autocomplete"]);
        Assert.Equal("false", inputNode.Attributes["spellcheck"]);
        Assert.Equal("24", inputNode.Attributes["maxlength"]);
        Assert.Equal("center", inputNode.GetStyleValue("textAlign"));
        Assert.Equal("6px", inputNode.GetStyleValue("paddingTop"));
        Assert.Equal("2px", inputNode.GetStyleValue("paddingBottom"));
    }

    [Fact]
    public void SearchBarHandler_NonDefaultKeyboard_StillSupportsKeyboardSearchSubmitEvent()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var handler = CreateHandler(coordinator);
        var searchCount = 0;
        var searchBar = new SearchBar
        {
            Keyboard = Keyboard.Email,
            Text = "alpha"
        };
        searchBar.SearchButtonPressed += (_, _) => searchCount++;

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);

        Assert.Equal("search", inputNode.Attributes["type"]);
        Assert.Equal("email", inputNode.Attributes["inputmode"]);

        coordinator.OnEvent(inputNode.NodeId, "search", "alpha");
        Assert.Equal(1, searchCount);
    }

    [Theory]
    [InlineData("FontFamily")]
    [InlineData("FontSize")]
    [InlineData("FontAttributes")]
    [InlineData("FontAutoScalingEnabled")]
    public void SearchBarHandler_FontAliasMapperKeys_UpdateInnerInputProjection(string mapperKey)
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar
        {
            Text = "value",
            FontFamily = "OpenSansRegular",
            FontSize = 19,
            FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
            FontAutoScalingEnabled = true
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);

        inputNode.SetStyle("fontFamily", "\"stale\", sans-serif");
        inputNode.SetStyle("fontSize", "1px");
        inputNode.SetStyle("fontWeight", "100");
        inputNode.SetStyle("fontStyle", "normal");

        switch (mapperKey)
        {
            case "FontFamily":
                searchBar.FontFamily = "Space Grotesk";
                break;
            case "FontSize":
                searchBar.FontSize = 27;
                break;
            case "FontAttributes":
                searchBar.FontAttributes = FontAttributes.Bold;
                break;
            case "FontAutoScalingEnabled":
                searchBar.FontAutoScalingEnabled = false;
                break;
            default:
                throw new InvalidOperationException($"Unexpected mapper key '{mapperKey}'.");
        }

        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, mapperKey);

        Assert.Equal(handler.PlatformView.GetStyleValue("fontFamily"), inputNode.GetStyleValue("fontFamily"));
        Assert.Equal(handler.PlatformView.GetStyleValue("fontSize"), inputNode.GetStyleValue("fontSize"));
        Assert.Equal(handler.PlatformView.GetStyleValue("fontWeight"), inputNode.GetStyleValue("fontWeight"));
        Assert.Equal(handler.PlatformView.GetStyleValue("fontStyle"), inputNode.GetStyleValue("fontStyle"));
    }

    [Fact]
    public void SearchBarHandler_MapsPlaceholderColorToken()
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar
        {
            Placeholder = "Find",
            PlaceholderColor = Colors.Goldenrod
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);
        Assert.Equal("rgba(218,165,32,1)", inputNode.GetStyleValue("--mirage-searchbar-placeholder-color"));
    }

    [Fact]
    public void SearchBarHandler_TextProjection_DoesNotOverrideCollapsedVisibility()
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar
        {
            Text = "first"
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);

        searchBar.IsVisible = false;
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, "IsVisible");
        Assert.Equal("none", handler.PlatformView.GetStyleValue("display"));

        searchBar.Text = "second";
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(ISearchBar.Text));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("display"));
    }

    [Fact]
    public void SearchBarHandler_TextProjection_DoesNotOverrideSizeRequestStyles()
    {
        var handler = CreateHandler();
        var searchBar = new SearchBar
        {
            WidthRequest = 280,
            MinimumHeightRequest = 48
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(VisualElement.WidthRequest));
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(VisualElement.MinimumHeightRequest));

        Assert.Equal("280px", handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("48px", handler.PlatformView.GetStyleValue("minHeight"));

        searchBar.Text = "updated";
        MirageSearchBarHandler.Mapper.UpdateProperty(handler, searchBar, nameof(ISearchBar.Text));

        Assert.Equal("280px", handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("48px", handler.PlatformView.GetStyleValue("minHeight"));
    }

    [Fact]
    public void SearchBarHandler_FocusAndUnfocusCommands_TargetInputNodeViaRuntimeSeam()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new FocusRuntimeProbe();
        coordinator.AttachRuntime(runtime);
        var handler = CreateHandler(coordinator);
        var searchBar = new SearchBar
        {
            IsEnabled = true
        };

        ((IElementHandler)handler).SetVirtualView(searchBar);
        var inputNode = GetInputNode(handler);

        MirageSearchBarHandler.MapFocus(handler, searchBar, null);
        Assert.Equal(1, runtime.FocusCallCount);
        Assert.True(runtime.LastFocusState);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);

        MirageSearchBarHandler.MapUnfocus(handler, searchBar, null);
        Assert.Equal(2, runtime.FocusCallCount);
        Assert.False(runtime.LastFocusState);
        Assert.Equal(inputNode.NodeId, runtime.LastFocusedNodeId);
    }

    private static MirageSearchBarHandler CreateHandler(MirageRenderCoordinator? coordinator = null)
    {
        var services = new ServiceCollection();
        if (coordinator is not null)
        {
            services.AddSingleton<IMirageRenderCoordinator>(coordinator);
        }

        var handler = new MirageSearchBarHandler();
        ((IElementHandler)handler).SetMauiContext(new MauiContext(services.BuildServiceProvider()));
        return handler;
    }

    private static MiragePlatformView GetInputNode(MirageSearchBarHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "input", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-searchbar-input", StringComparer.Ordinal));
    }

    private static MiragePlatformView GetClearButtonNode(MirageSearchBarHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "button", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-searchbar-clear", StringComparer.Ordinal));
    }

    private static MiragePlatformView GetSubmitButtonNode(MirageSearchBarHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "button", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-searchbar-submit", StringComparer.Ordinal));
    }

    private sealed class FocusRuntimeProbe : IMirageRuntime
    {
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

        public bool TryOpenNodePicker(string nodeId)
        {
            _ = nodeId;
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
            return true;
        }

        public void Dispose()
        {
        }
    }
}
