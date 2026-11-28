<!-- The Singularity Workshop moniker -->
<p align="center">
  <a href="https://github.com/TrentBest">
    <img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop — animated workshop moniker" width="720" />
  </a>
</p>

# FSM_API Benchmark Lab

[![Sith Policy](assets/sith-policy.svg)](https://github.com/TrentBest/FSM_API_Benchmark/actions/workflows/sith-policy.yml)

**[Open the executive performance brief](https://trentbest.github.io/FSM_API_Benchmark/)** — a plain-language comparison of application logic alone, integer-backed FSM execution, and string-backed FSM execution. The report is intentionally empty until a matched, successful benchmark run produces all all matched measurements.

## What we measure

The matched workload executes one active two-state FSM update. Every lane performs the same counter increment and parity-based transition decision:

| Lane | What it measures |
|---|---|
| Application logic only | The same update/decision delegates without an FSM scheduler, definition lookup, state lookup, or handle lifecycle |
| Integer-backed FSM | The workload through integer FSM/state/group identities |
| String-backed FSM | The same workload through named FSM states and a named processing group |

The executive report shows total time per operation, incremental overhead above the application-logic baseline, the string penalty relative to the integer runtime, measurement spread, allocation per operation, and the exact machine/source/run identity.

**Interpretation boundary:** the application-only lane is a controlled lower-bound comparison, not an idle-CPU benchmark. Results are specific to the workload, runtime, and runner. They are not universal processor constants.

## Reproducibility and source dependency

The integer-backed runtime is not present in the published FSM_API 1.0.13 release line. This harness therefore builds against an immutable Git submodule pin of the FSM_API development source. The commit is recorded in the benchmark evidence. The project targets .NET 10.

- [FSM_API source](https://github.com/TrentBest/FSM_API)
- [Benchmark source and workflows](https://github.com/TrentBest/FSM_API_Benchmark)
- [Executive report methodology](docs/EXECUTIVE_BENCHMARK_REPORT.md)

Pull requests build the harness and smoke-test all five paths. Development-branch changes run the full BenchmarkDotNet evidence workflow, preserve the raw CSV output, create the executive summary from those measurements, and publish the measured site through CiAi. The same workflow can also be run manually from development. No synthetic or hand-entered performance numbers are used.

## Why CiAi is linked

These measurements establish the cost of one foundational runtime component. They do **not** establish that autonomous development works. CiAi's commercial proof must separately show exact-source CI evidence, bounded repair authority, and a real red → diagnostic evidence → repair → green cycle.

**[Explore CiAi — verified CI evidence and the Autonomous Development Pipeline](https://trentbest.github.io/TheSingularityWorkshop.CiAi/)**

---

<p align="center">
  <a href="https://github.com/TrentBest/TheSingularityWorkshop" aria-label="The Singularity Workshop">
    <img src="https://raw.githubusercontent.com/TrentBest/WebPage/development/TheSingularityWorkshop/wwwroot/Images/TheSingularityWorkshopLivingAvatar.svg" alt="The Singularity Workshop living avatar" width="112" />
  </a><br />
  <strong>The Singularity Workshop</strong><br />
  <em>Evidence before claims. Performance before promises.</em>
</p>
