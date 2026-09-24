using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the layers involved in executing one FSM instance.
    ///
    /// These benchmarks deliberately separate:
    ///     FSMHandle.Update()
    ///     FSM_API.Internal.TickAll()
    ///     FSM_API.Interaction.Update()
    ///
    /// The goal is to identify scheduler/wrapper overhead instead of treating
    /// one public Update call as a single indivisible cost.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_ExecutionBenchmarks
    {
        private TheSingularityWorkshop.FSM_API.FSMHandle _handle = null!;
        private Action<string> _directTickAll = null!;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();
            _handle = BenchmarkSupport.BuildSingleHandle(transitionCount: 0);
            _directTickAll = BenchmarkSupport.CreateDirectTickAllDelegate();
        }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark(Baseline = true)]
        public void HandleUpdate()
        {
            _handle.Update(BenchmarkSupport.Group);
        }

        [Benchmark]
        public void DirectTickAll()
        {
            _directTickAll(BenchmarkSupport.Group);
        }

        [Benchmark]
        public void InteractionUpdate()
        {
            TheSingularityWorkshop.FSM_API.FSM_API.Interaction.Update(BenchmarkSupport.Group);
        }

        [Benchmark]
        public void ForcedTransition()
        {
            _handle.TransitionTo("State_1");
            _handle.TransitionTo("State_0");
        }
    }
}
