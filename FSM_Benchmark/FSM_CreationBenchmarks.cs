using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures definition construction and registration.
    ///
    /// State count is the controlled variable. The benchmark includes the fluent
    /// definition-building work and final BuildDefinition registration because
    /// that is the actual API operation an application performs.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_DefinitionCreationBenchmarks
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
    }

    /// <summary>
    /// Measures the cost of creating a live FSMHandle after the definition already
    /// exists. Definition construction is performed outside the timed method.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_InstanceCreationBenchmarks
    {
        [IterationSetup]
        public void IterationSetup()
        {
            BenchmarkSupport.Reset();

            FSM_API.Create.CreateFiniteStateMachine(
                    "InstanceBenchmark",
                    processRate: 1,
                    processingGroup: BenchmarkSupport.Group)
                .State("State_0", null, null, null)
                .WithInitialState("State_0")
                .BuildDefinition();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            BenchmarkSupport.Reset();
        }

        [Benchmark]
        public void CreateInstance()
        {
            FSM_API.Create.CreateInstance(
                "InstanceBenchmark",
                new BenchmarkSupport.DummyContext(),
                BenchmarkSupport.Group);
        }
    }
}
