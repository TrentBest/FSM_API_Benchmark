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

## Expanded suite awaiting reconciliation

A broader implementation exists on the open [PR #1 — Complete FSM_API benchmark laboratory](https://github.com/TrentBest/FSM_API_Benchmark/pull/1), branch `benchmark-complete`. It is **not yet present on `development`**, so the following is a proposed ID mapping for review, not a claim that the branch has been merged, compiled here, or measured. Stable IDs should be attached to output/reporting before these scenarios become historical comparison series.

| Proposed stable ID | Branch entry point | Workload / behavior | Status |
|---|---|---|---|
| `FSMAPI.Builder.BuildDefinition.Cardinality` | `FSM_DefinitionCreationBenchmarks.BuildDefinition` | Build/register definitions at 1, 2, 10, 50 states | Proposed in PR #1; validate |
| `FSMAPI.Lifecycle.CreateInstance.ExistingDefinition` | `FSM_InstanceCreationBenchmarks.CreateInstance` | Create an instance after definition setup | Proposed in PR #1; validate |
| `FSMAPI.Execution.Handle.Update` | `FSM_ExecutionBenchmarks.HandleUpdate` | Direct handle update, steady-state | Proposed in PR #1; validate |
| `FSMAPI.Execution.Internal.TickAll` | `FSM_ExecutionBenchmarks.DirectTickAll` | Direct delegate to internal scheduler core | Diagnostic/internal path |
| `FSMAPI.Execution.Interaction.Update` | `FSM_ExecutionBenchmarks.InteractionUpdate` | Public processing-group update | Proposed in PR #1; validate |
| `FSMAPI.Execution.Handle.ForcedTransition` | `FSM_ExecutionBenchmarks.ForcedTransition` | Alternate forced target transition | Proposed in PR #1; validate |
| `FSMAPI.Handle.EvaluateConditions` | `FSM_HandleSurfaceBenchmarks.EvaluateConditions` | Manual condition evaluation | Proposed in PR #1; validate |
| `FSMAPI.Handle.Reset` | `FSM_HandleSurfaceBenchmarks.ResetFSMInstance` | Reset a live handle | Proposed in PR #1; validate |
| `FSMAPI.Handle.Property.IsValid` | `FSM_HandleSurfaceBenchmarks.IsValid` | Read validity | Proposed in PR #1; validate |
| `FSMAPI.Handle.Property.CurrentState` | `FSM_HandleSurfaceBenchmarks.CurrentState` | Read current state | Proposed in PR #1; validate |
| `FSMAPI.Handle.Property.Name` | `FSM_HandleSurfaceBenchmarks.Name` | Read name | Proposed in PR #1; validate |
| `FSMAPI.Handle.Property.Id` | `FSM_HandleSurfaceBenchmarks.Id` | Read identifier | Proposed in PR #1; validate |
| `FSMAPI.Handle.Property.HasEnteredCurrentState` | `FSM_HandleSurfaceBenchmarks.HasEnteredCurrentState` | Read lifecycle flag | Proposed in PR #1; validate |
| `FSMAPI.Interaction.Exists` | `FSM_InteractionBenchmarks.Exists` | Public definition/instance existence query | Proposed in PR #1; validate |
| `FSMAPI.Interaction.GetDefinition` | `FSM_InteractionBenchmarks.GetFSMDefinition` | Retrieve definition | Proposed in PR #1; validate |
| `FSMAPI.Interaction.GetInstance` | `FSM_InteractionBenchmarks.GetInstance` | Retrieve a specific instance | Proposed in PR #1; validate |
| `FSMAPI.Interaction.GetInstances` | `FSM_InteractionBenchmarks.GetInstances` | Retrieve group instances | Proposed in PR #1; validate |
| `FSMAPI.Interaction.GetDefinitionNames` | `FSM_InteractionBenchmarks.GetAllDefinitionNames` | Enumerate definition names | Proposed in PR #1; validate |
| `FSMAPI.Lookup.PublicExists.StringLength` | `FSM_LookupBenchmarks.PublicExistsLookup` | Public lookup at name lengths 8/32/128/512 | Proposed in PR #1; validate |
| `FSMAPI.Lookup.InternalBucket.StringLength` | `FSM_LookupBenchmarks.DirectBucketLookup` | Internal bucket lookup at name lengths 8/32/128/512 | Diagnostic/internal path |
| `FSMAPI.Scheduler.ProcessRate.Update` | `FSM_ProcessRateBenchmarks.InteractionUpdate` | ProcessRate 0/1/2/5/10 | Proposed in PR #1; validate fixture | `0` is documented to disable automatic stepping, so this is scheduler overhead for a non-auto-updating instance—not equivalent update work. Validate state/callback counts; consider `-1` as the separate every-tick case. |
| `FSMAPI.Scaling.LiveInstances.Update` | `FSM_InstanceScalingBenchmarks.Update` | 1–1000 live instances | Proposed in PR #1; validate |
| `FSMAPI.Scaling.Definitions.Update` | `FSM_DefinitionScalingBenchmarks.Update` | 1–500 definitions in a group | Proposed in PR #1; validate |
| `FSMAPI.Lookup.ProcessingGroups.Exists` | `FSM_ProcessingGroupLookupScalingBenchmarks.Exists` | 1–1000 registered groups | Proposed in PR #1; validate |
| `FSMAPI.Scaling.StateCardinality.Update` | `FSM_StateScalingBenchmarks.HandleUpdate` | 2–500 states, no transition rules | Proposed in PR #1; validate |
| `FSMAPI.Scaling.TransitionCardinality.Update` | `FSM_TransitionScalingBenchmarks.HandleUpdate` | 0–500 false transition rules | Proposed in PR #1; validate |
| `FSMAPI.Lifecycle.CreateProcessingGroup` | `FSM_SurfaceLifecycleBenchmarks.CreateProcessingGroup` | Create a group | Proposed in PR #1; validate |
| `FSMAPI.Mutation.AddState` | `FSM_SurfaceLifecycleBenchmarks.AddStateToFSM` | Add state to existing definition | Proposed in PR #1; validate |
| `FSMAPI.Mutation.RemoveState` | `FSM_SurfaceLifecycleBenchmarks.RemoveStateFromFSM` | Remove state from existing definition | Proposed in PR #1; validate |
| `FSMAPI.Mutation.RemoveTransition` | `FSM_SurfaceLifecycleBenchmarks.RemoveTransition` | Remove existing transition | Proposed in PR #1; validate |
| `FSMAPI.Lifecycle.DestroyInstance` | `FSM_SurfaceLifecycleBenchmarks.DestroyInstance` | Destroy a live instance | Proposed in PR #1; validate |
| `FSMAPI.Lifecycle.DestroyDefinition` | `FSM_SurfaceLifecycleBenchmarks.DestroyFiniteStateMachine` | Destroy a definition | Proposed in PR #1; validate |
| `FSMAPI.Lifecycle.RemoveProcessingGroup` | `FSM_SurfaceLifecycleBenchmarks.RemoveProcessingGroup` | Remove a processing group | Proposed in PR #1; validate |
| `FSMAPI.Mutation.AddTransition` | `FSM_TransitionMutationBenchmarks.AddTransition` | Add transition to a clean definition | Proposed in PR #1; validate |
| `FSMAPI.Timers.AddFloat` | `FSM_TimerBenchmarks.AddFloatTimer` | Add/set float timer | Proposed in PR #1; validate |
| `FSMAPI.Timers.AddInt` | `FSM_TimerBenchmarks.AddIntTimer` | Add/set integer timer | Proposed in PR #1; validate |
| `FSMAPI.Timers.ResetFloat` | `FSM_TimerBenchmarks.ResetFloatTimer` | Reset float timer | Proposed in PR #1; validate |
| `FSMAPI.Timers.ResetInt` | `FSM_TimerBenchmarks.ResetIntTimer` | Reset integer timer | Proposed in PR #1; validate |
| `FSMAPI.Timers.RemoveFloat` | `FSM_TimerBenchmarks.RemoveFloatTimer` | Remove float timer | Proposed in PR #1; validate |
| `FSMAPI.Timers.RemoveInt` | `FSM_TimerBenchmarks.RemoveIntTimer` | Remove integer timer | Proposed in PR #1; validate |
| `FSMAPI.Timers.Update` | `FSM_TimerBenchmarks.UpdateTimers` | Update timer subsystem | Proposed in PR #1; validate |
| `FSMAPI.Errors.InvokeInstance` | `FSM_ErrorHandlingBenchmarks.InvokeInstanceError` | Record instance error | Proposed in PR #1; validate |
| `FSMAPI.Errors.InvokeDefinition` | `FSM_ErrorHandlingBenchmarks.InvokeDefinitionError` | Record definition error | Proposed in PR #1; validate |
| `FSMAPI.Errors.InvokeInternalApi` | `FSM_ErrorHandlingBenchmarks.InvokeInternalApiError` | Record internal API error | Proposed in PR #1; validate |
| `FSMAPI.Errors.GetInstanceCounts` | `FSM_ErrorHandlingBenchmarks.GetInstanceErrorCounts` | Read instance error counts | Proposed in PR #1; validate |
| `FSMAPI.Errors.GetDefinitionCounts` | `FSM_ErrorHandlingBenchmarks.GetDefinitionErrorCounts` | Read definition error counts | Proposed in PR #1; validate |
| `FSMAPI.Errors.ResetInstanceCount` | `FSM_ErrorHandlingBenchmarks.ResetInstanceErrorCount` | Reset one instance count | Proposed in PR #1; validate |
| `FSMAPI.Errors.ResetDefinitionCount` | `FSM_ErrorHandlingBenchmarks.ResetDefinitionErrorCount` | Reset one definition count | Proposed in PR #1; validate |
| `FSMAPI.Errors.ResetAll` | `FSM_ErrorHandlingBenchmarks.ResetAllErrors` | Reset error subsystem | Proposed in PR #1; validate |
| `FSMAPI.Execution.Callback.NormalUpdate` | `FSM_ThrownErrorExecutionBenchmarks.NormalUpdate` | Normal callback path | Proposed in PR #1; validate |
| `FSMAPI.Execution.Callback.Throws` | `FSM_ThrownErrorExecutionBenchmarks.ThrowingUpdate` | Callback throws; API catches/reports | Proposed in PR #1; validate |

**Review rule:** stable IDs are semantic contracts. Before adopting a row, verify that setup actually exercises the named behavior, that setup/cleanup is outside the timed operation where intended, and that any mutable global state is reset consistently. IDs should be emitted as structured metadata, not merely exist in this Markdown table.
 
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
