using System;
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
    /// Each benchmark returns an observable checksum so the JIT cannot eliminate
    /// the handwritten state/counter mutations as dead code.
    /// Primitive controls measure the minimum direct-code path. Delegate controls
    /// add callback/predicate dispatch for a closer comparison to FSM_API.
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
        private int _delegateIntState;
        private int _delegateIntUpdates;
        private int _delegateIntExits;
        private int _delegateStringUpdates;
        private int _delegateStringExits;
        private string _delegateStringState = "State_0";
        private Func<bool> _transitionPredicate = null!;
        private Action _delegateIntUpdate = null!;
        private Action _delegateIntExit = null!;
        private Action _delegateStringUpdate = null!;
        private Action _delegateStringExit = null!;
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
            _transitionPredicate = () => TransitionRequested;
            _delegateIntUpdate = () => _delegateIntUpdates++;
            _delegateIntExit = () => _delegateIntExits++;
            _delegateStringUpdate = () => _delegateStringUpdates++;
            _delegateStringExit = () => _delegateStringExits++;

            ValidateEquivalentWorkAndReset();

            _ifState = 0;
            _ifUpdates = 0;
            _ifExits = 0;
            _switchState = 0;
            _switchUpdates = 0;
            _switchExits = 0;
            _delegateIntState = 0;
            _delegateIntUpdates = 0;
            _delegateIntExits = 0;
            _delegateStringState = "State_0";
            _delegateStringUpdates = 0;
            _delegateStringExits = 0;
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
            ManualDelegateInt();
            ManualDelegateInt();
            ManualDelegateString();
            ManualDelegateString();
            _apiHandle.Update(Group);
            _apiHandle.Update(Group);

            int expectedExits = TransitionRequested ? 2 : 0;
            bool equivalent =
                _ifState == 0 && _switchState == 0 &&
                _ifStringState == "State_0" && _switchStringState == "State_0" &&
                _delegateIntState == 0 && _delegateStringState == "State_0" &&
                _ifUpdates == 2 && _switchUpdates == 2 &&
                _ifStringUpdates == 2 && _switchStringUpdates == 2 &&
                _delegateIntUpdates == 2 && _delegateStringUpdates == 2 &&
                _ifExits == expectedExits && _switchExits == expectedExits &&
                _ifStringExits == expectedExits && _switchStringExits == expectedExits &&
                _delegateIntExits == expectedExits && _delegateStringExits == expectedExits &&
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
        public int IfElse()
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
            return _ifUpdates + _ifExits + _ifState;
        }

        [Benchmark]
        public int Switch()
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
            return _switchUpdates + _switchExits + _switchState;
        }


        [Benchmark]
        public int IfElseString()
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
            return _ifStringUpdates + _ifStringExits + (_ifStringState == "State_0" ? 1 : 0);
        }

        [Benchmark]
        public int SwitchString()
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
            return _switchStringUpdates + _switchStringExits + (_switchStringState == "State_0" ? 1 : 0);
        }


        [Benchmark]
        public int ManualDelegateInt()
        {
            _delegateIntUpdate();
            if (_transitionPredicate())
            {
                _delegateIntExit();
                _delegateIntState = _delegateIntState == 0 ? 1 : 0;
            }
            return _delegateIntUpdates + _delegateIntExits + _delegateIntState;
        }

        [Benchmark]
        public int ManualDelegateString()
        {
            _delegateStringUpdate();
            if (_transitionPredicate())
            {
                _delegateStringExit();
                _delegateStringState = _delegateStringState == "State_0" ? "State_1" : "State_0";
            }
            return _delegateStringUpdates + _delegateStringExits +
                (_delegateStringState == "State_0" ? 1 : 0);
        }

        [Benchmark]
        public int FSMApiHandleUpdate()
        {
            _apiHandle.Update(Group);
            return _apiContext.Updates + _apiContext.Exits + (_apiHandle.CurrentState == "State_0" ? 1 : 0);
        }
    }
}
