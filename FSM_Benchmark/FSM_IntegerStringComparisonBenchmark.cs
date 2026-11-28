using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using TheSingularityWorkshop.FSM_API;
using IntegerFsmApi = TheSingularityWorkshop.FSM_API.IntegerBacked.FsmApi;
using StringFsmApi = TheSingularityWorkshop.FSM_API.FsmApi;

namespace FSM_Benchmark;

/// <summary>
/// Matched-workload comparison of application logic, integer-backed FSM execution,
/// and string-backed FSM execution. Setup is excluded from the measured update operation.
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
        public int CounterA { get; set; }
        public int CounterB { get; set; }
        public int Total => CounterA + CounterB;
        public int Checksum => (CounterA * 31) + CounterB;
        public bool IsValid { get; set; } = true;
        public string Name { get; set; } = "IntegerStringComparison";
    }

    private readonly BenchmarkContext _stringContext = new();
    private readonly BenchmarkContext _integerContext = new();
    private readonly BenchmarkContext _applicationContext = new();
    private FSMHandle _stringHandle;
    private TheSingularityWorkshop.FSM_API.IntegerBacked.FSMHandleInt _integerHandle;
    private int _applicationState;

    private static readonly Action<BenchmarkContext> ApplicationUpdateA = context => context.CounterA++;
    private static readonly Action<BenchmarkContext> ApplicationUpdateB = context => context.CounterB++;
    private static readonly Func<BenchmarkContext, bool> ApplicationToB = context => (context.Total & 1) == 1;
    private static readonly Func<BenchmarkContext, bool> ApplicationToA = context => (context.Total & 1) == 0;

    [GlobalSetup]
    public void Setup()
    {
        var identity = Interlocked.Increment(ref _nextIdentity);
        _stringGroup = $"IntegerStringComparison.Group.{identity}";
        _stringDefinition = $"IntegerStringComparison.FSM.{identity}";
        _integerGroup = identity;
        _integerDefinition = identity;

        StringFsmApi.Create.CreateFiniteStateMachine(_stringDefinition, processRate: 1, processingGroup: _stringGroup)
            .State("StateA", null, IncrementA, null)
            .State("StateB", null, IncrementB, null)
            .WithInitialState("StateA")
            .Transition("StateA", "StateB", IsOdd)
            .Transition("StateB", "StateA", IsEven)
            .BuildDefinition();
        _stringHandle = StringFsmApi.Create.CreateInstance(_stringDefinition, _stringContext, _stringGroup);

        var integerDefinition = IntegerFsmApi.Create.CreateFiniteStateMachine(_integerDefinition, processRate: 1, processingGroupID: _integerGroup)
            .State(0, null, IncrementA, null)
            .State(1, null, IncrementB, null)
            .WithInitialState(0)
            .Transition(0, 1, IsOdd)
            .Transition(1, 0, IsEven)
            .BuildDefinition();
        IntegerFsmApi.Create.RegisterDefinition(integerDefinition);
        _integerHandle = IntegerFsmApi.Create.CreateInstance(_integerDefinition, _integerContext);
    }

    /// <summary>
    /// Executes the same callback and transition decision without an FSM scheduler,
    /// definition lookup, state lookup, or handle lifecycle. This is the application-logic
    /// floor, not a claim about an otherwise idle processor.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ApplicationLogicBaseline()
    {
        if (_applicationState == 0)
        {
            ApplicationUpdateA(_applicationContext);
            if (ApplicationToB(_applicationContext))
                _applicationState = 1;
        }
        else
        {
            ApplicationUpdateB(_applicationContext);
            if (ApplicationToA(_applicationContext))
                _applicationState = 0;
        }

        return _applicationContext.Checksum;
    }

    [Benchmark]
    public int IntegerScheduledTick()
    {
        IntegerFsmApi.Interaction.Update(_integerGroup);
        return _integerContext.Checksum;
    }

    [Benchmark]
    public int StringScheduledTick()
    {
        StringFsmApi.Interaction.Update(_stringGroup);
        return _stringContext.Checksum;
    }

    [Benchmark]
    public int IntegerHandleUpdate()
    {
        _integerHandle.Update();
        return _integerContext.Checksum;
    }

    [Benchmark]
    public int StringHandleUpdate()
    {
        _stringHandle.Update();
        return _stringContext.Checksum;
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
        benchmark.AssertInitialState("direct handle update");

        for (var i = 0; i < iterations; i++)
        {
            benchmark.ApplicationLogicBaseline();
            benchmark.IntegerScheduledTick();
            benchmark.StringScheduledTick();
        }

        benchmark.AssertEquivalent("scheduled group update", iterations * 2);
        benchmark.AssertInitialState("scheduled group update");

        Console.WriteLine($"PASS: application, integer-backed, and string-backed paths produced equivalent callback counts and final states across {iterations} direct and {iterations} scheduled updates.");
    }

    private void AssertEquivalent(string workload, int expectedOperations)
    {
        if (_applicationContext.Total != expectedOperations ||
            _integerContext.Total != expectedOperations ||
            _stringContext.Total != expectedOperations ||
            _applicationContext.CounterA != _integerContext.CounterA ||
            _applicationContext.CounterA != _stringContext.CounterA ||
            _applicationContext.CounterB != _integerContext.CounterB ||
            _applicationContext.CounterB != _stringContext.CounterB ||
            _applicationContext.Checksum != _integerContext.Checksum ||
            _applicationContext.Checksum != _stringContext.Checksum)
        {
            throw new InvalidOperationException(
                $"{workload} diverged: application=({_applicationContext.CounterA},{_applicationContext.CounterB}), integer=({_integerContext.CounterA},{_integerContext.CounterB}), string=({_stringContext.CounterA},{_stringContext.CounterB}), expected operations={expectedOperations}.");
        }
    }

    private void AssertInitialState(string workload)
    {
        if (_stringHandle.CurrentState != "StateA" ||
            _integerHandle.CurrentStateID != 0 ||
            _applicationState != 0)
        {
            throw new InvalidOperationException($"{workload} did not return all implementations to the same initial state.");
        }
    }

    private static void IncrementA(IStateContext context)
    {
        if (context is BenchmarkContext benchmarkContext)
            benchmarkContext.CounterA++;
    }

    private static void IncrementB(IStateContext context)
    {
        if (context is BenchmarkContext benchmarkContext)
            benchmarkContext.CounterB++;
    }

    private static bool IsOdd(IStateContext context) =>
        context is BenchmarkContext benchmarkContext && (benchmarkContext.Total & 1) == 1;

    private static bool IsEven(IStateContext context) =>
        context is BenchmarkContext benchmarkContext && (benchmarkContext.Total & 1) == 0;
}
