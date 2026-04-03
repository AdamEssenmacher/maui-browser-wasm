using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace Mirage.Maui.Handlers;

internal sealed class MirageGestureRecognizerManager : IMirageRuntimeEventTarget, IDisposable
{
    private const double PanActivationThreshold = 8;
    private const double SwipeThreshold = 48;
    private const double CoordinateEpsilon = 0.01;

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly View _view;
    private readonly MiragePlatformView _platformView;
    private readonly IList<IGestureRecognizer> _compositeGestureRecognizers;
    private readonly IList<IGestureRecognizer> _directGestureRecognizers;
    private readonly INotifyCollectionChanged? _compositeCollectionChanged;
    private readonly INotifyCollectionChanged? _directCollectionChanged;
    private readonly HashSet<IGestureRecognizer> _trackedRecognizers = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<long, ActivePointer> _activePointers = new();
    private readonly HashSet<string> _baselineEvents;
    private readonly HashSet<string> _gestureOwnedEvents = new(StringComparer.Ordinal);
    private readonly List<PanGestureState> _activePanGestures = [];
    private readonly Dictionary<long, int> _pendingSecondaryTapCounts = new();
    private readonly Action? _requestRender;

    private PinchGestureState? _activePinchGesture;
    private int _nextPanGestureId = 1;
    private bool _suppressNextPrimaryClick;
    private bool _suppressNextSecondaryContextMenu;
    private bool _disposed;

    public MirageGestureRecognizerManager(View view, MiragePlatformView platformView, Action? requestRender = null)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _platformView = platformView ?? throw new ArgumentNullException(nameof(platformView));
        _requestRender = requestRender;

        _directGestureRecognizers = view.GestureRecognizers;
        _compositeGestureRecognizers = ((IGestureController)view).CompositeGestureRecognizers;
        _baselineEvents = [.. platformView.Events];

        _directCollectionChanged = _directGestureRecognizers as INotifyCollectionChanged;
        _compositeCollectionChanged = _compositeGestureRecognizers as INotifyCollectionChanged;

        if (_directCollectionChanged is not null)
        {
            _directCollectionChanged.CollectionChanged += OnGestureCollectionChanged;
        }

        if (_compositeCollectionChanged is not null && !ReferenceEquals(_compositeCollectionChanged, _directCollectionChanged))
        {
            _compositeCollectionChanged.CollectionChanged += OnGestureCollectionChanged;
        }

        RefreshRecognizers();
        RefreshEventSubscriptions();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_directCollectionChanged is not null)
        {
            _directCollectionChanged.CollectionChanged -= OnGestureCollectionChanged;
        }

        if (_compositeCollectionChanged is not null && !ReferenceEquals(_compositeCollectionChanged, _directCollectionChanged))
        {
            _compositeCollectionChanged.CollectionChanged -= OnGestureCollectionChanged;
        }

        foreach (var recognizer in _trackedRecognizers.OfType<INotifyPropertyChanged>())
        {
            recognizer.PropertyChanged -= OnGestureRecognizerPropertyChanged;
        }

        _trackedRecognizers.Clear();
        _activePointers.Clear();
        _activePanGestures.Clear();
        _pendingSecondaryTapCounts.Clear();
        _activePinchGesture = null;
        _suppressNextPrimaryClick = false;
        _suppressNextSecondaryContextMenu = false;

        foreach (var eventName in _gestureOwnedEvents.ToArray())
        {
            _platformView.SetEvent(eventName, enabled: false);
        }

        _gestureOwnedEvents.Clear();
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (_disposed)
        {
            return;
        }

        switch (eventName)
        {
            case "click":
            case "contextmenu":
            case "pointerdown":
            case "pointermove":
            case "pointerup":
            case "pointercancel":
            case "pointerenter":
            case "pointerleave":
                break;
            default:
                return;
        }

        if (!TryParsePointerPayload(payload, out var pointerPayload))
        {
            return;
        }

        switch (eventName)
        {
            case "click":
                HandleClick(pointerPayload);
                break;
            case "contextmenu":
                HandleContextMenu(pointerPayload);
                break;
            case "pointerdown":
                HandlePointerDown(pointerPayload);
                break;
            case "pointermove":
                HandlePointerMove(pointerPayload);
                break;
            case "pointerup":
                HandlePointerUp(pointerPayload);
                break;
            case "pointercancel":
                HandlePointerCancel(pointerPayload);
                break;
            case "pointerenter":
                DispatchPointerRecognizers(static (recognizer, owner, getPosition, args, buttonsMask) =>
                    GestureDispatchBridge.SendPointerEntered(recognizer, owner, getPosition, args, buttonsMask), pointerPayload);
                break;
            case "pointerleave":
                DispatchPointerRecognizers(static (recognizer, owner, getPosition, args, buttonsMask) =>
                    GestureDispatchBridge.SendPointerExited(recognizer, owner, getPosition, args, buttonsMask), pointerPayload);
                break;
        }
    }

    private void HandleClick(MirageGesturePointerPayload payload)
    {
        if (_suppressNextPrimaryClick)
        {
            _suppressNextPrimaryClick = false;
            return;
        }

        var buttonMask = ResolveButtonMask(payload, defaultToPrimary: true);
        foreach (var recognizer in EnumerateTapRecognizers())
        {
            if (!IsTapButtonMatch(recognizer, buttonMask))
            {
                continue;
            }

            if (Math.Max(1, recognizer.NumberOfTapsRequired) != Math.Max(1, payload.ClickCount))
            {
                continue;
            }

            GestureDispatchBridge.SendTapped(recognizer, _view, CreatePositionResolver(_view, payload));
        }
    }

    private void HandleContextMenu(MirageGesturePointerPayload payload)
    {
        if (_suppressNextSecondaryContextMenu)
        {
            _suppressNextSecondaryContextMenu = false;
            return;
        }

        const ButtonsMask buttonMask = ButtonsMask.Secondary;
        var clickCount = ResolveSecondaryTapCount(payload);
        foreach (var recognizer in EnumerateTapRecognizers())
        {
            if (!IsTapButtonMatch(recognizer, buttonMask))
            {
                continue;
            }

            if (Math.Max(1, recognizer.NumberOfTapsRequired) != clickCount)
            {
                continue;
            }

            GestureDispatchBridge.SendTapped(recognizer, _view, CreatePositionResolver(_view, payload));
        }
    }

    private void HandlePointerDown(MirageGesturePointerPayload payload)
    {
        if (_activePointers.Count == 0)
        {
            ClearTapSuppression();
        }

        var pointerId = payload.PointerId == 0 && _activePointers.Count == 0 ? 1 : payload.PointerId;
        var buttonMask = ResolveButtonMask(payload, defaultToPrimary: true);

        if (buttonMask.HasFlag(ButtonsMask.Secondary) && payload.ClickCount > 0)
        {
            _pendingSecondaryTapCounts[pointerId] = payload.ClickCount;
        }
        else
        {
            _pendingSecondaryTapCounts.Remove(pointerId);
        }

        _activePointers[pointerId] = new ActivePointer(
            pointerId,
            buttonMask,
            payload.ClientX,
            payload.ClientY,
            payload.X,
            payload.Y,
            payload.ClientX,
            payload.ClientY,
            payload.X,
            payload.Y,
            false,
            false);

        if (_activePinchGesture is not null && _activePointers.TryGetValue(pointerId, out var pinchActivePointer))
        {
            _activePointers[pointerId] = pinchActivePointer with { ParticipatedInPinch = true };
        }

        DispatchPointerRecognizers(static (recognizer, owner, getPosition, args, buttonsMask) =>
            GestureDispatchBridge.SendPointerPressed(recognizer, owner, getPosition, args, buttonsMask), payload);

        if (TryStartPinchIfReady())
        {
            return;
        }

        TryStartPanGestures();
    }

    private void HandlePointerMove(MirageGesturePointerPayload payload)
    {
        if (_activePointers.TryGetValue(payload.PointerId, out var activePointer))
        {
            var exceededTapThreshold = activePointer.ExceededTapThreshold
                || Distance(activePointer.StartClientX, activePointer.StartClientY, payload.ClientX, payload.ClientY) >= PanActivationThreshold;
            activePointer = activePointer with
            {
                LastClientX = payload.ClientX,
                LastClientY = payload.ClientY,
                LastX = payload.X,
                LastY = payload.Y,
                ExceededTapThreshold = exceededTapThreshold
            };
            _activePointers[payload.PointerId] = activePointer;

            if (exceededTapThreshold)
            {
                MarkTapSuppression(activePointer.ButtonMask);
            }
        }

        DispatchPointerRecognizers(static (recognizer, owner, getPosition, args, buttonsMask) =>
            GestureDispatchBridge.SendPointerMoved(recognizer, owner, getPosition, args, buttonsMask), payload);

        if (TryUpdatePinch(payload))
        {
            return;
        }

        UpdatePanGestures();
    }

    private void HandlePointerUp(MirageGesturePointerPayload payload)
    {
        var resolvedPayload = payload;
        var skipSwipe = _activePinchGesture is not null;
        if (TryResolveTerminalPointerPayload(payload, out resolvedPayload, out var activePointer))
        {
            skipSwipe = skipSwipe || activePointer.ParticipatedInPinch;
        }

        DispatchPointerRecognizers(static (recognizer, owner, getPosition, args, buttonsMask) =>
            GestureDispatchBridge.SendPointerReleased(recognizer, owner, getPosition, args, buttonsMask), resolvedPayload);

        CompletePinchIfPointerEnded(payload.PointerId);
        CompletePanGesturesForPointer(payload.PointerId, canceled: false);
        if (!skipSwipe)
        {
            DetectSwipe(payload.PointerId);
        }

        if (_activePointers.Remove(payload.PointerId, out var pointer) && pointer.ExceededTapThreshold)
        {
            MarkTapSuppression(pointer.ButtonMask);
        }
    }

    private void HandlePointerCancel(MirageGesturePointerPayload payload)
    {
        var resolvedPayload = payload;
        if (TryResolveTerminalPointerPayload(payload, out resolvedPayload, out _))
        {
            DispatchPointerRecognizers(static (recognizer, owner, getPosition, args, buttonsMask) =>
                GestureDispatchBridge.SendPointerReleased(recognizer, owner, getPosition, args, buttonsMask), resolvedPayload);
        }

        CompletePinchIfPointerEnded(payload.PointerId, canceled: true);
        CompletePanGesturesForPointer(payload.PointerId, canceled: true);

        if (_activePointers.Remove(payload.PointerId, out var pointer) && pointer.ExceededTapThreshold)
        {
            MarkTapSuppression(pointer.ButtonMask);
        }
    }

    private void TryStartPanGestures()
    {
        if (_activePinchGesture is not null)
        {
            return;
        }

        foreach (var recognizer in EnumeratePanRecognizers())
        {
            if (_activePanGestures.Any(state => ReferenceEquals(state.Recognizer, recognizer)))
            {
                continue;
            }

            var touchPoints = Math.Max(1, recognizer.TouchPoints);
            if (_activePointers.Count != touchPoints)
            {
                continue;
            }

            var pointerIds = _activePointers.Keys.OrderBy(static id => id).Take(touchPoints).ToArray();
            if (!ArePrimaryPanPointers(pointerIds))
            {
                continue;
            }

            var state = new PanGestureState(recognizer, _nextPanGestureId++, pointerIds, CalculateCentroid(pointerIds), HasStarted: false);
            _activePanGestures.Add(state);
        }
    }

    private void UpdatePanGestures()
    {
        if (_activePinchGesture is not null || _activePanGestures.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _activePanGestures.Count; i++)
        {
            var state = _activePanGestures[i];
            if (!TryCalculateCentroid(state.PointerIds, out var centroid))
            {
                continue;
            }

            var deltaX = centroid.X - state.StartCentroid.X;
            var deltaY = centroid.Y - state.StartCentroid.Y;
            if (!state.HasStarted)
            {
                if (Distance(state.StartCentroid.X, state.StartCentroid.Y, centroid.X, centroid.Y) < PanActivationThreshold)
                {
                    continue;
                }

                ((IPanGestureController)state.Recognizer).SendPanStarted(_view, state.GestureId);
                state = state with { HasStarted = true };
                _activePanGestures[i] = state;
            }

            ((IPanGestureController)state.Recognizer).SendPan(_view, deltaX, deltaY, state.GestureId);
        }
    }

    private void CompletePanGesturesForPointer(long pointerId, bool canceled)
    {
        if (_activePanGestures.Count == 0)
        {
            return;
        }

        foreach (var state in _activePanGestures.Where(state => state.PointerIds.Contains(pointerId)).ToArray())
        {
            if (canceled)
            {
                if (state.HasStarted)
                {
                    ((IPanGestureController)state.Recognizer).SendPanCanceled(_view, state.GestureId);
                }
            }
            else
            {
                if (state.HasStarted)
                {
                    ((IPanGestureController)state.Recognizer).SendPanCompleted(_view, state.GestureId);
                }
            }

            _activePanGestures.Remove(state);
        }
    }

    private bool TryStartPinchIfReady()
    {
        var recognizer = EnumeratePinchRecognizers().FirstOrDefault();
        if (recognizer is null || _activePinchGesture is not null || _activePointers.Count < 2)
        {
            return false;
        }

        var pointerIds = _activePointers.Keys.OrderBy(static id => id).Take(2).ToArray();
        if (!TryCalculateDistance(pointerIds, out var initialDistance, out var initialMidpoint))
        {
            return false;
        }

        foreach (var activePan in _activePanGestures.ToArray())
        {
            if (activePan.HasStarted)
            {
                ((IPanGestureController)activePan.Recognizer).SendPanCanceled(_view, activePan.GestureId);
            }

            _activePanGestures.Remove(activePan);
        }

        foreach (var pointerId in _activePointers.Keys.ToArray())
        {
            if (_activePointers.TryGetValue(pointerId, out var activePointer))
            {
                _activePointers[pointerId] = activePointer with { ParticipatedInPinch = true };
            }
        }

        var origin = CreateScaleOrigin(initialMidpoint);
        _activePinchGesture = new PinchGestureState(recognizer, pointerIds, initialDistance, origin);
        ((IPinchGestureController)recognizer).SendPinchStarted(_view, origin);
        return true;
    }

    private bool TryUpdatePinch(MirageGesturePointerPayload payload)
    {
        if (_activePinchGesture is null)
        {
            return false;
        }

        if (!_activePinchGesture.PointerIds.Contains(payload.PointerId))
        {
            return true;
        }

        if (!TryCalculateDistance(_activePinchGesture.PointerIds, out var distance, out var midpoint))
        {
            return true;
        }

        if (_activePinchGesture.InitialDistance <= CoordinateEpsilon)
        {
            return true;
        }

        var scale = distance / _activePinchGesture.InitialDistance;
        var origin = CreateScaleOrigin(midpoint);
        ((IPinchGestureController)_activePinchGesture.Recognizer).SendPinch(_view, scale, origin);
        return true;
    }

    private void CompletePinchIfPointerEnded(long pointerId, bool canceled = false)
    {
        if (_activePinchGesture is null || !_activePinchGesture.PointerIds.Contains(pointerId))
        {
            return;
        }

        if (canceled)
        {
            ((IPinchGestureController)_activePinchGesture.Recognizer).SendPinchCanceled(_view);
        }
        else
        {
            ((IPinchGestureController)_activePinchGesture.Recognizer).SendPinchEnded(_view);
        }

        _activePinchGesture = null;
    }

    private void DetectSwipe(long pointerId)
    {
        if (!_activePointers.TryGetValue(pointerId, out var pointer))
        {
            return;
        }

        if (!pointer.ButtonMask.HasFlag(ButtonsMask.Primary))
        {
            return;
        }

        var totalX = pointer.LastClientX - pointer.StartClientX;
        var totalY = pointer.LastClientY - pointer.StartClientY;
        var absX = Math.Abs(totalX);
        var absY = Math.Abs(totalY);

        SwipeDirection direction;
        if (absX >= SwipeThreshold && absX >= absY)
        {
            direction = totalX >= 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else if (absY >= SwipeThreshold && absY >= absX)
        {
            direction = totalY >= 0 ? SwipeDirection.Down : SwipeDirection.Up;
        }
        else
        {
            return;
        }

        foreach (var recognizer in EnumerateSwipeRecognizers())
        {
            if ((recognizer.Direction & direction) != 0)
            {
                recognizer.SendSwiped(_view, direction);
            }
        }
    }

    private void DispatchPointerRecognizers(
        Action<PointerGestureRecognizer, View, Func<IElement, Point?>, PlatformPointerEventArgs, ButtonsMask> dispatch,
        MirageGesturePointerPayload payload)
    {
        var recognizers = EnumeratePointerRecognizers().ToArray();
        if (recognizers.Length == 0)
        {
            return;
        }

        var getPosition = CreatePositionResolver(_view, payload);
        var platformArgs = GestureDispatchBridge.CreatePlatformPointerEventArgs();
        var buttonsMask = ResolveButtonMask(payload, defaultToPrimary: false);
        foreach (var recognizer in recognizers)
        {
            dispatch(recognizer, _view, getPosition, platformArgs, buttonsMask);
        }
    }

    private void RefreshRecognizers()
    {
        var nextRecognizers = _compositeGestureRecognizers
            .Where(static recognizer => recognizer is not null)
            .ToHashSet<IGestureRecognizer>(ReferenceEqualityComparer.Instance);

        CancelRemovedActiveRecognizers(nextRecognizers);

        foreach (var removed in _trackedRecognizers.Except(nextRecognizers, ReferenceEqualityComparer.Instance).OfType<INotifyPropertyChanged>())
        {
            removed.PropertyChanged -= OnGestureRecognizerPropertyChanged;
        }

        foreach (var added in nextRecognizers.Except(_trackedRecognizers, ReferenceEqualityComparer.Instance).OfType<INotifyPropertyChanged>())
        {
            added.PropertyChanged += OnGestureRecognizerPropertyChanged;
        }

        _trackedRecognizers.Clear();
        foreach (var recognizer in nextRecognizers)
        {
            _trackedRecognizers.Add(recognizer);
        }

        _activePanGestures.RemoveAll(state => !_trackedRecognizers.Contains(state.Recognizer));
        if (_activePinchGesture is not null && !_trackedRecognizers.Contains(_activePinchGesture.Recognizer))
        {
            _activePinchGesture = null;
        }
    }

    private void CancelRemovedActiveRecognizers(HashSet<IGestureRecognizer> nextRecognizers)
    {
        foreach (var activePan in _activePanGestures.Where(state => !nextRecognizers.Contains(state.Recognizer)).ToArray())
        {
            if (activePan.HasStarted)
            {
                ((IPanGestureController)activePan.Recognizer).SendPanCanceled(_view, activePan.GestureId);
            }

            _activePanGestures.Remove(activePan);
        }

        if (_activePinchGesture is not null && !nextRecognizers.Contains(_activePinchGesture.Recognizer))
        {
            ((IPinchGestureController)_activePinchGesture.Recognizer).SendPinchCanceled(_view);
            _activePinchGesture = null;
        }
    }

    private void RefreshEventSubscriptions()
    {
        var requiresClick = EnumerateTapRecognizers().Any(UsesPrimaryButton);
        var requiresContextMenu = EnumerateTapRecognizers().Any(UsesSecondaryButton);
        var requiresPointerHover = EnumeratePointerRecognizers().Any();
        var requiresPointerTracking = requiresPointerHover
            || EnumeratePanRecognizers().Any()
            || EnumeratePinchRecognizers().Any()
            || EnumerateSwipeRecognizers().Any()
            || EnumerateTapRecognizers().Any();

        var changed = false;
        changed |= SetGestureEventEnabled("click", requiresClick);
        changed |= SetGestureEventEnabled("contextmenu", requiresContextMenu);
        changed |= SetGestureEventEnabled("pointerdown", requiresPointerTracking);
        changed |= SetGestureEventEnabled("pointermove", requiresPointerTracking);
        changed |= SetGestureEventEnabled("pointerup", requiresPointerTracking);
        changed |= SetGestureEventEnabled("pointercancel", requiresPointerTracking);
        changed |= SetGestureEventEnabled("pointerenter", requiresPointerHover);
        changed |= SetGestureEventEnabled("pointerleave", requiresPointerHover);

        if (changed)
        {
            _requestRender?.Invoke();
        }
    }

    private bool SetGestureEventEnabled(string eventName, bool enabled)
    {
        if (enabled)
        {
            if (_baselineEvents.Contains(eventName) || _gestureOwnedEvents.Contains(eventName))
            {
                return false;
            }

            if (_platformView.Events.Contains(eventName))
            {
                return false;
            }

            _platformView.SetEvent(eventName, enabled: true);
            _gestureOwnedEvents.Add(eventName);
            return true;
        }

        if (!_gestureOwnedEvents.Remove(eventName))
        {
            return false;
        }

        _platformView.SetEvent(eventName, enabled: false);
        return true;
    }

    private void OnGestureCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        RefreshRecognizers();
        RefreshEventSubscriptions();
    }

    private void OnGestureRecognizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        RefreshEventSubscriptions();
    }

    private IEnumerable<TapGestureRecognizer> EnumerateTapRecognizers() => _trackedRecognizers.OfType<TapGestureRecognizer>();

    private IEnumerable<PanGestureRecognizer> EnumeratePanRecognizers() => _trackedRecognizers.OfType<PanGestureRecognizer>();

    private IEnumerable<SwipeGestureRecognizer> EnumerateSwipeRecognizers() => _trackedRecognizers.OfType<SwipeGestureRecognizer>();

    private IEnumerable<PointerGestureRecognizer> EnumeratePointerRecognizers() => _trackedRecognizers.OfType<PointerGestureRecognizer>();

    private IEnumerable<PinchGestureRecognizer> EnumeratePinchRecognizers() => _trackedRecognizers.OfType<PinchGestureRecognizer>().Take(1);

    private static bool UsesPrimaryButton(TapGestureRecognizer recognizer)
    {
        var buttons = NormalizeTapButtons(recognizer);
        return (buttons & ButtonsMask.Primary) != 0;
    }

    private static bool UsesSecondaryButton(TapGestureRecognizer recognizer)
    {
        var buttons = NormalizeTapButtons(recognizer);
        return (buttons & ButtonsMask.Secondary) != 0;
    }

    private static ButtonsMask NormalizeTapButtons(TapGestureRecognizer recognizer)
    {
        var buttons = recognizer.Buttons;
        return buttons == 0 ? ButtonsMask.Primary : buttons;
    }

    private static bool IsTapButtonMatch(TapGestureRecognizer recognizer, ButtonsMask actualButtons)
    {
        var expected = NormalizeTapButtons(recognizer);
        return (expected & actualButtons) != 0;
    }

    private void MarkTapSuppression(ButtonsMask buttonsMask)
    {
        if ((buttonsMask & ButtonsMask.Primary) != 0)
        {
            _suppressNextPrimaryClick = true;
        }

        if ((buttonsMask & ButtonsMask.Secondary) != 0)
        {
            _suppressNextSecondaryContextMenu = true;
        }
    }

    private void ClearTapSuppression()
    {
        _suppressNextPrimaryClick = false;
        _suppressNextSecondaryContextMenu = false;
    }

    private static Func<IElement, Point?> CreatePositionResolver(View sourceView, MirageGesturePointerPayload payload)
    {
        return element =>
        {
            if (element is null)
            {
                return new Point(payload.ClientX, payload.ClientY);
            }

            if (element is not VisualElement target)
            {
                return null;
            }

            if (!TryGetAbsoluteOrigin(target, out var origin))
            {
                return null;
            }

            if (target == sourceView)
            {
                return new Point(payload.X, payload.Y);
            }

            return new Point(payload.ClientX - origin.X, payload.ClientY - origin.Y);
        };
    }

    private static bool TryGetAbsoluteOrigin(VisualElement element, out Point origin)
    {
        double x = 0;
        double y = 0;
        Element? current = element;
        while (current is VisualElement visual)
        {
            x += visual.X + visual.TranslationX;
            y += visual.Y + visual.TranslationY;

            if (visual.Parent is IScrollView scrollView)
            {
                x -= scrollView.HorizontalOffset;
                y -= scrollView.VerticalOffset;
            }

            current = visual.Parent;
        }

        origin = new Point(x, y);
        return true;
    }

    private bool TryCalculateCentroid(long[] pointerIds, out Point centroid)
    {
        if (pointerIds.Length == 0)
        {
            centroid = Point.Zero;
            return false;
        }

        double totalX = 0;
        double totalY = 0;
        foreach (var pointerId in pointerIds)
        {
            if (!_activePointers.TryGetValue(pointerId, out var pointer))
            {
                centroid = Point.Zero;
                return false;
            }

            totalX += pointer.LastClientX;
            totalY += pointer.LastClientY;
        }

        centroid = new Point(totalX / pointerIds.Length, totalY / pointerIds.Length);
        return true;
    }

    private Point CalculateCentroid(long[] pointerIds)
    {
        _ = TryCalculateCentroid(pointerIds, out var centroid);
        return centroid;
    }

    private bool TryCalculateDistance(long[] pointerIds, out double distance, out Point midpoint)
    {
        distance = 0;
        midpoint = Point.Zero;
        if (pointerIds.Length < 2
            || !_activePointers.TryGetValue(pointerIds[0], out var first)
            || !_activePointers.TryGetValue(pointerIds[1], out var second))
        {
            return false;
        }

        distance = Distance(first.LastClientX, first.LastClientY, second.LastClientX, second.LastClientY);
        midpoint = new Point((first.LastX + second.LastX) / 2d, (first.LastY + second.LastY) / 2d);
        return true;
    }

    private Point CreateScaleOrigin(Point midpoint)
    {
        var width = ResolveReferenceLength(_view.Width, _view.Frame.Width, _view.Bounds.Width, _view.WidthRequest);
        var height = ResolveReferenceLength(_view.Height, _view.Frame.Height, _view.Bounds.Height, _view.HeightRequest);

        return new Point(
            Math.Clamp(midpoint.X / width, 0, 1),
            Math.Clamp(midpoint.Y / height, 0, 1));
    }

    private static double ResolveReferenceLength(params double[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!double.IsNaN(candidate) && candidate > CoordinateEpsilon)
            {
                return candidate;
            }
        }

        return CoordinateEpsilon;
    }

    private static ButtonsMask ResolveButtonMask(MirageGesturePointerPayload payload, bool defaultToPrimary)
    {
        if ((payload.Buttons & 2) != 0 || payload.Button == 2)
        {
            return ButtonsMask.Secondary;
        }

        if ((payload.Buttons & 1) != 0)
        {
            return ButtonsMask.Primary;
        }

        if (payload.Button == 0 && payload.Buttons == 0)
        {
            return defaultToPrimary ? ButtonsMask.Primary : 0;
        }

        return 0;
    }

    private bool ArePrimaryPanPointers(long[] pointerIds)
    {
        foreach (var pointerId in pointerIds)
        {
            if (!_activePointers.TryGetValue(pointerId, out var pointer))
            {
                return false;
            }

            if (!pointer.ButtonMask.HasFlag(ButtonsMask.Primary) || pointer.ButtonMask.HasFlag(ButtonsMask.Secondary))
            {
                return false;
            }
        }

        return true;
    }

    private int ResolveSecondaryTapCount(MirageGesturePointerPayload payload)
    {
        if (payload.ClickCount > 0)
        {
            return Math.Max(1, payload.ClickCount);
        }

        if (_pendingSecondaryTapCounts.Remove(payload.PointerId, out var clickCount) && clickCount > 0)
        {
            return clickCount;
        }

        if (payload.PointerId == 0 && _pendingSecondaryTapCounts.Remove(1, out clickCount) && clickCount > 0)
        {
            return clickCount;
        }

        return 1;
    }

    private bool TryResolveTerminalPointerPayload(
        MirageGesturePointerPayload payload,
        out MirageGesturePointerPayload resolvedPayload,
        out ActivePointer activePointer)
    {
        resolvedPayload = payload;
        activePointer = default;
        if (!_activePointers.TryGetValue(payload.PointerId, out activePointer))
        {
            return false;
        }

        activePointer = activePointer with
        {
            LastClientX = payload.ClientX,
            LastClientY = payload.ClientY,
            LastX = payload.X,
            LastY = payload.Y
        };
        _activePointers[payload.PointerId] = activePointer;

        resolvedPayload = payload with
        {
            Buttons = payload.Buttons == 0 ? ToBrowserButtons(activePointer.ButtonMask) : payload.Buttons,
            Button = payload.Button == 0 && activePointer.ButtonMask == ButtonsMask.Secondary ? 2 : payload.Button
        };
        return true;
    }

    private static int ToBrowserButtons(ButtonsMask buttonsMask)
    {
        var result = 0;
        if ((buttonsMask & ButtonsMask.Primary) != 0)
        {
            result |= 1;
        }

        if ((buttonsMask & ButtonsMask.Secondary) != 0)
        {
            result |= 2;
        }

        return result;
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        var deltaX = x2 - x1;
        var deltaY = y2 - y1;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private static bool TryParsePointerPayload(string? payload, out MirageGesturePointerPayload pointerPayload)
    {
        pointerPayload = default!;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<MirageGesturePointerPayload>(payload, PayloadJsonOptions);
            if (parsed is null)
            {
                return false;
            }

            pointerPayload = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private readonly record struct ActivePointer(
        long PointerId,
        ButtonsMask ButtonMask,
        double StartClientX,
        double StartClientY,
        double StartX,
        double StartY,
        double LastClientX,
        double LastClientY,
        double LastX,
        double LastY,
        bool ExceededTapThreshold,
        bool ParticipatedInPinch);

    private sealed record PanGestureState(
        PanGestureRecognizer Recognizer,
        int GestureId,
        long[] PointerIds,
        Point StartCentroid,
        bool HasStarted);

    private sealed record PinchGestureState(
        PinchGestureRecognizer Recognizer,
        long[] PointerIds,
        double InitialDistance,
        Point InitialOrigin);

    private sealed record MirageGesturePointerPayload
    {
        public long PointerId { get; init; }

        public string? PointerType { get; init; }

        public double X { get; init; }

        public double Y { get; init; }

        public double ClientX { get; init; }

        public double ClientY { get; init; }

        public int Button { get; init; }

        public int Buttons { get; init; }

        public int ClickCount { get; init; }

        public bool IsInsideBounds { get; init; }
    }

    private static class GestureDispatchBridge
    {
        private static readonly MethodInfo TapSendTappedMethod = ResolveInstanceMethod(
            typeof(TapGestureRecognizer),
            "SendTapped",
            [typeof(View), typeof(Func<IElement, Point?>)]);

        private static readonly MethodInfo PointerSendEnteredMethod = ResolveInstanceMethod(
            typeof(PointerGestureRecognizer),
            "SendPointerEntered",
            [typeof(View), typeof(Func<IElement, Point?>), typeof(PlatformPointerEventArgs), typeof(ButtonsMask)]);

        private static readonly MethodInfo PointerSendExitedMethod = ResolveInstanceMethod(
            typeof(PointerGestureRecognizer),
            "SendPointerExited",
            [typeof(View), typeof(Func<IElement, Point?>), typeof(PlatformPointerEventArgs), typeof(ButtonsMask)]);

        private static readonly MethodInfo PointerSendMovedMethod = ResolveInstanceMethod(
            typeof(PointerGestureRecognizer),
            "SendPointerMoved",
            [typeof(View), typeof(Func<IElement, Point?>), typeof(PlatformPointerEventArgs), typeof(ButtonsMask)]);

        private static readonly MethodInfo PointerSendPressedMethod = ResolveInstanceMethod(
            typeof(PointerGestureRecognizer),
            "SendPointerPressed",
            [typeof(View), typeof(Func<IElement, Point?>), typeof(PlatformPointerEventArgs), typeof(ButtonsMask)]);

        private static readonly MethodInfo PointerSendReleasedMethod = ResolveInstanceMethod(
            typeof(PointerGestureRecognizer),
            "SendPointerReleased",
            [typeof(View), typeof(Func<IElement, Point?>), typeof(PlatformPointerEventArgs), typeof(ButtonsMask)]);

        private static readonly ConstructorInfo PlatformPointerArgsCtor = typeof(PlatformPointerEventArgs).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null)
            ?? throw new InvalidOperationException(
                "Mirage gesture bridge could not find a parameterless PlatformPointerEventArgs constructor. Upstream MAUI likely changed and the bridge must be updated.");

        public static void SendTapped(TapGestureRecognizer recognizer, View sender, Func<IElement, Point?> getPosition)
        {
            TapSendTappedMethod.Invoke(recognizer, [sender, getPosition]);
        }

        public static PlatformPointerEventArgs CreatePlatformPointerEventArgs()
        {
            return (PlatformPointerEventArgs)(PlatformPointerArgsCtor.Invoke([]) ?? throw new InvalidOperationException("Failed to construct PlatformPointerEventArgs."));
        }

        public static void SendPointerEntered(PointerGestureRecognizer recognizer, View sender, Func<IElement, Point?> getPosition, PlatformPointerEventArgs args, ButtonsMask buttonsMask)
        {
            PointerSendEnteredMethod.Invoke(recognizer, [sender, getPosition, args, buttonsMask]);
        }

        public static void SendPointerExited(PointerGestureRecognizer recognizer, View sender, Func<IElement, Point?> getPosition, PlatformPointerEventArgs args, ButtonsMask buttonsMask)
        {
            PointerSendExitedMethod.Invoke(recognizer, [sender, getPosition, args, buttonsMask]);
        }

        public static void SendPointerMoved(PointerGestureRecognizer recognizer, View sender, Func<IElement, Point?> getPosition, PlatformPointerEventArgs args, ButtonsMask buttonsMask)
        {
            PointerSendMovedMethod.Invoke(recognizer, [sender, getPosition, args, buttonsMask]);
        }

        public static void SendPointerPressed(PointerGestureRecognizer recognizer, View sender, Func<IElement, Point?> getPosition, PlatformPointerEventArgs args, ButtonsMask buttonsMask)
        {
            PointerSendPressedMethod.Invoke(recognizer, [sender, getPosition, args, buttonsMask]);
        }

        public static void SendPointerReleased(PointerGestureRecognizer recognizer, View sender, Func<IElement, Point?> getPosition, PlatformPointerEventArgs args, ButtonsMask buttonsMask)
        {
            PointerSendReleasedMethod.Invoke(recognizer, [sender, getPosition, args, buttonsMask]);
        }

        private static MethodInfo ResolveInstanceMethod(Type type, string name, Type[] parameterTypes)
        {
            return type.GetMethod(
                       name,
                       BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                       binder: null,
                       parameterTypes,
                       modifiers: null)
                   ?? throw new InvalidOperationException(
                       $"Mirage gesture bridge could not find '{type.FullName}.{name}({string.Join(", ", parameterTypes.Select(static parameterType => parameterType.Name))})'. Upstream MAUI likely changed and the bridge must be updated.");
        }
    }
}
