# Escape hatches and known behaviours

This doc covers two related concerns:

1. How to provide **engine-specific implementations** for methods where the cross-engine subset is too restrictive (`GDTask.NextFrame()`, `MonoBehaviour.GetCancellationTokenOnDestroy()`, etc.).
2. How EngineTask treats **instance-member accesses** on Task-like values (e.g. `task.ConfigureAwait(false)`).

Together they replace what the original plan called the `#if ENGINETASK_*` conditional-sections feature.

## Engine-specific code: hand-roll the mirror method

When a single source method can't be written portably (the body genuinely needs `GDTask.NextFrame()` or `await this.GetCancellationTokenOnDestroy()`), suppress the generated version and write the mirror partial yourself.

**Step 1.** Mark the source method `[MirrorIgnore]` (or rely on the collision detector — see below).

```csharp
// MyLib.Core / WorkService.cs
using System.Threading.Tasks;
using EngineTask;

namespace MyLib.Core;

[GenerateMirror(TaskFlavour.GDTask)]
[GenerateMirror(TaskFlavour.UniTask)]
public partial class WorkService
{
    [MirrorIgnore]
    public Task SetupAsync() => Task.CompletedTask;  // no-op on the source side
}
```

**Step 2.** Provide the mirror method as a hand-written partial in the mirror namespace:

```csharp
// MyLib.Core / WorkService.GDTask.cs
namespace MyLib.Core.GDTask;

public partial class WorkService
{
    public global::GodotTask.GDTask SetupAsync() =>
        global::GodotTask.GDTask.NextFrame();
}
```

```csharp
// MyLib.Core / WorkService.UniTask.cs
namespace MyLib.Core.UniTask;

public partial class WorkService
{
    public global::Cysharp.Threading.Tasks.UniTask SetupAsync() =>
        global::Cysharp.Threading.Tasks.UniTask.NextFrame();
}
```

The generator skips `SetupAsync` because it carries `[MirrorIgnore]`. The hand-written partials are picked up by the mirror namespace as if they were generated.

### Variant: rely on ENGTASK004 instead of `[MirrorIgnore]`

If you'd rather leave the source side as a normal portable method (e.g. for testing), drop the `[MirrorIgnore]` and let the collision detector handle it. The generator detects a hand-written partial with the same name and arity, surfaces `ENGTASK004` as a warning, and skips its own emission — your version wins.

This is identical in behaviour to `[MirrorIgnore]` except that the source-side method is still mirrored to flavours **without** a hand-rolled partial. Useful when you only need an override for one of several flavours.

## Why this replaced `#if ENGINETASK_*` sections

One natural-sounding alternative is per-flavour `#if ENGINETASK_GDTASK` / `#if ENGINETASK_UNITASK` / `#if ENGINETASK_SOURCE` blocks inside the source method. EngineTask deliberately doesn't implement that, because the `Namespace`/`ClassSuffix` attribute overrides plus the `[MirrorIgnore]` + manual-partial pattern documented above cover every use case conditional sections were designed for, without the substantial Roslyn complexity (re-parsing the source with different preprocessor symbols, or running the rewriter over inactive trivia).

If you need genuinely per-flavour code, write it as separate partial files in the mirror namespace — they're explicit, IDE-navigable, and run through the normal compile path.

## Instance-member accesses pass through verbatim

The rewriter rewrites **types** (`Task<int>` → `GDTask<int>`) and **static factories** (`Task.Delay(100)` → `global::GodotTask.GDTask.Delay(100)`). It does **not** rewrite instance-member names:

```csharp
public async Task<int> WorkAsync(Task<int> input)
{
    return await input.ConfigureAwait(false);  // ConfigureAwait stays as-is in the mirror
}
```

In the mirror, `input`'s type becomes `GDTask<int>` (or `UniTask<int>`), and the call becomes `input.ConfigureAwait(false)` against that type. This works because GDTask and UniTask both expose a `ConfigureAwait` method with the same signature shape — the receiver-type change does the heavy lifting.

### What works

| Source instance member | Notes |
|---|---|
| `task.ConfigureAwait(bool)` | GDTask and UniTask both expose `.ConfigureAwait(bool)`. |
| `tcs.SetResult(value)` | `TaskCompletionSource<T>` is mapped at the *type* level to `GDTaskCompletionSource<T>` / `UniTaskCompletionSource<T>`; both expose `.SetResult(T)`. |
| `tcs.Task` | Both flavour-specific completion sources have a `.Task` property returning their own task-like type. The rewriter explicitly does NOT flag instance accesses for ENGTASK001 (only statics). |
| `task.GetAwaiter()` | Standard awaitable shape. |

### What doesn't work — and the workaround

If you call an instance member that the target flavour does NOT expose (or exposes under a different name), the mirror won't compile. There's currently no automatic rename or diagnostic for this case.

**Workaround**: extract the divergent call into its own method on the source class, then use the escape hatch above (`[MirrorIgnore]` + per-flavour hand-rolled partial) to provide engine-specific implementations.

```csharp
[GenerateMirror(TaskFlavour.GDTask)]
[GenerateMirror(TaskFlavour.UniTask)]
public partial class WorkService
{
    public async Task<int> OuterAsync(Task<int> input)
    {
        var configured = await DoConfiguredAwaitAsync(input);
        return configured;
    }

    // Source: just await as normal.
    [MirrorIgnore]
    private Task<int> DoConfiguredAwaitAsync(Task<int> input) => input;
}
```

```csharp
// MyLib.Core / WorkService.GDTask.cs — engine-specific instance member
namespace MyLib.Core.GDTask;

public partial class WorkService
{
    private global::GodotTask.GDTask<int> DoConfiguredAwaitAsync(global::GodotTask.GDTask<int> input)
        => input;  // use GDTask-specific instance API here
}
```

Most users won't hit this — Task/GDTask/UniTask have very compatible instance surfaces. If your library does need it, the pattern is the same as the engine-specific-code escape hatch above.

## Summary

| Concern | Mechanism |
|---|---|
| Per-flavour method body (engine-specific APIs) | `[MirrorIgnore]` + manual partial in mirror namespace |
| Per-flavour override of just one flavour | Manual partial in mirror namespace; ENGTASK004 surfaces the override |
| Cross-flavour instance member with the same name | Just write it — silent passthrough works |
| Cross-flavour instance member that diverges | Extract to a helper method, then escape-hatch the helper |
