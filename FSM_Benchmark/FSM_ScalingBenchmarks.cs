using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures how runtime execution scales with the number of live FSM instances
    /// sharing one definition.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_InstanceScalingBenchmarks
    {
        [Params(1, 10, 50, 100, 500, 1000)]
        public int InstanceCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();
            BenchmarkSupport.BuildDefinitions(1, InstanceCount);
        }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark]
        public void Update()
        {
            FSM_API.Interaction.Update(BenchmarkSupport.Group);
        }
    }

    /// <summary>
    /// Measures how one processing-group tick scales with the number of FSM
    /// definitions contained in that group.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_DefinitionScalingBenchmarks
    {
        [Params(1, 10, 50, 100, 500)]
        public int DefinitionCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();
            BenchmarkSupport.BuildDefinitions(DefinitionCount, 1);
        }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark]
        public void Update()
        {
            FSM_API.Interaction.Update(BenchmarkSupport.Group);
        }
    }

    /// <summary>
    /// Measures a registry existence lookup while the number of independent
    /// processing groups changes.
    ///
    /// This is deliberately an API lookup benchmark rather than a tick benchmark.
    /// It answers whether the registry lookup itself changes materially as the
    /// number of groups grows.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_ProcessingGroupLookupScalingBenchmarks
    {
        [Params(1, 10, 50, 100, 500, 1000)]
        public int GroupCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();
            BenchmarkSupport.BuildGroups(GroupCount);
        }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark]
        public bool Exists()
        {
            return FSM_API.Interaction.Exists("FSM_0", "Group_0");
        }
    }
}
