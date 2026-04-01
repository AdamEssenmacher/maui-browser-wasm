namespace Mirage.Core.Providers;

public enum EventTarget
{
    Self,
    ValueElement,
    SecondaryElement
}

public enum EventPayloadKind
{
    None,
    InputValue,
    InputChecked,
    SourceAttribute,
    Key,
    Pointer
}

public sealed record ProviderEventDescriptor(
    string DomEventName,
    EventTarget Target = EventTarget.Self,
    EventPayloadKind PayloadKind = EventPayloadKind.None);
