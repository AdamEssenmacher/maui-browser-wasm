using Mirage.Core.Semantics;

namespace Mirage.Core.Providers;

public interface IMirageComponentProvider
{
    ProviderCapabilities Capabilities { get; }

    ControlComponentDescriptor ResolveControl(ComponentKind kind);

    // Generic web-node realization is validation/capability-oriented and must
    // preserve the requested element identity.
    WebComponentDescriptor ResolveWebNode(string elementName);
}
