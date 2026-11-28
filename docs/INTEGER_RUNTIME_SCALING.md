# Integer Runtime Scaling Benchmarks

**Status:** benchmark definitions added; no performance result is claimed until a successful BenchmarkDotNet run records the exact source and harness revisions.

These scenarios target the integer-backed runtime in the pinned FSM_API development source. They are designed to establish a measured baseline before changing storage or scheduling structures.

## Scheduler population

`FSM_IntegerRuntimePopulationBenchmarks` holds one definition constant and varies live instances:

| Parameter | Values |
|---|---|
| Definitions | 1 |
| Live handles / contexts | 1, 100, 10,000, 100,000, 1,000,000 |
| Processing group | Integer ID 0 |
| Process rate | Every tick |
| States | One |
| Transitions | None |

The setup creates the runtime, definition, handles, and contexts outside the measured operation. It performs one priming update, verifies exactly one callback per context, and resets the callback counters. The timed operation is a steady-state runtime tick.

This answers how the runtime behaves with a large population sharing one small FSM. It does not measure a million-state definition.

## Definition-by-population matrix

`FSM_IntegerRuntimeDefinitionMatrixBenchmarks` varies both registered definitions and instances per definition:

| Parameter | Values |
|---|---|
| Definitions | 1, 10, 50, 100 |
| Instances per definition | 1, 10, 50, 100 |
| Maximum total live handles | 10,000 |
| Processing group | Shared integer ID 0 |
| Process rate | Every tick |
| States / transitions | One state / no transitions |

This matrix is deliberately important: it distinguishes the cost of updating many instances under one definition from the cost of scanning many definitions against a shared population. The setup verifies one callback per context before measurement.

The measured runtime tick includes its current handle snapshot allocation and definition/handle traversal. Those costs are part of the scheduler operation and must remain visible.

## Sparse identity storage

Two cold-path scenarios measure the storage cost of sparse identities:

- `FSM_IntegerSparseDefinitionIdentityBenchmarks`: creates a fresh runtime and registers one definition at ID 0, 255, 65,535, or 1,048,575.
- `FSM_IntegerSparseStateIdentityBenchmarks`: creates a fresh definition and adds one state at ID 0, 255, 65,535, or 1,048,575.

Each invocation uses a fresh object so array growth is included in the measurement. These cases measure one item at a high identity; they do not represent a definition containing one million actual states.

## Interpretation rules

- Compare cases within the same BenchmarkDotNet run and environment.
- Review mean, spread/error, allocated bytes, and GC counts—not just mean time.
- The single-definition population benchmark measures useful work plus scheduler overhead. The definition matrix exposes scheduler traversal growth.
- Sparse identity cases measure cold registration/definition construction and allocation. They do not establish steady-state lookup speed.
- No result should be attributed to a dictionary, list, or array in isolation unless the benchmark actually isolates that structure.
- Do not change production storage or scheduling based on these cases until the baseline is measured and an alternative implementation is measured with the same workload and correctness checks.

## Stable benchmark IDs

- `FSMAPI.Execution.Update.IntegerRuntime.OneDefinition.Population`
- `FSMAPI.Execution.Update.IntegerRuntime.DefinitionPopulationMatrix`
- `FSMAPI.Registry.Register.IntegerRuntime.SparseDefinitionID`
- `FSMAPI.Definition.AddState.IntegerRuntime.SparseStateID`

The source code is the authoritative fixture definition. Preserve raw BenchmarkDotNet output and exact FSM_API / benchmark repository commit SHAs with every accepted run.
