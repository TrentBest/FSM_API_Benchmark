using BenchmarkDotNet.Attributes;
using TheSingularityWorkshop.FSM_API;
using IntegerFsmApi = TheSingularityWorkshop.FSM_API.IntegerBacked.FsmApi;
using StringFsmApi = TheSingularityWorkshop.FSM_API.FsmApi;

namespace FSM_Benchmark;

/// <summary>
/// Matched-workload comparison of the application logic floor, integer-backed FSM runtime,
/// and string-backed FSM runtime. Setup is excluded from the measured update operation.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Runtime representation")]
public class FSM_IntegerStringComparisonBenchmark
{
    private const string StringGroup = "IntegerStringComparison.Group";
    private const string StringDefinition = "IntegerStringComparison.FSM";
    private const int IntegerGroup = 2;
    private const int IntegerDefinition = 2;

    private sealed class BenchmarkContext : IStateContext
    {
        public int Counter { get; set; }
        public bool IsValid { get; set; } = true;
        public string Name { get; set; } = "IntegerStringComparison";
    }

    private readonly BenchmarkContext _stringContext = new();
    private readonly BenchmarkContext _integerContext = new();
    private readonly BenchmarkContext _applicationContext = new();
    private int _applicationState;

    private static readonly Action<BenchmarkContext> ApplicationUpdate = context => context.Counter++;
    private static readonly Func<BenchmarkContext, bool> ApplicationToB = context => (context.Counter & 1) == 1;
    private static readonly Func<BenchmarkContext, bool> ApplicationToA = context => (context.Counter & 1) == 0;

    [GlobalSetup]
    public void Setup()
    {
        StringFsmApi.Create.CreateFiniteStateMachine(StringDefinition, processRate: 1, processingGroup: StringGroup)
            .State("StateA", null, Increment, null)
            .State("StateB", null, Increment, null)
            .WithInitialState("StateA")
            .Transition("StateA", "StateB", IsOdd)
            .Transition("StateB", "StateA", IsEven)
            .BuildDefinition();
        StringFsmApi.Create.CreateInstance(StringDefinition, _stringContext, StringGroup);

        IntegerFsmApi.Create.CreateFiniteStateMachine(IntegerDefinition, processRate: 1, processingGroupID: IntegerGroup)
            .State(0, null, Increment, null)
            .State(1, null, Increment, null)
            .WithInitialState(0)
            .Transition(0, 1, IsOdd)
            .Transition(1, 0, IsEven)
            .BuildDefinition();
        IntegerFsmApi.Create.CreateInstance(IntegerDefinition, _integerContext);
    }

    /// <summary>
    /// Executes the same counter update and parity decision without an FSM scheduler,
    /// definition lookup, state lookup, or handle lifecycle. This is the application-logic floor,
    /// not a claim about an otherwise idle processor.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void ApplicationLogicBaseline()
    {
        ApplicationUpdate(_applicationContext);
        if (_applicationState == 0 && ApplicationToB(_applicationContext))
            _applicationState = 1;
        else if (_applicationState == 1 && ApplicationToA(_applicationContext))
            _applicationState = 0;
    }

    [Benchmark]
    public void IntegerBackedTick() => IntegerFsmApi.Interaction.Update(IntegerGroup);

    [Benchmark]
    public void StringBackedTick() => StringFsmApi.Interaction.Update(StringGroup);

    private static void Increment(IStateContext context)
    {
        if (context is BenchmarkContext benchmarkContext)
            benchmarkContext.Counter++;
    }

    private static bool IsOdd(IStateContext context) =>
        context is BenchmarkContext benchmarkContext && (benchmarkContext.Counter & 1) == 1;

    private static bool IsEven(IStateContext context) =>
        context is BenchmarkContext benchmarkContext && (benchmarkContext.Counter & 1) == 0;
}
