using Mirage.Core.Semantics;
using Mirage.Web.Runtime;
using System.Text.Json;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class DomMutationBatchTests
{
    [Fact]
    public void Drain_CoalescesLatestMutationsPerHandle()
    {
        var batch = new DomMutationBatch();

        batch.QueueStyle((nint)12, "color", "red");
        batch.QueueStyle((nint)12, "color", "blue");
        batch.QueueText((nint)12, "before");
        batch.QueueText((nint)12, "after");
        batch.QueueAttribute((nint)12, "role", "button");
        batch.QueueProperty((nint)12, "value", MirageValue.String("first"));
        batch.QueueProperty((nint)12, "value", MirageValue.String("second"));
        batch.QueueAddClass((nint)12, "active");
        batch.QueueRemoveClass((nint)12, "active");
        batch.QueueAddClass((nint)12, "active");

        var payload = batch.Drain();

        Assert.False(batch.HasPending);
        Assert.Single(payload.Elements);
        Assert.Equal(5, payload.MutationCount);

        var element = payload.Elements[0];
        Assert.Equal(12L, element.Handle);
        Assert.True(element.HasText);
        Assert.Equal("after", element.Text);

        var style = Assert.Single(element.Styles);
        Assert.Equal("color", style.Name);
        Assert.Equal("blue", style.Value);

        var attribute = Assert.Single(element.Attributes);
        Assert.Equal("role", attribute.Name);
        Assert.Equal("button", attribute.Value);

        var property = Assert.Single(element.Properties);
        Assert.Equal("value", property.Name);
        Assert.Equal("string", property.Kind);
        Assert.Equal("second", property.StringValue);

        var addedClass = Assert.Single(element.ClassesToAdd);
        Assert.Equal("active", addedClass.Name);
        Assert.Empty(element.ClassesToRemove);
    }

    [Fact]
    public void Drain_PreservesFirstSeenHandleOrder()
    {
        var batch = new DomMutationBatch();

        batch.QueueStyle((nint)42, "width", "10px");
        batch.QueueStyle((nint)7, "height", "20px");
        batch.QueueStyle((nint)42, "opacity", "1");

        var payload = batch.Drain();

        Assert.Equal(3, payload.MutationCount);
        Assert.Collection(
            payload.Elements,
            element => Assert.Equal(42L, element.Handle),
            element => Assert.Equal(7L, element.Handle));
    }

    [Fact]
    public void Drain_PreservesRemoveAndInputSpecificPropertySemantics()
    {
        var batch = new DomMutationBatch();

        batch.QueueRemoveProperty((nint)1, "dataset");
        batch.QueueInputValue((nint)1, null);
        batch.QueueInputChecked((nint)1, value: true);

        var payload = batch.Drain();
        var properties = Assert.Single(payload.Elements).Properties.ToDictionary(property => property.Name, StringComparer.Ordinal);

        Assert.Equal("remove", properties["dataset"].Kind);
        Assert.Equal("input-value", properties["value"].Kind);
        Assert.Null(properties["value"].StringValue);
        Assert.Equal("input-checked", properties["checked"].Kind);
        Assert.True(properties["checked"].BoolValue);
    }

    [Fact]
    public void ToJson_WritesExpectedWireShape()
    {
        var batch = new DomMutationBatch();
        batch.QueueText((nint)9, "hello");
        batch.QueueAttribute((nint)9, "role", "button");
        batch.QueueStyle((nint)9, "color", "red");
        batch.QueueProperty((nint)9, "count", MirageValue.Number(3));
        batch.QueueAddClass((nint)9, "active");

        var json = batch.Drain().ToJson();
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        var element = Assert.Single(root.GetProperty("elements").EnumerateArray());
        Assert.Equal(9, element.GetProperty("handle").GetInt32());
        Assert.True(element.GetProperty("hasText").GetBoolean());
        Assert.Equal("hello", element.GetProperty("text").GetString());
        Assert.Equal("role", element.GetProperty("attributes")[0].GetProperty("name").GetString());
        Assert.Equal("button", element.GetProperty("attributes")[0].GetProperty("value").GetString());
        Assert.Equal("color", element.GetProperty("styles")[0].GetProperty("name").GetString());
        Assert.Equal("red", element.GetProperty("styles")[0].GetProperty("value").GetString());
        Assert.Equal("count", element.GetProperty("properties")[0].GetProperty("name").GetString());
        Assert.Equal("number", element.GetProperty("properties")[0].GetProperty("kind").GetString());
        Assert.Equal(3, element.GetProperty("properties")[0].GetProperty("numberValue").GetInt32());
        Assert.Equal("active", element.GetProperty("classesToAdd")[0].GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Array, element.GetProperty("classesToRemove").ValueKind);
    }
}
