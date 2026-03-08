using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mirage.Core.Semantics;

public enum MirageValueKind
{
    Null,
    String,
    Bool,
    Number,
    Json
}

public readonly struct MirageValue : IEquatable<MirageValue>
{
    private readonly string? _stringValue;
    private readonly bool _boolValue;
    private readonly double _numberValue;
    private readonly JsonNode? _jsonValue;

    private MirageValue(
        MirageValueKind kind,
        string? stringValue,
        bool boolValue,
        double numberValue,
        JsonNode? jsonValue)
    {
        Kind = kind;
        _stringValue = stringValue;
        _boolValue = boolValue;
        _numberValue = numberValue;
        _jsonValue = jsonValue;
    }

    public MirageValueKind Kind { get; }

    public static MirageValue Null() => new(MirageValueKind.Null, null, boolValue: false, numberValue: 0, jsonValue: null);

    public static MirageValue String(string? value) => value is null
        ? Null()
        : new MirageValue(MirageValueKind.String, value, boolValue: false, numberValue: 0, jsonValue: null);

    public static MirageValue Bool(bool value) => new(MirageValueKind.Bool, null, value, numberValue: 0, jsonValue: null);

    public static MirageValue Number(double value)
    {
        var normalized = double.IsFinite(value) ? value : 0;
        return new MirageValue(MirageValueKind.Number, null, boolValue: false, normalized, jsonValue: null);
    }

    public static MirageValue Json(JsonNode? value)
    {
        if (value is null)
        {
            return Null();
        }

        if (value is not JsonObject && value is not JsonArray)
        {
            throw new ArgumentException("MirageValue.Json supports JsonObject or JsonArray payloads only.", nameof(value));
        }

        return new MirageValue(MirageValueKind.Json, null, boolValue: false, numberValue: 0, value.DeepClone());
    }

    public string? GetStringOrNull() => Kind switch
    {
        MirageValueKind.Null => null,
        MirageValueKind.String => _stringValue,
        MirageValueKind.Bool => _boolValue ? "true" : "false",
        MirageValueKind.Number => string.Create(CultureInfo.InvariantCulture, $"{_numberValue:0.###############}"),
        MirageValueKind.Json => ToJsonString(),
        _ => null
    };

    public bool TryGetString(out string? value)
    {
        if (Kind == MirageValueKind.String)
        {
            value = _stringValue;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetBool(out bool value)
    {
        if (Kind == MirageValueKind.Bool)
        {
            value = _boolValue;
            return true;
        }

        value = false;
        return false;
    }

    public bool TryGetNumber(out double value)
    {
        if (Kind == MirageValueKind.Number)
        {
            value = _numberValue;
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetJson(out JsonNode? value)
    {
        if (Kind == MirageValueKind.Json)
        {
            value = _jsonValue?.DeepClone();
            return true;
        }

        value = null;
        return false;
    }

    public string? ToJsonString()
    {
        return Kind == MirageValueKind.Json
            ? _jsonValue?.ToJsonString()
            : null;
    }

    public override string ToString() => GetStringOrNull() ?? string.Empty;

    public bool Equals(MirageValue other)
    {
        if (Kind != other.Kind)
        {
            return false;
        }

        return Kind switch
        {
            MirageValueKind.Null => true,
            MirageValueKind.String => string.Equals(_stringValue, other._stringValue, StringComparison.Ordinal),
            MirageValueKind.Bool => _boolValue == other._boolValue,
            MirageValueKind.Number => _numberValue.Equals(other._numberValue),
            MirageValueKind.Json => JsonNode.DeepEquals(_jsonValue, other._jsonValue),
            _ => false
        };
    }

    public override bool Equals(object? obj) => obj is MirageValue other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add((int)Kind);

        switch (Kind)
        {
            case MirageValueKind.String:
                hash.Add(_stringValue, StringComparer.Ordinal);
                break;
            case MirageValueKind.Bool:
                hash.Add(_boolValue);
                break;
            case MirageValueKind.Number:
                hash.Add(_numberValue);
                break;
            case MirageValueKind.Json:
                hash.Add(ToJsonString(), StringComparer.Ordinal);
                break;
        }

        return hash.ToHashCode();
    }

    public static implicit operator MirageValue(string? value) => String(value);

    public static implicit operator MirageValue(bool value) => Bool(value);

    public static implicit operator MirageValue(int value) => Number(value);

    public static implicit operator MirageValue(double value) => Number(value);

    public static explicit operator JsonNode?(MirageValue value)
    {
        if (!value.TryGetJson(out var json))
        {
            throw new InvalidOperationException($"MirageValue of kind '{value.Kind}' is not Json.");
        }

        return json;
    }
}
