using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures lookup costs independently from FSM execution.
    /// These are intentionally not called "runtime tick costs": they answer
    /// different questions about the public API's registry access patterns.
    /// </summary>
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_LookupBenchmarks
    {
        [Params(8, 32, 128, 512)]
        public int StringLength { get; set; }

        private string _fsmName = null!;
        private string _groupName = null!;

        [GlobalSetup]
        public void Setup()
        {
            BenchmarkSupport.Reset();

            _groupName = BenchmarkSupport.CreateString(StringLength);
            _fsmName = BenchmarkSupport.CreateString(StringLength + 1);

            FSM_API.Create.CreateFiniteStateMachine(
                    _fsmName,
                    processRate: 1,
                    processingGroup: _groupName)
                .State("State_0", null, null, null)
                .WithInitialState("State_0")
                .BuildDefinition();

            FSM_API.Create.CreateInstance(
                _fsmName,
                new BenchmarkSupport.DummyContext(),
                _groupName);
        }

        [GlobalCleanup]
        public void Cleanup() => BenchmarkSupport.Reset();

        [Benchmark]
        public bool PublicExistsLookup()
        {
            return FSM_API.Interaction.Exists(_fsmName, _groupName);
        }

        [Benchmark]
        public FSM_API.Internal.FsmBucket? DirectBucketLookup()
        {
            return FSM_API.Internal.GetBucket(_fsmName, _groupName);
        }
    }
}
