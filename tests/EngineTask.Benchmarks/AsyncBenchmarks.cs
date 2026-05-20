using BenchmarkDotNet.Attributes;
using EngineTask.Benchmarks.GDTask;
using EngineTask.Benchmarks.UniTask;

namespace EngineTask.Benchmarks;

// Async-keyword variant of FromResultBenchmarks. These exercise the
// state-machine path: the C# compiler picks the AsyncMethodBuilder
// for each return type, builds a struct state machine in Release, and
// (for synchronously-completing awaits) keeps the whole chain on the
// stack.
//
// Carry-over from Phase 5: the UniTask side requires a meaningfully-
// accurate AsyncUniTaskMethodBuilder, which the Phase 7.4 shim now
// supplies. The benchmark proves the resulting state machine doesn't
// allocate on the synchronous path.
[MemoryDiagnoser]
public class AsyncBenchmarks
{
    private readonly AsyncBenchTarget                              _source = new();
    private readonly EngineTask.Benchmarks.GDTask.AsyncBenchTarget _gdMirror = new();
    private readonly EngineTask.Benchmarks.UniTask.AsyncBenchTarget _uniMirror = new();

    private int _a = 12_345;
    private int _b = 67_890;

    [Benchmark(Baseline = true)]
    public System.Threading.Tasks.Task<int> Task_AsyncFromCompletedTask() => _source.AddAsync(_a, _b);

    [Benchmark]
    public global::GodotTask.GDTask<int> GDTask_AsyncFromCompletedTask() => _gdMirror.AddAsync(_a, _b);

    [Benchmark]
    public global::Cysharp.Threading.Tasks.UniTask<int> UniTask_AsyncFromCompletedTask() => _uniMirror.AddAsync(_a, _b);
}
