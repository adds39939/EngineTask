# EngineTask

> Write your async code once against `Task<T>`. Ship allocation-free mirrors targeting `GDTask`, `UniTask`, or any custom task-like.

EngineTask is a C# source generator that takes a class authored in plain `System.Threading.Tasks` and emits, alongside it, parallel implementations that return engine-specific task-likes — `GDTask` for Godot, `UniTask` for Unity, or anything else you can describe in a small JSON file.

## What problem does this solve?

If you have a library you want to use from **both** a Godot project and a Unity project, you face a choice:

- **Stick with `Task`.** Works everywhere, but every async method allocates a `Task<T>` reference on the heap, which is exactly the cost both engines' specialised task-likes were built to avoid.
- **Wrap.** Write a `Task`-based core and convert at the boundary with `.AsTask()` / `.AsUniTask()`. The conversion costs aren't zero, and the original `Task<T>` allocation still happens.
- **Fork the library.** Maintain two copies. Forever.

EngineTask lets you author **one** library and emit **separate `async`-method-built state machines** per target — when a Godot consumer awaits the `GDTask` mirror, the C# compiler builds the state machine with `AsyncGDTaskMethodBuilder` and no `Task` is ever allocated. Same for UniTask.

## Quick example

You write:

```csharp
using System.Threading.Tasks;
using EngineTask;

namespace MyLib;

[GenerateMirror(TaskFlavour.GDTask)]
[GenerateMirror(TaskFlavour.UniTask)]
public partial class Calculator
{
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
}
```

EngineTask emits two mirrors. In `MyLib.GDTask.Calculator.GDTask.g.cs`:

```csharp
namespace MyLib.GDTask
{
    public partial class Calculator
    {
        public global::GodotTask.GDTask<int> AddAsync(int a, int b)
            => global::GodotTask.GDTask.FromResult(a + b);
    }
}
```

And in `MyLib.UniTask.Calculator.UniTask.g.cs`:

```csharp
namespace MyLib.UniTask
{
    public partial class Calculator
    {
        public global::Cysharp.Threading.Tasks.UniTask<int> AddAsync(int a, int b)
            => global::Cysharp.Threading.Tasks.UniTask.FromResult(a + b);
    }
}
```

Same source, three state machines, three allocation profiles.

## Install

```
dotnet add package EngineTask
```

EngineTask ships as a Roslyn analyzer — no runtime dependency, no DLL in your output. The `[GenerateMirror]` and `[MirrorIgnore]` attributes are emitted into your compilation; you don't need a separate attributes package.

You'll also need a runtime reference to the task-like you're mirroring against — `GDTask` from the [Delsin-Yu/GDTask.Nuget](https://github.com/Delsin-Yu/GDTask.Nuget) package for Godot, or [Cysharp/UniTask](https://github.com/Cysharp/UniTask) for Unity.

## How it's allocation-free

C# allows `async` methods to return any task-like type carrying an `[AsyncMethodBuilder]` attribute. `GDTask`, `UniTask`, and `Task` all qualify. The compiler picks the builder based on the declared return type — emit the same method body with a different return-type annotation and you get a different state machine for free.

The generator's job is therefore just:

1. Detect classes with `[GenerateMirror]`.
2. Emit a parallel method per flavour with the return type swapped.
3. Rewrite the body so `Task.Delay`, `Task.WhenAll`, `Task.FromResult` etc. become the flavour-specific equivalents.

The translation tables for the built-in flavours are listed in [`docs/translation-tables.md`](docs/translation-tables.md), generated directly from the in-code tables and snapshot-tested so the docs cannot drift.

## Allocation numbers

The central claim, expressed in bytes per call. Measured on .NET 10.0.0, x64, BenchmarkDotNet 0.15.8.

### Synchronous path — `Task.FromResult(a + b)`

| Method | Mean | Allocated |
|---|---:|---:|
| `Task_FromResult` (baseline) | 3.89 ns | **72 B** |
| `GDTask_FromResult` | 4.03 ns | **0 B** |
| `UniTask_FromResult` | 0.00 ns | **0 B** |

### Async-keyword path — `async Task<int> { await Task.CompletedTask; return a + b; }`

| Method | Mean | Allocated |
|---|---:|---:|
| `Task_AsyncFromCompletedTask` (baseline) | 6.80 ns | **72 B** |
| `GDTask_AsyncFromCompletedTask` | 24.39 ns | **0 B** |
| `UniTask_AsyncFromCompletedTask` | 3.87 ns | **0 B** |

To reproduce:

```
dotnet run --project tests/EngineTask.Benchmarks -c Release -- --job short
```

## Custom flavours

If you target Stride, Unity's new `Awaitable`, or any other task-like, drop an `enginetask.json` into your project as an `AdditionalFile`:

```json
{
  "flavours": [
    {
      "id": "Awaitable",
      "namespaceSuffix": "UnityAwaitable",
      "typeMappings": {
        "System.Threading.Tasks.Task":   "global::UnityEngine.Awaitable",
        "System.Threading.Tasks.Task`1": "global::UnityEngine.Awaitable"
      },
      "memberMappings": {
        "System.Threading.Tasks.Task.FromResult":    "global::UnityEngine.Awaitable.FromResult",
        "System.Threading.Tasks.Task.CompletedTask": "global::UnityEngine.Awaitable.CompletedTask"
      }
    }
  ]
}
```

Then `[GenerateMirror("Awaitable")]` resolves against it at compile time. Full walkthrough in [`docs/extending.md`](docs/extending.md).

## Diagnostics

EngineTask reports six diagnostics:

| Id | Severity | Meaning |
|---|---|---|
| `ENGTASK001` | Warning | Method body uses a Task-related API with no mapping; method skipped |
| `ENGTASK002` | Warning | `[GenerateMirror]` applied to a non-partial class |
| `ENGTASK003` | Error | `async void` cannot be mirrored |
| `ENGTASK004` | Warning | A user-written partial of the mirror already declares this method; generated version skipped |
| `ENGTASK005` | Warning | `[GenerateMirror("X")]` references a flavour not declared in any `enginetask.json` |
| `ENGTASK006` | Warning | An `enginetask.json` is unparseable |

`ENGTASK004` is also the basis for the recommended escape-hatch pattern when you need engine-specific code paths — see [`docs/escape-hatches.md`](docs/escape-hatches.md).

## Documentation

- [`docs/translation-tables.md`](docs/translation-tables.md) — every type/member mapping for every built-in flavour. Generated from the in-code tables.
- [`docs/cancellation.md`](docs/cancellation.md) — recommended `CancellationToken` patterns for Godot and Unity consumers.
- [`docs/extending.md`](docs/extending.md) — adding a custom flavour via `enginetask.json`, with a Unity `Awaitable` worked example.
- [`docs/escape-hatches.md`](docs/escape-hatches.md) — engine-specific code patterns and how instance-member accesses pass through the rewriter.

## Status

Pre-release. Built and tested against .NET 10. The API shape, attribute parameters, and translation tables are likely stable but not formally frozen — semver becomes binding from 1.0.0 onward.

## Building from source

```
git clone https://github.com/adds39939/EngineTask
cd EngineTask
dotnet test EngineTask.slnx
```

The generator project targets `netstandard2.0` (a Roslyn requirement). Every other project targets `net10.0`. CI runs the same `dotnet test` on every push to `main` and every pull request via [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

## Contributing

PRs welcome. The snapshot tests under `tests/EngineTask.Generator.Tests/Snapshots/` are the de-facto spec — reviewing the diff is part of reviewing a change. If you add a translation-table entry or a new diagnostic, the relevant test files will surface it as a snapshot change for you to accept deliberately.

## License

[MIT](LICENSE).
