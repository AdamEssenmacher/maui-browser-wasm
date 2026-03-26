using System.Globalization;
using Mirage.Web.Dom;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Mirage.Maui.Essentials;

internal sealed class MirageBrowserPreferences : IPreferences
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(float),
        typeof(DateTime),
        typeof(DateTimeOffset)
    ];

    private readonly IAppInfo _appInfo;

    public MirageBrowserPreferences(IAppInfo appInfo)
    {
        _appInfo = appInfo ?? throw new ArgumentNullException(nameof(appInfo));
    }

    public bool ContainsKey(string key, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return DomElement.ContainsLocalStorageKey(BuildStorageKey(key, sharedName));
    }

    public void Remove(string key, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        DomElement.RemoveLocalStorageItem(BuildStorageKey(key, sharedName));
    }

    public void Clear(string? sharedName = null)
    {
        DomElement.ClearLocalStorageByPrefix(BuildScopePrefix(sharedName));
    }

    public void Set<T>(string key, T value, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var type = typeof(T);
        EnsureSupportedType(type);

        var envelope = CreateEnvelope(type, value);
        DomElement.SetLocalStorageItem(BuildStorageKey(key, sharedName), SerializeEnvelope(envelope));
    }

    public T Get<T>(string key, T defaultValue, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var type = typeof(T);
        EnsureSupportedType(type);

        var payload = DomElement.GetLocalStorageItem(BuildStorageKey(key, sharedName));
        if (string.IsNullOrWhiteSpace(payload))
        {
            return defaultValue;
        }

        if (!TryDeserializeEnvelope(payload, out var envelope)
            || !TryReadEnvelope(type, envelope, out var result))
        {
            return defaultValue;
        }

        return (T)result!;
    }

    private string BuildScopePrefix(string? sharedName)
    {
        var packageName = string.IsNullOrWhiteSpace(_appInfo.PackageName) ? "Mirage" : _appInfo.PackageName.Trim();
        var scope = string.IsNullOrWhiteSpace(sharedName) ? "__default" : sharedName.Trim();
        return $"mirage.preferences::{Uri.EscapeDataString(packageName)}::{Uri.EscapeDataString(scope)}::";
    }

    private string BuildStorageKey(string key, string? sharedName) =>
        BuildScopePrefix(sharedName) + Uri.EscapeDataString(key);

    private static void EnsureSupportedType(Type type)
    {
        if (!SupportedTypes.Contains(type))
        {
            throw new NotSupportedException($"Preferences using '{type}' type is not supported");
        }
    }

    private static PreferenceEnvelope CreateEnvelope<T>(Type type, T value)
    {
        if (type == typeof(string))
        {
            return new PreferenceEnvelope("string", value as string, value is not null);
        }

        if (type == typeof(bool))
        {
            return new PreferenceEnvelope("bool", Convert.ToString(value, CultureInfo.InvariantCulture), true);
        }

        if (type == typeof(int))
        {
            return new PreferenceEnvelope("int", Convert.ToString(value, CultureInfo.InvariantCulture), true);
        }

        if (type == typeof(long))
        {
            return new PreferenceEnvelope("long", Convert.ToString(value, CultureInfo.InvariantCulture), true);
        }

        if (type == typeof(double))
        {
            return new PreferenceEnvelope("double", ((double)(object)value!).ToString("R", CultureInfo.InvariantCulture), true);
        }

        if (type == typeof(float))
        {
            return new PreferenceEnvelope("float", ((float)(object)value!).ToString("R", CultureInfo.InvariantCulture), true);
        }

        if (type == typeof(DateTime))
        {
            return new PreferenceEnvelope("datetime", ((DateTime)(object)value!).ToString("O", CultureInfo.InvariantCulture), true);
        }

        if (type == typeof(DateTimeOffset))
        {
            return new PreferenceEnvelope("datetimeoffset", ((DateTimeOffset)(object)value!).ToString("O", CultureInfo.InvariantCulture), true);
        }

        throw new NotSupportedException($"Preferences using '{type}' type is not supported");
    }

    private static bool TryReadEnvelope(Type type, PreferenceEnvelope envelope, out object? value)
    {
        value = null;

        if (type == typeof(string))
        {
            if (!string.Equals(envelope.Kind, "string", StringComparison.Ordinal))
            {
                return false;
            }

            value = envelope.HasValue ? envelope.Value : null;
            return true;
        }

        if (string.IsNullOrWhiteSpace(envelope.Value))
        {
            return false;
        }

        if (type == typeof(bool)
            && string.Equals(envelope.Kind, "bool", StringComparison.Ordinal)
            && bool.TryParse(envelope.Value, out var boolValue))
        {
            value = boolValue;
            return true;
        }

        if (type == typeof(int)
            && string.Equals(envelope.Kind, "int", StringComparison.Ordinal)
            && int.TryParse(envelope.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            value = intValue;
            return true;
        }

        if (type == typeof(long)
            && string.Equals(envelope.Kind, "long", StringComparison.Ordinal)
            && long.TryParse(envelope.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            value = longValue;
            return true;
        }

        if (type == typeof(double)
            && string.Equals(envelope.Kind, "double", StringComparison.Ordinal)
            && double.TryParse(envelope.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
        {
            value = doubleValue;
            return true;
        }

        if (type == typeof(float)
            && string.Equals(envelope.Kind, "float", StringComparison.Ordinal)
            && float.TryParse(envelope.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatValue))
        {
            value = floatValue;
            return true;
        }

        if (type == typeof(DateTime)
            && string.Equals(envelope.Kind, "datetime", StringComparison.Ordinal)
            && DateTime.TryParseExact(envelope.Value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeValue))
        {
            value = dateTimeValue;
            return true;
        }

        if (type == typeof(DateTimeOffset)
            && string.Equals(envelope.Kind, "datetimeoffset", StringComparison.Ordinal)
            && DateTimeOffset.TryParseExact(envelope.Value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffsetValue))
        {
            value = dateTimeOffsetValue;
            return true;
        }

        return false;
    }

    private static string SerializeEnvelope(PreferenceEnvelope envelope)
    {
        var escapedValue = Uri.EscapeDataString(envelope.Value ?? string.Empty);
        return $"{envelope.Kind}|{(envelope.HasValue ? "1" : "0")}|{escapedValue}";
    }

    private static bool TryDeserializeEnvelope(string payload, out PreferenceEnvelope envelope)
    {
        envelope = default!;

        var firstSeparator = payload.IndexOf('|', StringComparison.Ordinal);
        if (firstSeparator <= 0)
        {
            return false;
        }

        var secondSeparator = payload.IndexOf('|', firstSeparator + 1);
        if (secondSeparator <= firstSeparator)
        {
            return false;
        }

        var kind = payload[..firstSeparator];
        var hasValueToken = payload[(firstSeparator + 1)..secondSeparator];
        var encodedValue = payload[(secondSeparator + 1)..];
        if (kind.Length == 0)
        {
            return false;
        }

        var hasValue = hasValueToken switch
        {
            "1" => true,
            "0" => false,
            _ => false
        };

        var value = Uri.UnescapeDataString(encodedValue);
        envelope = new PreferenceEnvelope(kind, hasValue ? value : null, hasValue);
        return true;
    }

    private sealed record PreferenceEnvelope(string Kind, string? Value, bool HasValue);
}
