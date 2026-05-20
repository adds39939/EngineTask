using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace EngineTask.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        // The GDTask NuGet package ships as a non-optimised build, which
        // trips BenchmarkDotNet's OptimizationsValidator. We don't own
        // that dependency and care about the *generated* mirror's
        // behaviour, not GDTask's own micro-optimisations, so the
        // validator is intentionally disabled.
        var config = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
