using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class FSM_StringGroupIterationBenchmark
    {
        private class DummyContext : IStateContext
        {
            public int Counter { get; set; } = 0;
            public bool IsValid { get; set; } = true;
            public string Name { get; set; } = "DummyContext";
        }

        // The Params attribute tells BenchmarkDotNet to run the same benchmark multiple times, 
        // injecting these different values to see how your API's performance scales under load.
        [Params(1, 10, 50)]
        public int ActiveProcessGroups { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Register multiple isolated process groups to force string-key lookups
            for (int i = 0; i < 50; i++)
            {
                string groupName = $"Group_{i}";
                string fsmName = $"FSM_{i}";

                if (!FSM_API.Interaction.Exists(fsmName, groupName))
                {
                    FSM_API.Create.CreateFiniteStateMachine(fsmName, processingGroup: groupName, processRate: 1)
                        .State("StateA", null, ctx => { if (ctx is DummyContext d) d.Counter++; }, null)
                        .State("StateB", null, null, null)
                        .WithInitialState("StateA")
                        .Transition("StateA", "StateB", ctx => ctx is DummyContext d && d.Counter > 1000)
                        .BuildDefinition();
                }

                FSM_API.Create.CreateInstance(fsmName, new DummyContext(), groupName);
            }
        }

        [Benchmark]
        public void UpdateMultipleGroups()
        {
            // Forces the API to hash and look up strings repeatedly per tick
            for (int i = 0; i < ActiveProcessGroups; i++)
            {
                FSM_API.Interaction.Update($"Group_{i}");
            }
        }
    }
}