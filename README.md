# EngineTask

A C# source generator that lets you author async code once — against `Task` / `Task<T>` / `ValueTask<T>` — and emits parallel, natively-compiled mirrors targeting engine-specific task-like types: `GDTask` for Godot, `UniTask` for Unity, and (extensibly) others.

The point isn't wrapping. A wrapper would call the `Task` version and convert at the boundary, preserving the original `Task` allocation. EngineTask emits a *separate `async`-method-built state machine* per target — when a consumer in a Godot project awaits the GDTask mirror, the C# compiler builds the state machine using `AsyncGDTaskMethodBuilder` and no `Task` is ever allocated.

## Status

Early development. See [`EngineTask-Plan.md`](EngineTask-Plan.md) for the seven-phase delivery plan.

- [x] **Phase 1** — Walking skeleton: attribute via internal-attribute pattern, GDTask signature swap.
- [x] **Phase 2** — Body rewriting: `MirrorRewriter` translates `Task` / `Task<T>` / `ValueTask` / `ValueTask<T>` / `TaskCompletionSource<T>` and static-factory calls (`Task.Delay`, `Task.WhenAll`, `Task.WhenAny`, `Task.FromResult`, `Task.CompletedTask`, `Task.FromException`, `Task.FromCanceled`). One integration test proves the generated state machine uses `AsyncGDTaskMethodBuilder`.
- [x] **Phase 3** — Diagnostics (`ENGTASK001` unmapped API, `ENGTASK002` non-partial, `ENGTASK003` async void, `ENGTASK004` collision), `[MirrorIgnore]` enforcement, source-using preservation, edge cases (generics, constraints, multi-statement bodies, `CancellationToken`).
- [x] **Phase 4** — UniTask flavour + flavour abstraction. `[GenerateMirror(TaskFlavour.UniTask)]` emits a `.UniTask` mirror; one class can carry both `[GenerateMirror(GDTask)]` and `[GenerateMirror(UniTask)]` and gets two mirrors. End-to-end integration test in `EngineTask.UniTask.Tests` runs against a minimal Cysharp.Threading.Tasks shim; a folder-layout Unity sample documents the manual smoke-test path.
- [x] **Phase 5** — Allocation verification: BenchmarkDotNet project + per-thread allocation regression tests. See [Allocation numbers](#allocation-numbers) below.
- [ ] **Phase 6** — Configuration + niceties + first NuGet release.
- [ ] **Phase 7** — User-defined flavours.

## Quick look

A consumer writes:

```csharp
using System.Threading.Tasks;
using EngineTask;

[GenerateMirror(TaskFlavour.GDTask)]
public partial class Calculator
{
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
}
```

The generator emits, alongside, in namespace `<source-ns>.GDTask`:

```csharp
public partial class Calculator
{
    public global::GodotTask.GDTask<int> AddAsync(int a, int b)
        => global::GodotTask.GDTask.FromResult(a + b);
}
```

Same source, two state machines, two allocation profiles.

## Docs

- [`docs/translation-tables.md`](docs/translation-tables.md) — every type/member mapping for every flavour. Generated from the in-code tables; checked by a unit test so it cannot drift.
- [`docs/cancellation.md`](docs/cancellation.md) — recommended `CancellationToken` patterns for Godot and Unity consumers.

## Repository layout

```
src/
  EngineTask.Generator/            IIncrementalGenerator (netstandard2.0)
    Flavours/                      per-flavour translation tables
samples/
  EngineTask.Sample.Core/          library using [GenerateMirror(GDTask)]
  EngineTask.Sample.Unity/         Unity manual-smoke-test sample (UniTask)
tests/
  EngineTask.Generator.Tests/      Verify.SourceGenerators snapshot tests
  EngineTask.GDTask.Tests/         runtime integration + allocation regression tests
  EngineTask.UniTask.Tests/        runtime integration + allocation regression tests
  EngineTask.UniTask.Shim/         minimal Cysharp.Threading.Tasks stand-in
  EngineTask.Benchmarks/           BenchmarkDotNet allocation measurements
EngineTask-Plan.md                  full project plan
CLAUDE.md                           agent guidelines
```

## Allocation numbers

This is the central claim of the project, expressed in bytes. Numbers from `tests/EngineTask.Benchmarks` running the same `BenchTarget.AddAsync(a, b)` method through three flavour return types, with `a` and `b` set so the sum is outside `AsyncTaskCache.Int32Tasks` (the small-int cache `Task.FromResult` consults).

| Method | Mean | Allocated | Alloc Ratio |
|---|---:|---:|---:|
| `Task_FromResult` (baseline) | 3.79 ns | **72 B** | 1.00 |
| `GDTask_FromResult` | 3.87 ns | **0 B** | 0.00 |
| `UniTask_FromResult` | 0.01 ns | **0 B** | 0.00 |

Captured on .NET 8.0.21, Windows 11, x64 AVX-512, BenchmarkDotNet 0.14.0, ShortRun (warmup 3, iterations 3). UniTask runs against the local Cysharp.Threading.Tasks shim (see `tests/EngineTask.UniTask.Shim/`) — UniTask<T> there is shaped identically to the real package on the synchronously-completed path. The wall-time delta is noise; the `Allocated` column is the load-bearing observation: the source path allocates a Task<int> per call, the mirrors allocate nothing.

The `Allocated == 0` property is also asserted as a regression boundary in `AllocationTests.cs` in each integration test project, so a future change that breaks the no-alloc claim will fail in `dotnet test`, not just in the manual benchmark run.

To reproduce:

```
dotnet run --project tests/EngineTask.Benchmarks -c Release -- --job short --filter "*FromResult*"
```

## Build and test

```
dotnet test EngineTask.slnx
```

## License

[MIT](LICENSE).
