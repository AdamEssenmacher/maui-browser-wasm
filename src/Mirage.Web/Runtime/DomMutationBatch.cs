using Mirage.Core.Semantics;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Mirage.Web.Runtime;

internal sealed class DomMutationBatch
{
    private readonly Dictionary<long, PendingElementMutation> _pendingByHandle = new();
    private readonly List<long> _handleOrder = [];

    public bool HasPending => _handleOrder.Count > 0;

    public void QueueText(nint handle, string? text)
    {
        GetOrCreate(handle).SetText(text);
    }

    public void QueueAttribute(nint handle, string name, string? value)
    {
        GetOrCreate(handle).SetAttribute(name, value);
    }

    public void QueueStyle(nint handle, string name, string? value)
    {
        GetOrCreate(handle).SetStyle(name, value);
    }

    public void QueueAddClass(nint handle, string className)
    {
        GetOrCreate(handle).AddClass(className);
    }

    public void QueueRemoveClass(nint handle, string className)
    {
        GetOrCreate(handle).RemoveClass(className);
    }

    public void QueueProperty(nint handle, string name, MirageValue value)
    {
        GetOrCreate(handle).SetProperty(name, DomPropertyMutation.FromValue(name, value));
    }

    public void QueueRemoveProperty(nint handle, string name)
    {
        GetOrCreate(handle).SetProperty(name, DomPropertyMutation.Remove(name));
    }

    public void QueueInputValue(nint handle, string? value)
    {
        GetOrCreate(handle).SetProperty("value", DomPropertyMutation.InputValue(value));
    }

    public void QueueInputChecked(nint handle, bool value)
    {
        GetOrCreate(handle).SetProperty("checked", DomPropertyMutation.InputChecked(value));
    }

    public DomMutationBatchPayload Drain()
    {
        var elements = new List<DomElementMutation>(_handleOrder.Count);
        var mutationCount = 0;

        foreach (var handle in _handleOrder)
        {
            var mutation = _pendingByHandle[handle].ToMutation(handle);
            mutationCount += mutation.CountMutations();
            elements.Add(mutation);
        }

        _pendingByHandle.Clear();
        _handleOrder.Clear();

        return new DomMutationBatchPayload(elements, mutationCount);
    }

    private PendingElementMutation GetOrCreate(nint handle)
    {
        var normalizedHandle = checked((long)handle);
        if (_pendingByHandle.TryGetValue(normalizedHandle, out var existing))
        {
            return existing;
        }

        var created = new PendingElementMutation();
        _pendingByHandle[normalizedHandle] = created;
        _handleOrder.Add(normalizedHandle);
        return created;
    }

    private sealed class PendingElementMutation
    {
        private readonly Dictionary<string, string?> _attributes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _styles = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DomPropertyMutation> _properties = new(StringComparer.Ordinal);
        private readonly HashSet<string> _classesToAdd = new(StringComparer.Ordinal);
        private readonly HashSet<string> _classesToRemove = new(StringComparer.Ordinal);

        private bool _hasText;
        private string? _text;

        public void SetText(string? text)
        {
            _hasText = true;
            _text = text;
        }

        public void SetAttribute(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            _attributes[name] = value;
        }

        public void SetStyle(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            _styles[name] = value;
        }

        public void AddClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            _classesToRemove.Remove(className);
            _classesToAdd.Add(className);
        }

        public void RemoveClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            _classesToAdd.Remove(className);
            _classesToRemove.Add(className);
        }

        public void SetProperty(string name, DomPropertyMutation mutation)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            _properties[name] = mutation;
        }

        public DomElementMutation ToMutation(long handle)
        {
            return new DomElementMutation(
                handle,
                _hasText,
                _text,
                _attributes.Count == 0 ? [] : _attributes.Select(static pair => new DomStringMutation(pair.Key, pair.Value)).ToArray(),
                _styles.Count == 0 ? [] : _styles.Select(static pair => new DomStringMutation(pair.Key, pair.Value)).ToArray(),
                _properties.Count == 0 ? [] : _properties.Values.ToArray(),
                _classesToAdd.Count == 0 ? [] : _classesToAdd.Select(static name => new DomClassMutation(name, true)).ToArray(),
                _classesToRemove.Count == 0 ? [] : _classesToRemove.Select(static name => new DomClassMutation(name, false)).ToArray());
        }
    }
}

internal sealed record DomMutationBatchPayload(IReadOnlyList<DomElementMutation> Elements, int MutationCount)
{
    public string ToJson()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WritePropertyName("elements");
        writer.WriteStartArray();

        foreach (var element in Elements)
        {
            WriteElement(writer, element);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteElement(Utf8JsonWriter writer, DomElementMutation element)
    {
        writer.WriteStartObject();
        writer.WriteNumber("handle", element.Handle);
        writer.WriteBoolean("hasText", element.HasText);

        if (element.HasText)
        {
            WriteNullableString(writer, "text", element.Text);
        }

        WriteStringMutationArray(writer, "attributes", element.Attributes);
        WriteStringMutationArray(writer, "styles", element.Styles);
        WritePropertyMutationArray(writer, "properties", element.Properties);
        WriteClassMutationArray(writer, "classesToAdd", element.ClassesToAdd);
        WriteClassMutationArray(writer, "classesToRemove", element.ClassesToRemove);
        writer.WriteEndObject();
    }

    private static void WriteStringMutationArray(Utf8JsonWriter writer, string propertyName, IReadOnlyList<DomStringMutation> values)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();

        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("name", value.Name);
            WriteNullableString(writer, "value", value.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WritePropertyMutationArray(Utf8JsonWriter writer, string propertyName, IReadOnlyList<DomPropertyMutation> values)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();

        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("name", value.Name);
            writer.WriteString("kind", value.Kind);

            switch (value.Kind)
            {
                case "string":
                case "json":
                case "input-value":
                    WriteNullableString(writer, "stringValue", value.StringValue);
                    break;
                case "bool":
                case "input-checked":
                    writer.WriteBoolean("boolValue", value.BoolValue);
                    break;
                case "number":
                    writer.WriteNumber("numberValue", value.NumberValue);
                    break;
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteClassMutationArray(Utf8JsonWriter writer, string propertyName, IReadOnlyList<DomClassMutation> values)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();

        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("name", value.Name);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteString(propertyName, value);
    }
}

internal sealed record DomElementMutation(
    long Handle,
    bool HasText,
    string? Text,
    IReadOnlyList<DomStringMutation> Attributes,
    IReadOnlyList<DomStringMutation> Styles,
    IReadOnlyList<DomPropertyMutation> Properties,
    IReadOnlyList<DomClassMutation> ClassesToAdd,
    IReadOnlyList<DomClassMutation> ClassesToRemove)
{
    public int CountMutations()
    {
        return (HasText ? 1 : 0)
               + Attributes.Count
               + Styles.Count
               + Properties.Count
               + ClassesToAdd.Count
               + ClassesToRemove.Count;
    }
}

internal sealed record DomStringMutation(string Name, string? Value);

internal sealed record DomClassMutation(string Name, bool Add);

internal sealed record DomPropertyMutation(
    string Name,
    string Kind,
    string? StringValue = null,
    double NumberValue = 0,
    bool BoolValue = false)
{
    public static DomPropertyMutation FromValue(string name, MirageValue value)
    {
        return value.Kind switch
        {
            MirageValueKind.String => new DomPropertyMutation(name, "string", value.TryGetString(out var stringValue) ? stringValue : null),
            MirageValueKind.Bool => new DomPropertyMutation(name, "bool", BoolValue: value.TryGetBool(out var boolValue) && boolValue),
            MirageValueKind.Number => new DomPropertyMutation(name, "number", NumberValue: value.TryGetNumber(out var numberValue) ? numberValue : 0),
            MirageValueKind.Json => new DomPropertyMutation(name, "json", value.ToJsonString()),
            _ => new DomPropertyMutation(name, "null")
        };
    }

    public static DomPropertyMutation Remove(string name) => new(name, "remove");

    public static DomPropertyMutation InputValue(string? value) => new("value", "input-value", value);

    public static DomPropertyMutation InputChecked(bool value) => new("checked", "input-checked", BoolValue: value);
}
