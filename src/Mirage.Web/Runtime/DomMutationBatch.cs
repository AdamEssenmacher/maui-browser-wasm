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
        private readonly Dictionary<MutationKey, PendingMutation> _mutations = new();
        private int _nextSequence;

        public void SetText(string? text)
        {
            SetMutation(MutationKey.Text, DomMutationOperation.Text(text));
        }

        public void SetAttribute(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            SetMutation(MutationKey.Attribute(name), DomMutationOperation.Attribute(name, value));
        }

        public void SetStyle(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            SetMutation(MutationKey.Style(name), DomMutationOperation.Style(name, value));
        }

        public void AddClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            SetMutation(MutationKey.Class(className), DomMutationOperation.ClassAdd(className));
        }

        public void RemoveClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            SetMutation(MutationKey.Class(className), DomMutationOperation.ClassRemove(className));
        }

        public void SetProperty(string name, DomPropertyMutation mutation)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            SetMutation(MutationKey.Property(name), DomMutationOperation.Property(mutation));
        }

        public DomElementMutation ToMutation(long handle)
        {
            var operations = _mutations
                .Values
                .OrderBy(static mutation => mutation.Sequence)
                .Select(static mutation => mutation.Operation)
                .ToArray();

            return new DomElementMutation(handle, operations);
        }

        private void SetMutation(MutationKey key, DomMutationOperation operation)
        {
            _mutations[key] = new PendingMutation(_nextSequence++, operation);
        }
    }

    private readonly record struct MutationKey(string Kind, string Name)
    {
        public static MutationKey Text => new("text", string.Empty);

        public static MutationKey Attribute(string name) => new("attribute", name);

        public static MutationKey Style(string name) => new("style", name);

        public static MutationKey Property(string name) => new("property", name);

        public static MutationKey Class(string name) => new("class", name);
    }

    private readonly record struct PendingMutation(int Sequence, DomMutationOperation Operation);
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
        WriteOperationArray(writer, "operations", element.Operations);
        writer.WriteEndObject();
    }

    private static void WriteOperationArray(Utf8JsonWriter writer, string propertyName, IReadOnlyList<DomMutationOperation> operations)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();

        foreach (var operation in operations)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", operation.Kind);

            if (!string.IsNullOrWhiteSpace(operation.Name))
            {
                writer.WriteString("name", operation.Name);
            }

            switch (operation.Kind)
            {
                case "text":
                case "attribute":
                case "style":
                case "property-string":
                case "property-json":
                case "property-input-value":
                    WriteNullableString(writer, "stringValue", operation.StringValue);
                    break;
                case "property-bool":
                case "property-input-checked":
                    writer.WriteBoolean("boolValue", operation.BoolValue);
                    break;
                case "property-number":
                    writer.WriteNumber("numberValue", operation.NumberValue);
                    break;
            }

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
    IReadOnlyList<DomMutationOperation> Operations)
{
    public int CountMutations()
    {
        return Operations.Count;
    }
}

internal sealed record DomMutationOperation(
    string Kind,
    string? Name = null,
    string? StringValue = null,
    double NumberValue = 0,
    bool BoolValue = false)
{
    public static DomMutationOperation Text(string? value) => new("text", StringValue: value);

    public static DomMutationOperation Attribute(string name, string? value) => new("attribute", name, value);

    public static DomMutationOperation Style(string name, string? value) => new("style", name, value);

    public static DomMutationOperation ClassAdd(string name) => new("class-add", name);

    public static DomMutationOperation ClassRemove(string name) => new("class-remove", name);

    public static DomMutationOperation Property(DomPropertyMutation mutation)
    {
        return mutation.Kind switch
        {
            "string" => new("property-string", mutation.Name, mutation.StringValue),
            "bool" => new("property-bool", mutation.Name, BoolValue: mutation.BoolValue),
            "number" => new("property-number", mutation.Name, NumberValue: mutation.NumberValue),
            "json" => new("property-json", mutation.Name, mutation.StringValue),
            "null" => new("property-null", mutation.Name),
            "remove" => new("property-remove", mutation.Name),
            "input-value" => new("property-input-value", mutation.Name, mutation.StringValue),
            "input-checked" => new("property-input-checked", mutation.Name, BoolValue: mutation.BoolValue),
            _ => new("property-null", mutation.Name)
        };
    }
}

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
