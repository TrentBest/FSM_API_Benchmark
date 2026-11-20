# Benchmark Regression Policy

**Current mode: report-only until measurement noise and environment comparability are characterized.**  
**No benchmark gate is currently claimed to be active.**

## Principles

1. Measure the same semantic operation under equivalent workload and environment conditions.
2. A benchmark result is evidence, not a verdict by itself. Repeat suspicious measurements and preserve all runs.
3. No credible, material, unexplained regression is accepted silently.
4. Correctness and security requirements are not removed to improve a score. Measure the cost of the requirement and optimize the implementation while preserving it.
5. Never compare unrelated machines or runtime versions as though they were a controlled A/B experiment.
6. Do not overwrite a baseline or historical result. Baseline changes require a reviewed rationale.

## Staged rollout

### Stage 0 — Inventory and preserve

- Catalog existing benchmark entry points and stable IDs.
- Preserve existing source and historical artifacts.
- Record missing environment/provenance as unknown rather than guessing.
- Establish a normalized result schema and deterministic run naming.

### Stage 1 — Repeatable baseline

- Run Release builds with a recorded SDK/runtime and environment.
- Keep benchmark setup outside the measured operation unless setup is the operation being measured.
- Validate fixture isolation: a benchmark invocation must not accidentally change the work performed on subsequent iterations in a way that invalidates comparisons.
- Repeat the same commit/environment enough times to estimate normal variation.
- Store raw BenchmarkDotNet output and normalized CSV/Markdown with immutable run identity.

### Stage 2 — Report-only comparisons

- Compare each result to a compatible baseline for the same stable ID and workload.
- Report absolute and percentage deltas, allocation/GC deltas, uncertainty, environment compatibility, and any rerun.
- Warn and rerun results that are noisy, near a threshold, or inconsistent across repetitions.
- Keep performance status separate from build/test correctness status.

### Stage 3 — Calibrated regression gates

Enable failure gates only after representative same-environment history establishes operation-specific noise and budgets. A candidate gate should require:

- A material slowdown beyond the operation's calibrated budget.
- Evidence that the difference exceeds normal measurement variation.
- Comparable runtime, hardware class, configuration, and workload.
- A rerun or confirmation for suspicious results, unless the result is unambiguously severe.
- A clear report identifying the stable ID, baseline, current value, absolute/percentage delta, and disposition.

Avoid a universal nanosecond threshold across hosted runners. Use controlled/pinned runners where practical and a broader scheduled suite for expensive cases.

## Detecting algorithmic regressions

Benchmark multiple input sizes for operations whose cost may scale with states, transitions, groups, instances, or queued mutations. Keep the operation ID stable while recording input size as a dimension. Analyze the shape of the growth curve and ratios between sizes; do not infer a formal complexity guarantee from a small timing sample alone.

## Security and correctness tradeoffs

When a required validation, guard, concurrency fix, or security hardening adds cost:

- Benchmark valid, invalid, boundary, and adversarial cases as applicable.
- Compare before/after on the same environment and workload.
- Document the threat/correctness requirement, measured cost, and why the change is necessary.
- Record the accepted tradeoff and any follow-up optimization.
- Never remove required protection solely to make the benchmark faster.

## Incident record

Each confirmed regression should have a human-readable record containing:

- Stable benchmark ID and affected commit(s).
- Baseline and regressed run IDs, measurements, environments, and deltas.
- Reproduction and diagnosis.
- Fix commit and post-fix run.
- Comparison of fixed result to both the original baseline and regressed result.
- Disposition: fixed, accepted with explicit rationale, still investigating, or measurement artifact.

Do not fabricate an incident narrative for illustrative numbers. Distinguish measured evidence from examples.

## Required report statuses

- **PASS** — comparable measurement; no material regression beyond calibrated budget.
- **WARN** — uncertain/noisy result, incompatible environment, or uncalibrated budget.
- **FAIL** — confirmed material regression beyond an approved operation-specific budget.
- **BLOCKED** — no valid comparable baseline or benchmark fixture is not trustworthy.
- **NOT RUN** — benchmark was not executed.

Until Stage 3 is explicitly established with checked-in configuration and CI evidence, results must not be described as regression-gated.
