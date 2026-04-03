using System.Text.Json;
using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageGestureRecognizerManagerTests
{
    [Fact]
    public void GestureRecognizerManager_RuntimeRecognizerChanges_RequestRenderWhenEventSubscriptionsChange()
    {
        var view = new ContentView();
        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        var requestRenderCount = 0;
        using var manager = new MirageGestureRecognizerManager(view, platformView, () => requestRenderCount++);

        Assert.Equal(0, requestRenderCount);

        var pinch = new PinchGestureRecognizer();
        view.GestureRecognizers.Add(pinch);

        Assert.Contains("pointerdown", platformView.Events);
        Assert.True(requestRenderCount >= 1);

        var requestRenderCountAfterAdd = requestRenderCount;
        view.GestureRecognizers.Remove(pinch);

        Assert.DoesNotContain("pointerdown", platformView.Events);
        Assert.True(requestRenderCount > requestRenderCountAfterAdd);
    }

    [Fact]
    public void GestureRecognizerManager_AddingAndRemovingRecognizers_UpdatesRuntimeEventSubscriptions()
    {
        var view = new Label();
        var platformView = MiragePlatformView.CreateControl(ComponentKind.Label);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        Assert.DoesNotContain("click", platformView.Events);
        Assert.DoesNotContain("pointerdown", platformView.Events);

        var tap = new TapGestureRecognizer();
        view.GestureRecognizers.Add(tap);

        Assert.Contains("click", platformView.Events);
        Assert.Contains("pointerdown", platformView.Events);
        Assert.Contains("pointermove", platformView.Events);
        Assert.Contains("pointerup", platformView.Events);
        Assert.Contains("pointercancel", platformView.Events);

        view.GestureRecognizers.Remove(tap);

        Assert.DoesNotContain("click", platformView.Events);
        Assert.DoesNotContain("pointerdown", platformView.Events);
        Assert.DoesNotContain("pointermove", platformView.Events);
        Assert.DoesNotContain("pointerup", platformView.Events);
        Assert.DoesNotContain("pointercancel", platformView.Events);
    }

    [Fact]
    public void GestureRecognizerManager_TapRecognizer_SupportsPrimarySecondaryMultiTapCommandsAndPositions()
    {
        var container = new VerticalStackLayout
        {
            TranslationX = 20,
            TranslationY = 30
        };
        var view = new Label
        {
            WidthRequest = 160,
            HeightRequest = 40,
            TranslationX = 3,
            TranslationY = 5
        };
        container.Children.Add(view);

        var commandLog = new List<string>();
        var eventLog = new List<string>();
        Point? selfPosition = null;
        Point? containerPosition = null;
        Point? windowPosition = null;

        var singleTap = new TapGestureRecognizer
        {
            Command = new Command<string>(value => commandLog.Add(value)),
            CommandParameter = "single"
        };
        singleTap.Tapped += (_, args) =>
        {
            eventLog.Add("single");
            selfPosition = args.GetPosition(view);
            containerPosition = args.GetPosition(container);
            windowPosition = args.GetPosition(null);
        };

        var doubleTap = new TapGestureRecognizer
        {
            NumberOfTapsRequired = 2,
            Buttons = ButtonsMask.Secondary | ButtonsMask.Primary
        };
        doubleTap.Tapped += (_, _) => eventLog.Add("double");

        var tripleTap = new TapGestureRecognizer
        {
            NumberOfTapsRequired = 3
        };
        tripleTap.Tapped += (_, _) => eventLog.Add("triple");

        var secondaryTap = new TapGestureRecognizer
        {
            Buttons = ButtonsMask.Secondary
        };
        secondaryTap.Tapped += (_, _) => eventLog.Add("secondary");

        view.GestureRecognizers.Add(singleTap);
        view.GestureRecognizers.Add(doubleTap);
        view.GestureRecognizers.Add(tripleTap);
        view.GestureRecognizers.Add(secondaryTap);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.Label);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("click", CreatePointerPayload(pointerId: 1, x: 12, y: 18, clientX: 35, clientY: 53, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("click", CreatePointerPayload(pointerId: 1, x: 12, y: 18, clientX: 35, clientY: 53, button: 0, buttons: 1, clickCount: 2, isInsideBounds: true));
        manager.OnRuntimeEvent("click", CreatePointerPayload(pointerId: 1, x: 12, y: 18, clientX: 35, clientY: 53, button: 0, buttons: 1, clickCount: 3, isInsideBounds: true));
        manager.OnRuntimeEvent("contextmenu", CreatePointerPayload(pointerId: 2, x: 10, y: 16, clientX: 33, clientY: 51, button: 2, buttons: 2, clickCount: 1, isInsideBounds: true));

        Assert.Equal(["single"], commandLog);
        Assert.Equal(["single", "double", "triple", "secondary"], eventLog);
        Assert.Equal(new Point(12, 18), selfPosition);
        Assert.Equal(new Point(15, 23), containerPosition);
        Assert.Equal(new Point(35, 53), windowPosition);
    }

    [Fact]
    public void GestureRecognizerManager_PointerRecognizer_RaisesEventsCommandsAndWindowPositions()
    {
        var view = new Label
        {
            WidthRequest = 200,
            HeightRequest = 60
        };

        var eventLog = new List<string>();
        var commandLog = new List<string>();
        Point? movedSelfPosition = null;
        Point? movedWindowPosition = null;

        var pointer = new PointerGestureRecognizer
        {
            PointerEnteredCommand = new Command<string>(value => commandLog.Add(value)),
            PointerEnteredCommandParameter = "entered",
            PointerExitedCommand = new Command<string>(value => commandLog.Add(value)),
            PointerExitedCommandParameter = "exited"
        };
        pointer.PointerEntered += (_, _) => eventLog.Add("entered");
        pointer.PointerMoved += (_, args) =>
        {
            eventLog.Add("moved");
            movedSelfPosition = args.GetPosition(view);
            movedWindowPosition = args.GetPosition(null);
        };
        pointer.PointerPressed += (_, _) => eventLog.Add("pressed");
        pointer.PointerReleased += (_, _) => eventLog.Add("released");
        pointer.PointerExited += (_, _) => eventLog.Add("exited");
        view.GestureRecognizers.Add(pointer);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.Label);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerenter", CreatePointerPayload(pointerId: 3, x: 9, y: 11, clientX: 49, clientY: 71, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 3, x: 15, y: 17, clientX: 55, clientY: 77, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 3, x: 15, y: 17, clientX: 55, clientY: 77, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 3, x: 16, y: 18, clientX: 56, clientY: 78, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerleave", CreatePointerPayload(pointerId: 3, x: 16, y: 18, clientX: 56, clientY: 78, button: 0, buttons: 0, clickCount: 1, isInsideBounds: false));

        Assert.Equal(["entered", "moved", "pressed", "released", "exited"], eventLog);
        Assert.Equal(["entered", "exited"], commandLog);
        Assert.Equal(new Point(15, 17), movedSelfPosition);
        Assert.Equal(new Point(55, 77), movedWindowPosition);
    }

    [Fact]
    public void GestureRecognizerManager_PointerCancel_DispatchesReleased()
    {
        var view = new Label();
        var eventLog = new List<string>();

        var pointer = new PointerGestureRecognizer();
        pointer.PointerPressed += (_, _) => eventLog.Add("pressed");
        pointer.PointerReleased += (_, _) => eventLog.Add("released");
        view.GestureRecognizers.Add(pointer);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.Label);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 7, x: 15, y: 17, clientX: 55, clientY: 77, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointercancel", CreatePointerPayload(pointerId: 7, x: 15, y: 17, clientX: 55, clientY: 77, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Equal(["pressed", "released"], eventLog);
    }

    [Fact]
    public void GestureRecognizerManager_PanRecognizer_DoesNotStartOrCompleteWithoutMovement()
    {
        var view = new ContentView();
        var updates = new List<GestureStatus>();
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, args) => updates.Add(args.StatusType);
        view.GestureRecognizers.Add(pan);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 10, y: 10, clientX: 10, clientY: 10, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 1, x: 10, y: 10, clientX: 10, clientY: 10, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Empty(updates);
    }

    [Fact]
    public void GestureRecognizerManager_PanRecognizer_TracksTouchPointsAndCumulativeDeltas()
    {
        var view = new ContentView
        {
            WidthRequest = 200,
            HeightRequest = 120
        };

        var updates = new List<(GestureStatus Status, double TotalX, double TotalY)>();
        var pan = new PanGestureRecognizer
        {
            TouchPoints = 2
        };
        pan.PanUpdated += (_, args) => updates.Add((args.StatusType, args.TotalX, args.TotalY));
        view.GestureRecognizers.Add(pan);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 10, y: 10, clientX: 10, clientY: 10, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        Assert.Empty(updates);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 2, x: 30, y: 10, clientX: 30, clientY: 10, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 1, x: 20, y: 20, clientX: 20, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 2, x: 40, y: 20, clientX: 40, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 2, x: 40, y: 20, clientX: 40, clientY: 20, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Equal(GestureStatus.Started, updates[0].Status);
        Assert.Contains(updates, update => update.Status == GestureStatus.Running && Math.Abs(update.TotalX - 10) < 0.001 && Math.Abs(update.TotalY - 10) < 0.001);
        Assert.Equal(GestureStatus.Completed, updates[^1].Status);
    }

    [Fact]
    public void GestureRecognizerManager_RemovingActivePanRecognizer_SendsCanceled()
    {
        var view = new ContentView();
        var updates = new List<GestureStatus>();
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, args) => updates.Add(args.StatusType);
        view.GestureRecognizers.Add(pan);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 10, y: 10, clientX: 10, clientY: 10, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 1, x: 25, y: 28, clientX: 25, clientY: 28, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        Assert.Equal([GestureStatus.Started, GestureStatus.Running], updates);

        view.GestureRecognizers.Remove(pan);

        Assert.Equal([GestureStatus.Started, GestureStatus.Running, GestureStatus.Canceled], updates);
    }

    [Fact]
    public void GestureRecognizerManager_PinchRecognizer_CancelsPanAndReportsScaleAndOrigin()
    {
        var view = new ContentView
        {
            WidthRequest = 200,
            HeightRequest = 100
        };
        ((IView)view).Arrange(new Rect(0, 0, 200, 100));

        var panUpdates = new List<GestureStatus>();
        var pinchUpdates = new List<(GestureStatus Status, double Scale, Point Origin)>();

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, args) => panUpdates.Add(args.StatusType);
        view.GestureRecognizers.Add(pan);

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += (_, args) => pinchUpdates.Add((args.Status, args.Scale, args.ScaleOrigin));
        view.GestureRecognizers.Add(pinch);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 20, y: 20, clientX: 20, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 1, x: 34, y: 24, clientX: 34, clientY: 24, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 2, x: 180, y: 20, clientX: 180, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 2, x: 200, y: 20, clientX: 200, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 2, x: 200, y: 20, clientX: 200, clientY: 20, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Equal([GestureStatus.Started, GestureStatus.Running, GestureStatus.Canceled], panUpdates);
        Assert.Equal(GestureStatus.Started, pinchUpdates[0].Status);
        Assert.Equal(new Point(0.535, 0.22), pinchUpdates[0].Origin);
        Assert.Contains(pinchUpdates, update => update.Status == GestureStatus.Running && update.Scale > 1.1d && update.Origin.X > 0.54d);
        Assert.Equal(GestureStatus.Completed, pinchUpdates[^1].Status);
    }

    [Fact]
    public void GestureRecognizerManager_RemovingActivePinchRecognizer_SendsCanceled()
    {
        var view = new ContentView
        {
            WidthRequest = 200,
            HeightRequest = 100
        };

        var pinchUpdates = new List<GestureStatus>();
        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += (_, args) => pinchUpdates.Add(args.Status);
        view.GestureRecognizers.Add(pinch);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 20, y: 20, clientX: 20, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 2, x: 180, y: 20, clientX: 180, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        Assert.Equal([GestureStatus.Started], pinchUpdates);

        view.GestureRecognizers.Remove(pinch);

        Assert.Equal([GestureStatus.Started, GestureStatus.Canceled], pinchUpdates);
    }

    [Fact]
    public void GestureRecognizerManager_ViewValidation_AllowsOnlyOnePinchRecognizer()
    {
        var view = new ContentView();
        view.GestureRecognizers.Add(new PinchGestureRecognizer());

        var exception = Assert.Throws<InvalidOperationException>(() => view.GestureRecognizers.Add(new PinchGestureRecognizer()));
        Assert.Contains("Only one PinchGestureRecognizer per view is allowed", exception.Message);
    }

    [Fact]
    public void GestureRecognizerManager_SwipeRecognizer_RecognizesDominantDirections_AndSuppressesTapAfterDrag()
    {
        var view = new ContentView
        {
            WidthRequest = 200,
            HeightRequest = 200
        };

        var swipes = new List<SwipeDirection>();
        var tapCount = 0;

        foreach (var direction in new[] { SwipeDirection.Left, SwipeDirection.Right, SwipeDirection.Up, SwipeDirection.Down })
        {
            var swipe = new SwipeGestureRecognizer
            {
                Direction = direction
            };
            swipe.Swiped += (_, args) => swipes.Add(args.Direction);
            view.GestureRecognizers.Add(swipe);
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => tapCount++;
        view.GestureRecognizers.Add(tap);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        DispatchSwipe(manager, startX: 120, startY: 80, endX: 40, endY: 82);
        DispatchSwipe(manager, startX: 40, startY: 80, endX: 120, endY: 78);
        DispatchSwipe(manager, startX: 80, startY: 140, endX: 82, endY: 50);
        DispatchSwipe(manager, startX: 80, startY: 40, endX: 78, endY: 120);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 5, x: 60, y: 60, clientX: 60, clientY: 60, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 5, x: 72, y: 72, clientX: 72, clientY: 72, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 5, x: 72, y: 72, clientX: 72, clientY: 72, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("click", CreatePointerPayload(pointerId: 5, x: 72, y: 72, clientX: 72, clientY: 72, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 6, x: 40, y: 40, clientX: 40, clientY: 40, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 6, x: 65, y: 58, clientX: 65, clientY: 58, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 6, x: 65, y: 58, clientX: 65, clientY: 58, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Equal(
            [SwipeDirection.Left, SwipeDirection.Right, SwipeDirection.Up, SwipeDirection.Down],
            swipes);
        Assert.Equal(0, tapCount);
    }

    [Fact]
    public void GestureRecognizerManager_SwipeRecognizer_UsesPointerUpCoordinates_WhenNoMoveEventOccurs()
    {
        var view = new ContentView
        {
            WidthRequest = 220,
            HeightRequest = 220
        };

        var swipes = new List<SwipeDirection>();
        var swipe = new SwipeGestureRecognizer
        {
            Direction = SwipeDirection.Right
        };
        swipe.Swiped += (_, args) => swipes.Add(args.Direction);
        view.GestureRecognizers.Add(swipe);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 21, x: 20, y: 60, clientX: 20, clientY: 60, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 21, x: 100, y: 62, clientX: 100, clientY: 62, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Equal([SwipeDirection.Right], swipes);
    }

    [Fact]
    public void GestureRecognizerManager_PinchSequence_DoesNotAlsoFireSwipe()
    {
        var view = new ContentView
        {
            WidthRequest = 220,
            HeightRequest = 220
        };

        var pinchStatuses = new List<GestureStatus>();
        var swipes = new List<SwipeDirection>();

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += (_, args) => pinchStatuses.Add(args.Status);
        view.GestureRecognizers.Add(pinch);

        var swipe = new SwipeGestureRecognizer
        {
            Direction = SwipeDirection.Right
        };
        swipe.Swiped += (_, args) => swipes.Add(args.Direction);
        view.GestureRecognizers.Add(swipe);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 40, y: 120, clientX: 40, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 2, x: 120, y: 120, clientX: 120, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 1, x: 85, y: 120, clientX: 85, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 2, x: 175, y: 120, clientX: 175, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 1, x: 85, y: 120, clientX: 85, clientY: 120, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 2, x: 175, y: 120, clientX: 175, clientY: 120, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Equal(GestureStatus.Started, pinchStatuses[0]);
        Assert.Contains(pinchStatuses, static status => status == GestureStatus.Running);
        Assert.Equal(GestureStatus.Completed, pinchStatuses[^1]);
        Assert.Empty(swipes);
    }

    [Fact]
    public void GestureRecognizerManager_ThirdPointerLift_DuringActivePinch_DoesNotFireSwipe()
    {
        var view = new ContentView
        {
            WidthRequest = 220,
            HeightRequest = 220
        };

        var pinchStatuses = new List<GestureStatus>();
        var swipes = new List<SwipeDirection>();

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += (_, args) => pinchStatuses.Add(args.Status);
        view.GestureRecognizers.Add(pinch);

        var swipe = new SwipeGestureRecognizer
        {
            Direction = SwipeDirection.Right
        };
        swipe.Swiped += (_, args) => swipes.Add(args.Direction);
        view.GestureRecognizers.Add(swipe);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 40, y: 120, clientX: 40, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 2, x: 120, y: 120, clientX: 120, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 1, x: 85, y: 120, clientX: 85, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 2, x: 175, y: 120, clientX: 175, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 3, x: 20, y: 80, clientX: 20, clientY: 80, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 3, x: 150, y: 82, clientX: 150, clientY: 82, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 3, x: 150, y: 82, clientX: 150, clientY: 82, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 1, x: 85, y: 120, clientX: 85, clientY: 120, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 2, x: 175, y: 120, clientX: 175, clientY: 120, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Equal(GestureStatus.Started, pinchStatuses[0]);
        Assert.Contains(pinchStatuses, static status => status == GestureStatus.Running);
        Assert.Equal(GestureStatus.Completed, pinchStatuses[^1]);
        Assert.Empty(swipes);
    }

    [Fact]
    public void GestureRecognizerManager_ThirdPointerLift_AfterPinchEnds_DoesNotFireSwipe()
    {
        var view = new ContentView
        {
            WidthRequest = 220,
            HeightRequest = 220
        };

        var pinchStatuses = new List<GestureStatus>();
        var swipes = new List<SwipeDirection>();

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += (_, args) => pinchStatuses.Add(args.Status);
        view.GestureRecognizers.Add(pinch);

        var swipe = new SwipeGestureRecognizer
        {
            Direction = SwipeDirection.Right
        };
        swipe.Swiped += (_, args) => swipes.Add(args.Direction);
        view.GestureRecognizers.Add(swipe);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 40, y: 120, clientX: 40, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 3, x: 20, y: 80, clientX: 20, clientY: 80, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 2, x: 120, y: 120, clientX: 120, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 1, x: 85, y: 120, clientX: 85, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 2, x: 175, y: 120, clientX: 175, clientY: 120, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 3, x: 150, y: 82, clientX: 150, clientY: 82, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 1, x: 85, y: 120, clientX: 85, clientY: 120, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 3, x: 150, y: 82, clientX: 150, clientY: 82, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 2, x: 175, y: 120, clientX: 175, clientY: 120, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Equal(GestureStatus.Started, pinchStatuses[0]);
        Assert.Contains(pinchStatuses, static status => status == GestureStatus.Running);
        Assert.Equal(GestureStatus.Completed, pinchStatuses[^1]);
        Assert.Empty(swipes);
    }

    [Fact]
    public void GestureRecognizerManager_DragWithoutClick_DoesNotSuppressNextUnrelatedTap()
    {
        var view = new ContentView
        {
            WidthRequest = 200,
            HeightRequest = 200
        };

        var tapCount = 0;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => tapCount++;
        view.GestureRecognizers.Add(tap);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 40, y: 40, clientX: 40, clientY: 40, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 1, x: 58, y: 58, clientX: 58, clientY: 58, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 1, x: 58, y: 58, clientX: 58, clientY: 58, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 2, x: 20, y: 20, clientX: 20, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 2, x: 20, y: 20, clientX: 20, clientY: 20, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("click", CreatePointerPayload(pointerId: 2, x: 20, y: 20, clientX: 20, clientY: 20, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 3, x: 70, y: 70, clientX: 70, clientY: 70, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 3, x: 90, y: 90, clientX: 90, clientY: 90, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointercancel", CreatePointerPayload(pointerId: 3, x: 90, y: 90, clientX: 90, clientY: 90, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 4, x: 24, y: 28, clientX: 24, clientY: 28, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 4, x: 24, y: 28, clientX: 24, clientY: 28, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("click", CreatePointerPayload(pointerId: 4, x: 24, y: 28, clientX: 24, clientY: 28, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));

        Assert.Equal(2, tapCount);
    }

    [Fact]
    public void GestureRecognizerManager_PanRecognizer_IgnoresSecondaryButtonDrag()
    {
        var view = new ContentView();
        var updates = new List<GestureStatus>();
        var pan = new PanGestureRecognizer
        {
            TouchPoints = 2
        };
        pan.PanUpdated += (_, args) => updates.Add(args.StatusType);
        view.GestureRecognizers.Add(pan);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 1, x: 20, y: 20, clientX: 20, clientY: 20, button: 2, buttons: 2, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 1, x: 80, y: 60, clientX: 80, clientY: 60, button: 2, buttons: 2, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 1, x: 80, y: 60, clientX: 80, clientY: 60, button: 2, buttons: 0, clickCount: 1, isInsideBounds: true));

        Assert.Empty(updates);
    }

    [Fact]
    public void GestureRecognizerManager_AuxiliaryButtonDrag_DoesNotStartPanSwipeOrSuppressNextPrimaryTap()
    {
        var view = new ContentView
        {
            WidthRequest = 220,
            HeightRequest = 220
        };

        var panUpdates = new List<GestureStatus>();
        var swipes = new List<SwipeDirection>();
        var tapCount = 0;

        var pan = new PanGestureRecognizer
        {
            TouchPoints = 2
        };
        pan.PanUpdated += (_, args) => panUpdates.Add(args.StatusType);
        view.GestureRecognizers.Add(pan);

        var swipe = new SwipeGestureRecognizer
        {
            Direction = SwipeDirection.Right
        };
        swipe.Swiped += (_, args) => swipes.Add(args.Direction);
        view.GestureRecognizers.Add(swipe);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => tapCount++;
        view.GestureRecognizers.Add(tap);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 30, x: 30, y: 40, clientX: 30, clientY: 40, button: 1, buttons: 4, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 30, x: 120, y: 42, clientX: 120, clientY: 42, button: 1, buttons: 4, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 30, x: 120, y: 42, clientX: 120, clientY: 42, button: 1, buttons: 0, clickCount: 1, isInsideBounds: true));

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 31, x: 15, y: 18, clientX: 15, clientY: 18, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 31, x: 15, y: 18, clientX: 15, clientY: 18, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("click", CreatePointerPayload(pointerId: 31, x: 15, y: 18, clientX: 15, clientY: 18, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));

        Assert.Empty(panUpdates);
        Assert.Empty(swipes);
        Assert.Equal(1, tapCount);
    }

    [Fact]
    public void GestureRecognizerManager_PositionResolver_AccountsForScrollViewOffsets()
    {
        var scrollView = new ScrollView();
        ((IScrollView)scrollView).HorizontalOffset = 30;
        ((IScrollView)scrollView).VerticalOffset = 45;

        var content = new VerticalStackLayout();
        var view = new Label();

        ((IView)content).Arrange(new Rect(8, 12, 200, 120));
        ((IView)view).Arrange(new Rect(14, 22, 80, 30));

        Point? positionInsideScrollView = null;
        Point? positionInsideContent = null;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, args) =>
        {
            positionInsideScrollView = args.GetPosition(scrollView);
            positionInsideContent = args.GetPosition(content);
        };

        view.GestureRecognizers.Add(tap);
        content.Children.Add(view);
        scrollView.Content = content;

        var platformView = MiragePlatformView.CreateControl(ComponentKind.Label);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("click", CreatePointerPayload(pointerId: 1, x: 9, y: 11, clientX: 31, clientY: 40, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));

        Assert.Equal(new Point(31, 40), positionInsideScrollView);
        Assert.Equal(new Point(53, 73), positionInsideContent);
    }

    [Fact]
    public void GestureRecognizerManager_SecondaryDoubleTap_UsesPointerDownCount_WhenContextMenuDetailIsZero()
    {
        var view = new Label();
        var eventLog = new List<string>();

        var singleSecondaryTap = new TapGestureRecognizer
        {
            Buttons = ButtonsMask.Secondary
        };
        singleSecondaryTap.Tapped += (_, _) => eventLog.Add("single");

        var doubleSecondaryTap = new TapGestureRecognizer
        {
            Buttons = ButtonsMask.Secondary,
            NumberOfTapsRequired = 2
        };
        doubleSecondaryTap.Tapped += (_, _) => eventLog.Add("double");

        view.GestureRecognizers.Add(singleSecondaryTap);
        view.GestureRecognizers.Add(doubleSecondaryTap);

        var platformView = MiragePlatformView.CreateControl(ComponentKind.Label);
        using var manager = new MirageGestureRecognizerManager(view, platformView);

        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 0, x: 10, y: 12, clientX: 50, clientY: 52, button: 2, buttons: 2, clickCount: 2, isInsideBounds: true));
        manager.OnRuntimeEvent("contextmenu", CreatePointerPayload(pointerId: 0, x: 10, y: 12, clientX: 50, clientY: 52, button: 2, buttons: 2, clickCount: 0, isInsideBounds: true));

        Assert.Equal(["double"], eventLog);
    }

    private static void DispatchSwipe(MirageGestureRecognizerManager manager, double startX, double startY, double endX, double endY)
    {
        const long pointerId = 9;
        manager.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId, startX, startY, startX, startY, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId, endX, endY, endX, endY, button: 0, buttons: 1, clickCount: 1, isInsideBounds: true));
        manager.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId, endX, endY, endX, endY, button: 0, buttons: 0, clickCount: 1, isInsideBounds: true));
    }

    private static string CreatePointerPayload(
        long pointerId,
        double x,
        double y,
        double clientX,
        double clientY,
        int button,
        int buttons,
        int clickCount,
        bool isInsideBounds)
    {
        return JsonSerializer.Serialize(new
        {
            pointerId,
            pointerType = "mouse",
            x,
            y,
            clientX,
            clientY,
            button,
            buttons,
            clickCount,
            isInsideBounds
        });
    }
}
