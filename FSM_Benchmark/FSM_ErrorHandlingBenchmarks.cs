using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the API's explicit error-reporting machinery.
    ///
    /// FSM_API deliberately catches user-code failures and records instance/definition
    /// error state. These benchmarks make that resilience measurable rather than treating
    /// error handling as "free".
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 5, iterationCount: 15, invocationCount: 1)]
    public class FSM_ErrorHandlingBenchmarks
    {
        private const string Group = "ErrorGroup";
        private const string Fsm = "ErrorFSM";

        private FSMHandle _handle = null!;
        private Exception _exception = null!;

        [IterationSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            FSM_API.Error.InstanceErrorThreshold = int.MaxValue;
            FSM_API.Error.DefinitionErrorThreshold = int.MaxValue;

            FSM_API.Create.CreateFiniteStateMachine(
                    Fsm,
                    processRate: 1,
                    processingGroup: Group)
                .State("State_0", null, null, null)
                .WithInitialState("State_0")
                .BuildDefinition();

            _handle = FSM_API.Create.CreateInstance(
                Fsm,
                new BenchmarkSupport.DummyContext(),
                Group);

            _exception = new InvalidOperationException("Benchmark exception");
        }

        [IterationCleanup]
        public void Cleanup()
        {
            BenchmarkSupport.Reset();
            FSM_API.Error.InstanceErrorThreshold = 5;
            FSM_API.Error.DefinitionErrorThreshold = 3;
        }

        [Benchmark]
        public void InvokeInstanceError()
        {
            FSM_API.Error.InvokeInstanceError(
                _handle,
                "Benchmark instance error",
                _exception,
                Group);
        }

        [Benchmark]
        public void InvokeDefinitionError()
        {
            FSM_API.Error.InvokeDefinitionError(
                Fsm,
                "Benchmark definition error");
        }

        [Benchmark]
        public void InvokeInternalApiError()
        {
            FSM_API.Error.InvokeInternalApiError(
                "Benchmark internal API error",
                _exception);
        }

        [Benchmark]
        public Dictionary<FSMHandle, int> GetInstanceErrorCounts()
        {
            return FSM_API.Error.GetErrorCounts();
        }

        [Benchmark]
        public Dictionary<string, int> GetDefinitionErrorCounts()
        {
            return FSM_API.Error.GetDefinitionErrorCounts();
        }

        [Benchmark]
        public void ResetInstanceErrorCount()
        {
            FSM_API.Error.ResetInstanceErrorCount(_handle);
        }

        [Benchmark]
        public void ResetDefinitionErrorCount()
        {
            FSM_API.Error.ResetDefinitionErrorCount(Fsm);
        }

        [Benchmark]
        public void ResetAllErrors()
        {
            FSM_API.Error.Reset();
        }
    }
}
