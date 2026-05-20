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

    // Baseline: the original Task<int> path. Task.FromResult allocates a
    // Task<int> reference for values outside the small-int cache, so this
    // is the number to beat.
    [Benchmark(Baseline = true)]
    public System.Threading.Tasks.Task<int> Task_FromResult() => _source.AddAsync(2, 3);

    // The GDTask mirror — generated body is `GDTask.FromResult(a + b)`.
    // GDTask<T> is a readonly struct, so the entire call should be
    // stack-allocated.
    [Benchmark]
    public global::GodotTask.GDTask<int> GDTask_FromResult() => _gdMirror.AddAsync(2, 3);

    // The UniTask mirror — generated body is `UniTask.FromResult(a + b)`.
    // The shim's UniTask<T> is also a readonly struct, so the same no-alloc
    // story applies. The real Cysharp UniTask<T> is shaped identically and
    // gives the same number — see tests/EngineTask.UniTask.Shim/UniTaskShim.cs.
    [Benchmark]
    public global::Cysharp.Threading.Tasks.UniTask<int> UniTask_FromResult() => _uniMirror.AddAsync(2, 3);
}
