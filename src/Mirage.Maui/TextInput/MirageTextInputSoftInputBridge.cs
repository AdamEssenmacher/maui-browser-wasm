using Microsoft.Maui;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.TextInput;

internal sealed class MirageSoftInputHideRequest
{
    public bool Result { get; private set; }

    public void TrySetResult(bool result)
    {
        Result = result;
    }
}

internal interface IMirageTextInputSoftInputBridge
{
    Task<bool> ShowSoftInputAsync(ITextInput textInput, CancellationToken cancellationToken);

    Task<bool> HideSoftInputAsync(ITextInput textInput, CancellationToken cancellationToken);
}

internal sealed class MirageTextInputSoftInputBridge : IMirageTextInputSoftInputBridge
{
    public Task<bool> ShowSoftInputAsync(ITextInput textInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(textInput);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        if (textInput is not IView view
            || view.Handler is not IElementHandler handler)
        {
            return Task.FromResult(false);
        }

        var focusRequest = new FocusRequest();
        handler.Invoke(nameof(IView.Focus), focusRequest);
        return Task.FromResult(focusRequest.Result);
    }

    public Task<bool> HideSoftInputAsync(ITextInput textInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(textInput);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        if (textInput is not IView view
            || view.Handler is not IElementHandler handler)
        {
            return Task.FromResult(false);
        }

        var hideRequest = new MirageSoftInputHideRequest();
        handler.Invoke(nameof(IView.Unfocus), hideRequest);
        return Task.FromResult(hideRequest.Result);
    }
}
