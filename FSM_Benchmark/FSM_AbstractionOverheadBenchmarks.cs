using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Compares the runtime cost of the same two-state update/transition logic
    /// implemented as if/else, switch, and FSM_API.
    ///
    /// Definition/instance construction is deliberately outside the timed path.
    /// Each implementation increments an update counter, evaluates the same
    /// transition-request flag, and increments an exit counter when transitioning.
    /// </summary>
    [MemoryDiagnoser]
    public class FSM_AbstractionOverheadBenchmarks
    {
        private const string Group = "AbstractionOverheadGroup";
        private const string Definition = "AbstractionOverheadFSM";

        [Params(false, true)]
        public bool TransitionRequested { get; set; }

        private FSMHandle _apiHandle = null!;
        private int _ifState;
        private int _ifUpdates;
        private int _ifExits;
        private int _switchState;
        private int _switchUpdates;
        private int _switchExits;

        private sealed class AbstractionContext : IStateContext
        {
            public string Name { get; set; } = "AbstractionOverheadContext";
            public bool IsValid { get; set; } = true;
            public bool TransitionRequested { get; set; }
            public int Updates { get; set; }
            public int Exits { get; set; }
        }

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            FSM_API.Create.CreateFiniteStateMachine(
                    Definition,
                    processRate: 1,
                    processingGroup: Group)
                .State(
                    "State_0",
                    onEnter: null,
                    onUpdate: static context => ((AbstractionContext)context).Updates++,
                    onExit: static context => ((AbstractionContext)context).Exits++)
                .State(
                    "State_1",
                    onEnter: null,
                    onUpdate: static context => ((AbstractionContext)context).Updates++,
                    onExit: static context => ((AbstractionContext)context).Exits++)
                .WithInitialState("State_0")
                .Transition(
                    "State_0",
                    "State_1",
                    context => ((AbstractionContext)context).TransitionRequested)
                .Transition(
                    "State_1",
                    "State_0",
                    context => ((AbstractionContext)context).TransitionRequested)
                .BuildDefinition();

            _apiHandle = FSM_API.Create.CreateInstance(
                Definition,
                new AbstractionContext { TransitionRequested = TransitionRequested },
                Group);

            _ifState = 0;
            _ifUpdates = 0;
            _ifExits = 0;
            _switchState = 0;
            _switchUpdates = 0;
            _switchExits = 0;
        }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark(Baseline = true)]
        public void IfElse()
        {
            if (_ifState == 0)
            {
                _ifUpdates++;
                if (TransitionRequested)
                {
                    _ifExits++;
                    _ifState = 1;
                }
            }
            else
            {
                _ifUpdates++;
                if (TransitionRequested)
                {
                    _ifExits++;
                    _ifState = 0;
                }
            }
        }

        [Benchmark]
        public void Switch()
        {
            switch (_switchState)
            {
                case 0:
                    _switchUpdates++;
                    if (TransitionRequested)
                    {
                        _switchExits++;
                        _switchState = 1;
                    }
                    break;

                default:
                    _switchUpdates++;
                    if (TransitionRequested)
                    {
                        _switchExits++;
                        _switchState = 0;
                    }
                    break;
            }
        }

        [Benchmark]
        public void FSMApiHandleUpdate()
        {
            _apiHandle.Update(Group);
        }
    }
}
