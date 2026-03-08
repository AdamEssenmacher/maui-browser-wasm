using Mirage.Core.Semantics;

namespace Mirage.Core.Providers;

public interface IMirageComponentProvider
{
    ProviderCapabilities Capabilities { get; }

    TypedComponentDescriptor ResolveTyped(ComponentKind kind);

    ElementComponentDescriptor ResolveElement(string elementName);
}
