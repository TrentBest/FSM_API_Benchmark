using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the cost of transition cardinality.
    ///
    /// Every transition is deliberately false, so the FSM must inspect the
    /// transition collection without taking an early successful branch.
    /// This isolates the "how many transition rules exist?" question.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_TransitionScalingBenchmarks
    {
        [Params(0, 1, 2, 10, 50, 100, 500)]
        public int TransitionCount { get; set; }

        private TheSingularityWorkshop.FSM_API.FSMHandle _handle = null!;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();
            _handle = BenchmarkSupport.BuildSingleHandle(
                transitionCount: TransitionCount,
                // Keep the state population fixed and larger than the maximum
                // transition count so transition cardinality is the changing variable.
                stateCount: 502,
                transitionsAlwaysFalse: true);
        }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark]
        public void HandleUpdate()
        {
            _handle.Update(BenchmarkSupport.Group);
        }
    }
}
