using BenchmarkDotNet.Running;

namespace FSM_Benchmark_2_0
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
