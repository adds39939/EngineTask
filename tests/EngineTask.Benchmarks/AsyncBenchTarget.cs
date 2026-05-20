using System.Threading.Tasks;
using EngineTask;

namespace EngineTask.Benchmarks;

// Async-keyword counterpart of BenchTarget. The mirror generators emit
// `async global::GodotTask.GDTask<int>` and `async global::Cysharp...
// .UniTask<int>` versions; in each case the C# compiler builds the
// state machine using the flavour's AsyncMethodBuilder.
[GenerateMirror(TaskFlavour.GDTask)]
[GenerateMirror(TaskFlavour.UniTask)]
public partial class AsyncBenchTarget
{
    public async Task<int> AddAsync(int a, int b)
    {
        await Task.CompletedTask;
        return a + b;
    }
}
