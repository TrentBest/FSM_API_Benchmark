# FSM_API Benchmark Lab

[![FSM_API](https://img.shields.io/nuget/v/TheSingularityWorkshop.FSM_API?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/TheSingularityWorkshop.FSM_API)
[![BenchmarkDotNet](https://img.shields.io/nuget/v/BenchmarkDotNet?style=flat-square&logo=dotnet&logoColor=white)](https://www.nuget.org/packages/BenchmarkDotNet)
[![Repository](https://img.shields.io/github/last-commit/TrentBest/FSM_API_Benchmark/master?style=flat-square&logo=github)](https://github.com/TrentBest/FSM_API_Benchmark)

**The Singularity Workshop — measuring FSM_API instead of guessing about it.**

This repository is the performance laboratory for
[TheSingularityWorkshop.FSM_API](https://github.com/TrentBest/FSM_API).

The purpose is not to produce one impressive nanosecond number.

The deeper purpose is to understand the cost surface of the API so that developers can choose the appropriate abstraction without unnecessary cognitive friction.

FSM_API is intentionally designed around two complementary representations:

- a string-backed authoring model for human-readable development, debugging, tooling, and low-friction adoption;
- an integer-backed runtime model for workloads where transition density, instance count, or other measured characteristics make representation cost significant.

The integer side is not intended to replace the string API. It is the performance-oriented half of the same design. A future publishing pipeline can determine when the integer representation is justified rather than forcing every developer to make that decision manually.

This benchmark project therefore has two jobs:

1. establish what the current string-backed API actually costs;
2. provide the measurements needed to know when a different representation is worth the additional complexity.

The benchmark is not an optimization contest. It is an instrument for reducing uncertainty.

The purpose is to answer a much more useful engineering question:

> **What does FSM_API actually cost, and how does that cost change as the system gets larger?**

FSM_API is a framework-agnostic C# finite-state-machine system built around reusable FSM definitions, live FSMHandle instances, processing groups, states, transitions, and a managed update cycle.

That means there is no single meaningful "FSM cost."

There is the cost of:

- defining an FSM;
- registering it;
- creating a live instance;
- updating one handle directly;
- running the managed processing-group scheduler;
- evaluating transitions;
- maintaining many states;
- maintaining many definitions;
- maintaining many instances;
- maintaining many processing groups;
- looking things up by name;
- and allocating memory while doing all of the above.

This benchmark suite exists to measure those dimensions separately.

---

# 🔬 Why This Project Exists

The first useful benchmark for FSM_API measured a single public update path and produced a surprisingly small result:

- **1 active process group:** 305.1 ns / 360 B
- **10 active process groups:** 3,115.7 ns / 3,600 B
- **50 active process groups:** 15,736.6 ns / 18,000 B

Those numbers were valuable because they showed that the update machinery was fast and scaled approximately linearly in that particular scenario.

But they also exposed the next question.

**What is inside that number?**

A call to:

~~~csharp
FSM_API.Interaction.Update("Update");
~~~

is not merely a string lookup.

In the current FSM_API implementation, the public update path:

1. starts a Stopwatch;
2. calls the internal TickAll(processingGroup) engine;
3. processes the appropriate FSM buckets;
4. respects each bucket's ProcessRate;
5. copies bucket and instance collections before iteration;
6. executes OnEnter when required;
7. executes the current state's OnUpdate;
8. obtains and evaluates transitions;
9. performs state exit/change bookkeeping when a transition succeeds;
10. processes deferred modifications;
11. stops the Stopwatch;
12. checks the performance warning threshold.

So the historical **305 ns** result should be understood as a measurement of a complete update scenario, not as the intrinsic cost of one string operation.

This project now decomposes that machinery.

The important architectural boundary is that authoring cost and runtime cost do not have to be the same thing. FSM_API is intended to remain useful as a lightweight public library. More aggressive optimization belongs where measured workload justifies it.

The longer-term Singularity Workshop direction is to perform expensive work during authoring and publication rather than repeatedly rediscovering answers at runtime:

~~~text
AUTHOR
  │
  ├── string-based FSM
  ├── analysis
  ├── baking
  ├── arbitration
  └── performance observation
          │
          ▼
       PUBLISH
          │
          ▼
      runtime manifest
          │
          ├── baked decisions
          ├── capacity/allocation requirements
          ├── data recipes
          └── runtime representation
          │
          ▼
       EXECUTE
~~~

That future publishing architecture is not part of the current benchmarked FSM_API. It is documented here because it explains why the benchmark measures representation, scaling, allocation, lifecycle, and auxiliary subsystem costs separately: those measurements eventually inform decisions about what should be baked, what should remain dynamic, and when an integer-backed representation is justified.

This benchmark suite now has a source file for each major measurement dimension. Start with the [execution benchmarks](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) to understand the original update number, then follow the [state](FSM_Benchmark/FSM_StateScalingBenchmarks.cs), [transition](FSM_Benchmark/FSM_TransitionScalingBenchmarks.cs), [runtime scaling](FSM_Benchmark/FSM_ScalingBenchmarks.cs), [lookup](FSM_Benchmark/FSM_LookupBenchmarks.cs), and [creation](FSM_Benchmark/FSM_CreationBenchmarks.cs) suites. Shared fixture construction lives in [BenchmarkSupport.cs](FSM_Benchmark/BenchmarkSupport.cs).

---

# 🧭 What We Measure

The benchmark suite is organized around the actual architecture of FSM_API.

~~~text
                         FSM_API
                            │
             ┌──────────────┼──────────────┐
             │              │              │
        Definition       Runtime        Registry
             │              │              │
        ┌────┼────┐     ┌───┼────┐      ┌───┼────┐
        │    │    │     │   │    │      │   │    │
      States Transitions │ Handles │    Groups Definitions
                         │        │
                         │        │
                      Update   Transition
                         │
                         ▼
                    Tick / Step
~~~

The questions are intentionally different.

| Area | Benchmark | Question |
|---|---|---|
| Definition creation | [FSM_CreationBenchmarks.cs](FSM_Benchmark/FSM_CreationBenchmarks.cs) | What does it cost to construct and register an FSM? |
| Instance creation | [FSM_CreationBenchmarks.cs](FSM_Benchmark/FSM_CreationBenchmarks.cs) | What does one live FSMHandle cost to create? |
| Handle execution | [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) | What does one direct FSM instance update cost? |
| ProcessRate | [FSM_ProcessRateBenchmarks.cs](FSM_Benchmark/FSM_ProcessRateBenchmarks.cs) | How does scheduler throttling change the cost of an update call? |
| TickAll | [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) | What does the scheduler core cost without the public wrapper? |
| Interaction.Update | [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) | What does the normal public update boundary cost? |
| Handle surface | [FSM_HandleSurfaceBenchmarks.cs](FSM_Benchmark/FSM_HandleSurfaceBenchmarks.cs) | What do manual condition evaluation, reset, and property access cost? |
| Forced transition | [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) | What does an explicit state change cost? |
| States | [FSM_StateScalingBenchmarks.cs](FSM_Benchmark/FSM_StateScalingBenchmarks.cs) | Does merely having more states increase tick cost? |
| Transitions | [FSM_TransitionScalingBenchmarks.cs](FSM_Benchmark/FSM_TransitionScalingBenchmarks.cs) | How does transition cardinality affect a tick? |
| Add transition | [FSM_TransitionMutationBenchmarks.cs](FSM_Benchmark/FSM_TransitionMutationBenchmarks.cs) | What does adding a transition to an existing definition cost? |
| Instances | [FSM_ScalingBenchmarks.cs](FSM_Benchmark/FSM_ScalingBenchmarks.cs) | How does one definition behave with many live instances? |
| Definitions | [FSM_ScalingBenchmarks.cs](FSM_Benchmark/FSM_ScalingBenchmarks.cs) | How does one processing group behave with many definitions? |
| Processing groups | [FSM_ScalingBenchmarks.cs](FSM_Benchmark/FSM_ScalingBenchmarks.cs) | Does registry size materially affect name lookup? |
| Public queries | [FSM_InteractionBenchmarks.cs](FSM_Benchmark/FSM_InteractionBenchmarks.cs) | What do Exists/GetDefinition/GetInstance/GetInstances/name enumeration cost? |
| String length | [FSM_LookupBenchmarks.cs](FSM_Benchmark/FSM_LookupBenchmarks.cs) | How does string-key size affect registry lookup? |
| Lifecycle mutation | [FSM_SurfaceLifecycleBenchmarks.cs](FSM_Benchmark/FSM_SurfaceLifecycleBenchmarks.cs) | What do adding/removing/destroying runtime structures cost? |
| Error handling | [FSM_ErrorHandlingBenchmarks.cs](FSM_Benchmark/FSM_ErrorHandlingBenchmarks.cs) | What does resilient error reporting actually cost? |
| Thrown callback path | [FSM_ThrownErrorExecutionBenchmarks.cs](FSM_Benchmark/FSM_ThrownErrorExecutionBenchmarks.cs) | What does a real user callback exception cost when FSM_API catches and reports it? |
| Timers | [FSM_TimerBenchmarks.cs](FSM_Benchmark/FSM_TimerBenchmarks.cs) | What does the timer subsystem cost independently? |
| Allocation | All benchmark classes | How many bytes and GC events accompany each operation? |

---

# 🧪 Benchmark Categories

## 1. Execution Layers

### [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs)

This is the most important benchmark for understanding the original 305 ns result.

It compares three layers:

~~~text
FSMHandle.Update()
       │
       ▼
    FSM.Step()
~~~

versus:

~~~text
FSM_API.Internal.TickAll()
~~~

versus:

~~~text
FSM_API.Interaction.Update()
       │
       ▼
    TickAll()
       │
       ▼
ProcessDeferredModifications()
~~~

### HandleUpdate

Measures a single live FSM instance through the public FSMHandle.Update() method.

This is deliberately different from the public scheduler.

It tells us:

> **What does the FSM itself cost when we already have the handle?**

### DirectTickAll

TickAll is an internal method in FSM_API, not a normal consumer API.

The benchmark obtains a delegate to it during setup using reflection. The reflection operation itself is **not measured**.

The measured operation is the delegate invocation:

~~~csharp
_directTickAll("BenchmarkGroup");
~~~

This gives us an advanced diagnostic view of the scheduler core without the public Interaction.Update() wrapper.

It should not be interpreted as a recommendation to consumers to call internal API.

### InteractionUpdate

This is the normal application-facing path:

~~~csharp
FSM_API.Interaction.Update("BenchmarkGroup");
~~~

This is the number most representative of normal use.

Comparing all three tells us where the overhead is coming from.

---

# 2. Transition Scaling

### [FSM_TransitionScalingBenchmarks.cs](FSM_Benchmark/FSM_TransitionScalingBenchmarks.cs)

The benchmark varies:

~~~text
0
1
2
10
50
100
500
~~~

transitions.

The transition conditions are deliberately false.

Why?

Because if the first transition succeeds, the FSM can stop evaluating the collection early.

A false-condition workload asks a cleaner question:

> **How much does the FSM pay to inspect additional transition rules?**

The state population is held at 502 states so that state cardinality does not change as transition count changes.

This is an important benchmarking principle:

> **Change one dimension at a time whenever possible.**

---

# 3. State Scaling

### [FSM_StateScalingBenchmarks.cs](FSM_Benchmark/FSM_StateScalingBenchmarks.cs)

This varies:

~~~text
2
10
50
100
500
~~~

states while keeping runtime transition work at zero.

The question is different from transition scaling:

> **Does simply making an FSM definition larger make an ordinary state update more expensive?**

If the result remains relatively flat, that tells us that state storage itself is not the dominant per-tick cost for this execution path.

---

# 4. Instance Scaling

### [FSM_ScalingBenchmarks.cs — Instance scaling](FSM_Benchmark/FSM_ScalingBenchmarks.cs)

This benchmark creates one definition and varies the number of live instances:

~~~text
1
10
50
100
500
1000
~~~

Then one processing-group update is measured.

This answers:

> **How does the runtime cost grow when more things are actually running?**

This is particularly important because FSM_API is designed around:

~~~text
Define once
     │
     ├── Instance
     ├── Instance
     ├── Instance
     └── ...
~~~

The definition should not be confused with the cost of its live manifestations.

---

# 5. Definition Scaling

### [FSM_ScalingBenchmarks.cs — Definition scaling](FSM_Benchmark/FSM_ScalingBenchmarks.cs)

This creates multiple FSM definitions in one processing group, with one instance per definition.

The benchmark varies:

~~~text
1
10
50
100
500
~~~

definitions.

This asks:

> **What happens to one processing-group tick when the group contains more FSM blueprints?**

This is distinct from instance scaling.

A system with:

~~~text
1 definition × 500 instances
~~~

is structurally different from:

~~~text
500 definitions × 1 instance
~~~

Even if both contain 500 live handles.

---

# 6. Processing-Group Lookup Scaling

### [FSM_ScalingBenchmarks.cs — Processing-group lookup scaling](FSM_Benchmark/FSM_ScalingBenchmarks.cs)

This varies the total number of independent processing groups:

~~~text
1
10
50
100
500
1000
~~~

and performs:

~~~csharp
FSM_API.Interaction.Exists("FSM_0", "Group_0");
~~~

This is deliberately **not** a tick benchmark.

It asks a registry question:

> **Does increasing the number of processing groups materially change the cost of looking one up?**

This is useful because a data structure can scale well for lookup even while the total amount of stored data grows.

---

# 7. String-Length Lookup

### [FSM_LookupBenchmarks.cs](FSM_Benchmark/FSM_LookupBenchmarks.cs)

The registry is string-keyed.

Therefore we also want to distinguish:

~~~text
number of entries
~~~

from:

~~~text
length of the key
~~~

This benchmark varies key length:

~~~text
8
32
128
512
~~~

and compares:

- public Interaction.Exists(...);
- direct Internal.GetBucket(...).

The strings are created during setup.

Therefore the benchmark is measuring lookup/hash/equality work, **not string construction**.

---

# 8. Definition Creation

### [FSM_CreationBenchmarks.cs — Definition creation](FSM_Benchmark/FSM_CreationBenchmarks.cs)

This measures the actual fluent definition-building path:

~~~csharp
FSM_API.Create.CreateFiniteStateMachine(...)
    .State(...)
    .State(...)
    .Transition(...)
    .BuildDefinition();
~~~

State count varies.

Setup and cleanup occur outside the timed benchmark iteration.

This matters.

If we created an FSM and then destroyed the entire registry inside the benchmark method, we'd be measuring:

~~~text
creation + destruction + cleanup
~~~

instead of definition creation.

The benchmark therefore isolates the operation as carefully as practical.

---

# 9. Instance Creation

### [FSM_CreationBenchmarks.cs — Instance creation](FSM_Benchmark/FSM_CreationBenchmarks.cs)

The FSM definition already exists before the measured operation.

The benchmark measures:

~~~csharp
FSM_API.Create.CreateInstance(
    "InstanceBenchmark",
    context,
    "BenchmarkGroup");
~~~

This answers:

> **What does it cost to turn an existing blueprint into a live FSM instance?**

Again, construction of the definition is deliberately outside the measured operation.

---

# 🧪 Measuring the Whole API Surface

The benchmark lab is intentionally becoming **retentive**.

We do not want to benchmark only the path that happens millions of times per second. We want a cost model for the operations a developer can actually choose to use.

That includes three different kinds of cost:

### 1. Steady-state cost

These are the operations that may happen every update:

- FSMHandle.Update
- FSM_API.Interaction.Update
- TickAll
- transition evaluation
- manual EvaluateConditions
- instance/definition/group cardinality

These should be run as conventional BenchmarkDotNet throughput benchmarks.

### 2. Structural/lifecycle cost

These are operations that change the shape of the FSM system:

- create a processing group;
- add a state;
- add a transition;
- remove a state;
- remove a transition;
- destroy an instance;
- destroy a definition;
- remove a processing group;
- create a definition;
- create an instance.

These operations have side effects by design. The dedicated lifecycle benchmarks therefore rebuild the fixture before each measured invocation. They are intentionally treated as **operation-cost measurements**, not as ordinary steady-state nanobenchmarks.

The important number for these operations is not merely "how many nanoseconds?" It is:

> **What does choosing this abstraction or structural operation cost me?**

### 3. Resilience and auxiliary subsystems

FSM_API promises more than state transitions.

It also provides:

- cascading error handling;
- instance and definition error counters;
- explicit error reset/reporting;
- public registry queries;
- timers.

Those are now measured independently.

This matters for the eventual user-facing performance documentation. We should be able to say things such as:

> "An ordinary steady-state update costs approximately X under workload Y."

and separately:

> "Adding structural complexity costs approximately Y."

and:

> "When your state logic throws and the cascading error system catches/reports it, the failure path costs approximately Z."

Those are fundamentally different engineering facts.

---

# 🧠 Authoring Cost vs. Runtime Cost

A central principle of the broader FSM architecture is that the cheapest runtime is often the runtime that does not have to make a decision at runtime.

The benchmark lab currently measures the public FSM_API itself. It does not yet benchmark a publisher, MetaDev system, Warehouse-backed runtime, or FSM_COS. Those are future layers.

The intended direction is:

~~~text
Editor / Authoring
    │
    ├── human-friendly strings
    ├── expensive analysis
    ├── baking
    ├── arbitration
    └── performance observation
             │
             ▼
        Publication
             │
             ▼
          Manifest
             │
             ├── baked arbitration result
             ├── expected capacities
             ├── subdivision/allocation recipe
             ├── data sources
             └── selected runtime representation
             │
             ▼
          Runtime
             │
             └── execute the prepared result
~~~

This creates an important distinction for interpreting benchmarks:

> A cost is not automatically a runtime problem merely because it is expensive.

An expensive editor-time calculation may be entirely acceptable if it removes substantially more work from every subsequent runtime update.

Likewise, an inexpensive authoring operation can still be undesirable if it forces repeated dynamic discovery, allocation, arbitration, or conversion at runtime.

The future MetaDev concept is intended to record extrinsic metadata about the authored system: observed performance characteristics, allocation behavior, capacity requirements, and other information useful during publication. It can then surface threshold violations and suggest concrete remedies.

One anticipated remedy is preallocation.

If publication can determine:

- the exact runtime capacity required;
- how that capacity should be subdivided;
- and which data sources populate it;

then the published manifest can carry those requirements forward instead of requiring the runtime to repeatedly grow and reorganize structures.

That is a future architecture, not a claim about the current FSM_API implementation.

---

# 🔀 String and Integer Representations

FSM_API is not being designed as:

~~~text
strings → eventually throw strings away → integers
~~~

It is being designed as:

~~~text
                 FSM_API
                    │
          ┌─────────┴─────────┐
          │                   │
     String-backed       Integer-backed
       authoring            runtime
          │                   │
    human-friendly       performance-oriented
    low-friction         dense representation
    inspectable          hot workloads
          │                   │
          └─────────┬─────────┘
                    │
              bridge / publish
~~~

The string side is valuable precisely because developers should not have to think like a CPU while creating an FSM.

The integer side exists for the workload where the measurements say that representation overhead matters.

This benchmark project therefore treats string cost as a measurable characteristic, not a defect.

In particular, transition scaling is expected to become increasingly interesting as the number of transitions evaluated per instance grows. If an FSM spends significant time evaluating large numbers of string-oriented transition rules, the integer-backed representation is the natural next experiment.

The benchmark should answer:

> At what workload does the additional complexity of integer backing buy enough runtime performance to justify using it?

That is more useful than simply asking whether integers are faster.

---

# 🧭 From Benchmarks to a Cost Model

The eventual goal is not a table of unrelated benchmark numbers.

It is a **cost map**.

For an operation with a variable dimension N, we want to understand:

~~~text
Cost(N)
   │
   ├── fixed cost
   │
   ├── marginal cost per additional item
   │
   └── allocation growth
~~~

For example, instance scaling should eventually let us estimate something like:

~~~text
UpdateCost(instances)
    ≈ scheduler fixed cost
    + instance marginal cost × instances
~~~

Likewise transition scaling can expose:

~~~text
TransitionCost(transitions)
    ≈ state-step fixed cost
    + transition evaluation marginal cost × transitions
~~~

We will **not** hard-code those equations into the benchmark project before the data exists.

The benchmark results come first.

Then we fit the simplest useful model to the measured data and verify that model against additional points.

That distinction is important: the benchmark measures the implementation; the cost model interprets the measurements.

---

# 🧮 The Numbers We Ultimately Want

For each variable dimension, the useful outputs are:

| Measurement | Why we care |
|---|---|
| Mean time | Direct observed execution cost |
| Median | Useful when distributions contain outliers |
| Error / StdDev | Stability of the measurement |
| Allocated bytes | Managed memory cost |
| Gen0/Gen1/Gen2 | Garbage-collection pressure |
| Scaling slope | Approximate marginal cost of another item |
| Intercept | Approximate fixed cost of the path |
| Breakpoints | Places where behavior changes shape |
| Failure-path cost | What resilience costs when things go wrong |

This gives us a much better vocabulary for the future FSM_API documentation.

Instead of saying:

> "FSM_API is fast."

we can eventually say:

> "Here is what the operation costs, here is how it scales, here is what it allocates, and here is when the cost changes."

That is a much more useful promise.

---

# 💥 Error Handling Is Part of the Performance Contract

The FSM_API documentation explicitly describes cascading degradation:

1. user callbacks and transition conditions are protected by exception handling;
2. errors are reported;
3. repeated instance failures are counted;
4. unstable instances can be removed;
5. repeated definition failures can eventually destroy a definition;
6. IStateContext.IsValid can cause an invalid instance to be unregistered.

That machinery is deliberately **not** treated as free.

FSM_ErrorHandlingBenchmarks.cs measures the explicit error subsystem, including:

- instance error reporting;
- definition error reporting;
- internal API error reporting;
- instance error dictionary access;
- definition error dictionary access;
- resetting one instance;
- resetting one definition;
- resetting all error state.

The actual thrown-user-callback path is now benchmarked separately in [FSM_ThrownErrorExecutionBenchmarks.cs](FSM_Benchmark/FSM_ThrownErrorExecutionBenchmarks.cs).

That distinction matters because explicit error reporting and a real exception flowing through FSMHandle.Update are not the same workload.

~~~text
Normal callback
      │
      └── no exception ─────────────── normal update cost

Throwing callback
      │
      ├── exception construction
      ├── catch
      ├── error reporting
      ├── error counting
      └── threshold/degradation work
~~~

We want those pieces separated rather than collapsing them into one scary "exception cost" number.

---

# 🧱 Structural Cost vs. Runtime Cost

This distinction is becoming a central principle of the lab.

Adding a state is not the same kind of operation as updating a state.

Adding an FSM definition is not the same kind of operation as ticking an FSM instance.

Destroying a processing group is not the same kind of operation as looking one up.

The benchmark project therefore intentionally measures both:

~~~text
BUILD / MUTATE
      │
      ├── definition
      ├── state
      ├── transition
      ├── instance
      └── group

RUN
      │
      ├── handle
      ├── scheduler
      ├── transitions
      └── scaling

QUERY
      │
      ├── Exists
      ├── GetDefinition
      ├── GetInstance
      ├── GetInstances
      └── name enumeration

FAIL
      │
      ├── instance error
      ├── definition error
      └── internal API error

AUXILIARY
      │
      └── timers
~~~

That is much closer to the actual API contract than a single "FSM update benchmark."

---

# 📏 Benchmarking Philosophy

This repository is also part of learning how to benchmark FSM_API properly.

A benchmark is not just:

~~~csharp
[Benchmark]
public void Something()
{
    DoSomething();
}
~~~

The difficult part is deciding **what Something means**.

A useful benchmark needs to answer four questions:

### 1. What operation are we measuring?

Example:

~~~text
Interaction.Update()
~~~

### 2. What work must be excluded?

Example:

~~~text
Definition construction
Benchmark setup
Registry cleanup
Reflection used to acquire the diagnostic delegate
~~~

### 3. What variable are we changing?

Example:

~~~text
Number of transitions
~~~

### 4. What variables are we holding constant?

Example:

~~~text
State population
Context type
Processing group
ProcessRate
Transition outcome
~~~

That is what turns a timing number into engineering information.

---

# 🧠 Understanding N

Many benchmark results are most useful when viewed as:

~~~text
Cost(N)
~~~

rather than as one number.

For example:

~~~text
Instances:       1     10     100     1000
                  │      │       │        │
                  ▼      ▼       ▼        ▼
Measured cost:    ?      ?       ?        ?
~~~

The shape tells us more than any single point.

### Approximately flat

~~~text
───────
~~~

The tested dimension may have little effect on the measured path.

### Approximately linear

~~~text
      /
    /
  /
/
~~~

Each additional item is adding roughly similar work.

### Step-like

~~~text
____
    |____
         |____
~~~

This can indicate thresholds, batching, resizing, cache effects, or other implementation boundaries.

### Non-linear

~~~text
     /
   /
  /
 /
/
~~~

with the slope changing as N grows.

That is often where the interesting engineering questions begin.

---

# 🧮 Complexity vs. Measurement

Benchmarks do not prove Big-O complexity by themselves.

They provide measured evidence about the implementation under a particular workload and environment.

For example, if:

~~~text
1 definition     →  X ns
10 definitions   →  ~10X ns
100 definitions  →  ~100X ns
~~~

that is evidence of approximately linear scaling in that tested range.

It does not mean the entire API is mathematically proven to be O(N) in every situation.

Likewise, a dictionary lookup can remain approximately constant-time on average while the total memory consumed by the registry continues to grow.

The benchmark report and the implementation should always be considered together.

The same principle applies when comparing the string-backed and integer-backed halves of FSM_API. A faster representation is only useful if the workload actually spends enough time in the affected path to justify the additional abstraction.

---

# 🚦 Performance Thresholds Are Guidance, Not Dogma

The long-term goal is not to make every developer manually optimize every FSM.

A useful performance system should eventually be able to say:

~~~text
Your authored workload is within expected limits.
    │
    └── publish normally.

Your authored workload crossed a measured threshold.
    │
    ├── preallocate more capacity;
    ├── reduce unnecessary runtime work;
    ├── bake a decision;
    └── consider integer-backed execution.
~~~

The benchmark lab provides the evidence from which those thresholds can eventually be established.

Until the workload, environment, and publication pipeline exist, the repository deliberately does not pretend to know universal thresholds.

The engineering rule is:

> Measure first. Then automate the advice.

---

# 🧠 What We Know About the Current FSM_API Architecture

The current FSM_API implementation stores its runtime registry as nested dictionaries:

~~~text
Processing Group
    │
    └── FSM Definition
            │
            └── FsmBucket
                    │
                    ├── Definition
                    ├── ProcessRate
                    ├── Counter
                    └── Instances
~~~

The FsmBucket therefore represents an important unit of runtime work.

A processing-group tick copies the bucket collection before iterating it.

Each bucket may then copy its instance collection before iterating it.

For a runnable instance, the current tick cycle performs work including:

~~~text
HasEnteredCurrentState?
        │
        ├── yes ────────────────┐
        │                       │
        └── no → OnEnter        │
                                ▼
                             OnUpdate
                                │
                                ▼
                       GetAllTransitions()
                                │
                                ▼
                         Evaluate rules
                                │
                     transition succeeds?
                         │          │
                        no         yes
                         │          │
                         │       OnExit
                         │          │
                         │       Change state
                         │          │
                         │       Reset entered flag
                         │
                         ▼
                       next FSM
~~~

That architecture is why this benchmark suite separates states, transitions, definitions, instances, and scheduler layers.

---

# 💾 Allocation Measurements

Every benchmark uses BenchmarkDotNet's MemoryDiagnoser.

This reports managed allocation behavior alongside execution time.

This matters because:

~~~text
fast + allocates heavily
~~~

is a different engineering result from:

~~~text
fast + allocation-free
~~~

The original benchmark results showed approximately:

~~~text
1 group  → 360 B
10 groups → 3,600 B
50 groups → 18,000 B
~~~

That pattern is one of the reasons allocation is treated as a first-class measurement in this repository.

Do not optimize allocations merely because a number looks large, however.

The first question is:

> **Where are those allocations actually coming from?**

That is exactly what the decomposition benchmarks are intended to help answer.

---

# 🛠️ Running the Benchmarks

Build the project in Release configuration:

~~~bash
dotnet build -c Release
~~~

Then run:

~~~bash
dotnet run -c Release
~~~

BenchmarkDotNet will discover the benchmark classes automatically.

For a focused run, BenchmarkDotNet also supports filtering:

~~~bash
dotnet run -c Release --filter *FSM_ExecutionBenchmarks*
~~~

Examples:

~~~bash
dotnet run -c Release --filter *Transition*
dotnet run -c Release --filter *Instance*
dotnet run -c Release --filter *Definition*
dotnet run -c Release --filter *Lookup*
~~~

### Always benchmark Release builds

Do not use Debug timings to make performance conclusions.

BenchmarkDotNet is designed to execute proper benchmark jobs and reports warmup, measurement, iteration, statistical, and memory information.

---

# 🖥️ Hardware and Runtime Matter

Benchmark results are measurements of:

~~~text
FSM_API version
+
.NET runtime
+
BenchmarkDotNet version
+
operating system
+
CPU
+
memory subsystem
+
current machine state
~~~

Therefore:

> **A benchmark result without its environment is incomplete evidence.**

Do not compare a result from one machine to another as if the numbers were interchangeable.

When performance becomes a published claim, record the environment alongside the result.

---

# 📚 Relationship to FSM_API Tests

The FSM_API unit tests answer:

> **Does the system behave correctly?**

The benchmark project answers:

> **How much does that behavior cost?**

Those are different questions.

A benchmark should not replace a unit test.

Likewise, a passing unit test does not tell us whether an operation takes:

~~~text
50 ns
500 ns
5 μs
50 μs
~~~

The two projects complement one another.

---

# 🏗️ Project Structure

~~~text
FSM_API_Benchmark
│
├── [FSM_Benchmark](FSM_Benchmark/)
│   │
│   ├── [BenchmarkSupport.cs](FSM_Benchmark/BenchmarkSupport.cs)
│   ├── [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs)
│   ├── [FSM_CreationBenchmarks.cs](FSM_Benchmark/FSM_CreationBenchmarks.cs)
│   ├── [FSM_ScalingBenchmarks.cs](FSM_Benchmark/FSM_ScalingBenchmarks.cs)
│   ├── [FSM_StateScalingBenchmarks.cs](FSM_Benchmark/FSM_StateScalingBenchmarks.cs)
│   ├── [FSM_TransitionScalingBenchmarks.cs](FSM_Benchmark/FSM_TransitionScalingBenchmarks.cs)
│   ├── [FSM_TransitionMutationBenchmarks.cs](FSM_Benchmark/FSM_TransitionMutationBenchmarks.cs)
│   ├── [FSM_SurfaceLifecycleBenchmarks.cs](FSM_Benchmark/FSM_SurfaceLifecycleBenchmarks.cs)
│   ├── [FSM_HandleSurfaceBenchmarks.cs](FSM_Benchmark/FSM_HandleSurfaceBenchmarks.cs)
│   ├── [FSM_ProcessRateBenchmarks.cs](FSM_Benchmark/FSM_ProcessRateBenchmarks.cs)
│   ├── [FSM_InteractionBenchmarks.cs](FSM_Benchmark/FSM_InteractionBenchmarks.cs)
│   ├── [FSM_ErrorHandlingBenchmarks.cs](FSM_Benchmark/FSM_ErrorHandlingBenchmarks.cs)
│   ├── [FSM_ThrownErrorExecutionBenchmarks.cs](FSM_Benchmark/FSM_ThrownErrorExecutionBenchmarks.cs)
│   ├── [FSM_TimerBenchmarks.cs](FSM_Benchmark/FSM_TimerBenchmarks.cs)
│   └── [FSM_LookupBenchmarks.cs](FSM_Benchmark/FSM_LookupBenchmarks.cs)
│
├── FSM_Benchmark.slnx
├── FSM_Benchmark/
│   └── FSM_Benchmark.csproj
│
└── README.md
~~~

The benchmark source is intentionally ordinary C#.

There is no custom benchmark framework hiding the important parts.

The goal is for the benchmark itself to remain readable enough that another developer can look at it and answer:

> **What exactly is being measured?**

---

# 🎯 The Long-Term Benchmark Map

This is the broader measurement map this project is growing toward:

~~~text
FSM_API Benchmark Lab
│
├── Execution
│   ├── FSMHandle.Update
│   ├── FSM.Step
│   ├── Internal.TickAll
│   ├── Interaction.Update
│   └── Forced Transition
│
├── Definitions
│   ├── Builder creation
│   ├── State creation
│   ├── Transition creation
│   └── BuildDefinition / registration
│
├── Runtime Cardinality
│   ├── States
│   ├── Transitions
│   ├── Definitions
│   ├── Instances
│   └── Processing Groups
│
├── Lookup
│   ├── Definition existence
│   ├── Bucket lookup
│   ├── Handle lookup
│   └── String length
│
├── Error handling
│   ├── Instance error reporting
│   ├── Definition error reporting
│   ├── Internal API error reporting
│   └── Error counter maintenance
│
├── Timers
│   ├── Add / set
│   ├── Reset
│   ├── Remove
│   └── Update
│
└── Allocation
    ├── Definition creation
    ├── Instance creation
    ├── Update
    ├── Transition evaluation
    └── Registry operations
~~~

This is deliberately broader than the original 305 ns benchmark.

The goal is to build a performance map of FSM_API rather than collect isolated timing trivia.

---

# 🌐 Long-Term Scope

This repository measures the substrate, not the entire future Singularity Workshop runtime.

The broader architecture eventually aims toward micro-bundles of functionality that can be composed into manifest-driven experiences. A bundle may contain behavior, assets, data, and an FSM, and the same bundle may participate in multiple experiences.

That creates a future distinction between:

~~~text
AUTHORING
    │
    ├── compose micro-bundles
    ├── analyze dependencies
    ├── arbitrate conflicts
    ├── bake decisions
    └── observe performance
             │
             ▼
        PUBLISHED MANIFEST
             │
             ▼
          FSM_COS
             │
             ▼
          ANYAPP
~~~

The benchmark project does not attempt to implement that architecture prematurely.

Instead, it establishes the empirical foundation underneath it.

The long-term optimization opportunity is to move expensive reasoning left:

> Do the hard thinking while the creator is editing. Publish the answer. Make runtime execution consume the answer.

A future Warehouse-backed runtime may go further by allowing published manifests to describe exact capacity, subdivision, and data-population requirements. That is intentionally outside the current free-standing FSM_API benchmark target.

The benchmark lab therefore remains focused on a simple question:

> How much does the current primitive actually cost?

Everything built above it should have evidence for when it is worth introducing.

---

# 🚦 Important Interpretation Rule

A benchmark result is not automatically a problem.

If a result gets slower as N increases, the next question is:

> **Is that scaling expected because the API is doing more real work?**

For example:

~~~text
1 instance  → update 1 instance
1000 instances → update 1000 instances
~~~

A larger number is expected.

The useful question becomes:

~~~text
How much additional cost does each additional instance contribute?
~~~

That is the difference between:

**"This number is bigger."**

and:

**"We understand why this number is bigger."**

The second one is what this project is trying to achieve.

---

# 🔬 Current Status

The benchmark suite now covers the major runtime dimensions of the FSM_API:

- [x] Single-instance execution
- [x] Direct FSMHandle.Update
- [x] Internal TickAll diagnostic path
- [x] Public Interaction.Update
- [x] Forced transitions
- [x] State cardinality
- [x] Transition cardinality
- [x] Instance cardinality
- [x] Definition cardinality
- [x] Processing-group lookup scaling
- [x] String-length lookup
- [x] Definition creation
- [x] Instance creation
- [x] Public query surface
- [x] Lifecycle mutation surface
- [x] Handle manual-operation surface
- [x] ProcessRate behavior
- [x] Error reporting and error-counter operations
- [x] Actual thrown-callback failure path
- [x] Timer subsystem
- [x] Memory allocation diagnostics
- [x] CPU diagnostics

The suite is now broad enough that the next stage is **measurement, not speculation**.

The immediate objective is to establish a clean performance baseline for the current public FSM_API. The current exploratory run demonstrated the shape of several costs, but attached-debugger measurements should not be treated as final published performance claims.

The next measurement pass should therefore:

1. run with the debugger detached;
2. save the complete BenchmarkDotNet output;
3. retain CSV/Markdown/HTML artifacts produced by the run;
4. record the exact FSM_API, .NET, BenchmarkDotNet, OS, and CPU environment;
5. identify fixed costs;
6. identify marginal costs;
7. identify allocation growth;
8. identify non-linear breakpoints;
9. separate normal-path work from failure-path work;
10. compare public abstractions against lower-level diagnostic equivalents;
11. establish where the string representation begins to matter enough to justify the integer-backed path.

Then build the first cost map:

1. identify fixed costs;
2. identify marginal costs;
3. identify allocation growth;
4. identify non-linear breakpoints;
5. separate normal-path work from failure-path work;
6. compare public abstractions against their lower-level diagnostic equivalents;
7. turn the measured deltas into user-facing performance guidance.

The .diagsession files from the earlier Visual Studio profiling runs are useful for diagnostics, but they are not the canonical benchmark-result format for this project. The canonical evidence should be the BenchmarkDotNet Markdown/CSV/HTML result set for each run, accompanied by its environment. If a future exporter adds JSON output to the run, that machine-readable artifact can be retained as well; the benchmark project should never claim an artifact exists unless the run actually produced it.

That is where benchmarking becomes an engineering instrument rather than a stopwatch.

---

# 📦 Dependencies

This project currently targets:

- **.NET 8**
- **BenchmarkDotNet 0.15.2**
- **Microsoft Visual Studio DiagnosticsHub BenchmarkDotNet Diagnosers**
- **TheSingularityWorkshop.FSM_API 1.0.13**

FSM_API itself is the system under measurement.

---

# 📄 License

See the repository for licensing information.

---

# 🧠 The Singularity Workshop

This benchmark lab is part of **The Singularity Workshop**.

The principle is simple:

> **Build the system. Measure the system. Understand the system. Then optimize the system.**

Because performance should be measured, not imagined.
