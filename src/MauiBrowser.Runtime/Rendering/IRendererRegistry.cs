namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Provides lookup and registration for view renderer factories.
/// </summary>
public interface IRendererRegistry
{
    /// <summary>
    /// Registers a renderer factory for a virtual view type.
    /// </summary>
    /// <typeparam name="TVirtualView">The virtual view type associated with the factory.</typeparam>
    /// <param name="factory">The renderer factory to register.</param>
    /// <param name="priority">Priority used when multiple factories match. Higher values win.</param>
    void Register<TVirtualView>(IRendererFactory factory, int priority = 0);

    /// <summary>
    /// Attempts to resolve a renderer factory for the provided virtual view instance.
    /// </summary>
    /// <param name="virtualView">The virtual view instance to render.</param>
    /// <param name="factory">The resolved renderer factory when found.</param>
    /// <returns><c>true</c> when a matching renderer is found; otherwise, <c>false</c>.</returns>
    bool TryResolve(object virtualView, out IRendererFactory factory);
}
