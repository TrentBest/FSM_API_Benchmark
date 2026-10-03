using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the effect of the FSM definition's ProcessRate on the scheduler.
    /// ProcessRate is a behavioral performance knob, so it belongs in the cost map.
    /// </summary>
    [MemoryDiagnoser]
    public class FSM_ProcessRateBenchmarks
    {
        [Params(0, 1, 2, 5, 10)]
        public int ProcessRate { get; set; }

        private FSMHandle _handle = null!;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            FSM_API.Create.CreateFiniteStateMachine(
                    "ProcessRateFSM",
                    processRate: ProcessRate,
                    processingGroup: BenchmarkSupport.Group)
                .State(
                    "State_0",
                    null,
                    static ctx =>
                    {
                        if (ctx is BenchmarkSupport.DummyContext d)
                            d.Counter++;
                    },
                    null)
                .WithInitialState("State_0")
                .BuildDefinition();

            _handle = FSM_API.Create.CreateInstance(
                "ProcessRateFSM",
                new BenchmarkSupport.DummyContext(),
                BenchmarkSupport.Group);

            _handle.Update(BenchmarkSupport.Group);
        }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark]
        public void InteractionUpdate()
        {
            FSM_API.Interaction.Update(BenchmarkSupport.Group);
        }
    }
}
