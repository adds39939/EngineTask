using System.Threading.Tasks;
using EngineTask;

namespace EngineTask.Benchmarks;

// Single source-of-truth subject for the allocation benchmarks: one
// Task<int> method, mirrored to GDTask<int> and UniTask<int>. The
// benchmark project measures all three flavours of `AddAsync(2, 3)`
// side-by-side, so the central claim ("the mirror does not allocate a
// Task") is a directly-observable diff in the BenchmarkDotNet output.
[GenerateMirror(TaskFlavour.GDTask)]
[GenerateMirror(TaskFlavour.UniTask)]
public partial class BenchTarget
{
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
}
