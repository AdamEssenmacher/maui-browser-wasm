using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
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
        if (string.Equals(VirtualView.Text, value, StringComparison.Ordinal))
        {
            return;
        }

        if (diagnosticsEnabled)
        {
            MirageDiagnostics.Log(
                "entry.input",
                $"eventId={inputEventId} phase=received nodeId={PlatformView.NodeId} valueLength={value.Length} baselineRenderSequence={baselineRenderSequence}");
        }

        _isApplyingRuntimeInput = true;
        try
        {
            var setStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
            VirtualView.Text = value;
            if (diagnosticsEnabled)
            {
                var setElapsedMs = MirageDiagnostics.GetElapsedMilliseconds(setStartTimestamp, MirageDiagnostics.GetTimestamp());
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={inputEventId} phase=virtual-text-set setMs={setElapsedMs:0.###}");
            }
        }
        finally
        {
            _isApplyingRuntimeInput = false;
        }

        if (diagnosticsEnabled)
        {
            _ = TrackRenderCompletionForInputAsync(inputEventId, baselineRenderSequence, inputReceivedTimestamp);
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
