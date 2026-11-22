using System;
using BenchmarkDotNet.Running;

namespace FSM_Benchmark_2_0
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length >= 2 && string.Equals(args[0], "--inventory", StringComparison.OrdinalIgnoreCase))
            {
                PublicApiInventory.Write(args[1]);
                return;
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
