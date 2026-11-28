// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using System;
using BenchmarkDotNet.Running;

namespace FSM_Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1 && string.Equals(args[0], "--validate", StringComparison.Ordinal))
            {
                FSM_IntegerStringComparisonBenchmark.ValidateEquivalentExecution();
                return;
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
