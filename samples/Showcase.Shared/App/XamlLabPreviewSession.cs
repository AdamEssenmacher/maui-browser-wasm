using Microsoft.Maui;

namespace Showcase.Shared.App;

public sealed record XamlLabPreviewUpdate(
    string SourceText,
    bool IsSuccess,
    IView? Preview,
    IReadOnlyList<string> Errors,
    bool PreservedLastGoodPreview,
    int Revision);

public sealed class XamlLabPreviewSession : IDisposable
{
    private readonly IXamlLabFragmentCompiler _compiler;
    private readonly TimeSpan _debounceDelay;
    private readonly object _sync = new();

    private CancellationTokenSource? _pendingApplyCts;
    private Task _pendingApplyTask = Task.CompletedTask;
    private string _sourceText = string.Empty;
    private int _revision;
    private bool _disposed;

    public XamlLabPreviewSession(IXamlLabFragmentCompiler compiler, TimeSpan debounceDelay)
    {
        _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
        _debounceDelay = debounceDelay <= TimeSpan.Zero
            ? TimeSpan.FromMilliseconds(350)
            : debounceDelay;
    }

    public event EventHandler<XamlLabPreviewUpdate>? Updated;

    public IView? LastGoodPreview { get; private set; }

    public void UpdateSource(string text)
    {
        ThrowIfDisposed();
        lock (_sync)
        {
            _sourceText = text ?? string.Empty;
            ScheduleDebouncedApplyNoLock();
        }
    }

    public void ApplyNow()
    {
        ThrowIfDisposed();

        string source;
        lock (_sync)
        {
            source = _sourceText;
            CancelPendingApplyNoLock();
        }

        ApplySource(source);
    }

    public Task WaitForPendingAsync()
    {
        lock (_sync)
        {
            return _pendingApplyTask;
        }
    }

    private void ScheduleDebouncedApplyNoLock()
    {
        CancelPendingApplyNoLock();
        var cts = new CancellationTokenSource();
        _pendingApplyCts = cts;
        _pendingApplyTask = RunDebouncedApplyAsync(cts.Token);
    }

    private async Task RunDebouncedApplyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_debounceDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        string source;
        lock (_sync)
        {
            if (cancellationToken.IsCancellationRequested || _disposed)
            {
                return;
            }

            source = _sourceText;
            _pendingApplyCts = null;
        }

        ApplySource(source);
    }

    private void ApplySource(string sourceText)
    {
        var compileResult = _compiler.Compile(sourceText);
        var preservedLastGoodPreview = false;
        IView? preview = compileResult.Preview;
        if (compileResult.IsSuccess)
        {
            LastGoodPreview = compileResult.Preview;
        }
        else
        {
            preservedLastGoodPreview = LastGoodPreview is not null;
            preview = LastGoodPreview;
        }

        var revision = Interlocked.Increment(ref _revision);
        Updated?.Invoke(
            this,
            new XamlLabPreviewUpdate(
                SourceText: sourceText,
                IsSuccess: compileResult.IsSuccess,
                Preview: preview,
                Errors: compileResult.Errors,
                PreservedLastGoodPreview: preservedLastGoodPreview,
                Revision: revision));
    }

    private void CancelPendingApplyNoLock()
    {
        if (_pendingApplyCts is null)
        {
            return;
        }

        _pendingApplyCts.Cancel();
        _pendingApplyCts.Dispose();
        _pendingApplyCts = null;
        _pendingApplyTask = Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            CancelPendingApplyNoLock();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
