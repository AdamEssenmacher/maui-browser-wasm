using System.Text.Json.Nodes;
using Mirage.Core.Diff;
using Mirage.Core.Semantics;
using Xunit;

namespace Mirage.Core.Tests;

public sealed class SemanticDiffEngineTests
{
    private readonly SemanticDiffEngine _diff = new();

    [Fact]
    public void Diff_InitialTree_EmitsCreateThenMutationsDeterministically()
    {
        NodeBase root = new ControlNode(
            nodeId: "root-1",
            kind: ComponentKind.Root,
            children:
            [
                new ControlNode(
                    nodeId: "label-1",
                    kind: ComponentKind.Label,
                    properties: new Dictionary<string, MirageValue> { ["Text"] = MirageValue.String("Hello") },
                    styles: new Dictionary<string, string?> { ["fontWeight"] = "700" },
                    events: ["click"]),
                new WebNode(
                    nodeId: "probe-1",
                    elementName: "vendor-pill",
                    properties: new Dictionary<string, MirageValue> { ["open"] = MirageValue.Bool(true) },
                    classes: ["hero-probe"],
                    attributes: new Dictionary<string, string?> { ["data-kind"] = "probe" })
            ]);

        var operations = _diff.Diff(previousRoot: null, nextRoot: root);

        Assert.Collection(
            operations.Take(8),
            op => Assert.IsType<CreateNodeOperation>(op),
            op => Assert.IsType<CreateNodeOperation>(op),
            op => Assert.IsType<SetPropertyOperation>(op),
            op => Assert.IsType<SetStyleOperation>(op),
            op => Assert.IsType<AttachEventOperation>(op),
            op => Assert.IsType<CreateNodeOperation>(op),
            op => Assert.IsType<SetPropertyOperation>(op),
            op => Assert.IsType<AddClassOperation>(op));

        Assert.Contains(operations, static operation => operation is SetAttributeOperation { NodeId: "probe-1", AttributeName: "data-kind" });
    }

    [Fact]
    public void Diff_RemovedSubtree_DetachesAndRemovesChildrenBeforeParent()
    {
        NodeBase previous = new ControlNode(
            "root-1",
            ComponentKind.Root,
            children:
            [
                new ControlNode(
                    "container-1",
                    ComponentKind.ContentContainer,
                    children:
                    [
                        new ControlNode("label-1", ComponentKind.Label, events: ["click"])
                    ])
            ]);

        NodeBase next = new ControlNode("root-1", ComponentKind.Root);

        var operations = _diff.Diff(previous, next);
        var removeOps = operations.OfType<RemoveNodeOperation>().ToArray();

        Assert.Equal(2, removeOps.Length);
        Assert.Equal("label-1", removeOps[0].NodeId);
        Assert.Equal("container-1", removeOps[1].NodeId);
    }

    [Fact]
    public void Diff_MixedTree_ProducesDeterministicOrder()
    {
        NodeBase previous = new ControlNode("root-1", ComponentKind.Root);
        NodeBase next = new ControlNode(
            "root-1",
            ComponentKind.Root,
            children:
            [
                new ControlNode("button-1", ComponentKind.Button, properties: new Dictionary<string, MirageValue> { ["Text"] = "Go" }, events: ["click"]),
                new WebNode(
                    "widget-1",
                    "vendor-widget",
                    properties: new Dictionary<string, MirageValue>
                    {
                        ["isActive"] = MirageValue.Bool(true),
                        ["payload"] = MirageValue.Json(JsonNode.Parse("""{"x":1}"""))
                    },
                    attributes: new Dictionary<string, string?> { ["aria-label"] = "Widget" },
                    classes: ["a", "b"],
                    events: ["custom-event"])
            ]);

        var first = _diff.Diff(previous, next).Select(op => op.ToString()).ToArray();
        var second = _diff.Diff(previous, next).Select(op => op.ToString()).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Diff_ThrowsWhenNodeTypeChangesForStableId()
    {
        NodeBase previous = new ControlNode(
            "root-1",
            ComponentKind.Root,
            children: [new ControlNode("same-id", ComponentKind.Label)]);
        NodeBase next = new ControlNode(
            "root-1",
            ComponentKind.Root,
            children: [new WebNode("same-id", "vendor-label")]);

        var exception = Assert.Throws<InvalidOperationException>(() => _diff.Diff(previous, next));
        Assert.Contains("changed node type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VisualVisibility_HiddenAndCollapsedMapToDistinctStyles()
    {
        var hidden = new ControlNode("label-hidden", ComponentKind.Label, visibility: MirageVisibility.Hidden);
        var collapsed = new ControlNode("label-collapsed", ComponentKind.Label, visibility: MirageVisibility.Collapsed);

        Assert.True(hidden.Styles.TryGetValue("visibility", out var hiddenVisibility));
        Assert.Equal("hidden", hiddenVisibility);
        Assert.False(hidden.Styles.ContainsKey("display"));

        Assert.True(collapsed.Styles.TryGetValue("display", out var collapsedDisplay));
        Assert.Equal("none", collapsedDisplay);
        Assert.True(collapsed.Styles.TryGetValue("visibility", out var collapsedVisibility));
        Assert.Equal("hidden", collapsedVisibility);
    }
}
