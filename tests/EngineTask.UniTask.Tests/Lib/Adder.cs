using System.Threading.Tasks;
using EngineTask;

namespace EngineTask.IntegrationTests.UniTaskFlavour.Lib;

[GenerateMirror(TaskFlavour.UniTask)]
public partial class Adder
{
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);

    // Async-keyword version — proves the generator emits an
    // `async global::Cysharp.Threading.Tasks.UniTask<int>` method
    // that the compiler builds with AsyncUniTaskMethodBuilder<int>.
    // The benchmark + allocation test in this project assert this
    // path stays zero-alloc on the synchronous-completion side.
    public async Task<int> AddAsync_Async(int a, int b)
    {
        await Task.CompletedTask;
        return a + b;
    }
}
