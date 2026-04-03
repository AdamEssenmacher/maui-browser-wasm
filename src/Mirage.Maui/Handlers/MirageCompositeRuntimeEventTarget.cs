namespace Mirage.Maui.Handlers;

internal sealed class MirageCompositeRuntimeEventTarget : IMirageRuntimeEventTarget
{
    private readonly IMirageRuntimeEventTarget[] _targets;

    private MirageCompositeRuntimeEventTarget(IMirageRuntimeEventTarget[] targets)
    {
        _targets = targets;
    }

    public static IMirageRuntimeEventTarget? Create(params IMirageRuntimeEventTarget?[] targets)
    {
        var realized = targets
            .Where(static target => target is not null)
            .Cast<IMirageRuntimeEventTarget>()
            .ToArray();

        return realized.Length switch
        {
            0 => null,
            1 => realized[0],
            _ => new MirageCompositeRuntimeEventTarget(realized)
        };
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        foreach (var target in _targets)
        {
            target.OnRuntimeEvent(eventName, payload);
        }
    }
}
