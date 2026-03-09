using Mirage.Core.Semantics;

namespace Mirage.Core.Providers;

public interface IMirageComponentProvider
{
    ProviderCapabilities Capabilities { get; }

    ControlComponentDescriptor ResolveControl(ComponentKind kind);

    WebComponentDescriptor ResolveWebNode(string elementName);
}
