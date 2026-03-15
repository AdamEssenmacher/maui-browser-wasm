using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Showcase.Fluent.App;
using Showcase.Shared.App;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class ShowcaseXamlLabTests
{
    [Fact]
    public void MonacoEditorHandler_MapsProperties_AndClearsTextDeterministically()
    {
        var handler = CreateHandler();
        var editorView = new ShowcaseXamlEditorView
        {
            Text = "<Label Text=\"Hi\" />",
            Language = "xml",
            Theme = "vs-dark",
            IsReadOnly = true
        };

        ((IElementHandler)handler).SetVirtualView(editorView);

        Assert.Equal("<Label Text=\"Hi\" />", handler.PlatformView.GetPropertyString("value"));
        Assert.Equal("xml", handler.PlatformView.GetPropertyString("language"));
        Assert.Equal("vs-dark", handler.PlatformView.GetPropertyString("theme"));
        Assert.True(handler.PlatformView.Properties.TryGetValue("readOnly", out var readOnlyValue));
        Assert.True(readOnlyValue.TryGetBool(out var isReadOnly));
        Assert.True(isReadOnly);

        editorView.Text = string.Empty;
        ShowcaseMonacoEditorHandler.MapText(handler, editorView);
        Assert.Equal(string.Empty, handler.PlatformView.GetPropertyString("value"));
    }

    [Fact]
    public void MonacoEditorHandler_RuntimeInputEvent_UpdatesVirtualText()
    {
        var handler = CreateHandler();
        var editorView = new ShowcaseXamlEditorView
        {
            Text = "<Label Text=\"Original\" />"
        };

        ((IElementHandler)handler).SetVirtualView(editorView);
        handler.OnRuntimeEvent("input", "<Label Text=\"Updated\" />");

        Assert.Equal("<Label Text=\"Updated\" />", editorView.Text);
    }

    [Fact]
    public void FragmentCompiler_ValidFragment_ReturnsPreview()
    {
        var compiler = new XamlLabFragmentCompiler();
        var result = compiler.Compile(
            """
            <VerticalStackLayout Spacing="8">
                <Label Text="Hello" />
                <Button Text="Tap" />
            </VerticalStackLayout>
            """);

        Assert.True(result.IsSuccess, string.Join(" | ", result.Errors));
        Assert.NotNull(result.Preview);
        Assert.IsType<VerticalStackLayout>(result.Preview);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FragmentCompiler_InvalidXml_ReturnsParseError()
    {
        var compiler = new XamlLabFragmentCompiler();
        var result = compiler.Compile("<VerticalStackLayout><Label Text=\"Missing close\"></VerticalStackLayout>");

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("parse", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FragmentCompiler_DisallowedControlsAndEvents_AreRejected()
    {
        var compiler = new XamlLabFragmentCompiler();

        var disallowedControl = compiler.Compile("<NavigationPage />");
        Assert.False(disallowedControl.IsSuccess);
        Assert.Contains(disallowedControl.Errors, message => message.Contains("not allowed", StringComparison.OrdinalIgnoreCase)
                                                             || message.Contains("not supported", StringComparison.OrdinalIgnoreCase));

        var disallowedEvent = compiler.Compile("<Button Text=\"Run\" Clicked=\"OnRun\" />");
        Assert.False(disallowedEvent.IsSuccess);
        Assert.Contains(disallowedEvent.Errors, message => message.Contains("event handler", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PreviewSession_DebouncesRapidEdits_AndPreservesLastGoodPreviewOnInvalidSource()
    {
        var compileInputs = new List<string>();
        var session = new XamlLabPreviewSession(
            new DelegateFragmentCompiler(source =>
            {
                compileInputs.Add(source);
                if (source.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                {
                    return XamlLabCompileResult.Failure("Invalid fragment");
                }

                return XamlLabCompileResult.Success(new Label { Text = source });
            }),
            TimeSpan.FromMilliseconds(35));

        var updates = new List<XamlLabPreviewUpdate>();
        session.Updated += (_, update) => updates.Add(update);

        session.UpdateSource("first");
        session.UpdateSource("second");
        session.UpdateSource("third");
        await session.WaitForPendingAsync();

        Assert.Single(compileInputs);
        Assert.Equal("third", compileInputs[0]);
        Assert.Single(updates);
        Assert.True(updates[0].IsSuccess);
        Assert.NotNull(session.LastGoodPreview);

        session.UpdateSource("invalid fragment");
        await session.WaitForPendingAsync();

        Assert.Equal(2, compileInputs.Count);
        Assert.Equal("invalid fragment", compileInputs[1]);
        Assert.Equal(2, updates.Count);
        Assert.False(updates[1].IsSuccess);
        Assert.True(updates[1].PreservedLastGoodPreview);
        Assert.Same(session.LastGoodPreview, updates[1].Preview);
    }

    private static ShowcaseMonacoEditorHandler CreateHandler()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new ShowcaseMonacoEditorHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        return handler;
    }

    private sealed class DelegateFragmentCompiler(Func<string, XamlLabCompileResult> compile) : IXamlLabFragmentCompiler
    {
        public XamlLabCompileResult Compile(string fragmentText) => compile(fragmentText);
    }
}
