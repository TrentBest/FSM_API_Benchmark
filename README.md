# FSM_API Benchmark

This repository contains the executable performance experiments used to measure the runtime cost of **TheSingularityWorkshop.FSM_API**.

A benchmark is an experiment, not part of the production library. Keeping the experiment here lets a reader inspect exactly what was measured, how it was measured, and how the published numbers should be interpreted.

## Why benchmark FSM_API?

FSM_API is deliberately built around a practical runtime question:

> **What does it cost to update state?**

The current implementation uses string identifiers around processing groups. Rather than arguing that strings are "fast" or "slow", this benchmark establishes an observed baseline for the current implementation.

## The benchmarks to look for

- `FSM_StringPerformanceBaseline` — one real FSM update tick.
- `FSM_StringGroupIterationBenchmark` — updating 1, 10, and 50 processing groups.

Look for BenchmarkDotNet's `[Benchmark]` attribute to find what is actually measured and `[Params]` to find the workload being varied.

## Recorded results

The September 2026 benchmark run measured:

| Active process groups | Mean | Allocated |
|---:|---:|---:|
| 1 | **305.1 ns** | **360 B** |
| 10 | **3,115.7 ns** | **3,600 B** |
| 50 | **15,736.6 ns** | **18,000 B** |

For this particular workload, the measured cost grows approximately linearly across the tested range.

That is a **baseline observation**, not a universal complexity guarantee.

## What is actually being exercised?

### Single FSM baseline

`FSM_StringPerformanceBaseline` builds a two-state FSM with two transitions and measures:

~~~csharp
FSM_API.Interaction.Update(ProcessingGroup);
~~~

This is a real API operation rather than an isolated string or dictionary micro-test.

### Multiple processing groups

`FSM_StringGroupIterationBenchmark` creates named processing groups and measures:

~~~csharp
for (int i = 0; i < ActiveProcessGroups; i++)
{
    FSM_API.Interaction.Update($"Group_{i}");
}
~~~

The `[Params(1, 10, 50)]` attribute makes the workload explicit.

## How to read the result

### Mean

`Mean` is the average measured duration for one benchmark operation.

For example:

~~~text
305.1 ns
~~~

is approximately 305 billionths of a second.

### Allocated

`Allocated` is managed memory allocated by one benchmark operation.

The 50-group experiment reports **15,736.6 ns / 18,000 B**. That is evidence about this benchmark workload; it is not a promise that every application updating 50 groups allocates exactly 18 KB per frame.

## Why record both time and allocation?

Performance has multiple dimensions:

~~~text
CPU time
   +
managed allocation
   =
different performance costs
~~~

A fast operation can still create garbage-collection pressure. Recording both gives future optimization work a more useful baseline.

## Why the benchmark lives outside FSM_API

The production package should remain focused on state behavior.

The benchmark needs:

- BenchmarkDotNet;
- experiment-specific setup;
- workload parameters;
- diagnostics;
- result interpretation.

The relationship is:

~~~text
TheSingularityWorkshop.FSM_API
        │
        │ package under test
        ▼
FSM_API_Benchmark
        │
        ├── experiment
        ├── measurement
        └── interpretation
~~~

## Reproducing the experiment

The project targets .NET 8 and references:

- BenchmarkDotNet 0.15.2
- TheSingularityWorkshop.FSM_API 1.0.13

Run it in Release configuration:

~~~bash
dotnet run -c Release
~~~

Benchmark numbers are environment-sensitive. CPU, .NET runtime, operating system, thermal state, background processes, and benchmark configuration can all affect the result.

## Why there is no ordinary CI performance gate

A unit test asks:

> Did this behavior remain correct?

A benchmark asks:

> How expensive was this behavior on this machine under this workload?

The second question is inherently environment-sensitive. This repository is therefore the **executable experiment archive**, rather than an assertion that every commit has an immutable performance score.

## How to follow the evidence

A reader should be able to move from a package claim to the exact measured operation:

~~~text
FSM_API README
     │
     ▼
FSM_API_Benchmark
     │
     ▼
benchmark class
     │
     ▼
[Benchmark] method
     │
     ▼
BenchmarkDotNet output
     │
     ▼
interpretation
~~~

## When should this benchmark run again?

Rerun it after changes to:

- processing-group lookup;
- FSM registration;
- update traversal;
- allocation behavior;
- state transition execution;
- runtime identifier representation.

The benchmark is most valuable when it measures a real change.

**Measure first. Change one thing. Measure again.**
