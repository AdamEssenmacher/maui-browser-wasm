using System.Text.Json.Nodes;
using Mirage.Core.Semantics;
using Xunit;

namespace Mirage.Core.Tests;

public sealed class MirageValueTests
{
    [Fact]
    public void MirageValue_StringBoolNumberRoundTrip()
    {
        var s = MirageValue.String("abc");
        var b = MirageValue.Bool(true);
        var n = MirageValue.Number(12.5);

        Assert.Equal(MirageValueKind.String, s.Kind);
        Assert.Equal("abc", s.GetStringOrNull());

        Assert.True(b.TryGetBool(out var boolValue));
        Assert.True(boolValue);

        Assert.True(n.TryGetNumber(out var numberValue));
        Assert.Equal(12.5, numberValue);
    }

    [Fact]
    public void MirageValue_Json_UsesStructuredPayload()
    {
        var json = JsonNode.Parse("""{"name":"mirage","arr":[1,2,3]}""");
        var value = MirageValue.Json(json);

        Assert.Equal(MirageValueKind.Json, value.Kind);
        Assert.NotNull(value.ToJsonString());
        Assert.Contains("\"name\":\"mirage\"", value.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MirageValue_Json_DeepCloneGuarantee()
    {
        var source = JsonNode.Parse("""{"name":"original","items":[1]}""");
        var value = MirageValue.Json(source);

        source!["name"] = "changed";
        source["items"]![0] = 999;

        var serialized = value.ToJsonString();
        Assert.Contains("\"name\":\"original\"", serialized, StringComparison.Ordinal);
        Assert.Contains("\"items\":[1]", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void MirageValue_Json_RejectsPrimitiveNodes()
    {
        var primitive = JsonNode.Parse("42");
        var exception = Assert.Throws<ArgumentException>(() => MirageValue.Json(primitive));
        Assert.Contains("JsonObject or JsonArray", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MirageValue_Equality_UsesValueSemantics()
    {
        var left = MirageValue.Json(JsonNode.Parse("""{"a":1,"b":[2]}"""));
        var right = MirageValue.Json(JsonNode.Parse("""{"a":1,"b":[2]}"""));

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void MirageValue_Null_ProducesNullString()
    {
        var value = MirageValue.Null();
        Assert.Equal(MirageValueKind.Null, value.Kind);
        Assert.Null(value.GetStringOrNull());
    }
}
