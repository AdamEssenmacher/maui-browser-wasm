# Mirage Benchmarks

This folder contains lightweight architecture benchmarks used to evaluate mixed typed/generic tree behavior before widening the control surface.

## Mirage.ArchitectureBenchmarks

Benchmarked scenarios:

- Mixed typed/generic tree diff churn
- Frequent property churn
- Event attach/detach churn
- JSON property updates
- Generic custom-element-heavy subtree churn

Run:

```bash
dotnet run --project tools/benchmarks/Mirage.ArchitectureBenchmarks/Mirage.ArchitectureBenchmarks.csproj
```

Notes:

- This is a lightweight harness (Stopwatch-based), not a microbenchmark framework.
- Use it as an architectural baseline to detect major regressions while evolving diff/runtime behavior.
