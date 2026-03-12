using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageEntryHandler : MirageViewHandler<IEntry>, IMirageRuntimeEventTarget
{
    public static IPropertyMapper<IEntry, MirageEntryHandler> Mapper =
        new PropertyMapper<IEntry, MirageEntryHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IEntry.Text)] = MapText,
            [nameof(IPlaceholder.Placeholder)] = MapPlaceholder
        };

    private bool _isApplyingRuntimeInput;
    private int _nextInputEventId;
    private readonly object _inputSync = new();
    private string? _pendingInputValue;
    private bool _inputFlushScheduled;
    private int _pendingInputEventCount;
    private int _pendingInputEventId;
    private long _pendingInputReceivedTimestamp;
    private long _pendingBaselineRenderSequence;
    private int _inputEventReceivedCount;
    private int _inputFlushApplyCount;
    private int _inputCoalescedEventCount;

    public MirageEntryHandler()
        : base(ComponentKind.Entry, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "block");
        PlatformView.SetStyle("width", "100%");
        PlatformView.SetStyle("minHeight", "36px");
        PlatformView.SetEvent("input", enabled: true);
        base.ConnectHandler(platformView);
        RequestRender();
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (!string.Equals(eventName, "input", StringComparison.Ordinal))
        {
            return;
        }

        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var inputEventId = diagnosticsEnabled ? Interlocked.Increment(ref _nextInputEventId) : 0;
        var inputReceivedTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        var baselineRenderSequence = diagnosticsEnabled ? MirageDiagnostics.GetLastRenderCompletedSequence() : 0;

        var value = payload ?? string.Empty;
        var shouldScheduleFlush = false;
        var queueDepth = 0;
        lock (_inputSync)
        {
            if (!_inputFlushScheduled
                && _pendingInputEventCount == 0
                && string.Equals(VirtualView.Text, value, StringComparison.Ordinal))
            {
                return;
            }

            _pendingInputValue = value;
            _pendingInputEventId = inputEventId;
            _pendingInputReceivedTimestamp = inputReceivedTimestamp;
            _pendingBaselineRenderSequence = baselineRenderSequence;
            _pendingInputEventCount++;
            queueDepth = _pendingInputEventCount;
            if (_pendingInputEventCount > 1)
            {
                _inputCoalescedEventCount++;
            }

            if (!_inputFlushScheduled)
            {
                _inputFlushScheduled = true;
                shouldScheduleFlush = true;
            }
        }

        _inputEventReceivedCount++;

        if (diagnosticsEnabled)
        {
            MirageDiagnostics.Log(
                "entry.input",
                $"eventId={inputEventId} phase=received nodeId={PlatformView.NodeId} valueLength={value.Length} baselineRenderSequence={baselineRenderSequence} queueDepth={queueDepth} inputEventsReceived={_inputEventReceivedCount} inputEventsCoalesced={_inputCoalescedEventCount}");
        }

        if (shouldScheduleFlush)
        {
            SchedulePendingInputFlush();
        }
    }

    public static void MapText(MirageEntryHandler handler, IEntry entry)
    {
        if (handler._isApplyingRuntimeInput && string.Equals(entry.Text, handler.PlatformView.GetPropertyString("Text"), StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("Text", entry.Text);
        handler.RequestRender();

        if (MirageDiagnostics.IsEnabled)
        {
            MirageDiagnostics.Log(
                "entry.map-text",
                $"nodeId={handler.PlatformView.NodeId} textLength={(entry.Text?.Length ?? 0)}");
        }
    }

    public static void MapPlaceholder(MirageEntryHandler handler, IEntry entry)
    {
        handler.PlatformView.SetProperty("Placeholder", entry.Placeholder);
        handler.RequestRender();
    }

    private void SchedulePendingInputFlush()
    {
        var dispatcher = ResolveDispatcher();
        if (dispatcher is null)
        {
            FlushPendingInput();
            return;
        }

        if (!dispatcher.DispatchDelayed(TimeSpan.Zero, FlushPendingInput))
        {
            if (!dispatcher.Dispatch(FlushPendingInput))
            {
                FlushPendingInput();
            }
        }
    }

    private void FlushPendingInput()
    {
        string? value;
        var eventId = 0;
        var eventCount = 0;
        var inputReceivedTimestamp = 0L;
        var baselineRenderSequence = 0L;
        lock (_inputSync)
        {
            value = _pendingInputValue;
            eventId = _pendingInputEventId;
            eventCount = _pendingInputEventCount;
            inputReceivedTimestamp = _pendingInputReceivedTimestamp;
            baselineRenderSequence = _pendingBaselineRenderSequence;
            _pendingInputValue = null;
            _pendingInputEventCount = 0;
            _pendingInputEventId = 0;
            _pendingInputReceivedTimestamp = 0;
            _pendingBaselineRenderSequence = 0;
            _inputFlushScheduled = false;
        }

        if (eventCount == 0)
        {
            return;
        }

        _inputFlushApplyCount++;
        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var valueToApply = value ?? string.Empty;
        if (string.Equals(VirtualView.Text, valueToApply, StringComparison.Ordinal))
        {
            if (diagnosticsEnabled)
            {
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={eventId} phase=flush-skip eventCount={eventCount} inputFlushApplies={_inputFlushApplyCount} inputEventsCoalesced={_inputCoalescedEventCount}");
            }

            return;
        }

        PlatformView.SetProperty("Text", valueToApply);

        _isApplyingRuntimeInput = true;
        try
        {
            var setStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
            VirtualView.Text = valueToApply;
            if (diagnosticsEnabled)
            {
                var setElapsedMs = MirageDiagnostics.GetElapsedMilliseconds(setStartTimestamp, MirageDiagnostics.GetTimestamp());
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={eventId} phase=virtual-text-set setMs={setElapsedMs:0.###} eventCount={eventCount} inputFlushApplies={_inputFlushApplyCount} inputEventsCoalesced={_inputCoalescedEventCount}");
            }
        }
        finally
        {
            _isApplyingRuntimeInput = false;
        }

        if (diagnosticsEnabled)
        {
            _ = TrackRenderCompletionForInputAsync(eventId, baselineRenderSequence, inputReceivedTimestamp);
        }
    }

    private IDispatcher? ResolveDispatcher()
    {
        return MauiContext?.Services.GetService<IDispatcher>();
    }

    private static async Task TrackRenderCompletionForInputAsync(int inputEventId, long baselineRenderSequence, long inputReceivedTimestamp)
    {
        const int pollIntervalMs = 10;
        const int maxWaitMs = 5000;

        var waitedMs = 0;
        while (waitedMs < maxWaitMs)
        {
            var completedSequence = MirageDiagnostics.GetLastRenderCompletedSequence();
            if (completedSequence > baselineRenderSequence)
            {
                var completedTimestamp = MirageDiagnostics.GetLastRenderCompletedTimestamp();
                var latencyMs = MirageDiagnostics.GetElapsedMilliseconds(inputReceivedTimestamp, completedTimestamp);
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={inputEventId} phase=render-complete renderSequence={completedSequence} endToEndMs={latencyMs:0.###}");
                return;
            }

            await Task.Delay(pollIntervalMs);
            waitedMs += pollIntervalMs;
        }

        MirageDiagnostics.Log(
            "entry.input",
            $"eventId={inputEventId} phase=render-timeout baselineRenderSequence={baselineRenderSequence} waitedMs={maxWaitMs}");
    }
}
