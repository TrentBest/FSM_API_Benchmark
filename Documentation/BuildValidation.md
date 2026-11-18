# Benchmark build validation

The benchmark laboratory targets **.NET 10** and references the published
`TheSingularityWorkshop.FSM_API` NuGet package at version `1.0.13`.
The benchmark runtime is intentionally .NET 10; the package remains the
released API contract under measurement.

## Reproduce the build

Install the .NET 10 SDK, then run from the repository root:

```bash
dotnet --info
dotnet restore FSM_Benchmark/FSM_Benchmark.csproj
dotnet build FSM_Benchmark/FSM_Benchmark.csproj --configuration Release --no-restore
dotnet run --project FSM_Benchmark/FSM_Benchmark.csproj --configuration Release --no-build --framework net10.0 -- --filter "*FSM_AbstractionOverheadBenchmarks*" --job Dry
```

The GitHub Actions **Benchmark Build** workflow performs restore, Release
compilation, and a BenchmarkDotNet Dry-job smoke test on pull requests
targeting `development`.

The Dry job validates benchmark discovery, setup, and invocation. It is not a
performance measurement and must not be treated as timing evidence or a
regression threshold.

A successful build and smoke test establish compilation, dependency
resolution, and basic runtime execution on the configured .NET 10 runner.
They do not establish benchmark correctness or a measured operating envelope.
Those require separately controlled BenchmarkDotNet runs with retained
environment and result artifacts.
