namespace Mirage.Core.Providers;

public enum PropertyTarget
{
    TextContent,
    Attribute,
    InputValue,
    InputChecked,
    Style
}

public enum PropertyElementTarget
{
    Self,
    ValueElement,
    SecondaryElement
}

public sealed record ProviderPropertyDescriptor(
    PropertyTarget Target,
    string? Name = null,
    PropertyElementTarget ElementTarget = PropertyElementTarget.Self);
