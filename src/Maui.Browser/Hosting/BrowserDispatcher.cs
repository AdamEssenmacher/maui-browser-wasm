using Microsoft.Maui.Dispatching;

namespace Maui.Browser.Hosting;

internal sealed class BrowserDispatcherProvider : IDispatcherProvider
{
    private static readonly IDispatcher s_dispatcher = new BrowserDispatcher();

    public IDispatcher? GetForCurrentThread() => s_dispatcher;
}

internal sealed class BrowserDispatcher : IDispatcher
{
    private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;

    public bool IsDispatchRequired =>
        _synchronizationContext is not null
        && !ReferenceEquals(SynchronizationContext.Current, _synchronizationContext);

    public bool Dispatch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_synchronizationContext is null || ReferenceEquals(SynchronizationContext.Current, _synchronizationContext))
        {
            action();
            return true;
        }

        _synchronizationContext.Post(static state => ((Action)state!).Invoke(), action);
        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (delay <= TimeSpan.Zero)
        {
            return Dispatch(action);
        }

        if (_synchronizationContext is not null)
        {
            _ = Task.Delay(delay).ContinueWith(
                _ => _synchronizationContext.Post(static state => ((Action)state!).Invoke(), action),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return true;
        }

        _ = Task.Delay(delay).ContinueWith(
            _ => Dispatch(action),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return true;
    }

    public IDispatcherTimer CreateTimer() => new BrowserDispatcherTimer(this);
}

internal sealed class BrowserDispatcherTimer : IDispatcherTimer
{
    private readonly BrowserDispatcher _dispatcher;
    private System.Threading.Timer? _timer;
    private bool _isRunning;

    public BrowserDispatcherTimer(BrowserDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public event EventHandler? Tick;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(16);

    public bool IsRepeating { get; set; } = true;

    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        var dueTime = Interval <= TimeSpan.Zero ? TimeSpan.Zero : Interval;
        var period = IsRepeating ? dueTime : Timeout.InfiniteTimeSpan;

        _timer = new System.Threading.Timer(OnTimerFired, null, dueTime, period);
        _isRunning = true;
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;
    }

    private void OnTimerFired(object? _)
    {
        _dispatcher.Dispatch(() => Tick?.Invoke(this, EventArgs.Empty));

        if (!IsRepeating)
        {
            Stop();
        }
    }
}
