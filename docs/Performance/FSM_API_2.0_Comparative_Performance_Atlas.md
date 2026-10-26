# The Singularity Workshop

# FSM_API 2.0 Comparative Performance Atlas

This document defines the evidence model for comparing the string-backed FSM_API 1.x surface with the integer-backed and string-backed execution paths in FSM_API 2.0. It is a benchmark plan, **not a claim that the listed scenarios are already implemented or measured**.

Primary tracking: [FSM_API_Benchmark issue #6](https://github.com/TrentBest/FSM_API_Benchmark/issues/6) and [FSM_API issue #24](https://github.com/TrentBest/FSM_API/issues/24).

## Purpose

Make the engineering cost of moving from string-backed identity to integer-backed identity visible, operation by operation. The result should explain where integer backing removes work, where string-facing convenience remains costly, where costs are dominated by unrelated machinery, and where integer backing does not help.

The historical `Interaction.Update` observation of approximately 305.1 ns / 360 B for one group is a **reference point only** until its exact source, runtime, hardware, and raw benchmark artifacts are verified. It is not a current measurement, an inherent floor, or proof that all of that cost is string processing.

## Comparison rules

- Compare semantically equivalent work, not merely similarly named methods.
- Preserve correctness and behavior parity before interpreting performance differences.
- Record exact source SHA, branch, package version where applicable, SDK/runtime, OS, CPU, BenchmarkDotNet job, parameters, and raw artifact paths.
- Keep construction/authoring, name-to-ID resolution, conversion/bridge, instance creation, and steady-state execution as separate cost centers.
- Include latency distributions, allocations, GC counts, and scaling dimensions where meaningful.
- Treat complexity as a hypothesis to test across workload sizes; do not derive nanoseconds from Big-O or CPU frequency.
- Do not add setup or reset work to a timed region unless that work is explicitly the operation being measured.
- Keep report-only regression checks until environmental noise and per-operation variance are characterized.
- Do not publish NuGet packages as part of benchmark work; publication requires explicit authorization.

## Benchmark dimensions

| Dimension | String-backed path | Integer-backed path | Required interpretation |
|---|---|---|---|
| Definition authoring | String state/group/transition names | Integer state/group/transition IDs | End-to-end authoring cost and allocations |
| Name/ID resolution | String lookup and comparison | Direct integer lookup/indexing | Measure conversion separately from runtime lookup |
| Definition build | Fluent string definition | Integer definition / equivalent builder | Equivalent definition semantics and validation |
| Instance creation | String-facing API | Integer-facing API | Include and separately isolate any bridge |
| Steady-state update | Public string-backed update | Integer-backed update | Same number of eligible instances and same work |
| State lookup | Name-based lookup | ID-based lookup | Hit, miss, boundary, and repeated lookup |
| Transition evaluation | String-identified states/targets | Integer-identified states/targets | Same guard and transition topology |
| Transition execution | Name target / string bridge | Integer target | Include success, no-op, invalid target, and lifecycle callbacks |
| Reset / lifecycle | String-backed handle | Integer-backed handle | Initial-state reset and active-state reset must be separate |
| Scheduling / groups | Named processing groups | Integer group IDs where supported | 1, 10, 50, 100, and larger group counts where feasible |
| Deferred mutation | Named operations | Integer operations where supported | Same queueing, application, and safety semantics |
| Diagnostics / errors | String names/messages | Integer identity with equivalent diagnostics | Separate diagnostic formatting from the core hot path |
| Composite workflow | Full string-facing fluent chain | Equivalent integer-facing chain | End-to-end cost; do not assume isolated timings sum to it |
| Bridge / mixed mode | String call into integer runtime and reverse if supported | Native integer route | Make compatibility costs explicit |
| Cold vs warm | First use and steady state | First use and steady state | Distinguish initialization/JIT/cache effects from recurring cost |

## Required scenario states

For stateful operations, name the precondition and postcondition in the benchmark catalog. At minimum, cover:

- First-use/cold path when meaningful.
- Warm steady-state path.
- Successful operation.
- Missing name or ID / lookup miss.
- Valid no-op.
- Invalid input or failure path.
- Empty, single-item, and increasing-size workloads.
- Repeated lifecycle/reuse.
- Transition from a non-initial state versus reset while already in the initial state.
- Allocating versus allocation-free behavior.
- Supported concurrency/deferred-mutation scenarios where semantics permit.

## Composite-versus-component accounting

For each important workflow, benchmark both:

1. The complete user-facing operation or fluent chain.
2. Each meaningful stage with setup excluded from the timed region where appropriate.
3. Matched incremental comparisons that change one factor at a time.

Publish absolute time, absolute delta, percentage delta, allocations, and uncertainty. Treat component timings as explanatory evidence, not an arithmetic identity: inlining, validation, allocation, caching, and interactions can make the composite differ from the sum of isolated stages.

## Result ledger schema

Every scenario should have a stable ID in the form `FSMAPI.<Area>.<Operation>.<Scenario>` and record:

- API member(s) and source path.
- Source/package identity and exact commit SHA.
- String/integer/mixed representation.
- Semantic equivalence/parity test.
- Fixture preconditions and expected postconditions.
- Input dimensions and complexity hypothesis.
- Cold/warm classification.
- Latency distribution, allocation/GC observations, and confidence/noise qualification.
- Raw artifact references and normalized report row.
- Evidence state: planned, implemented, correctness-verified, measured, reviewed, regression-gated.
- Any limitation or reason an API member is not independently benchmarkable.

## Acceptance bar

The atlas is ready for a public engineering story only when the comparisons are reproducible, behaviorally equivalent, and backed by retained raw results. It should be possible to tabulate both the runtime savings and the engineering costs of the integer-backed implementation without overstating either.

![Avatar](../../Avatar.jpg)
