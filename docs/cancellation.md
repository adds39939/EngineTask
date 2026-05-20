# Cancellation in EngineTask mirrors

Source library code that accepts a `CancellationToken` parameter mirrors as-is — the parameter type is **not** rewritten and the parameter survives every flavour. The recommended pattern:

```csharp
[GenerateMirror(TaskFlavour.GDTask)]
[GenerateMirror(TaskFlavour.UniTask)]
public partial class WorkService
{
    public async Task<int> ComputeAsync(int input, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return input * 2;
    }
}
```

The generated GDTask mirror has the same signature with `Task<int>` rewritten to `global::GodotTask.GDTask<int>` and `Task.Delay` rewritten to `global::GodotTask.GDTask.Delay`. The `CancellationToken` parameter is preserved verbatim. Same for UniTask.

## Where the token comes from on each engine

EngineTask deliberately does **not** auto-inject engine-lifetime tokens — that would couple the source library to a specific engine and defeat the cross-engine premise. Pass them in at the call site instead.

### Godot

GDTask exposes `CancellationTokenSource`-style helpers on its async primitives. A common pattern is to tie cancellation to a `Node`'s lifetime via a `CancellationTokenSource` cancelled in `_ExitTree`:

```csharp
public partial class Player : Node
{
    private readonly CancellationTokenSource _cts = new();
    private readonly WorkService _service = new();

    public override void _ExitTree() => _cts.Cancel();

    public async void OnButtonPressed()
    {
        var result = await _service.ComputeAsync(7, _cts.Token); // GDTask mirror
        GD.Print(result);
    }
}
```

When `Player` leaves the scene tree, the in-flight `ComputeAsync` is cancelled.

### Unity (UniTask)

UniTask provides `MonoBehaviour.GetCancellationTokenOnDestroy()` and `Application.exitCancellationToken` out of the box — pass either directly to the mirror:

```csharp
public class PlayerBehaviour : MonoBehaviour
{
    private readonly WorkService _service = new();

    private async UniTaskVoid Start()
    {
        var result = await _service.ComputeAsync(7, this.GetCancellationTokenOnDestroy()); // UniTask mirror
        Debug.Log(result);
    }
}
```

When the GameObject is destroyed, the in-flight `ComputeAsync` is cancelled.

## Why not auto-inject

The plan deliberately excludes "magic" cancellation insertion (see [`EngineTask-Plan.md`](../EngineTask-Plan.md) §3 Phase 6). Two reasons:

1. **Source-side authoring stays portable.** A source library marked with `[GenerateMirror]` may target any number of flavours, including ones that don't exist yet. The set of "this engine's cancellation idiom" is per-engine and per-version; encoding it in the generator would make adding a new flavour a breaking change for existing libraries.
2. **Explicit is cheap and correct.** A `CancellationToken cancellationToken` parameter is one line at the source-library API boundary, and the call site is the right place to choose which token to pass (node-lifetime, request-lifetime, manual `CancellationTokenSource`, `CancellationToken.None`).

If you find yourself plumbing the same token through many call sites, that's a normal application-level concern — wrap it in your own service.
