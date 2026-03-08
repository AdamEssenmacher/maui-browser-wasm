namespace Mirage.Core.Providers;

public enum EventTarget
{
    Self,
    ValueElement
}

public enum EventPayloadKind
{
    None,
    InputValue
}

public sealed record ProviderEventDescriptor(
    string DomEventName,
    EventTarget Target = EventTarget.Self,
    EventPayloadKind PayloadKind = EventPayloadKind.None);
