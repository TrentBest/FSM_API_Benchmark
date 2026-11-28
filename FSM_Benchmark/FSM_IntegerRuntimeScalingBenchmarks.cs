using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using TheSingularityWorkshop.FSM_API;
using IntegerFsm = TheSingularityWorkshop.FSM_API.IntegerBacked.FSM;
using IntegerFsmRuntime = TheSingularityWorkshop.FSM_API.IntegerBacked.FSMRuntime;
using IntegerFsmState = TheSingularityWorkshop.FSM_API.IntegerBacked.FSMState;

namespace FSM_Benchmark
{
    /// <summary>
    /// Measures the integer runtime's per-tick cost as one definition owns an increasing
    /// number of live handles. The largest case intentionally reaches one million instances.
    /// </summary>
    /// <remarks>
    /// The runtime and fixture are created outside the measured operation. Initial state entry
    /// is primed and its callback count reset before measurement. This isolates steady-state
    /// scheduler/update cost, including the runtime's tick snapshot allocation.
    /// Stable ID: FSMAPI.Execution.Update.IntegerRuntime.OneDefinition.Population
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("Integer runtime scaling")]
    public class FSM_IntegerRuntimePopulationBenchmarks
    {
        [Params(1, 100, 10000, 100000, 1000000)]
        public int InstanceCount { get; set; }

        private IntegerFsmRuntime _runtime = null!;
        private BenchmarkContext[] _contexts = null!;

        [GlobalSetup]
        public void Setup()
        {
            _runtime = new IntegerFsmRuntime();
            _contexts = new BenchmarkContext[InstanceCount];

            var definition = new IntegerFsm(0, processRate: -1);
            definition.AddState(new IntegerFsmState(
                0,
                null,
                static context => ((BenchmarkContext)context).UpdateCount++,
                null));
            _runtime.Register(definition, processingGroupID: 0);

            for (var i = 0; i < InstanceCount; i++)
            {
                var context = new BenchmarkContext();
                _contexts[i] = context;
                _runtime.CreateInstance(0, context);
            }

            _runtime.Update(0);
            VerifyAndResetContexts();
        }

        [Benchmark]
        [BenchmarkCategory("FSMAPI.Execution.Update.IntegerRuntime.OneDefinition.Population")]
        public int ScheduledTick()
        {
            _runtime.Update(0);
            return _contexts[0].UpdateCount;
        }

        private void VerifyAndResetContexts()
        {
            for (var i = 0; i < _contexts.Length; i++)
            {
                if (_contexts[i].UpdateCount != 1)
                {
                    throw new InvalidOperationException(
                        "Population fixture did not execute exactly one callback for context " + i +
                        "; observed " + _contexts[i].UpdateCount + ".");
                }

                _contexts[i].UpdateCount = 0;
            }
        }
    }

    /// <summary>
    /// Measures the definition-by-instance matrix that exposes scheduler work which
    /// grows with both registered definitions and total live handles.
    /// </summary>
    /// <remarks>
    /// Each definition owns the same number of independent contexts. All definitions share
    /// processing group zero and process every tick. The setup verifies one callback per
    /// context before timing begins.
    /// Stable ID: FSMAPI.Execution.Update.IntegerRuntime.DefinitionPopulationMatrix
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("Integer runtime scaling")]
    public class FSM_IntegerRuntimeDefinitionMatrixBenchmarks
    {
        [Params(1, 10, 50, 100)]
        public int DefinitionCount { get; set; }

        [Params(1, 10, 50, 100)]
        public int InstancesPerDefinition { get; set; }

        private IntegerFsmRuntime _runtime = null!;
        private BenchmarkContext[] _contexts = null!;

        [GlobalSetup]
        public void Setup()
        {
            _runtime = new IntegerFsmRuntime();
            var totalInstances = DefinitionCount * InstancesPerDefinition;
            _contexts = new BenchmarkContext[totalInstances];

            var contextIndex = 0;
            for (var definitionID = 0; definitionID < DefinitionCount; definitionID++)
            {
                var definition = new IntegerFsm(definitionID, processRate: -1);
                definition.AddState(new IntegerFsmState(
                    0,
                    null,
                    static context => ((BenchmarkContext)context).UpdateCount++,
                    null));
                _runtime.Register(definition, processingGroupID: 0);

                for (var instance = 0; instance < InstancesPerDefinition; instance++)
                {
                    var context = new BenchmarkContext();
                    _contexts[contextIndex++] = context;
                    _runtime.CreateInstance(definitionID, context);
                }
            }

            _runtime.Update(0);
            VerifyAndResetContexts();
        }

        [Benchmark]
        [BenchmarkCategory("FSMAPI.Execution.Update.IntegerRuntime.DefinitionPopulationMatrix")]
        public int ScheduledTick()
        {
            _runtime.Update(0);
            return _contexts[0].UpdateCount;
        }

        private void VerifyAndResetContexts()
        {
            for (var i = 0; i < _contexts.Length; i++)
            {
                if (_contexts[i].UpdateCount != 1)
                {
                    throw new InvalidOperationException(
                        "Definition matrix fixture did not execute exactly one callback for context " + i +
                        "; observed " + _contexts[i].UpdateCount + ".");
                }

                _contexts[i].UpdateCount = 0;
            }
        }
    }

    /// <summary>
    /// Measures the cold cost and allocation footprint of registering a definition at
    /// increasingly sparse integer identities.
    /// </summary>
    /// <remarks>
    /// Each invocation creates a fresh runtime so every measurement includes the first
    /// capacity growth required by the selected ID. This is intentionally a registration
    /// benchmark, not a steady-state lookup benchmark.
    /// Stable ID: FSMAPI.Registry.Register.IntegerRuntime.SparseDefinitionID
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("Integer runtime identity storage")]
    public class FSM_IntegerSparseDefinitionIdentityBenchmarks
    {
        [Params(0, 255, 65535, 1048575)]
        public int DefinitionID { get; set; }

        [Benchmark]
        [BenchmarkCategory("FSMAPI.Registry.Register.IntegerRuntime.SparseDefinitionID")]
        public int CreateAndRegisterDefinitionAtID()
        {
            var runtime = new IntegerFsmRuntime();
            var definition = new IntegerFsm(DefinitionID, processRate: -1);
            definition.AddState(new IntegerFsmState(0, null, null, null));
            runtime.Register(definition, processingGroupID: 0);
            return runtime.Contains(DefinitionID) ? DefinitionID : -1;
        }
    }

    /// <summary>
    /// Measures the cold cost and allocation footprint of adding a state at an increasingly
    /// sparse integer identity while retaining direct array-index lookup semantics.
    /// </summary>
    /// <remarks>
    /// Each invocation creates a fresh definition so every measurement includes state-array
    /// growth. This does not measure the hot-path lookup cost after setup.
    /// Stable ID: FSMAPI.Definition.AddState.IntegerRuntime.SparseStateID
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("Integer runtime identity storage")]
    public class FSM_IntegerSparseStateIdentityBenchmarks
    {
        [Params(0, 255, 65535, 1048575)]
        public int StateID { get; set; }

        [Benchmark]
        [BenchmarkCategory("FSMAPI.Definition.AddState.IntegerRuntime.SparseStateID")]
        public int CreateDefinitionAndAddStateAtID()
        {
            var definition = new IntegerFsm(0, processRate: -1);
            definition.AddState(new IntegerFsmState(StateID, null, null, null));
            return definition.GetState(StateID)?.StateID ?? -1;
        }
    }

    internal sealed class BenchmarkContext : IStateContext
    {
        public int UpdateCount { get; set; }
        public bool IsValid { get; set; } = true;
        public string Name { get; set; } = "IntegerRuntimeScaling";
    }
}
