# Executive benchmark report methodology

## Purpose

This report translates a matched BenchmarkDotNet run into an executive-readable cost model. It is not a marketing calculator and it does not invent missing measurements.

The model is:

**Observed runtime time = application work + runtime overhead**

The application-logic baseline is the same counter update and parity decision executed through delegates. It excludes FSM scheduling, definition lookup, state lookup, and handle lifecycle. It is not an idle processor, a hardware instruction benchmark, or a claim that all application logic costs the same amount.

## Matched lanes

One BenchmarkDotNet class runs five methods against the same logical operation, giving two runtime views plus one shared application baseline:

1. **ApplicationLogicBaseline** — the application callback and transition decision without an FSM runtime.
2. **IntegerHandleUpdate** and **StringHandleUpdate** — call the instance handle directly, excluding processing-group scheduling and group lookup. This is the closer view of per-instance runtime representation cost.
3. **IntegerScheduledTick** and **StringScheduledTick** — advance the same machine through the public processing-group update API. This includes scheduler and group-level runtime costs.

The state callback, transition predicate, process rate, and one-tick workload are equivalent. Setup is performed in GlobalSetup and excluded from the measured operation. The application baseline is shared for both runtime views. The benchmark process is a single controlled runner; do not compare results from different machines as though they were one experiment.

## Executive columns

| Column | Meaning |
|---|---|
| Application logic baseline | Mean nanoseconds for the direct application callback/decision |
| Integer-backed total | Mean nanoseconds for the integer-backed handle or scheduled-group update |
| String-backed total | Mean nanoseconds for the string-backed handle or scheduled-group update |
| Integer runtime overhead | Integer-backed total minus application baseline; also shown as a percentage of baseline |
| String runtime overhead | String-backed total minus application baseline; also shown as a percentage of baseline |
| String penalty over integer | String-backed total minus integer-backed total; also shown as a percentage of integer-backed time |
| Measurement spread | BenchmarkDotNet error and standard deviation |
| Allocation | Bytes allocated per operation, when MemoryDiagnoser reports a value |
| Provenance | FSM_API source SHA, harness SHA, workflow run/attempt, runtime, OS, architecture, processor |

The page presents two rows: direct instance-handle update and scheduled processing-group update. The overhead columns are differences between measured means. They can be negative if measurement noise or runtime effects make a lane faster than the baseline. The report does not clamp or disguise negative values. The scheduled row includes each API's own scheduler and diagnostics implementation; the direct-handle row is the closer comparison of per-instance execution.

## Evidence rules

- The application baseline and all four runtime lanes must be present in the same exported BenchmarkDotNet result set.
- A missing, duplicated, malformed, or non-positive mean rejects report generation.
- The parser does not combine measurements from separate runs or machines.
- The report includes exact source and harness commit SHAs plus workflow run/attempt.
- Raw BenchmarkDotNet CSV files are uploaded as a separate evidence artifact.
- The static page renders no numbers when the report is absent, malformed, or not marked measured.

## Runtime and source pin

The published FSM_API 1.0.13 release line does not contain the integer-backed API. The benchmark harness therefore pins FSM_API development as a Git submodule and uses a project reference to that source. The actual gitlink commit is checked during CI and recorded in every measured report. This is a source-development comparison, not a comparison against the 1.0.13 NuGet binary.

The harness and workflows target .NET 10.

## Relationship to CiAi

This benchmark establishes the runtime cost of a foundational FSM operation. It does not establish that CiAi's Autonomous Development Pipeline is operational. That proof requires a distinct, provenance-bound demonstration:

1. A failing candidate commit produces authoritative CI evidence.
2. CiAi validates exact repository, workflow, run, attempt, candidate SHA, and required checks.
3. A bounded agent receives diagnostics and proposes a repair.
4. A trusted host independently authorizes any isolated-branch write.
5. A new candidate SHA receives a new exact-source CI verdict.
6. The successful result is returned as a reviewable handoff; merge, release, and publication remain human-controlled.

The benchmark page links to CiAi because the runtime measurement is one body of supporting evidence in a broader engineering product story—not because this benchmark alone proves autonomous development.
