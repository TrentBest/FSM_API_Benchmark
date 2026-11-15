# Benchmark build validation

The benchmark laboratory currently targets .NET 8 and references the published
`TheSingularityWorkshop.FSM_API` NuGet package at version `1.0.13`.
This is intentionally a benchmark lane for the published package contract; it
does not silently migrate to the newer `FsmApi` development surface.

## Reproduce the build

Install the .NET 8 SDK, then run from the repository root:

```bash
dotnet --info
dotnet restore FSM_Benchmark/FSM_Benchmark.csproj
dotnet build FSM_Benchmark/FSM_Benchmark.csproj --configuration Release --no-restore
```

The GitHub Actions **Benchmark Build** workflow performs these restore and
Release-build steps on pull requests targeting `development`. It is build
validation only: it does not execute BenchmarkDotNet, publish results, or
enforce performance thresholds.

A successful build establishes compilation and dependency resolution for this
project on the runner. It does not establish benchmark correctness, runtime
performance, or a measured operating envelope. Those require separately
controlled benchmark runs with retained environment and result artifacts.
