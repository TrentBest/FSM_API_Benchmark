using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures state-definition cardinality while keeping runtime transition
    /// work at zero. If this remains nearly flat as state count rises, the
    /// benchmark provides evidence that state storage itself is not the dominant
    /// per-tick cost in this execution path.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_StateScalingBenchmarks
    {
        [Params(2, 10, 50, 100, 500)]
        public int StateCount { get; set; }

        private FSMHandle _handle = null!;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();
            _handle = BenchmarkSupport.BuildSingleHandle(
                transitionCount: 0,
                stateCount: StateCount);
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
