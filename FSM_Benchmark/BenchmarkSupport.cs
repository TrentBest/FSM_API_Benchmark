using System;
using System.Collections.Generic;
using System.Reflection;
using TheSingularityWorkshop.FSM_API;

namespace FSM_Benchmark
{
    /// <summary>
    /// Shared construction and cleanup helpers used by the FSM API benchmark suite.
    /// Benchmark methods should measure the operation under test, not scenario construction.
    /// </summary>
    internal static class BenchmarkSupport
    {
        public const string Group = "BenchmarkGroup";
        public const string Fsm = "BenchmarkFSM";

        public sealed class DummyContext : IStateContext
        {
            public int Counter { get; set; }
            public bool Toggle { get; set; }
            public bool IsValid { get; set; } = true;
            public string Name { get; set; } = "BenchmarkContext";
        }

        public static void Reset()
        {
            FSM_API.Internal.ResetAPI(hardReset: true);
        }

        public static FSMHandle BuildSingleHandle(
            int transitionCount = 0,
            int stateCount = 2,
            bool transitionsAlwaysFalse = true)
        {
            if (stateCount < 2)
                stateCount = 2;

            var builder = FSM_API.Create.CreateFiniteStateMachine(
                Fsm,
                processRate: 1,
                processingGroup: Group);

            for (int i = 0; i < stateCount; i++)
            {
                string stateName = $"State_{i}";
                builder.State(
                    stateName,
                    onEnter: null,
                    onUpdate: ctx =>
                    {
                        if (ctx is DummyContext d)
                            d.Counter++;
                    },
                    onExit: null);
            }

            builder.WithInitialState("State_0");

            for (int i = 0; i < transitionCount; i++)
            {
                builder.Transition(
                    "State_0",
                    "State_1",
                    transitionsAlwaysFalse
                        ? static _ => false
                        : static ctx => ctx is DummyContext d && d.Counter < 0);
            }

            builder.BuildDefinition();

            return FSM_API.Create.CreateInstance(
                Fsm,
                new DummyContext(),
                Group);
        }

        public static void BuildDefinitions(int definitionCount, int instancesPerDefinition = 1)
        {
            for (int i = 0; i < definitionCount; i++)
            {
                string fsmName = $"FSM_{i}";

                FSM_API.Create.CreateFiniteStateMachine(
                        fsmName,
                        processRate: 1,
                        processingGroup: Group)
                    .State("State_0", null, static ctx =>
                    {
                        if (ctx is DummyContext d)
                            d.Counter++;
                    }, null)
                    .WithInitialState("State_0")
                    .BuildDefinition();

                for (int instance = 0; instance < instancesPerDefinition; instance++)
                {
                    FSM_API.Create.CreateInstance(
                        fsmName,
                        new DummyContext(),
                        Group);
                }
            }
        }

        public static void BuildGroups(int groupCount)
        {
            for (int i = 0; i < groupCount; i++)
            {
                string groupName = $"Group_{i}";
                string fsmName = $"FSM_{i}";

                FSM_API.Create.CreateFiniteStateMachine(
                        fsmName,
                        processRate: 1,
                        processingGroup: groupName)
                    .State("State_0", null, null, null)
                    .WithInitialState("State_0")
                    .BuildDefinition();

                FSM_API.Create.CreateInstance(
                    fsmName,
                    new DummyContext(),
                    groupName);
            }
        }

        public static string CreateString(int length)
        {
            if (length <= 0)
                return string.Empty;

            return new string('X', length);
        }

        /// <summary>
        /// Creates a delegate to the internal TickAll method once during setup.
        /// The reflection cost is therefore excluded from the measured invocation.
        /// This is intentionally an advanced diagnostic benchmark: TickAll is not
        /// part of the public consumer API.
        /// </summary>
        public static Action<string> CreateDirectTickAllDelegate()
        {
            MethodInfo? method = typeof(FSM_API.Internal).GetMethod(
                "TickAll",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (method is null)
                throw new InvalidOperationException(
                    "FSM_API.Internal.TickAll was not found. The benchmark package and source version may be incompatible.");

            return (Action<string>)method.CreateDelegate(typeof(Action<string>));
        }
    }
}
