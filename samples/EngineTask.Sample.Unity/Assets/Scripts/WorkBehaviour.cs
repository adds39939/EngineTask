// A Unity-side consumer of the UniTask mirror. The using brings the
// generated mirror namespace (MyGame.Core.UniTask) into scope so the
// type name `WorkService` here is the *UniTask flavour*, not the source
// Task version. The C# compiler builds the awaiter chain against
// Cysharp.Threading.Tasks.UniTask — no Task allocation on this path.

using Cysharp.Threading.Tasks;
using MyGame.Core.UniTask;
using UnityEngine;

namespace MyGame.Unity;

public class WorkBehaviour : MonoBehaviour
{
    private readonly WorkService _work = new();

    private async UniTaskVoid Start()
    {
        var result = await _work.ComputeAsync(2, 3);
        Debug.Log($"compute => {result}");

        var sum = await _work.SumAllAsync(new[] { 1, 2, 3, 4 });
        Debug.Log($"sum => {sum}");
    }
}
