using BenchmarkDotNet.Attributes;
using EngineTask.Benchmarks.GDTask;
using EngineTask.Benchmarks.UniTask;

namespace EngineTask.Benchmarks;

[MemoryDiagnoser]
public class FromResultBenchmarks
{
    private readonly BenchTarget                                  _source = new();
    private readonly EngineTask.Benchmarks.GDTask.BenchTarget     _gdMirror = new();
    private readonly EngineTask.Benchmarks.UniTask.BenchTarget    _uniMirror = new();

    // The inputs are stored in instance fields rather than passed as
    // literals so the JIT can't constant-fold the call away, and the
    // sum (80 235) is well outside .NET's small-int Task<int> cache
    // (`AsyncTaskCache.Int32Tasks`, covering -1..8 inclusive). Without
    // that, Task.FromResult would return a cached singleton and the
    // baseline would lie.
    private int _a = 12_345;
    private int _b = 67_890;

    // Baseline: the original Task<int> path. Task.FromResult on a
    // non-cached int allocates a new Task<int>, so this is the number
    // to beat.
    [Benchmark(Baseline = true)]
    public System.Threading.Tasks.Task<int> Task_FromResult() => _source.AddAsync(_a, _b);

    // The GDTask mirror — generated body is `GDTask.FromResult(a + b)`.
    // GDTask<T> is a readonly struct, so the entire call should be
    // stack-allocated.
    [Benchmark]
    public global::GodotTask.GDTask<int> GDTask_FromResult() => _gdMirror.AddAsync(_a, _b);

    // The UniTask mirror — generated body is `UniTask.FromResult(a + b)`.
    // The shim's UniTask<T> is also a readonly struct, so the same no-alloc
    // story applies. The real Cysharp UniTask<T> is shaped identically and
    // gives the same number — see tests/EngineTask.UniTask.Shim/UniTaskShim.cs.
    [Benchmark]
    public global::Cysharp.Threading.Tasks.UniTask<int> UniTask_FromResult() => _uniMirror.AddAsync(_a, _b);
}
