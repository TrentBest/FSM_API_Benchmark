using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the public read/query surface of FSM_API.
    /// The returned values are consumed by BenchmarkDotNet, so these calls are not
    /// accidentally optimized away.
    /// </summary>
    [MemoryDiagnoser]
    public class FSM_InteractionBenchmarks
    {
        private const string Group = "QueryGroup";
        private const string Fsm = "QueryFSM";

        private BenchmarkSupport.DummyContext _context = null!;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            _context = new BenchmarkSupport.DummyContext();

            FSM_API.Create.CreateFiniteStateMachine(
                    Fsm,
                    processRate: 1,
                    processingGroup: Group)
                .State("State_0", null, null, null)
                .State("State_1", null, null, null)
                .WithInitialState("State_0")
                .Transition("State_0", "State_1", static _ => false)
                .BuildDefinition();

            FSM_API.Create.CreateInstance(Fsm, _context, Group);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            BenchmarkSupport.Reset();
        }

        [Benchmark(Baseline = true)]
        public bool Exists()
        {
            return FSM_API.Interaction.Exists(Fsm, Group);
        }

        [Benchmark]
        public FSM GetFSMDefinition()
        {
            return FSM_API.Interaction.GetFSMDefinition(Fsm, Group);
        }

        [Benchmark]
        public FSMHandle GetInstance()
        {
            return FSM_API.Interaction.GetInstance(Fsm, _context, Group)!;
        }

        [Benchmark]
        public IReadOnlyList<FSMHandle> GetInstances()
        {
            return FSM_API.Interaction.GetInstances(Fsm, Group);
        }

        [Benchmark]
        public IReadOnlyCollection<string> GetAllDefinitionNames()
        {
            return FSM_API.Interaction.GetAllDefinitionNames(Group);
        }
    }
}
