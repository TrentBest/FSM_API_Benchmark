using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the cost of changing an already-created FSM through the public lifecycle/modification API.
    ///
    /// These operations mutate global FSM state, so IterationSetup is intentional:
    /// each measured invocation starts from the same clean fixture. BenchmarkDotNet
    /// therefore runs one measured invocation per iteration for this class.
    ///
    /// These are lifecycle measurements, not steady-state nanobenchmarks.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 5, iterationCount: 15, invocationCount: 1)]
    public class FSM_SurfaceLifecycleBenchmarks
    {
        private const string Group = "LifecycleGroup";
        private const string Fsm = "LifecycleFSM";

        private BenchmarkSupport.DummyContext _context = null!;
        private FSMHandle _handle = null!;

        [IterationSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            _context = new BenchmarkSupport.DummyContext();

            FSM_API.Create.CreateFiniteStateMachine(
                    Fsm,
                    processRate: 1,
                    processingGroup: Group)
                .State("State_A", null, null, null)
                .State("State_B", null, null, null)
                .WithInitialState("State_A")
                .Transition("State_A", "State_B", static _ => false)
                .BuildDefinition();

            _handle = FSM_API.Create.CreateInstance(Fsm, _context, Group);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            BenchmarkSupport.Reset();
        }

        [Benchmark]
        public void CreateProcessingGroup()
        {
            FSM_API.Create.CreateProcessingGroup("NewLifecycleGroup");
        }

        [Benchmark]
        public void AddStateToFSM()
        {
            FSM_API.Interaction.AddStateToFSM(
                Fsm,
                "State_Added",
                null,
                null,
                null,
                Group);
        }

        [Benchmark]
        public void AddTransition()
        {
            FSM_API.Interaction.AddTransition(
                Fsm,
                "State_A",
                "State_B",
                static _ => false,
                Group);
        }

        [Benchmark]
        public void RemoveStateFromFSM()
        {
            FSM_API.Interaction.RemoveStateFromFSM(
                Fsm,
                "State_B",
                "State_A",
                Group);
        }

        [Benchmark]
        public void RemoveTransition()
        {
            FSM_API.Interaction.RemoveTransition(
                Fsm,
                "State_A",
                "State_B",
                Group);
        }

        [Benchmark]
        public void DestroyInstance()
        {
            FSM_API.Interaction.DestroyInstance(_handle);
        }

        [Benchmark]
        public void DestroyFiniteStateMachine()
        {
            FSM_API.Interaction.DestroyFiniteStateMachine(Fsm, Group);
        }

        [Benchmark]
        public void RemoveProcessingGroup()
        {
            FSM_API.Interaction.RemoveProcessingGroup(Group);
        }
    }
}
