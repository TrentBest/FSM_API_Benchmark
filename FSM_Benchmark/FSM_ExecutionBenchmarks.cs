using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the layers involved in executing one FSM instance.
    ///
    /// HandleUpdate is the direct per-instance path.
    /// DirectTickAll reaches the scheduler core without the public Interaction.Update wrapper.
    /// InteractionUpdate is the normal public application-facing path.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_ExecutionBenchmarks
    {
        private FSMHandle _handle = null!;
        private Action<string> _directTickAll = null!;
        private bool _transitionTarget;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();
            _handle = BenchmarkSupport.BuildSingleHandle(transitionCount: 0);
            _directTickAll = BenchmarkSupport.CreateDirectTickAllDelegate();
            // Prime the instance so measured updates represent steady-state execution
            // rather than the one-time OnEnter path.
            _handle.Update(BenchmarkSupport.Group);
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
            FSM_API.Interaction.Update(BenchmarkSupport.Group);
        }

        [Benchmark]
        public void ForcedTransition()
        {
            _transitionTarget = !_transitionTarget;
            _handle.TransitionTo(_transitionTarget ? "State_1" : "State_0");
        }
    }
}
