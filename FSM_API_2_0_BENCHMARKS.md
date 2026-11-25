# FSM_API 2.0 — Representation Benchmarks

**Status:** Initial benchmark lane; measurements not yet collected.  
**Source:** The `development` branch of [TrentBest/FSM_API](https://github.com/TrentBest/FSM_API).  
**Benchmark engine:** BenchmarkDotNet 0.15.2, .NET 10.

This project benchmarks the current 2.0 source directly. It does not use the published 1.x NuGet package as a substitute for the integer runtime. The existing 1.x benchmark suite remains separate and preserves its historical package-based baseline.

## Initial coverage

| Stable benchmark ID | Methods | Workload dimensions | Question |
|---|---|---|---|
| `FSMAPI.Execution.Update.NoTransitions` | `StringBackedUpdate`, `IntegerBackedUpdate` | 1/10/50 definitions × 1/10/50 instances per definition | What does a complete scheduler update cost as the registry and live population grow? |
| `FSMAPI.Lookup.Definition.ByIdentity.Hit` | `StringBackedLookupHit`, `IntegerBackedLookupHit` | 1/10/50 definitions; successful lookup of the last definition | What is the cost of resolving an existing definition through each representation? |

Both execution methods call their respective public 2.0 update facades. The execution comparison holds constant:
- the same number of definitions and live instances;
- one processing group (string name versus integer ID);
- `ProcessRate = -1` (every update);
- one state and no transitions;
- the same context type and update callback;
- one warm-up update before measurement, so initial-state entry is not included in the steady-state result.

The setup verifies that both representations execute exactly one callback per instance before timing begins. A mismatch fails setup rather than producing misleading numbers.

The public integer facade owns a process-wide runtime, so the fixture unregisters the full tested ID range before setup and again during cleanup. The string-backed global runtime is hard-reset during setup and cleanup. This prevents a smaller parameter case from inheriting definitions or handles from a previous case. Integer IDs are dense and begin at zero, matching the intended array-indexed representation.

## Interpretation boundaries

These are end-to-end runtime comparisons, not a claim that every measured difference comes solely from integer-versus-string equality. The update benchmark includes each representation's registry traversal, scheduler, handle iteration, and public update boundary. The lookup benchmark isolates successful definition-presence lookup more narrowly. It compares each representation's public identity-resolution contract: the string API resolves a name within a named processing group, while the integer API resolves a globally unique FSM ID. These are analogous presence queries at their respective boundaries, not identical key shapes; interpret the result as representation-specific public lookup cost, not as a pure string-comparison-versus-integer-comparison microbenchmark.

In particular, the integer runtime currently scans its registered definition array and filters the tick's handle snapshot by definition. The multi-definition × multi-instance matrix deliberately exposes that implementation's scaling behavior; it must not be hidden by reporting only a one-definition result.

No performance conclusion is made until BenchmarkDotNet has produced actual results on a recorded machine. The hosted CI job builds both the legacy 1.x and source-linked 2.0 benchmark projects, then runs BenchmarkDotNet `Dry` smoke tests for each. This is compile/setup validation, not a performance baseline and not a regression gate.

## Reproduce locally

Clone this benchmark repository, then clone the FSM_API repository into a child directory named `FSM_API`:

```bash
git clone -b benchmark-complete https://github.com/TrentBest/FSM_API_Benchmark.git
cd FSM_API_Benchmark
git clone https://github.com/TrentBest/FSM_API.git FSM_API
git -C FSM_API switch development
dotnet run --project FSM_Benchmark_2_0/FSM_Benchmark_2_0.csproj -c Release -- --filter "*FSM_2_0_ExecutionBenchmarks*"
dotnet run --project FSM_Benchmark_2_0/FSM_Benchmark_2_0.csproj -c Release -- --filter "*FSM_2_0_LookupHitBenchmarks*"
```

For a short exploratory run, append `--job Short`. Use the default BenchmarkDotNet job for the canonical local measurement. Do not run under a debugger.

BenchmarkDotNet writes reports under `BenchmarkDotNet.Artifacts/results`. Preserve the raw CSV/Markdown/HTML output rather than copying only the mean.

## Required provenance for every accepted result

Record:
- benchmark repository commit SHA;
- exact FSM_API source commit SHA checked out for the run;
- BenchmarkDotNet and .NET SDK/runtime versions;
- OS, CPU model, logical processor count, and power/virtualization context where known;
- workload parameters and BenchmarkDotNet job configuration;
- mean, median, standard deviation/error, allocated bytes/op, and GC counts;
- absolute and percentage deltas between string-backed and integer-backed methods.

Do not compare hosted-runner timings with local-machine timings as if they were interchangeable. Compare representations within the same run and environment. Preserve reruns as distinct evidence; never overwrite prior results.

## Next benchmark families

Expand only after this first lane builds and its fixture verification passes:
1. direct handle update, separated from scheduler/registry overhead;
2. successful and unsuccessful state/definition lookups;
3. transition evaluation (no transition, first transition succeeds, late transition succeeds, no match);
4. definition and instance creation;
5. state/transition cardinality and processing-rate behavior;
6. lifecycle, mutation, invalid-context suspension/resumption, destruction, and error paths.

The string-backed and integer-backed implementations must remain behaviorally equivalent for every paired workload. A faster but behaviorally different path is not a valid optimization result.
