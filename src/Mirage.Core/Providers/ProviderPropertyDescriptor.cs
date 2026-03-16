namespace Mirage.Core.Providers;

public enum PropertyTarget
{
    TextContent,
    Attribute,
    InputValue,
    InputChecked
}

public enum PropertyElementTarget
{
    Self,
    ValueElement
}

public sealed record ProviderPropertyDescriptor(
    PropertyTarget Target,
    string? Name = null,
    PropertyElementTarget ElementTarget = PropertyElementTarget.Self);
