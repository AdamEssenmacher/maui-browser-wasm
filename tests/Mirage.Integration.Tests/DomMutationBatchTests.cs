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
        Assert.Collection(
            element.Operations,
            operation =>
            {
                Assert.Equal("style", operation.Kind);
                Assert.Equal("color", operation.Name);
                Assert.Equal("blue", operation.StringValue);
            },
            operation =>
            {
                Assert.Equal("text", operation.Kind);
                Assert.Null(operation.Name);
                Assert.Equal("after", operation.StringValue);
            },
            operation =>
            {
                Assert.Equal("attribute", operation.Kind);
                Assert.Equal("role", operation.Name);
                Assert.Equal("button", operation.StringValue);
            },
            operation =>
            {
                Assert.Equal("property-string", operation.Kind);
                Assert.Equal("value", operation.Name);
                Assert.Equal("second", operation.StringValue);
            },
            operation =>
            {
                Assert.Equal("class-add", operation.Kind);
                Assert.Equal("active", operation.Name);
            });
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
        var properties = Assert.Single(payload.Elements).Operations.ToDictionary(operation => operation.Name!, StringComparer.Ordinal);

        Assert.Equal("property-remove", properties["dataset"].Kind);
        Assert.Equal("property-input-value", properties["value"].Kind);
        Assert.Null(properties["value"].StringValue);
        Assert.Equal("property-input-checked", properties["checked"].Kind);
        Assert.True(properties["checked"].BoolValue);
    }

    [Fact]
    public void Drain_PreservesLastEffectiveMutationOrderPerHandle()
    {
        var batch = new DomMutationBatch();

        batch.QueueProperty((nint)3, "value", MirageValue.String("first"));
        batch.QueueAttribute((nint)3, "role", "button");
        batch.QueueProperty((nint)3, "value", MirageValue.String("second"));
        batch.QueueStyle((nint)3, "color", "red");
        batch.QueueAttribute((nint)3, "role", "link");

        var element = Assert.Single(batch.Drain().Elements);

        Assert.Collection(
            element.Operations,
            operation =>
            {
                Assert.Equal("property-string", operation.Kind);
                Assert.Equal("value", operation.Name);
                Assert.Equal("second", operation.StringValue);
            },
            operation =>
            {
                Assert.Equal("style", operation.Kind);
                Assert.Equal("color", operation.Name);
                Assert.Equal("red", operation.StringValue);
            },
            operation =>
            {
                Assert.Equal("attribute", operation.Kind);
                Assert.Equal("role", operation.Name);
                Assert.Equal("link", operation.StringValue);
            });
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
        var operations = element.GetProperty("operations");
        Assert.Equal(5, operations.GetArrayLength());
        Assert.Equal("text", operations[0].GetProperty("kind").GetString());
        Assert.Equal("hello", operations[0].GetProperty("stringValue").GetString());
        Assert.Equal("attribute", operations[1].GetProperty("kind").GetString());
        Assert.Equal("role", operations[1].GetProperty("name").GetString());
        Assert.Equal("button", operations[1].GetProperty("stringValue").GetString());
        Assert.Equal("style", operations[2].GetProperty("kind").GetString());
        Assert.Equal("color", operations[2].GetProperty("name").GetString());
        Assert.Equal("red", operations[2].GetProperty("stringValue").GetString());
        Assert.Equal("property-number", operations[3].GetProperty("kind").GetString());
        Assert.Equal("count", operations[3].GetProperty("name").GetString());
        Assert.Equal(3, operations[3].GetProperty("numberValue").GetInt32());
        Assert.Equal("class-add", operations[4].GetProperty("kind").GetString());
        Assert.Equal("active", operations[4].GetProperty("name").GetString());
    }
}
