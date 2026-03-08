using System.Diagnostics;
using System.Text.Json.Nodes;
using Mirage.Core.Diff;
using Mirage.Core.Semantics;

var runner = new ArchitectureBenchmarkRunner();
runner.Run();

internal sealed class ArchitectureBenchmarkRunner
{
    private readonly SemanticDiffEngine _diff = new();

    public void Run()
    {
        Console.WriteLine("Mirage Architecture Benchmarks");
        Console.WriteLine($"Timestamp (UTC): {DateTime.UtcNow:O}");
        Console.WriteLine();

        RunScenario("Mixed typed/generic tree churn", iterations: 1000, index =>
        {
            var previous = BuildMixedTree(index);
            var next = BuildMixedTree(index + 1);
            return _diff.Diff(previous, next).Count;
        });

        RunScenario("Frequent property churn", iterations: 5000, index =>
        {
            var previous = new TypedNode(
                "root",
                ComponentKind.Root,
                children:
                [
                    new TypedNode(
                        "label",
                        ComponentKind.Label,
                        properties: new Dictionary<string, MirageValue>
                        {
                            ["Text"] = MirageValue.String($"Value-{index}")
                        })
                ]);

            var next = new TypedNode(
                "root",
                ComponentKind.Root,
                children:
                [
                    new TypedNode(
                        "label",
                        ComponentKind.Label,
                        properties: new Dictionary<string, MirageValue>
                        {
                            ["Text"] = MirageValue.String($"Value-{index + 1}")
                        })
                ]);

            return _diff.Diff(previous, next).Count;
        });

        RunScenario("Event attach/detach churn", iterations: 4000, index =>
        {
            var withClick = (index % 2) == 0;

            var previous = new TypedNode(
                "root",
                ComponentKind.Root,
                children:
                [
                    new ElementNode(
                        "widget",
                        "vendor-widget",
                        events: withClick ? ["click"] : [])
                ]);

            var next = new TypedNode(
                "root",
                ComponentKind.Root,
                children:
                [
                    new ElementNode(
                        "widget",
                        "vendor-widget",
                        events: withClick ? [] : ["click"])
                ]);

            return _diff.Diff(previous, next).Count;
        });

        RunScenario("JSON property updates", iterations: 1500, index =>
        {
            var previous = new TypedNode(
                "root",
                ComponentKind.Root,
                children:
                [
                    new ElementNode(
                        "chart",
                        "vendor-chart",
                        properties: new Dictionary<string, MirageValue>
                        {
                            ["dataset"] = MirageValue.Json(JsonNode.Parse($$"""{"points":[{{index}},{{index + 1}},{{index + 2}}]}""")!)
                        })
                ]);

            var next = new TypedNode(
                "root",
                ComponentKind.Root,
                children:
                [
                    new ElementNode(
                        "chart",
                        "vendor-chart",
                        properties: new Dictionary<string, MirageValue>
                        {
                            ["dataset"] = MirageValue.Json(JsonNode.Parse($$"""{"points":[{{index + 1}},{{index + 2}},{{index + 3}}]}""")!)
                        })
                ]);

            return _diff.Diff(previous, next).Count;
        });

        RunScenario("Generic custom-element-heavy subtree", iterations: 700, index =>
        {
            var previous = BuildElementHeavyTree(index, elementCount: 120);
            var next = BuildElementHeavyTree(index + 1, elementCount: 120);
            return _diff.Diff(previous, next).Count;
        });
    }

    private static TypedNode BuildMixedTree(int version)
    {
        var rows = new List<NodeBase>(80);
        for (var i = 0; i < 40; i++)
        {
            rows.Add(new TypedNode(
                $"label-{i}",
                ComponentKind.Label,
                properties: new Dictionary<string, MirageValue>
                {
                    ["Text"] = MirageValue.String($"Label {i} :: v{version % 7}")
                },
                styles: new Dictionary<string, string?>
                {
                    ["fontWeight"] = (i + version) % 2 == 0 ? "600" : "500"
                }));

            rows.Add(new ElementNode(
                $"vendor-{i}",
                "vendor-pill",
                properties: new Dictionary<string, MirageValue>
                {
                    ["state"] = MirageValue.String((i + version) % 2 == 0 ? "active" : "idle"),
                    ["index"] = MirageValue.Number(i)
                },
                classes: (i + version) % 3 == 0 ? ["is-highlighted"] : [],
                attributes: new Dictionary<string, string?>
                {
                    ["data-kind"] = "pill"
                },
                events: ["click"]));
        }

        return new TypedNode(
            "root",
            ComponentKind.Root,
            children:
            [
                new TypedNode(
                    "content",
                    ComponentKind.ContentContainer,
                    children:
                    [
                        new TypedNode(
                            "stack",
                            ComponentKind.VerticalStack,
                            children: rows)
                    ])
            ]);
    }

    private static TypedNode BuildElementHeavyTree(int version, int elementCount)
    {
        var elements = new List<NodeBase>(elementCount);
        for (var i = 0; i < elementCount; i++)
        {
            elements.Add(new ElementNode(
                $"custom-{i}",
                "x-vendor-item",
                properties: new Dictionary<string, MirageValue>
                {
                    ["selected"] = MirageValue.Bool((i + version) % 4 == 0),
                    ["score"] = MirageValue.Number((i * 0.125) + (version % 10)),
                    ["payload"] = MirageValue.Json(JsonNode.Parse($$"""{"id":{{i}},"v":{{version}}}""")!)
                },
                classes: (i + version) % 2 == 0 ? ["row"] : ["row", "row-alt"],
                styles: new Dictionary<string, string?>
                {
                    ["opacity"] = (i + version) % 5 == 0 ? "0.9" : "1"
                },
                attributes: new Dictionary<string, string?>
                {
                    ["data-index"] = i.ToString(),
                    ["data-version"] = version.ToString()
                }));
        }

        return new TypedNode(
            "root",
            ComponentKind.Root,
            children:
            [
                new TypedNode(
                    "content",
                    ComponentKind.ContentContainer,
                    children:
                    [
                        new ElementNode(
                            "vendor-list",
                            "vendor-list",
                            children: elements)
                    ])
            ]);
    }

    private static void RunScenario(string name, int iterations, Func<int, int> executeIteration)
    {
        var stopwatch = Stopwatch.StartNew();
        long totalOps = 0;

        for (var i = 0; i < iterations; i++)
        {
            totalOps += executeIteration(i);
        }

        stopwatch.Stop();
        var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        var msPerIteration = elapsedMs / Math.Max(1, iterations);
        var opsPerSecond = elapsedMs <= 0 ? 0 : totalOps / (elapsedMs / 1000d);

        Console.WriteLine($"Scenario: {name}");
        Console.WriteLine($"  Iterations: {iterations:N0}");
        Console.WriteLine($"  Total operations emitted: {totalOps:N0}");
        Console.WriteLine($"  Elapsed: {elapsedMs:N2} ms");
        Console.WriteLine($"  Mean/iteration: {msPerIteration:N4} ms");
        Console.WriteLine($"  Approx operations/sec: {opsPerSecond:N0}");
        Console.WriteLine();
    }
}
