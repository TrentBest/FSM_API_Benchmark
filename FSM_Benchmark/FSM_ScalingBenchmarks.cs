using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures how execution cost changes as the amount of runtime work changes.
    ///
    /// Each benchmark changes one dimension while keeping the others intentionally
    /// simple. This lets us distinguish "more work exists" from "the scheduler itself
    /// became more expensive."
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_ScalingBenchmarks
    {
        [Params(1, 10, 50, 100, 500)]
        public int Count { get; set; }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark]
        public void InstancesPerDefinition()
        {
            BenchmarkSupport.Reset();
            BenchmarkSupport.BuildDefinitions(1, Count);
            FSM_API.Interaction.Update(BenchmarkSupport.Group);
        }

        [Benchmark]
        public void DefinitionsPerGroup()
        {
            BenchmarkSupport.Reset();
            BenchmarkSupport.BuildDefinitions(Count, 1);
            FSM_API.Interaction.Update(BenchmarkSupport.Group);
        }

        [Benchmark]
        public void ProcessingGroupsExistenceLookup()
        {
            BenchmarkSupport.Reset();
            BenchmarkSupport.BuildGroups(Count);
            FSM_API.Interaction.Exists("FSM_0", "Group_0");
        }
    }
}
