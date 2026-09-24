using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Compares the normal FSMHandle execution path with an actual user-callback
    /// exception that is caught by FSM_API's resilience machinery.
    ///
    /// The exception object is created during setup so the benchmark primarily
    /// measures the API's catch/report/count path rather than measuring exception
    /// object construction.
    /// </summary>
    [MemoryDiagnoser]
    public class FSM_ThrownErrorExecutionBenchmarks
    {
        private const string NormalGroup = "NormalErrorGroup";
        private const string ThrowingGroup = "ThrowingErrorGroup";

        private FSMHandle _normalHandle = null!;
        private FSMHandle _throwingHandle = null!;
        private Exception _exception = null!;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            FSM_API.Error.InstanceErrorThreshold = int.MaxValue;
            FSM_API.Error.DefinitionErrorThreshold = int.MaxValue;

            _exception = new InvalidOperationException("Benchmark callback failure");

            FSM_API.Create.CreateFiniteStateMachine(
                    "NormalErrorFSM",
                    processRate: 1,
                    processingGroup: NormalGroup)
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

            FSM_API.Create.CreateFiniteStateMachine(
                    "ThrowingErrorFSM",
                    processRate: 1,
                    processingGroup: ThrowingGroup)
                .State(
                    "State_0",
                    null,
                    ctx => throw _exception,
                    null)
                .WithInitialState("State_0")
                .BuildDefinition();

            _normalHandle = FSM_API.Create.CreateInstance(
                "NormalErrorFSM",
                new BenchmarkSupport.DummyContext(),
                NormalGroup);

            _throwingHandle = FSM_API.Create.CreateInstance(
                "ThrowingErrorFSM",
                new BenchmarkSupport.DummyContext(),
                ThrowingGroup);

            _normalHandle.Update(NormalGroup);
            _throwingHandle.Update(ThrowingGroup);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            BenchmarkSupport.Reset();
            FSM_API.Error.InstanceErrorThreshold = 5;
            FSM_API.Error.DefinitionErrorThreshold = 3;
        }

        [Benchmark(Baseline = true)]
        public void NormalUpdate()
        {
            _normalHandle.Update(NormalGroup);
        }

        [Benchmark]
        public void ThrowingUpdate()
        {
            _throwingHandle.Update(ThrowingGroup);
        }
    }
}
