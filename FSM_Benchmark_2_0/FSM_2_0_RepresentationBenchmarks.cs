using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using StringApi = TheSingularityWorkshop.FSM_API.FsmApi;
using IntegerApi = TheSingularityWorkshop.FSM_API.IntegerBacked.FsmApi;

namespace FSM_Benchmark_2_0
{
    /// <summary>
    /// Measures equivalent no-transition scheduler workloads through the 2.0
    /// string-backed and native integer-backed runtimes.
    /// </summary>
    /// <remarks>
    /// The workload dimensions are definition count and instances per definition.
    /// Both runtimes use ProcessRate=-1, the same callback, the same context type,
    /// dense integer IDs, one processing group, and the same number of update calls.
    /// Setup primes initial-state entry and verifies one callback per instance before
    /// counters are reset for measurement.
    /// </remarks>
    [MemoryDiagnoser]
    public class FSM_2_0_ExecutionBenchmarks
    {
        private const string StringGroup = "BenchmarkGroup";
        private const int IntegerGroup = 0;

        [Params(1, 10, 50)]
        public int DefinitionCount { get; set; }

        [Params(1, 10, 50)]
        public int InstancesPerDefinition { get; set; }

        private readonly List<BenchmarkContext> _stringContexts = new List<BenchmarkContext>();
        private readonly List<BenchmarkContext> _integerContexts = new List<BenchmarkContext>();

        [GlobalSetup]
        public void Setup()
        {
            StringApi.Internal.ResetAPI(hardReset: true);
            BenchmarkRuntimeCleanup.ClearIntegerDefinitions();
            _stringContexts.Clear();
            _integerContexts.Clear();

            for (var definitionID = 0; definitionID < DefinitionCount; definitionID++)
            {
                var stringName = "FSM_" + definitionID;

                StringApi.Create.CreateFiniteStateMachine(
                        stringName,
                        processRate: -1,
                        processingGroup: StringGroup)
                    .State(
                        "State_0",
                        null,
                        static context => ((BenchmarkContext)context).UpdateCount++,
                        null)
                    .WithInitialState("State_0")
                    .BuildDefinition();

                var integerDefinition = IntegerApi.Create.CreateFiniteStateMachine(
                        definitionID,
                        processRate: -1,
                        processingGroupID: IntegerGroup)
                    .State(
                        0,
                        onUpdate: static context => ((BenchmarkContext)context).UpdateCount++)
                    .WithInitialState(0)
                    .BuildDefinition();

                IntegerApi.Create.RegisterDefinition(integerDefinition);

                for (var instanceID = 0; instanceID < InstancesPerDefinition; instanceID++)
                {
                    var stringContext = new BenchmarkContext();
                    StringApi.Create.CreateInstance(stringName, stringContext, StringGroup);
                    _stringContexts.Add(stringContext);

                    var integerContext = new BenchmarkContext();
                    IntegerApi.Create.CreateInstance(definitionID, integerContext);
                    _integerContexts.Add(integerContext);
                }
            }

            // Prime both runtimes so measured updates exclude first-entry behavior.
            StringApi.Interaction.Update(StringGroup);
            IntegerApi.Interaction.Update(IntegerGroup);

            VerifyAndReset(_stringContexts, "string-backed");
            VerifyAndReset(_integerContexts, "integer-backed");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            StringApi.Internal.ResetAPI(hardReset: true);
            BenchmarkRuntimeCleanup.ClearIntegerDefinitions();
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("FSMAPI.Execution.Update.NoTransitions")]
        public void StringBackedUpdate()
        {
            StringApi.Interaction.Update(StringGroup);
        }

        [Benchmark]
        [BenchmarkCategory("FSMAPI.Execution.Update.NoTransitions")]
        public void IntegerBackedUpdate()
        {
            IntegerApi.Interaction.Update(IntegerGroup);
        }

        private void VerifyAndReset(List<BenchmarkContext> contexts, string representation)
        {
            var expected = DefinitionCount * InstancesPerDefinition;
            if (contexts.Count != expected)
            {
                throw new InvalidOperationException(
                    representation + " fixture contains " + contexts.Count +
                    " contexts; expected " + expected + ".");
            }

            for (var i = 0; i < contexts.Count; i++)
            {
                if (contexts[i].UpdateCount != 1)
                {
                    throw new InvalidOperationException(
                        representation + " fixture did not execute exactly one update " +
                        "for context " + i + "; observed " + contexts[i].UpdateCount + ".");
                }

                contexts[i].UpdateCount = 0;
            }
        }

        private sealed class BenchmarkContext : TheSingularityWorkshop.FSM_API.IStateContext
        {
            public string Name { get; set; } = "BenchmarkContext";
            public bool IsValid { get; set; } = true;
            public int UpdateCount { get; set; }
        }
    }

    /// <summary>
    /// Compares successful definition-presence lookups in both 2.0 registries.
    /// </summary>
    [MemoryDiagnoser]
    public class FSM_2_0_LookupHitBenchmarks
    {
        private const string StringGroup = "BenchmarkGroup";
        private const int IntegerGroup = 0;

        [Params(1, 10, 50)]
        public int DefinitionCount { get; set; }

        private string _targetName = null!;
        private int _targetID;

        [GlobalSetup]
        public void Setup()
        {
            StringApi.Internal.ResetAPI(hardReset: true);
            BenchmarkRuntimeCleanup.ClearIntegerDefinitions();
            _targetID = DefinitionCount - 1;
            _targetName = "FSM_" + _targetID;

            for (var definitionID = 0; definitionID < DefinitionCount; definitionID++)
            {
                StringApi.Create.CreateFiniteStateMachine(
                        "FSM_" + definitionID,
                        processRate: -1,
                        processingGroup: StringGroup)
                    .State("State_0", null, null, null)
                    .WithInitialState("State_0")
                    .BuildDefinition();

                var integerDefinition = IntegerApi.Create.CreateFiniteStateMachine(
                        definitionID,
                        processRate: -1,
                        processingGroupID: IntegerGroup)
                    .State(0)
                    .WithInitialState(0)
                    .BuildDefinition();

                IntegerApi.Create.RegisterDefinition(integerDefinition);
            }

            if (!StringApi.Interaction.Exists(_targetName, StringGroup) ||
                !IntegerApi.Interaction.Contains(_targetID))
            {
                throw new InvalidOperationException("Lookup fixtures were not registered as expected.");
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            StringApi.Internal.ResetAPI(hardReset: true);
            BenchmarkRuntimeCleanup.ClearIntegerDefinitions();
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("FSMAPI.Lookup.Definition.ById.Hit")]
        public bool StringBackedLookupHit()
        {
            return StringApi.Interaction.Exists(_targetName, StringGroup);
        }

        [Benchmark]
        [BenchmarkCategory("FSMAPI.Lookup.Definition.ById.Hit")]
        public bool IntegerBackedLookupHit()
        {
            return IntegerApi.Interaction.Contains(_targetID);
        }
    }

    internal static class BenchmarkRuntimeCleanup
    {
        public static void ClearIntegerDefinitions()
        {
            // The public integer facade owns a process-wide runtime. Clear the full
            // parameter range so smaller cases cannot inherit definitions from a prior case.
            for (var definitionID = 0; definitionID < 50; definitionID++)
            {
                IntegerApi.Interaction.Unregister(definitionID);
            }
        }
    }
}
