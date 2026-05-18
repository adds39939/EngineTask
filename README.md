# EngineTask

A C# source generator that lets you author async code once — against `Task` / `Task<T>` / `ValueTask<T>` — and emits parallel, natively-compiled mirrors targeting engine-specific task-like types: `GDTask` for Godot, `UniTask` for Unity, and (extensibly) others.

The point isn't wrapping. A wrapper would call the `Task` version and convert at the boundary, preserving the original `Task` allocation. EngineTask emits a *separate `async`-method-built state machine* per target — when a consumer in a Godot project awaits the GDTask mirror, the C# compiler builds the state machine using `AsyncGDTaskMethodBuilder` and no `Task` is ever allocated.

## Status

Early development. See [`EngineTask-Plan.md`](EngineTask-Plan.md) for the seven-phase delivery plan.

- [x] **Phase 1** — Walking skeleton: attribute via internal-attribute pattern, GDTask signature swap.
- [x] **Phase 2** — Body rewriting: `MirrorRewriter` translates `Task` / `Task<T>` / `ValueTask` / `ValueTask<T>` / `TaskCompletionSource<T>` and static-factory calls (`Task.Delay`, `Task.WhenAll`, `Task.WhenAny`, `Task.FromResult`, `Task.CompletedTask`, `Task.FromException`, `Task.FromCanceled`). One integration test proves the generated state machine uses `AsyncGDTaskMethodBuilder`.
- [ ] **Phase 3** — Diagnostics, escape hatches, edge cases.
- [ ] **Phase 4** — UniTask + flavour abstraction.
- [ ] **Phase 5** — Allocation verification via BenchmarkDotNet.
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

## Repository layout

```
src/
  EngineTask.Generator/            IIncrementalGenerator (netstandard2.0)
samples/
  EngineTask.Sample.Core/          library using [GenerateMirror]
tests/
  EngineTask.Generator.Tests/      Verify.SourceGenerators snapshot tests
  EngineTask.GDTask.Tests/         runtime integration tests
EngineTask-Plan.md                  full project plan
CLAUDE.md                           agent guidelines
```

## Build and test

```
dotnet test EngineTask.slnx
```

## License

TBD.
