using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageEntryHandler : MirageViewHandler<IEntry>, IMirageRuntimeEventTarget
{
    private static readonly TimeSpan InputFlushDelay = TimeSpan.FromMilliseconds(1);

    public static IPropertyMapper<IEntry, MirageEntryHandler> Mapper =
        new PropertyMapper<IEntry, MirageEntryHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IEntry.Text)] = MapText,
            [nameof(IPlaceholder.Placeholder)] = MapPlaceholder,
            [nameof(VisualElement.WidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.HeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests
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
    private long _pendingInputTextVersion;
    private int _inputEventReceivedCount;
    private int _inputFlushApplyCount;
    private int _inputCoalescedEventCount;
    private long _textVersion;

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
        UpdateControlHeightToken(VirtualView);
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
        var textVersion = Volatile.Read(ref _textVersion);
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
            _pendingInputTextVersion = textVersion;
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
        var previousPlatformText = handler.PlatformView.GetPropertyString("Text");
        var nextText = entry.Text;
        if (handler._isApplyingRuntimeInput && string.Equals(nextText, previousPlatformText, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("Text", nextText);
        if (!handler._isApplyingRuntimeInput
            && !string.Equals(previousPlatformText, nextText, StringComparison.Ordinal))
        {
            Interlocked.Increment(ref handler._textVersion);
        }

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

    public static void MapSizeRequests(MirageEntryHandler handler, IEntry entry)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, entry);
        changed |= handler.UpdateControlHeightToken(entry);
        if (!changed)
        {
            return;
        }

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

        if (!dispatcher.DispatchDelayed(InputFlushDelay, FlushPendingInput))
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
        var inputTextVersion = 0L;
        lock (_inputSync)
        {
            value = _pendingInputValue;
            eventId = _pendingInputEventId;
            eventCount = _pendingInputEventCount;
            inputReceivedTimestamp = _pendingInputReceivedTimestamp;
            baselineRenderSequence = _pendingBaselineRenderSequence;
            inputTextVersion = _pendingInputTextVersion;
            _pendingInputValue = null;
            _pendingInputEventCount = 0;
            _pendingInputEventId = 0;
            _pendingInputReceivedTimestamp = 0;
            _pendingBaselineRenderSequence = 0;
            _pendingInputTextVersion = 0;
            _inputFlushScheduled = false;
        }

        if (eventCount == 0)
        {
            return;
        }

        _inputFlushApplyCount++;
        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var valueToApply = value ?? string.Empty;
        var currentTextVersion = Volatile.Read(ref _textVersion);
        if (currentTextVersion != inputTextVersion
            && !string.Equals(VirtualView.Text, valueToApply, StringComparison.Ordinal))
        {
            if (diagnosticsEnabled)
            {
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={eventId} phase=flush-skip-stale eventCount={eventCount} staleTextVersion={inputTextVersion} currentTextVersion={currentTextVersion} inputFlushApplies={_inputFlushApplyCount} inputEventsCoalesced={_inputCoalescedEventCount}");
            }

            return;
        }

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

        var scheduledRenderSequenceBeforeSet = diagnosticsEnabled
            ? MirageDiagnostics.GetLastRenderScheduledSequence()
            : 0;

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
            var scheduledRenderSequenceAfterSet = MirageDiagnostics.GetLastRenderScheduledSequence();
            if (scheduledRenderSequenceAfterSet > scheduledRenderSequenceBeforeSet)
            {
                _ = TrackRenderCompletionForInputAsync(
                    eventId,
                    baselineRenderSequence,
                    scheduledRenderSequenceAfterSet,
                    inputReceivedTimestamp);
            }
            else
            {
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={eventId} phase=render-tracking-skipped reason=no-new-render-request scheduledBefore={scheduledRenderSequenceBeforeSet} scheduledAfter={scheduledRenderSequenceAfterSet}");
            }
        }
    }

    private IDispatcher? ResolveDispatcher()
    {
        return MauiContext?.Services.GetService<IDispatcher>();
    }

    private static async Task TrackRenderCompletionForInputAsync(
        int inputEventId,
        long baselineRenderSequence,
        long expectedRenderSequence,
        long inputReceivedTimestamp)
    {
        const int pollIntervalMs = 10;
        const int maxWaitMs = 5000;

        var waitedMs = 0;
        while (waitedMs < maxWaitMs)
        {
            var completedSequence = MirageDiagnostics.GetLastRenderCompletedSequence();
            if (completedSequence >= expectedRenderSequence)
            {
                var completedTimestamp = MirageDiagnostics.GetLastRenderCompletedTimestamp();
                var latencyMs = MirageDiagnostics.GetElapsedMilliseconds(inputReceivedTimestamp, completedTimestamp);
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={inputEventId} phase=render-complete renderSequence={completedSequence} expectedRenderSequence={expectedRenderSequence} endToEndMs={latencyMs:0.###}");
                return;
            }

            await Task.Delay(pollIntervalMs);
            waitedMs += pollIntervalMs;
        }

        MirageDiagnostics.Log(
            "entry.input",
            $"eventId={inputEventId} phase=render-timeout baselineRenderSequence={baselineRenderSequence} expectedRenderSequence={expectedRenderSequence} waitedMs={maxWaitMs}");
    }

    private bool SetStyleIfChanged(string styleName, string? styleValue)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), styleValue, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, styleValue);
        return true;
    }

    private bool UpdateControlHeightToken(IEntry entry)
    {
        if (entry is not VisualElement visualElement)
        {
            return SetStyleIfChanged("--base-height-multiplier", null);
        }

        var preferredHeight = ResolvePreferredControlHeight(visualElement);
        if (preferredHeight is null)
        {
            return SetStyleIfChanged("--base-height-multiplier", null);
        }

        const double fluentDesignUnit = 4d;
        var multiplier = Math.Max(1, preferredHeight.Value / fluentDesignUnit);
        var multiplierValue = $"{multiplier:0.###}";
        return SetStyleIfChanged("--base-height-multiplier", multiplierValue);
    }

    private static double? ResolvePreferredControlHeight(VisualElement visualElement)
    {
        double? preferredHeight = null;
        if (IsFinitePositive(visualElement.HeightRequest))
        {
            preferredHeight = visualElement.HeightRequest;
        }
        else if (IsFinitePositive(visualElement.MinimumHeightRequest))
        {
            preferredHeight = visualElement.MinimumHeightRequest;
        }

        if (preferredHeight is null)
        {
            return null;
        }

        if (IsFinitePositive(visualElement.MaximumHeightRequest))
        {
            preferredHeight = Math.Min(preferredHeight.Value, visualElement.MaximumHeightRequest);
        }

        return preferredHeight.Value;
    }

    private static bool IsFinitePositive(double value)
    {
        return double.IsFinite(value) && value > 0;
    }
}
