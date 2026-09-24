using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    [CPUUsageDiagnoser]
    [MemoryDiagnoser] // Tracks garbage collector allocations per tick
    public class FSM_StringPerformanceBaseline
    {
        private class DummyContext : IStateContext
        {
            public int Counter { get; set; } = 0;
            public bool Toggle { get; set; } = false;
            public bool IsValid { get; set; } = true;
            public string Name { get; set; } = "DummyContext";
        }

        private DummyContext _context;
        private const string FsmName = "BenchmarkFSM";
        private const string ProcessingGroup = "PerfGroup";

        [GlobalSetup]
        public void Setup()
        {
            _context = new DummyContext();

            // Define a multi-state FSM to test string comparison performance during transitions
            if (!FSM_API.Interaction.Exists(FsmName, ProcessingGroup))
            {
                FSM_API.Create.CreateFiniteStateMachine(FsmName, processingGroup: ProcessingGroup, processRate: 1)
                    .State("StateA", OnEnterA, OnUpdateA, null)
                    .State("StateB", OnEnterB, OnUpdateB, null)
                    .WithInitialState("StateA")
                    .Transition("StateA", "StateB", ShouldTransitionToB)
                    .Transition("StateB", "StateA", ShouldTransitionToA)
                    .BuildDefinition();
            }

            FSM_API.Create.CreateInstance(FsmName, _context, ProcessingGroup);
        }

        [Benchmark]
        public void SingleFsmUpdateTick()
        {
            // Ticks the FSM state machine once (evaluating string transitions and delegate callbacks)
            FSM_API.Interaction.Update(ProcessingGroup);
        }

        // --- State Delegates ---
        private static void OnEnterA(IStateContext ctx) { }
        private static void OnUpdateA(IStateContext ctx)
        {
            if (ctx is DummyContext d) d.Counter++;
        }

        private static void OnEnterB(IStateContext ctx) { }
        private static void OnUpdateB(IStateContext ctx)
        {
            if (ctx is DummyContext d) d.Counter++;
        }

        // --- Transition Conditions ---
        private static bool ShouldTransitionToB(IStateContext ctx)
        {
            return ctx is DummyContext d && d.Counter % 2 == 1;
        }

        private static bool ShouldTransitionToA(IStateContext ctx)
        {
            return ctx is DummyContext d && d.Counter % 2 == 0;
        }
    }
}