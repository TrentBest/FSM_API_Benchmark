using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures construction and registration costs separately from runtime ticking.
    ///
    /// Iteration setup/cleanup resets the static FSM registry outside the measured
    /// benchmark method so one iteration does not inherit thousands of definitions
    /// from the previous iteration.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_CreationBenchmarks
    {
        [Params(1, 2, 10, 50)]
        public int StateCount { get; set; }

        [IterationSetup]
        public void IterationSetup()
        {
            BenchmarkSupport.Reset();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            BenchmarkSupport.Reset();
        }

        [Benchmark]
        public void BuildDefinition()
        {
            var builder = FSM_API.Create.CreateFiniteStateMachine(
                "CreationBenchmark",
                processRate: 1,
                processingGroup: BenchmarkSupport.Group);

            for (int i = 0; i < StateCount; i++)
            {
                builder.State($"State_{i}", null, null, null);
            }

            builder.WithInitialState("State_0");

            if (StateCount > 1)
            {
                builder.Transition(
                    "State_0",
                    "State_1",
                    static _ => false);
            }

            builder.BuildDefinition();
        }

        [Benchmark]
        public void CreateInstance()
        {
            FSM_API.Create.CreateFiniteStateMachine(
                    "InstanceBenchmark",
                    processRate: 1,
                    processingGroup: BenchmarkSupport.Group)
                .State("State_0", null, null, null)
                .WithInitialState("State_0")
                .BuildDefinition();

            FSM_API.Create.CreateInstance(
                "InstanceBenchmark",
                new BenchmarkSupport.DummyContext(),
                BenchmarkSupport.Group);
        }
    }
}
