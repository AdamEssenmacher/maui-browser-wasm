using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

public abstract class MirageViewHandler<TVirtualView> : ViewHandler<TVirtualView, MiragePlatformView>, IMiragePlatformHandler
    where TVirtualView : class, IView
{
    private readonly Func<MiragePlatformView> _platformViewFactory;

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
        var eventTarget = this as IMirageRuntimeEventTarget;
        Coordinator?.Register(platformView, eventTarget);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        Coordinator?.Unregister(platformView);
        base.DisconnectHandler(platformView);
    }

    public void RequestRender()
    {
        Coordinator?.RequestRender();
    }

    protected bool TryGetChild(IView view, out IElementHandler childHandler, out MiragePlatformView childPlatformView)
    {
        if (MauiContext is null)
        {
            childHandler = null!;
            childPlatformView = null!;
            return false;
        }

        childHandler = view.ToHandler(MauiContext);
        if (childHandler.PlatformView is MiragePlatformView miragePlatformView)
        {
            childPlatformView = miragePlatformView;
            return true;
        }

        childHandler = null!;
        childPlatformView = null!;
        return false;
    }
}
