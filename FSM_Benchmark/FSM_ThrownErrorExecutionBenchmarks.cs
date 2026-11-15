using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Compares a normal update with an update whose user callback throws.
    ///
    /// Each measured invocation starts from a fresh API fixture and a fresh
    /// exception object. One invocation per iteration prevents error-store growth
    /// and exception reuse from changing the measured workload.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 5, iterationCount: 15, invocationCount: 1)]
    public class FSM_ThrownErrorExecutionBenchmarks
    {
        private const string Group = "ErrorExecutionGroup";
        private const string Fsm = "ErrorExecutionFSM";

        private FSMHandle _handle = null!;
        private Exception _exception = null!;

        [IterationSetup(Target = nameof(NormalUpdate))]
        public void SetupNormalUpdate()
        {
            SetupFixture(throwFromCallback: false);
        }

        [IterationSetup(Target = nameof(ThrowingUpdate))]
        public void SetupThrowingUpdate()
        {
            SetupFixture(throwFromCallback: true);
        }

        private void SetupFixture(bool throwFromCallback)
        {
            BenchmarkSupport.Reset();

            FSM_API.Error.InstanceErrorThreshold = int.MaxValue;
            FSM_API.Error.DefinitionErrorThreshold = int.MaxValue;

            _exception = new InvalidOperationException("Benchmark callback failure");

            var builder = FSM_API.Create.CreateFiniteStateMachine(
                Fsm,
                processRate: 1,
                processingGroup: Group);

            if (throwFromCallback)
            {
                builder.State(
                    "State_0",
                    null,
                    _ => throw _exception,
                    null);
            }
            else
            {
                builder.State(
                    "State_0",
                    null,
                    static ctx =>
                    {
                        if (ctx is BenchmarkSupport.DummyContext d)
                            d.Counter++;
                    },
                    null);
            }

            builder.WithInitialState("State_0").BuildDefinition();

            _handle = FSM_API.Create.CreateInstance(
                Fsm,
                new BenchmarkSupport.DummyContext(),
                Group);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            BenchmarkSupport.Reset();
            FSM_API.Error.InstanceErrorThreshold = 5;
            FSM_API.Error.DefinitionErrorThreshold = 3;
        }

        [Benchmark(Baseline = true)]
        public void NormalUpdate()
        {
            _handle.Update(Group);
        }

        [Benchmark]
        public void ThrowingUpdate()
        {
            _handle.Update(Group);
        }
    }
}
