#pragma warning disable BL0006
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiStyleBridgeTests
{
    [Fact]
    public void Render_Label_ProjectsCssVariablesAndSemantics()
    {
        var registry = new RendererRegistry();
        registry.Register<Label>(new MauiLabelRendererFactory());
        var renderer = new MauiViewRenderer(registry);

        var label = new Label
        {
            Text = "Styled label",
            TextColor = Color.FromArgb("#112233"),
            BackgroundColor = Color.FromArgb("#445566"),
            FontSize = 18,
            FontFamily = "monospace",
            Opacity = 0.75,
            Margin = new Thickness(4, 6, 8, 10)
        };

        var fragment = renderer.Render(label);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var style = FindAttributeValue(builder, "style")?.ToString();
        Assert.NotNull(style);
        Assert.Contains("color:var(--maui-text-color)", style, StringComparison.Ordinal);
        Assert.Contains("background-color:var(--maui-background-color)", style, StringComparison.Ordinal);
        Assert.Contains("font-size:var(--maui-font-size)", style, StringComparison.Ordinal);
        Assert.Contains("font-family:var(--maui-font-family)", style, StringComparison.Ordinal);
        Assert.Contains("opacity:var(--maui-opacity)", style, StringComparison.Ordinal);
        Assert.Contains("margin:var(--maui-margin)", style, StringComparison.Ordinal);
        Assert.Contains("--maui-text-color:rgba(17,34,51,1)", style, StringComparison.Ordinal);
        Assert.Contains("--maui-background-color:rgba(68,85,102,1)", style, StringComparison.Ordinal);
        Assert.Contains("--maui-font-size:18px", style, StringComparison.Ordinal);
        Assert.Contains("--maui-font-family:monospace", style, StringComparison.Ordinal);
        Assert.Contains("--maui-opacity:0.75", style, StringComparison.Ordinal);
        Assert.Contains("--maui-margin:6px 8px 10px 4px", style, StringComparison.Ordinal);
        Assert.Equal("Styled label", FindAttributeValue(builder, "aria-label")?.ToString());
    }

    [Fact]
    public void Render_Button_ProjectsCssVariablesAndDisablesForCannotExecuteCommand()
    {
        var registry = new RendererRegistry();
        registry.Register<Button>(new MauiButtonRendererFactory());
        var renderer = new MauiViewRenderer(registry);

        var button = new Button
        {
            Text = "Styled button",
            TextColor = Color.FromArgb("#FFFFFF"),
            BackgroundColor = Color.FromArgb("#1E40AF"),
            BorderColor = Color.FromArgb("#22C55E"),
            BorderWidth = 2,
            CornerRadius = 12,
            Padding = new Thickness(12, 8),
            Command = new DisabledCommand(),
            CommandParameter = "payload"
        };

        var fragment = renderer.Render(button);
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var style = FindAttributeValue(builder, "style")?.ToString();
        Assert.NotNull(style);
        Assert.Contains("color:var(--maui-text-color)", style, StringComparison.Ordinal);
        Assert.Contains("background-color:var(--maui-background-color)", style, StringComparison.Ordinal);
        Assert.Contains("border-color:var(--maui-border-color)", style, StringComparison.Ordinal);
        Assert.Contains("border-width:var(--maui-border-width)", style, StringComparison.Ordinal);
        Assert.Contains("border-radius:var(--maui-border-radius)", style, StringComparison.Ordinal);
        Assert.Contains("padding:var(--maui-padding)", style, StringComparison.Ordinal);
        Assert.Contains("--maui-text-color:rgba(255,255,255,1)", style, StringComparison.Ordinal);
        Assert.Contains("--maui-background-color:rgba(30,64,175,1)", style, StringComparison.Ordinal);
        Assert.Contains("--maui-border-color:rgba(34,197,94,1)", style, StringComparison.Ordinal);
        Assert.Contains("--maui-border-width:2px", style, StringComparison.Ordinal);
        Assert.Contains("--maui-border-radius:12px", style, StringComparison.Ordinal);
        Assert.Contains("--maui-padding:8px 12px 8px 12px", style, StringComparison.Ordinal);
        Assert.True(HasAttribute(builder, "disabled"));
        Assert.Equal("true", FindAttributeValue(builder, "aria-disabled")?.ToString());
        Assert.Equal("button", FindAttributeValue(builder, "role")?.ToString());
        Assert.Equal("Styled button", FindAttributeValue(builder, "aria-label")?.ToString());
        Assert.False(HasAttribute(builder, "onclick"));
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

    private sealed class DisabledCommand : System.Windows.Input.ICommand
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

        public bool CanExecute(object? parameter) => false;

        public void Execute(object? parameter)
        {
        }
    }
}
#pragma warning restore BL0006
