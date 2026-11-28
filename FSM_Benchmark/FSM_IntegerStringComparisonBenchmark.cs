using System;
using System.Threading;
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
    private static int _nextIdentity;
    private string _stringGroup;
    private string _stringDefinition;
    private int _integerGroup;
    private int _integerDefinition;

    private sealed class BenchmarkContext : IStateContext
    {
        public int Counter { get; set; }
        public bool IsValid { get; set; } = true;
        public string Name { get; set; } = "IntegerStringComparison";
    }

    private readonly BenchmarkContext _stringContext = new();
    private readonly BenchmarkContext _integerContext = new();
    private readonly BenchmarkContext _applicationContext = new();
    private TheSingularityWorkshop.FSM_API.FSMHandle _stringHandle;
    private TheSingularityWorkshop.FSM_API.IntegerBacked.FSMHandleInt _integerHandle;
    private int _applicationState;

    private static readonly Action<BenchmarkContext> ApplicationUpdate = context => context.Counter++;
    private static readonly Func<BenchmarkContext, bool> ApplicationToB = context => (context.Counter & 1) == 1;
    private static readonly Func<BenchmarkContext, bool> ApplicationToA = context => (context.Counter & 1) == 0;

    [GlobalSetup]
    public void Setup()
    {
        var identity = Interlocked.Increment(ref _nextIdentity);
        _stringGroup = $"IntegerStringComparison.Group.{identity}";
        _stringDefinition = $"IntegerStringComparison.FSM.{identity}";
        _integerGroup = identity;
        _integerDefinition = identity;

        StringFsmApi.Create.CreateFiniteStateMachine(_stringDefinition, processRate: 1, processingGroup: _stringGroup)
            .State("StateA", null, Increment, null)
            .State("StateB", null, Increment, null)
            .WithInitialState("StateA")
            .Transition("StateA", "StateB", IsOdd)
            .Transition("StateB", "StateA", IsEven)
            .BuildDefinition();
        _stringHandle = StringFsmApi.Create.CreateInstance(_stringDefinition, _stringContext, _stringGroup);

        var integerDefinition = IntegerFsmApi.Create.CreateFiniteStateMachine(_integerDefinition, processRate: 1, processingGroupID: _integerGroup)
            .State(0, null, Increment, null)
            .State(1, null, Increment, null)
            .WithInitialState(0)
            .Transition(0, 1, IsOdd)
            .Transition(1, 0, IsEven)
            .BuildDefinition();
        IntegerFsmApi.Create.RegisterDefinition(integerDefinition);
        _integerHandle = IntegerFsmApi.Create.CreateInstance(_integerDefinition, _integerContext);
    }

    /// <summary>
    /// Executes the same counter update and parity decision without an FSM scheduler,
    /// definition lookup, state lookup, or handle lifecycle. This is the application-logic floor,
    /// not a claim about an otherwise idle processor.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ApplicationLogicBaseline()
    {
        ApplicationUpdate(_applicationContext);
        if (_applicationState == 0 && ApplicationToB(_applicationContext))
            _applicationState = 1;
        else if (_applicationState == 1 && ApplicationToA(_applicationContext))
            _applicationState = 0;
        return _applicationContext.Counter + _applicationState;
    }

    [Benchmark]
    public int IntegerScheduledTick()
    {
        IntegerFsmApi.Interaction.Update(_integerGroup);
        return _integerContext.Counter;
    }

    [Benchmark]
    public int StringScheduledTick()
    {
        StringFsmApi.Interaction.Update(_stringGroup);
        return _stringContext.Counter;
    }

    [Benchmark]
    public int IntegerHandleUpdate()
    {
        _integerHandle.Update();
        return _integerContext.Counter;
    }

    [Benchmark]
    public int StringHandleUpdate()
    {
        _stringHandle.Update();
        return _stringContext.Counter;
    }

    public static void ValidateEquivalentExecution()
    {
        const int iterations = 1000;
        var benchmark = new FSM_IntegerStringComparisonBenchmark();
        benchmark.Setup();

        for (var i = 0; i < iterations; i++)
        {
            benchmark.ApplicationLogicBaseline();
            benchmark.IntegerHandleUpdate();
            benchmark.StringHandleUpdate();
        }

        benchmark.AssertEquivalent("direct handle update", iterations);
        if (benchmark._stringHandle.CurrentState != "StateA" ||
            benchmark._integerHandle.CurrentStateID != 0 ||
            benchmark._applicationState != 0)
        {
            throw new InvalidOperationException("Direct handle updates did not return all implementations to the same initial state.");
        }

        for (var i = 0; i < iterations; i++)
        {
            benchmark.ApplicationLogicBaseline();
            benchmark.IntegerScheduledTick();
            benchmark.StringScheduledTick();
        }

        benchmark.AssertEquivalent("scheduled group update", iterations * 2);
        if (benchmark._stringHandle.CurrentState != "StateA" ||
            benchmark._integerHandle.CurrentStateID != 0 ||
            benchmark._applicationState != 0)
        {
            throw new InvalidOperationException("Scheduled updates did not return all implementations to the same initial state.");
        }

        Console.WriteLine($"PASS: application, integer-backed, and string-backed paths produced equivalent results across {iterations} direct and {iterations} scheduled updates.");
    }

    private void AssertEquivalent(string workload, int expectedOperations)
    {
        if (_applicationContext.Counter != expectedOperations ||
            _integerContext.Counter != expectedOperations ||
            _stringContext.Counter != expectedOperations)
        {
            throw new InvalidOperationException(
                $"{workload} diverged: application={_applicationContext.Counter}, integer={_integerContext.Counter}, string={_stringContext.Counter}, expected={expectedOperations}.");
        }
    }

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
