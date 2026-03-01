using System;
using System.Collections.Generic;
using System.Threading;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Default in-memory renderer registry with priority-based resolution.
/// </summary>
public sealed class RendererRegistry : IRendererRegistry
{
    private readonly object _syncRoot = new();
    private readonly List<Registration> _registrations = new();
    private long _registrationOrder;

    /// <inheritdoc />
    public void Register<TVirtualView>(IRendererFactory factory, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var registration = new Registration(
            typeof(TVirtualView),
            factory,
            priority,
            Interlocked.Increment(ref _registrationOrder));

        lock (_syncRoot)
        {
            _registrations.Add(registration);
        }
    }

    /// <inheritdoc />
    public bool TryResolve(object virtualView, out IRendererFactory factory)
    {
        ArgumentNullException.ThrowIfNull(virtualView);

        var viewType = virtualView.GetType();
        Registration? match = null;

        lock (_syncRoot)
        {
            foreach (var registration in _registrations)
            {
                if (!registration.VirtualViewType.IsAssignableFrom(viewType))
                {
                    continue;
                }

                if (!registration.Factory.CanRender(virtualView))
                {
                    continue;
                }

                if (match is null
                    || registration.Priority > match.Priority
                    || (registration.Priority == match.Priority && registration.Order > match.Order))
                {
                    match = registration;
                }
            }
        }

        if (match is null)
        {
            factory = null!;
            return false;
        }

        factory = match.Factory;
        return true;
    }

    private sealed record Registration(Type VirtualViewType, IRendererFactory Factory, int Priority, long Order);
}
