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
        private AbstractionContext _apiContext = null!;
        private int _ifState;
        private int _ifUpdates;
        private int _ifExits;
        private int _switchState;
        private int _switchUpdates;
        private int _switchExits;
        private string _ifStringState = "State_0";
        private int _ifStringUpdates;
        private int _ifStringExits;
        private string _switchStringState = "State_0";
        private int _switchStringUpdates;
        private int _switchStringExits;

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

            _apiContext = new AbstractionContext { TransitionRequested = TransitionRequested };
            _apiHandle = FSM_API.Create.CreateInstance(Definition, _apiContext, Group);

            ValidateEquivalentWorkAndReset();

            _ifState = 0;
            _ifUpdates = 0;
            _ifExits = 0;
            _switchState = 0;
            _switchUpdates = 0;
            _switchExits = 0;
            _ifStringState = "State_0";
            _ifStringUpdates = 0;
            _ifStringExits = 0;
            _switchStringState = "State_0";
            _switchStringUpdates = 0;
            _switchStringExits = 0;
        }

        /// <summary>
        /// Runs two known steps through every implementation before measurement.
        /// Two steps guarantee the final state returns to State_0 in both modes.
        /// </summary>
        private void ValidateEquivalentWorkAndReset()
        {
            IfElse();
            IfElse();
            Switch();
            Switch();
            IfElseString();
            IfElseString();
            SwitchString();
            SwitchString();
            _apiHandle.Update(Group);
            _apiHandle.Update(Group);

            int expectedExits = TransitionRequested ? 2 : 0;
            bool equivalent =
                _ifState == 0 && _switchState == 0 &&
                _ifStringState == "State_0" && _switchStringState == "State_0" &&
                _ifUpdates == 2 && _switchUpdates == 2 &&
                _ifStringUpdates == 2 && _switchStringUpdates == 2 &&
                _ifExits == expectedExits && _switchExits == expectedExits &&
                _ifStringExits == expectedExits && _switchStringExits == expectedExits &&
                _apiHandle.CurrentState == "State_0" &&
                _apiContext.Updates == 2 && _apiContext.Exits == expectedExits;

            if (!equivalent)
            {
                throw new InvalidOperationException(
                    "Abstraction benchmark implementations performed different work " +
                    "(TransitionRequested=" + TransitionRequested + ").");
            }

            // Restore identical initial conditions before BenchmarkDotNet starts timing.
            _ifState = 0;
            _ifUpdates = 0;
            _ifExits = 0;
            _switchState = 0;
            _switchUpdates = 0;
            _switchExits = 0;
            _ifStringState = "State_0";
            _ifStringUpdates = 0;
            _ifStringExits = 0;
            _switchStringState = "State_0";
            _switchStringUpdates = 0;
            _switchStringExits = 0;
            _apiContext.Updates = 0;
            _apiContext.Exits = 0;
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
        public void IfElseString()
        {
            if (_ifStringState == "State_0")
            {
                _ifStringUpdates++;
                if (TransitionRequested)
                {
                    _ifStringExits++;
                    _ifStringState = "State_1";
                }
            }
            else
            {
                _ifStringUpdates++;
                if (TransitionRequested)
                {
                    _ifStringExits++;
                    _ifStringState = "State_0";
                }
            }
        }

        [Benchmark]
        public void SwitchString()
        {
            switch (_switchStringState)
            {
                case "State_0":
                    _switchStringUpdates++;
                    if (TransitionRequested)
                    {
                        _switchStringExits++;
                        _switchStringState = "State_1";
                    }
                    break;

                default:
                    _switchStringUpdates++;
                    if (TransitionRequested)
                    {
                        _switchStringExits++;
                        _switchStringState = "State_0";
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
