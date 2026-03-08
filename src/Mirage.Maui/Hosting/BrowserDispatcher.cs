using Microsoft.Maui.Dispatching;

namespace Mirage.Maui.Hosting;

internal sealed class BrowserDispatcherProvider : IDispatcherProvider
{
    private static readonly IDispatcher Dispatcher = new BrowserDispatcher();

    public IDispatcher? GetForCurrentThread() => Dispatcher;
}

internal sealed class BrowserDispatcher : IDispatcher
{
    public bool IsDispatchRequired => false;

    public bool Dispatch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

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
