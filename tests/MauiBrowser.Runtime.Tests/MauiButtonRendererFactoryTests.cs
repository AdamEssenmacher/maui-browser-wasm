#pragma warning disable BL0006
using System.Windows.Input;
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiButtonRendererFactoryTests
{
    [Fact]
    public void CanRender_RecognizesButtonOnly()
    {
        var factory = new MauiButtonRendererFactory();

        Assert.True(factory.CanRender(new Button()));
        Assert.False(factory.CanRender(new Label()));
    }

    [Fact]
    public void Create_EmitsButtonTextAndDisabledSemantics()
    {
        var factory = new MauiButtonRendererFactory();
        var button = new Button
        {
            Text = "Click me",
            IsEnabled = false
        };

        var fragment = factory.Create(button, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.True(HasElement(builder, "button"));
        Assert.True(HasAttribute(builder, "disabled"));
        Assert.Equal("true", FindAttributeValue(builder, "aria-disabled")?.ToString());

        var textContents = FindTextFrames(builder);
        Assert.Contains("Click me", textContents, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Create_ClickInvokesClickedAndCommand()
    {
        var factory = new MauiButtonRendererFactory();
        var command = new RecordingCommand();
        var button = new Button
        {
            Text = "Run action",
            Command = command,
            CommandParameter = "payload"
        };

        var clickedCount = 0;
        button.Clicked += (_, _) => clickedCount++;

        var fragment = factory.Create(button, RenderContext.Empty);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        await InvokeCallback(builder, "onclick");

        Assert.Equal(1, clickedCount);
        Assert.Equal(1, command.ExecuteCount);
        Assert.Equal("payload", command.LastParameter);
    }

    [Fact]
    public void Create_DoesNotEmitOnClickWhenDisabledFromSemantics()
    {
        var factory = new MauiButtonRendererFactory();
        var button = new Button { Text = "Cannot click" };
        var context = new RenderContext
        {
            StyleTokens = StyleTokens.Empty,
            Semantics = new MauiBrowser.Runtime.Rendering.Semantics
            {
                Disabled = true
            },
            Events = EventDispatcher.None
        };

        var fragment = factory.Create(button, context);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        Assert.False(HasAttribute(builder, "onclick"));
        Assert.True(HasAttribute(builder, "disabled"));
    }

    private static bool HasElement(RenderTreeBuilder builder, string elementName)
    {
        var frames = builder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Element
                && string.Equals(frame.ElementName, elementName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAttribute(RenderTreeBuilder builder, string attributeName)
    {
        return FindAttributeValue(builder, attributeName) is not null;
    }

    private static object? FindAttributeValue(RenderTreeBuilder builder, string attributeName)
    {
        var frames = builder.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute
                && string.Equals(frame.AttributeName, attributeName, StringComparison.Ordinal))
            {
                return frame.AttributeValue;
            }
        }

        return null;
    }

    private static async Task InvokeCallback(RenderTreeBuilder builder, string attributeName)
    {
        var attributeValue = FindAttributeValue(builder, attributeName);
        Assert.NotNull(attributeValue);

        switch (attributeValue)
        {
            case EventCallback eventCallback:
                await eventCallback.InvokeAsync(null);
                return;
            case MulticastDelegate callbackDelegate:
            {
                var parameterCount = callbackDelegate.Method.GetParameters().Length;
                if (parameterCount == 0)
                {
                    callbackDelegate.DynamicInvoke();
                    return;
                }

                if (parameterCount == 1)
                {
                    callbackDelegate.DynamicInvoke(null);
                    return;
                }

                break;
            }
        }

        throw new InvalidOperationException(
            $"Unsupported callback attribute type '{attributeValue.GetType().FullName}' for '{attributeName}'.");
    }

    private static IReadOnlyList<string> FindTextFrames(RenderTreeBuilder builder)
    {
        var frames = builder.GetFrames();
        var text = new List<string>();

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Text && frame.TextContent is not null)
            {
                text.Add(frame.TextContent);
            }
        }

        return text;
    }

    private sealed class RecordingCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add
            {
            }
            remove
            {
            }
        }

        public int ExecuteCount { get; private set; }

        public object? LastParameter { get; private set; }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            ExecuteCount++;
            LastParameter = parameter;
        }
    }
}
#pragma warning restore BL0006
