# FSM_API Benchmark Catalog

**Status:** initial inventory; coverage is intentionally incomplete.  
**Purpose:** stable identities and a visible map from API behavior to repeatable measurements.  
**Scope:** the published FSM_API 1.0.13 package is the current benchmark dependency in this project. This catalog does not claim to describe an unreleased implementation.

## Stable identity

Use `FSMAPI.<Area>.<Operation>.<Scenario>`. IDs describe the semantic operation and scenario, not a class name, source line, test order, commit, or BenchmarkDotNet-generated display name.

Workload sizes and environment are recorded as dimensions. Keep an ID stable when implementation changes. If semantics genuinely change, add a migration entry rather than silently reusing history for a different operation.

## Existing benchmark inventory

| Stable ID | Existing benchmark entry point | Workload / behavior | Coverage status | Notes |
|---|---|---|---|---|
| `FSMAPI.Execution.Update.SingleFsmTick` | `FSM_StringPerformanceBaseline.SingleFsmUpdateTick` | One processing-group update; two-state FSM with callbacks and alternating transition predicates | **Covered — legacy identity mapping** | Assign the stable ID in benchmark output/reporting before treating new runs as directly comparable. |
| `FSMAPI.Execution.Update.ManyGroups` | `FSM_StringGroupIterationBenchmark.UpdateMultipleGroups` | Calls Update for 1, 10, or 50 group names | **Covered — legacy identity mapping** | Existing parameters are 1/10/50. Extend the scale only after validating fixture setup and practical runtime. |

These are the only benchmark entry points confirmed in the current project tree at the time of this inventory. This is not proof that no other historical measurements exist.

## Initial coverage backlog

The rows below are **uncovered planning entries**, not claims that the operation is missing from FSM_API itself. Verify each against the public API and current implementation before adding benchmark code.

| Stable ID | Target behavior | Workload dimensions to record | Complexity hypothesis | Status |
|---|---|---|---|---|
| `FSMAPI.Builder.BuildDefinition.Empty` | Build an empty/minimal definition | state count, transition count | Measure baseline; no assumed bound yet | Uncovered |
| `FSMAPI.Builder.BuildDefinition.ManyStates` | Build a larger definition | states, transitions, guards/actions | Growth hypothesis to be established | Uncovered |
| `FSMAPI.Lifecycle.Create.Instance` | Create an instance from a known definition | definition size, context type | Measure | Uncovered |
| `FSMAPI.Lifecycle.Destroy.Reuse` | Destroy and recreate/reuse lifecycle | repetitions, active instance count | Measure; verify contract first | Uncovered |
| `FSMAPI.Execution.Update.NoTransitions` | Tick with no eligible transition | states, groups, instances | Measure | Uncovered |
| `FSMAPI.Execution.Update.OneTransition` | Tick with a successful transition | states, transition count | Measure | Uncovered |
| `FSMAPI.Execution.Update.NoTransitionMatch` | Tick with no transition match | outgoing transition count | Measure | Uncovered |
| `FSMAPI.Execution.Update.AnyStateTransition` | Tick with Any-State and ordinary transitions | both transition counts, matching order | Measure; include parity/correctness checks | Uncovered |
| `FSMAPI.Lookup.State.ById.Hit` | Successful state lookup | state count, lookup repetitions | Test scaling; do not assume O(1) | Uncovered |
| `FSMAPI.Lookup.State.ById.Miss` | Failed state lookup | state count, miss repetitions | Test scaling; do not assume O(1) | Uncovered |
| `FSMAPI.Enumeration.States` | Enumerate states/definitions | state count | Measure | Uncovered |
| `FSMAPI.Mutation.Queue.EnqueueOne` | Queue one runtime mutation | queue depth, mutation type | Measure | Uncovered |
| `FSMAPI.Mutation.Queue.DrainMany` | Drain deferred mutations | queue depth, success/failure mix | Test scaling | Uncovered |
| `FSMAPI.Context.ReadWrite` | Representative context access | context shape, operation count | Measure | Uncovered |
| `FSMAPI.Callback.GuardAction` | Guard/action delegate overhead | delegate shape, transition outcome | Measure | Uncovered |
| `FSMAPI.Security.InvalidInput.Reject` | Reject invalid/boundary inputs | input category, input size | Measure cost without weakening validation | Uncovered |
| `FSMAPI.Concurrency.ContendedOperation` | Supported concurrent access path | worker count, contention, operation | Only benchmark where the documented contract permits it | Blocked pending contract audit |

## Measurement metadata

Each run should record at least:

- Stable benchmark ID and catalog revision.
- FSM_API package version and benchmark source commit.
- Benchmark suite version, SDK/runtime, OS, architecture, CPU, build configuration.
- All workload parameters and fixture/setup description.
- Mean/median and distribution/uncertainty available from the runner; throughput where meaningful.
- Allocated bytes/op and GC data where meaningful.
- Baseline run ID, absolute delta, percentage delta, and gate decision once comparison is enabled.
- Rerun identity and environment identity; never overwrite a prior result.

## Historical evidence currently retained

The repository contains two Visual Studio Diagnostics Hub `.diagsession` files dated 2026-09-23 under `Data/`, associated with the two existing benchmark classes. Keep them as historical artifacts. Their presence alone does not provide normalized, cross-environment comparable measurements; extract and document the environment and results before using them as a baseline.

Historical values previously reported for Update workloads (1 group: 305.1 ns / 360 B; 10 groups: 3,115.7 ns / 3,600 B; 50 groups: 15,736.6 ns / 18,000 B) are **reference-only pending provenance verification and a fresh run**. Do not label them as current results or use them as a hard gate until the exact source revision, runtime, hardware, and measurement setup are confirmed.

## Expansion rule

For each API member in the verified public surface, add one or more benchmark scenarios or document why it is not performance-relevant / cannot be measured yet. Coverage completion means every member has an explicit disposition; it does not mean every overload needs a redundant benchmark. Add adversarial, failure, boundary, allocation, and scaling scenarios where behavior warrants them.
