// Library author writes vanilla Task code, exactly once.
// EngineTask generates `WorkService.UniTask.WorkService` alongside,
// returning Cysharp.Threading.Tasks.UniTask / UniTask<T> instead of Task.
//
// When a Unity consumer awaits the UniTask mirror, the compiler builds
// the state machine with AsyncUniTaskMethodBuilder — no Task allocation
// on the async path.

using System.Threading.Tasks;
using EngineTask;

namespace MyGame.Core;

[GenerateMirror(TaskFlavour.UniTask)]
public partial class WorkService
{
    public Task<int> ComputeAsync(int a, int b) =>
        Task.FromResult(a + b);

    public Task<int> SumAllAsync(int[] values)
    {
        var total = 0;
        foreach (var v in values) total += v;
        return Task.FromResult(total);
    }
}
