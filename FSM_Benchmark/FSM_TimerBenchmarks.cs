using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the timer subsystem independently from FSM execution.
    /// Timer operations are part of the public Interaction surface and should have
    /// explicit performance data just like state/transition operations.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 5, iterationCount: 15, invocationCount: 1)]
    public class FSM_TimerBenchmarks
    {
        private const string FloatKey = "BenchmarkFloatTimer";
        private const string IntKey = "BenchmarkIntTimer";

        [IterationSetup(Target = nameof(AddFloatTimer))]
        public void SetupAddFloat()
        {
            BenchmarkSupport.Reset();
        }

        [IterationSetup(Target = nameof(AddIntTimer))]
        public void SetupAddInt()
        {
            BenchmarkSupport.Reset();
        }

        [IterationSetup(Target = nameof(ResetFloatTimer))]
        public void SetupResetFloat()
        {
            BenchmarkSupport.Reset();
            FSM_API.Interaction.FSMTimers.AddOrSetFloatTimer(FloatKey, 1.0f);
        }

        [IterationSetup(Target = nameof(ResetIntTimer))]
        public void SetupResetInt()
        {
            BenchmarkSupport.Reset();
            FSM_API.Interaction.FSMTimers.AddOrSetIntTimer(IntKey, 1);
        }

        [IterationSetup(Target = nameof(RemoveFloatTimer))]
        public void SetupRemoveFloat()
        {
            BenchmarkSupport.Reset();
            FSM_API.Interaction.FSMTimers.AddOrSetFloatTimer(FloatKey, 1.0f);
        }

        [IterationSetup(Target = nameof(RemoveIntTimer))]
        public void SetupRemoveInt()
        {
            BenchmarkSupport.Reset();
            FSM_API.Interaction.FSMTimers.AddOrSetIntTimer(IntKey, 1);
        }

        [IterationSetup(Target = nameof(UpdateTimers))]
        public void SetupUpdateTimers()
        {
            BenchmarkSupport.Reset();
            FSM_API.Interaction.FSMTimers.AddOrSetFloatTimer(FloatKey, 1.0f);
            FSM_API.Interaction.FSMTimers.AddOrSetIntTimer(IntKey, 1);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            BenchmarkSupport.Reset();
        }

        [Benchmark]
        public void AddFloatTimer()
        {
            FSM_API.Interaction.FSMTimers.AddOrSetFloatTimer(FloatKey, 1.0f);
        }

        [Benchmark]
        public void AddIntTimer()
        {
            FSM_API.Interaction.FSMTimers.AddOrSetIntTimer(IntKey, 1);
        }

        [Benchmark]
        public void ResetFloatTimer()
        {
            FSM_API.Interaction.FSMTimers.ResetFloatTimer(FloatKey, 2.0f);
        }

        [Benchmark]
        public void ResetIntTimer()
        {
            FSM_API.Interaction.FSMTimers.ResetIntTimer(IntKey, 2);
        }

        [Benchmark]
        public void RemoveFloatTimer()
        {
            FSM_API.Interaction.FSMTimers.RemoveFloatTimer(FloatKey);
        }

        [Benchmark]
        public void RemoveIntTimer()
        {
            FSM_API.Interaction.FSMTimers.RemoveIntTimer(IntKey);
        }

        [Benchmark]
        public void UpdateTimers()
        {
            FSM_API.Interaction.FSMTimers.UpdateTimers(0.016f, 1);
        }
    }
}
