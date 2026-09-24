using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the smaller public FSMHandle surface separately from the full
    /// update/scheduler benchmarks.
    /// </summary>
    [MemoryDiagnoser]
    public class FSM_HandleSurfaceBenchmarks
    {
        private const string Group = "HandleSurfaceGroup";
        private const string Fsm = "HandleSurfaceFSM";

        private FSMHandle _handle = null!;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            FSM_API.Create.CreateFiniteStateMachine(
                    Fsm,
                    processRate: 1,
                    processingGroup: Group)
                .State("State_0", null, null, null)
                .State("State_1", null, null, null)
                .WithInitialState("State_0")
                .Transition("State_0", "State_1", static _ => false)
                .BuildDefinition();

            _handle = FSM_API.Create.CreateInstance(
                Fsm,
                new BenchmarkSupport.DummyContext(),
                Group);

            _handle.Update(Group);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            BenchmarkSupport.Reset();
        }

        [Benchmark]
        public void EvaluateConditions()
        {
            _handle.EvaluateConditions();
        }

        [Benchmark]
        public void ResetFSMInstance()
        {
            _handle.ResetFSMInstance();
        }

        [Benchmark]
        public bool IsValid()
        {
            return _handle.IsValid;
        }

        [Benchmark]
        public string CurrentState()
        {
            return _handle.CurrentState;
        }

        [Benchmark]
        public string Name()
        {
            return _handle.Name;
        }

        [Benchmark]
        public int Id()
        {
            return _handle.Id;
        }

        [Benchmark]
        public bool HasEnteredCurrentState()
        {
            return _handle.HasEnteredCurrentState;
        }
    }
}
