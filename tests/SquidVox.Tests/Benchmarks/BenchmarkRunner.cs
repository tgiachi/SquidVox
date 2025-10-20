using BenchmarkDotNet.Running;

namespace SquidVox.Tests.Benchmarks;

/// <summary>
/// Runner class for executing benchmarks.
/// To run benchmarks: dotnet run -c Release --project SquidVox.Tests.csproj -- --filter *
/// </summary>
public class BenchmarkRunner
{
    /// <summary>
    /// 
    /// </summary>
    public static void Main(string[] args)
    {
        // Run all benchmarks
        if (args.Length == 0 || args[0] == "--all")
        {
            BenchmarkSwitcher.FromAssembly(typeof(BenchmarkRunner).Assembly).Run(args);
        }
        // Run specific benchmarks
        else if (args[0] == "--collection")
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<SvoxGameObjectCollectionBenchmarks>();
        }
        else if (args[0] == "--comparison")
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<CollectionComparisonBenchmarks>();
        }
        else
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  --all          Run all benchmarks");
            Console.WriteLine("  --collection   Run SvoxGameObjectCollection benchmarks");
            Console.WriteLine("  --comparison   Run comparison benchmarks");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  dotnet run -c Release --project SquidVox.Tests.csproj -- --collection");
        }
    }
}
