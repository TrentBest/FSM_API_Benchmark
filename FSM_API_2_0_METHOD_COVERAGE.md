# FSM_API 2.0 — Method-Level Benchmark Coverage Ledger

**Purpose:** Account for every public method and observable API operation in the string-backed and integer-backed FSM_API 2.0 surfaces. This is a coverage ledger, not a claim that every method has already been benchmarked.

**Source of truth:** The checked-out `TrentBest/FSM_API` development source used by each run. The exact source commit SHA must be recorded in every result artifact. The API surface changes over time; regenerate/reconcile this ledger whenever the source SHA changes.

## Coverage rules

Every public callable member must have exactly one primary disposition:

- **Timed:** a BenchmarkDotNet case measures a representative, repeatable operation.
- **Scenario:** measured as part of a larger end-to-end lifecycle/workload, with the containing scenario named.
- **Contract:** correctness/behavior is the meaningful metric (validation, exceptions, state invariants, event behavior); cover with parity/functional tests and do not invent a nanoseconds claim.
- **Diagnostic:** observational snapshots, counters, event subscription, or error reporting; benchmark only when a realistic consumer workload exists.
- **Excluded:** compiler-generated/accessor/override plumbing or a method with no meaningful independent consumer operation; exclusion must include a reason.

Overloads are separate inventory entries. Generic methods, property accessors, constructors, and events are included in the reflection inventory. Private implementation methods are not public API coverage, but hot private methods should be profiled when a public benchmark identifies them as bottlenecks.

## Automated reflection snapshot

The first completed reflection pass against FSM_API source commit `9c06ec955713bbe6db1c4b5f395acdbd1bcfaa7a` produced **334 public API inventory entries**:

| Reflected member kind | Count |
|---|---:|
| Methods (including overloads) | 251 |
| Constructors | 16 |
| Properties | 65 |
| Events | 2 |
| **Total** | **334** |

All entries have a disposition; none are left unclassified:

| Disposition | Count |
|---|---:|
| Timed candidate | 59 |
| Scenario | 55 |
| Diagnostic | 109 |
| Contract | 111 |
| **Total** | **334** |

At that snapshot, eight entries were exercised by the paired benchmark path: six public-facade members and two integer-runtime members exercised transitively. The other 326 are accounted for but not yet measured. This is the honest starting coverage—not a claim of 100% performance testing.

The machine-readable inventory, including declaring type, surface type, signature, disposition, and coverage status, is retained in the [GitHub Actions inventory artifact](https://github.com/TrentBest/FSM_API_Benchmark/actions/runs/44983854777). Its provenance records benchmark commit `36333dda777f053d65a20a74f2b85c5aed557af2` and FSM_API source commit `9c06ec955713bbe6db1c4b5f395acdbd1bcfaa7a`. Re-run the inventory whenever the FSM_API source changes; counts and signatures are source-version-specific.

## Public API families and planned coverage

| API family | Members / operations to inventory | Measurement or validation family | Current status |
|---|---|---|---|
| Unified facade — Create | String and integer `CreateFiniteStateMachine` overloads; string/integer `CreateInstance`; `RegisterDefinition`; `CreateProcessingGroup` | Builder creation, definition build, registration, instance creation; allocation + lifecycle timing | **Partial** — current update fixture constructs in setup only |
| Unified facade — Transfer | `ToInteger`, `ToString` | Conversion throughput, allocations, definition/state/transition cardinality; round-trip parity | Not benchmarked |
| Unified facade — Interact | `Update(string)`, `Update(int)`, `UpdateAllIntegerBacked`, `Unregister`, `Contains`, `MoveInstance`, `AddStateToFSM`, `AddTransition`, `DestroyFiniteStateMachine`, `Exists`, `GetAllDefinitionNames`, `GetInstances`, `RemoveProcessingGroup`, string/integer `DestroyInstance`, `RemoveStateFromFSM`, `RemoveTransition`, `GetInstance`, string/integer `GetFSMDefinition`, integer `GetInstances` | Scheduler, lookup hit/miss, inspection/snapshot, mutation, lifecycle, and teardown benchmarks | **Partial** — Update and successful definition lookup only |
| Unified facade — Error | Threshold properties; event add/remove; internal/report notifications; instance/definition error recording and reset; count snapshots; global reset; integer count snapshots and resets | Error-path scenario, event subscription/report cost, snapshot allocation; contract tests for thresholds/deferred teardown | Not benchmarked |
| Integer facade — Create | `CreateFiniteStateMachine`, `RegisterDefinition`, `CreateInstance` | Builder/registration/instance creation and allocation | Not benchmarked independently |
| Integer facade — Interaction | `Update`, `UpdateAll`, `DestroyInstance`, `Unregister`, `Contains`, `GetDefinition` | Scheduler, lookup hit/miss, lifecycle teardown | **Partial** — Update and Contains only |
| Integer runtime | `Register`, `Contains`, `GetDefinition`, `CreateInstance`, `RemoveInstance`, `GetInstances`, `Unregister`, `Update`, `UpdateAll`, `GetHandleCount`, error-count snapshots/resets | Direct runtime operation benchmarks; snapshots include allocation cost | Not benchmarked independently |
| Integer builder | Constructor; `State`, `WithProcessRate`, `WithProcessingGroup`, `WithInitialState`, `Transition`, `AnyTransition`, `BuildDefinition` | Builder microbenchmarks by states/transitions; validation and duplicate-ID contract tests | Not benchmarked |
| Integer handle | Constructor; `Initialize`, `Update`, `EvaluateConditions`, `TransitionTo`, `ResetFSMInstance` | Direct handle update, transition evaluation, forced transition, reset, lifecycle callback scenarios | Not benchmarked |
| Integer FSM / state / transition / compiler / timers / utilities | Every public constructor, method, property and conversion entry point in `IntegerBacked` types | Definition operations, state/transition lookup and mutation, timer paths, compile/decompile, utility calls | Inventory pending reflection pass |
| String-backed public model | `FSM`, `FSMBuilder`, `FSMHandle`, `FSMState`, `FSMTransition`, timers, context contracts and public facade members | Paired equivalents where semantics match; string-only behavior gets its own benchmark/contract case | Existing 1.x suite has broad coverage; 2.0 paired map pending |
| Diagnostics and support | Public metrics, context types, events, and exposed diagnostic helpers | Diagnostic cost only where consumer-relevant; otherwise contract coverage | Inventory pending reflection pass |

## Current executable benchmark cases

| Stable ID | Methods | Parameters | What it establishes |
|---|---|---|---|
| `FSMAPI.Execution.Update.NoTransitions` | `StringBackedUpdate`, `IntegerBackedUpdate` | 1/10/50 definitions × 1/10/50 instances per definition | Public scheduler/update cost with identical callback workload |
| `FSMAPI.Lookup.Definition.ById.Hit` | `StringBackedLookupHit`, `IntegerBackedLookupHit` | 1/10/50 definitions | Successful definition-presence lookup |

These two cases are the starting point only. They do not constitute full method coverage.

## Required scenario matrix

The paired implementations must use the same observable workload and validate equivalent outcomes before timing:

1. **Execution:** zero transitions; first transition succeeds; late transition succeeds; no transition matches; normal and Any-State transitions; state callback variants.
2. **Scheduler:** `ProcessRate` -1, 0, 1, 2, and larger N; one/many groups; sparse/dense definition IDs; one/many handles.
3. **Lookup:** definition/state hit and miss; first/middle/last IDs; dense and sparse IDs; short/long names.
4. **Construction:** builder creation; adding states/transitions; building; registration; instance creation; allocations.
5. **Lifecycle:** initialize, reset, forced transition, destroy one handle, unregister one definition, remove a group, replace a definition, deferred mutation during callbacks.
6. **Validity and errors:** invalid context suspension/resumption; callback exception; error thresholds; deferred teardown; repeated errors.
7. **Inspection:** definition retrieval; instance enumeration; handle counts; error snapshots; returned collection allocations.
8. **Transfer:** string-to-integer compile, integer-to-string decompile, round-trip semantic parity, and cost as graph cardinality grows.

## Acceptance criteria

- Every reflected public method/overload has a ledger row and a disposition. No silent gaps.
- Every timed paired scenario asserts equivalent final state, callback counts, lifecycle order, and error outcomes outside the timed region.
- Setup/cleanup and fixture construction stay outside timed methods unless construction itself is the subject.
- Report mean, median, deviation/error, allocated bytes/op, GC counts, parameter values, and environment.
- Include benchmark commit SHA and exact FSM_API source SHA in the artifact metadata.
- Compare string and integer variants within the same process/run/environment. Never compare hosted-runner timing directly with a local machine as if equivalent.
- Keep historical 1.x numbers as historical evidence; do not turn them into a current regression gate without a controlled rerun.
- A method marked Contract or Diagnostic is still accounted for; it is not silently treated as a performance benchmark.

## Next implementation order

1. Generate the complete reflection inventory from the built FSM_API assembly, including overloads, properties, events, and constructors; reconcile this table against it.
2. Add direct handle update and transition-evaluation cases to separate scheduler overhead from per-instance execution.
3. Add construction, lookup miss, mutation, lifecycle, invalid-context, and transfer families.
4. Run the complete Release benchmark matrix and retain raw BenchmarkDotNet artifacts plus source/environment provenance.
5. Review hot-path scaling before changing FSM_API implementation; optimize only against demonstrated cost and parity tests.
