using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageRadioButtonHandlerTests
{
    [Fact]
    public void RadioButtonHandler_ProjectsBuiltInControlKind_AndHostStructure()
    {
        var (handler, _, _) = CreateAttachedHandler(new RadioButton
        {
            Content = "Option A"
        });

        Assert.Equal(ComponentKind.RadioButton, handler.PlatformView.ControlKind);
        Assert.Equal("inline-flex", handler.PlatformView.GetStyleValue("display"));

        var inputNode = GetInputNode(handler);
        var contentSlotNode = GetContentSlotNode(handler);
        var textNode = GetTextNode(handler);

        Assert.Equal("input", inputNode.ElementName);
        Assert.Equal("div", contentSlotNode.ElementName);
        Assert.Equal("span", textNode.ElementName);
        Assert.Contains("mirage-radiobutton-input", inputNode.Classes);
        Assert.Contains("mirage-radiobutton-content", contentSlotNode.Classes);
        Assert.Contains("mirage-radiobutton-text", textNode.Classes);
        Assert.Equal("Option A", textNode.GetPropertyString("textContent"));
    }

    [Fact]
    public void RadioButtonHandler_MapsCoreVisualProperties_Deterministically()
    {
        var radio = new RadioButton
        {
            Content = "Alpha",
            IsChecked = true,
            Background = Brush.Bisque,
            BorderColor = Colors.Crimson,
            BorderWidth = 2,
            CornerRadius = 6,
            TextColor = Colors.CornflowerBlue,
            CharacterSpacing = 1.25
        };
        var (handler, _, _) = CreateAttachedHandler(radio);
        var inputNode = GetInputNode(handler);
        var textNode = GetTextNode(handler);

        Assert.Equal("true", inputNode.GetPropertyString("checked"));
        Assert.Equal("true", inputNode.Attributes["aria-checked"]);
        Assert.Equal("2px", handler.PlatformView.GetStyleValue("borderWidth"));
        Assert.Equal("rgba(220,20,60,1)", handler.PlatformView.GetStyleValue("borderColor"));
        Assert.Equal("6px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Equal("rgba(255,228,196,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
        Assert.Equal("rgba(100,149,237,1)", textNode.GetStyleValue("color"));
        Assert.Equal("1.25px", textNode.GetStyleValue("letterSpacing"));
    }

    [Fact]
    public void RadioButtonHandler_IsChecked_SynchronizesRuntimeAndVirtualView()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, radio, _) = CreateAttachedHandler(new RadioButton
        {
            Content = "Runtime sync",
            IsEnabled = true
        }, coordinator);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "checkedchange", "true");
        Assert.True(radio.IsChecked);

        radio.IsChecked = false;
        MirageRadioButtonHandler.Mapper.UpdateProperty(handler, radio, nameof(IRadioButton.IsChecked));
        Assert.Equal("false", inputNode.GetPropertyString("checked"));
    }

    [Fact]
    public void RadioButtonHandler_HostClick_SelectsUncheckedRadio()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, radio, _) = CreateAttachedHandler(new RadioButton
        {
            Content = "Host click",
            IsEnabled = true,
            IsChecked = false
        }, coordinator);

        coordinator.OnEvent(handler.PlatformView.NodeId, "click", null);
        Assert.True(radio.IsChecked);
    }

    [Fact]
    public void RadioButtonHandler_DisabledState_BlocksRuntimeCheckedMutation()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, radio, _) = CreateAttachedHandler(new RadioButton
        {
            Content = "Disabled",
            IsChecked = false,
            IsEnabled = true
        }, coordinator);
        var inputNode = GetInputNode(handler);

        coordinator.OnEvent(inputNode.NodeId, "checkedchange", "true");
        Assert.True(radio.IsChecked);

        radio.IsChecked = false;
        radio.IsEnabled = false;
        MirageRadioButtonHandler.Mapper.UpdateProperty(handler, radio, nameof(IRadioButton.IsChecked));
        MirageRadioButtonHandler.Mapper.UpdateProperty(handler, radio, nameof(IView.IsEnabled));

        coordinator.OnEvent(inputNode.NodeId, "checkedchange", "true");
        coordinator.OnEvent(handler.PlatformView.NodeId, "click", null);
        Assert.False(radio.IsChecked);
        Assert.Equal("disabled", inputNode.Attributes["disabled"]);
    }

    [Fact]
    public void RadioButtonHandler_GroupNameProjection_UsesNamedAndParentScopeGroups()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var namedGroupHost = new VerticalStackLayout();
        RadioButtonGroup.SetGroupName(namedGroupHost, "named-group");
        var namedA = new RadioButton { Content = "A", Value = "A" };
        var namedB = new RadioButton { Content = "B", Value = "B" };
        namedGroupHost.Children.Add(namedA);
        namedGroupHost.Children.Add(namedB);

        var namedAHandler = AttachHandler(namedA, mauiContext);
        var namedBHandler = AttachHandler(namedB, mauiContext);
        var namedAInput = GetInputNode(namedAHandler);
        var namedBInput = GetInputNode(namedBHandler);

        Assert.Equal("named-group", namedAInput.Attributes["name"]);
        Assert.Equal("named-group", namedBInput.Attributes["name"]);

        var parentScopeHost = new VerticalStackLayout();
        var parentA = new RadioButton { Content = "Parent A", Value = "PA" };
        var parentB = new RadioButton { Content = "Parent B", Value = "PB" };
        parentScopeHost.Children.Add(parentA);
        parentScopeHost.Children.Add(parentB);

        var parentAHandler = AttachHandler(parentA, mauiContext);
        var parentBHandler = AttachHandler(parentB, mauiContext);
        var parentAInput = GetInputNode(parentAHandler);
        var parentBInput = GetInputNode(parentBHandler);

        coordinator.OnEvent(parentAInput.NodeId, "checkedchange", "true");
        coordinator.OnEvent(parentBInput.NodeId, "checkedchange", "true");

        Assert.False(parentA.IsChecked);
        Assert.True(parentB.IsChecked);
        Assert.False(parentAInput.Attributes.ContainsKey("name"));
        Assert.False(parentBInput.Attributes.ContainsKey("name"));
    }

    [Fact]
    public void RadioButtonHandler_GroupNameProjection_UpdatesWhenParentAttachedGroupNameChanges()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var host = new VerticalStackLayout();
        var radio = new RadioButton
        {
            Content = "Dynamic group",
            Value = "dynamic"
        };
        host.Children.Add(radio);

        var handler = AttachHandler(radio, mauiContext);
        var inputNode = GetInputNode(handler);

        Assert.False(inputNode.Attributes.ContainsKey("name"));

        RadioButtonGroup.SetGroupName(host, "dynamic-group");
        Assert.Equal("dynamic-group", inputNode.Attributes["name"]);

        RadioButtonGroup.SetGroupName(host, null);
        Assert.False(inputNode.Attributes.ContainsKey("name"));
    }

    [Fact]
    public void RadioButtonHandler_GroupSelection_RoundTripsSelectedValue()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var groupHost = new VerticalStackLayout();
        RadioButtonGroup.SetGroupName(groupHost, "pet-group");
        var cat = new RadioButton { Content = "Cat", Value = "cat" };
        var dog = new RadioButton { Content = "Dog", Value = "dog" };
        groupHost.Children.Add(cat);
        groupHost.Children.Add(dog);

        var catHandler = AttachHandler(cat, mauiContext);
        var dogHandler = AttachHandler(dog, mauiContext);
        var catInput = GetInputNode(catHandler);
        var dogInput = GetInputNode(dogHandler);

        RadioButtonGroup.SetSelectedValue(groupHost, "cat");
        Assert.True(cat.IsChecked);
        Assert.False(dog.IsChecked);
        MirageRadioButtonHandler.Mapper.UpdateProperty(catHandler, cat, nameof(IRadioButton.IsChecked));
        MirageRadioButtonHandler.Mapper.UpdateProperty(dogHandler, dog, nameof(IRadioButton.IsChecked));
        Assert.Equal("true", catInput.GetPropertyString("checked"));
        Assert.Equal("false", dogInput.GetPropertyString("checked"));

        coordinator.OnEvent(dogInput.NodeId, "checkedchange", "true");
        Assert.False(cat.IsChecked);
        Assert.True(dog.IsChecked);
        Assert.Equal("dog", RadioButtonGroup.GetSelectedValue(groupHost));
    }

    [Fact]
    public void RadioButtonHandler_ContentProjection_SupportsTextAndViewContent()
    {
        var (handler, radio, _) = CreateAttachedHandler(new RadioButton
        {
            Content = "Initial text"
        });
        var contentSlotNode = GetContentSlotNode(handler);
        Assert.Equal("Initial text", GetTextNode(handler).GetPropertyString("textContent"));

        var contentLabel = new Label
        {
            Text = "Hosted view"
        };
        var contentLabelHandler = new MirageLabelHandler();
        ((IElementHandler)contentLabelHandler).SetMauiContext(((IElementHandler)handler).MauiContext!);
        ((IElementHandler)contentLabelHandler).SetVirtualView(contentLabel);
        radio.Content = contentLabel;
        MirageRadioButtonHandler.Mapper.UpdateProperty(handler, radio, nameof(IRadioButton.Content));
        Assert.Single(contentSlotNode.Children);
        Assert.Equal(PlatformNodeType.Control, contentSlotNode.Children[0].NodeType);
        Assert.Equal(ComponentKind.Label, contentSlotNode.Children[0].ControlKind);

        radio.Content = "Back to text";
        MirageRadioButtonHandler.Mapper.UpdateProperty(handler, radio, nameof(IRadioButton.Content));
        Assert.Single(contentSlotNode.Children);
        Assert.Equal("span", contentSlotNode.Children[0].ElementName);
        Assert.Equal("Back to text", contentSlotNode.Children[0].GetPropertyString("textContent"));
    }

    [Fact]
    public void RadioButtonHandler_ViewContent_ResolvesAriaLabelFromNestedTextContent()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var nestedLabel = new Label
        {
            Text = "Earth (View Content)"
        };
        var nestedLabelHandler = new MirageLabelHandler();
        ((IElementHandler)nestedLabelHandler).SetMauiContext(mauiContext);
        ((IElementHandler)nestedLabelHandler).SetVirtualView(nestedLabel);

        var nestedContent = new ContentView
        {
            Content = nestedLabel
        };
        var nestedContentHandler = new MirageContentViewHandler();
        ((IElementHandler)nestedContentHandler).SetMauiContext(mauiContext);
        ((IElementHandler)nestedContentHandler).SetVirtualView(nestedContent);

        var radio = new RadioButton
        {
            Content = nestedContent
        };

        var handler = AttachHandler(radio, mauiContext);
        var inputNode = GetInputNode(handler);
        Assert.Equal("Earth (View Content)", inputNode.Attributes["aria-label"]);

        nestedLabel.Text = "Venus (Updated)";
        Assert.Equal("Venus (Updated)", inputNode.Attributes["aria-label"]);
    }

    [Fact]
    public void RadioButtonHandler_GroupObservation_RebindsWhenVirtualViewIsReplaced()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var hostA = new VerticalStackLayout();
        RadioButtonGroup.SetGroupName(hostA, "group-a");
        var radioA = new RadioButton
        {
            Content = "A",
            Value = "a"
        };
        hostA.Children.Add(radioA);

        var hostB = new VerticalStackLayout();
        RadioButtonGroup.SetGroupName(hostB, "group-b");
        var radioB = new RadioButton
        {
            Content = "B",
            Value = "b"
        };
        hostB.Children.Add(radioB);

        var handler = new MirageRadioButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(radioA);
        var inputNode = GetInputNode(handler);
        Assert.Equal("group-a", inputNode.Attributes["name"]);

        ((IElementHandler)handler).SetVirtualView(radioB);
        inputNode = GetInputNode(handler);
        Assert.Equal("group-b", inputNode.Attributes["name"]);

        RadioButtonGroup.SetGroupName(hostA, "group-a-updated");
        Assert.Equal("group-b", inputNode.Attributes["name"]);

        RadioButtonGroup.SetGroupName(hostB, "group-b-updated");
        Assert.Equal("group-b-updated", inputNode.Attributes["name"]);
    }

    [Fact]
    public void RadioButtonHandler_TextTransform_ProjectsConcreteControlsSemantics()
    {
        var radio = new RadioButton
        {
            Content = "MiXeD",
            TextTransform = TextTransform.Uppercase
        };
        var (handler, _, _) = CreateAttachedHandler(radio);

        Assert.Equal("MIXED", GetTextNode(handler).GetPropertyString("textContent"));

        radio.TextTransform = TextTransform.Lowercase;
        MirageRadioButtonHandler.Mapper.UpdateProperty(handler, radio, nameof(RadioButton.TextTransform));
        Assert.Equal("mixed", GetTextNode(handler).GetPropertyString("textContent"));
    }

    [Fact]
    public void RadioButtonHandler_StrokeAndCornerProjection_AreDeterministicBrowserApproximation()
    {
        var radio = new RadioButton
        {
            Content = "Styled",
            BorderColor = Colors.MediumPurple,
            BorderWidth = 3,
            CornerRadius = 9,
            Background = Brush.SkyBlue
        };
        var (handler, _, _) = CreateAttachedHandler(radio);

        Assert.Equal("solid", handler.PlatformView.GetStyleValue("borderStyle"));
        Assert.Equal("3px", handler.PlatformView.GetStyleValue("borderWidth"));
        Assert.Equal("rgba(147,112,216,1)", handler.PlatformView.GetStyleValue("borderColor"));
        Assert.Equal("9px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Equal("rgba(135,206,235,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
    }

    private static (MirageRadioButtonHandler Handler, RadioButton RadioButton, MirageRenderCoordinator Coordinator) CreateAttachedHandler(
        RadioButton radioButton,
        MirageRenderCoordinator? coordinator = null)
    {
        coordinator ??= new MirageRenderCoordinator(new BrowserDispatcher());

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = AttachHandler(radioButton, mauiContext);
        return (handler, radioButton, coordinator);
    }

    private static MirageRadioButtonHandler AttachHandler(RadioButton radioButton, MauiContext mauiContext)
    {
        var handler = new MirageRadioButtonHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(radioButton);
        return handler;
    }

    private static MiragePlatformView GetInputNode(MirageRadioButtonHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "input", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-radiobutton-input", StringComparer.Ordinal));
    }

    private static MiragePlatformView GetContentSlotNode(MirageRadioButtonHandler handler)
    {
        return handler.PlatformView.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "div", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-radiobutton-content", StringComparer.Ordinal));
    }

    private static MiragePlatformView GetTextNode(MirageRadioButtonHandler handler)
    {
        var contentSlotNode = GetContentSlotNode(handler);
        return contentSlotNode.Children
            .Where(static child => child.NodeType == PlatformNodeType.Web)
            .Where(static child => string.Equals(child.ElementName, "span", StringComparison.Ordinal))
            .Single(static child => child.Classes.Contains("mirage-radiobutton-text", StringComparer.Ordinal));
    }
}
