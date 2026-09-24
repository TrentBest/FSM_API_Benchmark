# FSM_API Benchmark Lab

[![FSM_API](https://img.shields.io/nuget/v/TheSingularityWorkshop.FSM_API?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/TheSingularityWorkshop.FSM_API)
[![BenchmarkDotNet](https://img.shields.io/nuget/v/BenchmarkDotNet?style=flat-square&logo=dotnet&logoColor=white)](https://www.nuget.org/packages/BenchmarkDotNet)
[![Repository](https://img.shields.io/github/last-commit/TrentBest/FSM_API_Benchmark/master?style=flat-square&logo=github)](https://github.com/TrentBest/FSM_API_Benchmark)

**The Singularity Workshop — measuring FSM_API instead of guessing about it.**

This repository is the performance laboratory for
[TheSingularityWorkshop.FSM_API](https://github.com/TrentBest/FSM_API).

The purpose is not to produce one impressive nanosecond number.

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
| Handle update | [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) | What does one direct FSM instance update cost? |
| TickAll | [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) | What does the scheduler core cost without the public wrapper? |
| Interaction.Update | [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) | What does the normal public update boundary cost? |
| Forced transition | [FSM_ExecutionBenchmarks.cs](FSM_Benchmark/FSM_ExecutionBenchmarks.cs) | What does an explicit state change cost? |
| States | [FSM_StateScalingBenchmarks.cs](FSM_Benchmark/FSM_StateScalingBenchmarks.cs) | Does merely having more states increase tick cost? |
| Transitions | [FSM_TransitionScalingBenchmarks.cs](FSM_Benchmark/FSM_TransitionScalingBenchmarks.cs) | How does transition cardinality affect a tick? |
| Instances | [FSM_ScalingBenchmarks.cs](FSM_Benchmark/FSM_ScalingBenchmarks.cs) | How does one definition behave with many live instances? |
| Definitions | [FSM_ScalingBenchmarks.cs](FSM_Benchmark/FSM_ScalingBenchmarks.cs) | How does one processing group behave with many definitions? |
| Processing groups | [FSM_ScalingBenchmarks.cs](FSM_Benchmark/FSM_ScalingBenchmarks.cs) | Does registry size materially affect name lookup? |
| String length | [FSM_LookupBenchmarks.cs](FSM_Benchmark/FSM_LookupBenchmarks.cs) | How does string-key size affect registry lookup? |
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
- [x] Memory allocation diagnostics
- [x] CPU diagnostics

The next stage is not simply adding more benchmark methods.

It is running these benchmarks on a controlled machine, examining the distributions, and using the results to identify where the actual cost centers are.

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
