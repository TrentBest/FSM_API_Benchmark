using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures adding a transition to an existing definition.
    /// The fixture intentionally contains the source and target states but no
    /// pre-existing transition, so the measured call is an actual add operation.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 5, iterationCount: 15, invocationCount: 1)]
    public class FSM_TransitionMutationBenchmarks
    {
        private const string Group = "TransitionMutationGroup";
        private const string Fsm = "TransitionMutationFSM";

        [IterationSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            FSM_API.Create.CreateFiniteStateMachine(
                    Fsm,
                    processRate: 1,
                    processingGroup: Group)
                .State("State_A", null, null, null)
                .State("State_B", null, null, null)
                .WithInitialState("State_A")
                .BuildDefinition();
        }

        [IterationCleanup]
        public void Cleanup()
        {
            BenchmarkSupport.Reset();
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
    }
}
