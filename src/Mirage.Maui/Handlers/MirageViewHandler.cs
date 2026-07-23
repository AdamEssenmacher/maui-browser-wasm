using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

public abstract class MirageViewHandler<TVirtualView> : ViewHandler<TVirtualView, MiragePlatformView>, IMiragePlatformHandler
    where TVirtualView : class, IView
{
    private static readonly IDisposable NoopRenderBatchScope = new NoopDisposable();
    private readonly Func<MiragePlatformView> _platformViewFactory;
    private IMirageRuntimeEventTarget? _registeredRuntimeEventTarget;
    private MirageGestureRecognizerManager? _gestureRecognizerManager;

    protected MirageViewHandler(
        ComponentKind kind,
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : this(() => MiragePlatformView.CreateControl(kind), mapper, commandMapper)
    {
    }

    protected MirageViewHandler(
        Func<MiragePlatformView> platformViewFactory,
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(mapper, commandMapper ?? MirageViewMapper.CommonViewCommandMapper)
    {
        _platformViewFactory = platformViewFactory ?? throw new ArgumentNullException(nameof(platformViewFactory));
    }

    public string? DefaultDisplayStyle { get; protected set; }

    MiragePlatformView IMiragePlatformHandler.MiragePlatformView => PlatformView;

    protected IMirageRenderCoordinator? Coordinator =>
        MauiContext?.Services.GetService(typeof(IMirageRenderCoordinator)) as IMirageRenderCoordinator;

    protected override MiragePlatformView CreatePlatformView() => _platformViewFactory();

    protected bool TryGetPlatformView(out MiragePlatformView platformView)
    {
        try
        {
            platformView = PlatformView;
            return true;
        }
        catch (InvalidOperationException)
        {
            platformView = null!;
            return false;
        }
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        _gestureRecognizerManager = VirtualView is View view
            ? new MirageGestureRecognizerManager(view, platformView, RequestRender)
            : null;
        _registeredRuntimeEventTarget = MirageCompositeRuntimeEventTarget.Create(
            this as IMirageRuntimeEventTarget,
            _gestureRecognizerManager);
        Coordinator?.Register(platformView, _registeredRuntimeEventTarget);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        _gestureRecognizerManager?.Dispose();
        _gestureRecognizerManager = null;
        _registeredRuntimeEventTarget = null;
        Coordinator?.Unregister(platformView);
        base.DisconnectHandler(platformView);
    }

    public void RequestRender()
    {
        Coordinator?.RequestRender();
    }

    protected IDisposable BeginRenderBatch()
    {
        return Coordinator?.BeginRenderBatch() ?? NoopRenderBatchScope;
    }

    protected bool TryGetChild(IView view, out IElementHandler childHandler, out MiragePlatformView childPlatformView)
    {
        if (MauiContext is null)
        {
            childHandler = null!;
            childPlatformView = null!;
            return false;
        }

        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var startTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        try
        {
            childHandler = view.ToHandler(MauiContext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create handler for child view type '{view.GetType().FullName}' under parent handler '{GetType().FullName}'.",
                ex);
        }

        if (diagnosticsEnabled)
        {
            var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
            MirageDiagnostics.Log(
                "handler.child",
                $"parentHandler={GetType().Name} view={view.GetType().Name} handler={childHandler.GetType().Name} toHandlerMs={elapsedMs:0.###}");
        }

        if (childHandler.PlatformView is MiragePlatformView miragePlatformView)
        {
            childPlatformView = miragePlatformView;
            return true;
        }

        childHandler = null!;
        childPlatformView = null!;
        return false;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
