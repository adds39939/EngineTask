# EngineTask — Project Plan

> **Audience:** This document is written for a Claude agent who will implement the project. It assumes Roslyn / source-generator literacy and access to the .NET SDK. Read it end to end before starting Phase 1.

---

## 1. What EngineTask Is

EngineTask is a C# source generator (plus a small runtime/attribute package) that lets a developer author **one** library using standard `Task` / `Task<T>` / `ValueTask<T>` async code, and have the generator emit **parallel, natively-compiled mirrors** of that code targeting engine-specific task-like types — `GDTask` for Godot, `UniTask` for Unity, and (extensibly) others.

The point is not wrapping. A wrapper would call the `Task` version and convert at the boundary, which preserves the underlying `Task` allocation. EngineTask instead emits a *separate `async`-method-built state machine* per target, so when a consumer in a Godot project calls the GDTask mirror, the C# compiler builds the state machine using `AsyncGDTaskMethodBuilder` and no `Task` is ever allocated. Same applies for UniTask via `AsyncUniTaskMethodBuilder`.

### Why this is possible

C# allows `async` methods to return any task-like type that has an `[AsyncMethodBuilder]` attribute. `GDTask`, `UniTask`, and `Task` all qualify. The compiler picks the builder based on the declared return type. So if we emit the same method body with a different return type annotation, we get a different state machine for free — the compiler does the work.

The generator's job is therefore:

1. Detect methods marked for mirroring.
2. Emit a parallel method (in a sibling namespace / partial class) with the return type swapped.
3. Rewrite the *body* so calls to `Task.Delay`, `Task.WhenAll`, etc. become `GDTask.Delay`, `GDTask.WhenAll`, etc.

### Why this doesn't exist yet

The closest existing tool is `Zomp.SyncMethodGenerator`, which does the same mirror-and-rewrite pattern but in the async→sync direction. The Task↔engine-task axis has no published generator. EngineTask fills that gap.

### Non-goals

- EngineTask is **not** an async runtime. It does not implement `IGDTaskSource` or its equivalents. Those come from the engine libraries (`GDTask.Nuget`, `UniTask`).
- EngineTask is **not** a portability layer for engine-specific awaitables. `GDTask.NextFrame()` and `UniTask.Yield()` have no shared abstraction here. If a library body needs frame-aware awaits, it isn't engine-agnostic and EngineTask can't help.
- EngineTask is **not** a runtime conversion utility. `.AsTask()` / `.AsGDTask()` wrappers already exist on the engine libraries; we don't duplicate that.

---

## 2. Architectural Overview

### Repository layout

```
EngineTask/
├─ src/
│  ├─ EngineTask.Attributes/        # netstandard2.0, the [GenerateMirror] attribute + enums
│  ├─ EngineTask.Generator/         # netstandard2.0, the IIncrementalGenerator
│  └─ EngineTask.Generator.Core/    # netstandard2.0, target-agnostic rewriter primitives (optional split, see Phase 2)
├─ tests/
│  ├─ EngineTask.Generator.Tests/   # snapshot tests for the generator output (Verify)
│  ├─ EngineTask.GDTask.Tests/      # integration: real GDTask runtime, real generated code
│  └─ EngineTask.UniTask.Tests/     # integration: real UniTask runtime, real generated code
├─ samples/
│  ├─ EngineTask.Sample.Core/       # library using [GenerateMirror]
│  ├─ EngineTask.Sample.Godot/      # consumes the GDTask mirror
│  └─ EngineTask.Sample.Unity/      # consumes the UniTask mirror
├─ docs/
│  ├─ README.md
│  ├─ extending.md                  # how to add a new task flavour
│  └─ translation-tables.md
├─ build/
└─ EngineTask.sln
```

### Target frameworks

- **Generator and Attributes projects:** `netstandard2.0` — non-negotiable for Roslyn source generators. Use `LangVersion=latest` so we can write modern C# in the generator itself.
- **Test projects:** `net8.0` (or whatever the engine libraries target — check `GDTask.Nuget` and `UniTask` package metadata).
- **Sample projects:** match their engine — Godot 4.x (.NET 8), Unity sample is a `.unitypackage`-style folder layout, not built in CI initially.

### Naming the public surface

- Package IDs: `EngineTask`, `EngineTask.Attributes`. The generator is delivered as analyzer assets inside `EngineTask`; consumers reference one package.
- Attribute: `[GenerateMirror(TaskFlavour.GDTask)]`. Repeatable so a single class can target multiple flavours.
- Generated namespace: configurable, default suffix-based — `MyLib.Core` → `MyLib.GDTask` and `MyLib.UniTask`. Class name preserved. Consumers swap by `using`.

### Key design decisions to lock in early

1. **`IIncrementalGenerator`, not `ISourceGenerator`.** Modern API, IDE-friendly. Do not use the legacy one even for prototyping.
2. **Match types by symbol, rewrite by syntax.** Use `INamedTypeSymbol` and `SymbolEqualityComparer` to identify `System.Threading.Tasks.Task<T>` etc. Never string-match on `"Task"` — users alias.
3. **Strict mode by default.** If the body uses a type / method that isn't in the translation table for the target flavour, emit a diagnostic (`ENGTASK001`-style) and skip that method. Better than silently emitting code that won't compile.
4. **Escape hatch:** `[MirrorIgnore]` on a method skips it. Conditional sections via `#if ENGINETASK_GDTASK` / `#if ENGINETASK_UNITASK` get preserved verbatim per target.
5. **Translation tables live in code, are extensible later.** Phase 1 hardcodes. Phase 4 adds a config mechanism.

---

## 3. Phased Delivery Plan

Each phase ends in a working, testable artifact. Don't move to the next phase until tests pass and a sample consumer compiles.

### Phase 1 — Walking skeleton (single flavour, signature-only)

**Goal:** Prove the pipeline end-to-end with the simplest possible transformation. No body rewriting, no diagnostics, no extensibility — just signature swap for GDTask.

**Deliverables:**

- `EngineTask.Attributes` project containing:
  - `TaskFlavour` enum with one value: `GDTask`. Leave room for `UniTask`, `ValueTask`, `Custom`.
  - `[GenerateMirrorAttribute(TaskFlavour flavour)]` — `AttributeUsage.Class`, `AllowMultiple = true`.
  - `[MirrorIgnoreAttribute]` — `AttributeUsage.Method`.
- `EngineTask.Generator` project:
  - `IIncrementalGenerator` implementation.
  - Pipeline: find classes with `[GenerateMirror(TaskFlavour.GDTask)]` → for each, emit a partial class in `{OriginalNamespace}.GDTask` with the same name.
  - For each method on the source class, emit a stub: same signature with `Task` swapped to `GDTask` and `Task<T>` to `GDTask<T>`, body is `throw new NotImplementedException();`.
  - Use `ForAttributeWithMetadataName` for the syntax provider (Roslyn 4.3+).
- One snapshot test in `EngineTask.Generator.Tests` using `Verify.SourceGenerators`:
  - Input: a small class with one `Task` method and one `Task<int>` method.
  - Verified output: the generated partial class with stubs.
- A `samples/EngineTask.Sample.Core` project that references the generator. Verify in an IDE that the generated namespace exists and the stub methods are visible.

**What "done" looks like:**

The sample project compiles. The generated mirror namespace shows up in IntelliSense. The snapshot test passes. No GDTask runtime is involved yet — we're just emitting type references; the consuming sample doesn't actually call anything.

**Explicitly out of scope for Phase 1:**

- Body rewriting (stubs only).
- UniTask.
- Diagnostics.
- `[MirrorIgnore]` enforcement (declare the attribute, don't act on it yet).
- Configurable namespace.
- Multi-target attribute handling (assume one `[GenerateMirror]` per class).

---

### Phase 2 — Body rewriting (single flavour)

**Goal:** Methods now have real, compilable bodies. The mirror runs.

**Deliverables:**

- A `MirrorRewriter : CSharpSyntaxRewriter` in the generator. Visits the source method body and:
  - Rewrites `Task` → `GDTask`, `Task<T>` → `GDTask<T>` in any type reference (variables, generics, casts).
  - Rewrites static-factory calls per the translation table (see §4).
  - Leaves everything else verbatim.
- Update the generator to invoke the rewriter and emit the rewritten body instead of `throw`.
- Reference the `GDTask` NuGet package (`GDTask.Nuget` — verify exact package name and version during implementation, may have changed) in `EngineTask.Sample.Core`'s GDTask consumer so the rewritten body compiles.
- Snapshot tests for every translation-table entry — one minimal class per entry, verify the rewritten output exactly. Group them logically.
- A first integration test in `EngineTask.GDTask.Tests`:
  - References the generator + sample library.
  - Calls a generated mirror method end-to-end (e.g. an `async GDTask<int>` that does `await GDTask.Delay(10)` and returns 42).
  - Asserts the result.
  - This test proves the generated state machine actually uses `AsyncGDTaskMethodBuilder` and doesn't allocate a `Task`. (Allocation verification comes in Phase 5 — this phase just proves correctness.)

**Translation table for Phase 2 (GDTask):**

| Source (`System.Threading.Tasks`) | Target (`GodotTask`) |
|---|---|
| `Task` | `GDTask` |
| `Task<T>` | `GDTask<T>` |
| `ValueTask` | `GDTask` |
| `ValueTask<T>` | `GDTask<T>` |
| `Task.Delay` | `GDTask.Delay` |
| `Task.WhenAll` | `GDTask.WhenAll` |
| `Task.WhenAny` | `GDTask.WhenAny` |
| `Task.FromResult` | `GDTask.FromResult` |
| `Task.CompletedTask` | `GDTask.CompletedTask` |
| `Task.FromException` | `GDTask.FromException` |
| `Task.FromCanceled` | `GDTask.FromCanceled` |
| `TaskCompletionSource<T>` | `GDTaskCompletionSource<T>` |

Confirm the exact symbol names against the current GDTask package during implementation. Some entries may not exist or may live under different names — log a TODO and skip rather than guessing.

**Out of scope for Phase 2:**

- UniTask. Build the rewriter so flavours are pluggable (translation table passed in, not hardcoded inside the visitor), but ship only GDTask in this phase.
- Diagnostics for untranslatable APIs (silently passthrough for now — known limitation, will be hardened in Phase 3).
- Method-call rewrites beyond static factories (e.g. `myTask.ConfigureAwait(false)` handling — note as known limitation).

---

### Phase 3 — Diagnostics, hardening, escape hatches

**Goal:** Strict mode. Errors instead of silent passthrough. Production-credible.

**Deliverables:**

- Diagnostic descriptors:
  - `ENGTASK001`: Method body uses a Task-related API not in the translation table for the target flavour.
  - `ENGTASK002`: `[GenerateMirror]` applied to a non-partial class.
  - `ENGTASK003`: Method returns `void` (`async void` can't be mirrored cleanly — warn or error, decide based on flavour support).
  - `ENGTASK004`: Method already has a manual definition in the target namespace (collision).
- `[MirrorIgnore]` enforcement: skip these methods entirely.
- Conditional sections:
  - `#if ENGINETASK_GDTASK` / `#if ENGINETASK_UNITASK` / `#if ENGINETASK_SOURCE` blocks are preserved as-is or stripped based on target. Source compilation defines `ENGINETASK_SOURCE`; each generated mirror defines its corresponding symbol.
  - Document this clearly — it's the escape hatch for engine-specific code.
- Handle edge cases:
  - Generic methods.
  - Methods with `where T : ...` constraints.
  - Methods using `CancellationToken` (preserve, don't strip).
  - Methods using `IAsyncEnumerable<T>` — emit a diagnostic and skip for now; GDTask/UniTask have their own async-enumerable types that need explicit mapping (defer to Phase 6).
  - Nested types.
  - Methods with expression bodies (`=>`).
- Expand snapshot test coverage to cover every diagnostic and edge case.

**Out of scope:**

- UniTask (still).
- Configuration files / external translation tables.

---

### Phase 4 — Second flavour (UniTask) + extensibility

**Goal:** Prove the design generalises. UniTask works as a first-class second target. The abstraction that supports two will support N.

**Deliverables:**

- Refactor the translation table into a `TranslationTable` abstraction (if not already done in Phase 2):
  - A `ITargetFlavour` interface or equivalent record type with: flavour name, target namespace template, type mappings, member mappings, diagnostic suppression rules.
  - Built-in flavours: `GDTaskFlavour`, `UniTaskFlavour`. Implement both as separate classes / records inside the generator.
- Add `TaskFlavour.UniTask` to the enum.
- Wire the generator to select the flavour based on the attribute argument.
- `EngineTask.UniTask.Tests` project mirrors the GDTask tests. References the `UniTask` NuGet package (verify exact ID — may be `UniTask` or `Cysharp.UniTask`).
- Snapshot tests duplicated per flavour where the output differs.
- Sample Unity project (a folder with `.cs` files and a `manifest.json`-style setup; doesn't need to build in CI but should be documented).

**UniTask translation table:**

| Source | Target (`Cysharp.Threading.Tasks`) |
|---|---|
| `Task` | `UniTask` |
| `Task<T>` | `UniTask<T>` |
| `ValueTask` | `UniTask` |
| `ValueTask<T>` | `UniTask<T>` |
| `Task.Delay` | `UniTask.Delay` |
| `Task.WhenAll` | `UniTask.WhenAll` |
| `Task.WhenAny` | `UniTask.WhenAny` |
| `Task.FromResult` | `UniTask.FromResult` |
| `Task.CompletedTask` | `UniTask.CompletedTask` |
| `Task.FromException` | `UniTask.FromException` |
| `Task.FromCanceled` | `UniTask.FromCanceled` |
| `TaskCompletionSource<T>` | `UniTaskCompletionSource<T>` |

Confirm against the current `UniTask` package during implementation.

**Out of scope:**

- User-defined flavours via external config.
- Custom rewriting rules beyond the table.

---

### Phase 5 — Allocation verification + perf tests

**Goal:** Empirically prove the central claim — no `Task` allocation when consumers use the engine-task mirror.

**Deliverables:**

- A BenchmarkDotNet project (`tests/EngineTask.Benchmarks/`) measuring:
  - Calling a method that returns `Task<int>` (baseline).
  - Calling the GDTask mirror of the same method.
  - Calling the UniTask mirror of the same method.
  - Memory diagnoser enabled, report allocated bytes per call.
- Expected result: zero or near-zero allocation for the mirror cases (modulo synchronisation primitives). Document the numbers in the README.
- An integration test that uses `GC.GetAllocatedBytesForCurrentThread()` to assert mirror call allocation stays under a threshold. Marks the regression boundary if someone later breaks the no-alloc property.

**Out of scope:**

- Optimising the generator's own compile-time performance (premature).

---

### Phase 6 — Niceties and extension points

**Goal:** Make it pleasant to use and easy to extend.

**Deliverables:**

- Configurable target namespace via attribute argument: `[GenerateMirror(TaskFlavour.GDTask, Namespace = "MyLib.Engine")]`.
- Optional class-name suffix: `[GenerateMirror(TaskFlavour.GDTask, ClassSuffix = "GD")]` → `WorkServiceGD`.
- `IAsyncEnumerable<T>` mapping where the target flavour supports it (UniTask has `IUniTaskAsyncEnumerable<T>`; GDTask check current support).
- Cancellation-token helpers — if a flavour exposes node-lifetime cancellation (e.g. UniTask's destroy token), document the pattern; don't auto-inject.
- A `MarkdownTableGenerator` or doc-extract step that emits `docs/translation-tables.md` from the in-code tables, so docs can't drift.
- README polish, NuGet package metadata, icon, license (MIT).
- First public release: `0.1.0-alpha` on NuGet.

---

### Phase 7 — User-defined flavours

**Goal:** Anyone can plug in a new task-like (e.g. Stride's `MicroThread`, Unity's `Awaitable`, custom in-house types) without forking.

**Deliverables:**

- An `enginetask.json` or `.editorconfig` mechanism (use whichever is more idiomatic for Roslyn analyzers — `AdditionalFiles` + JSON is conventional) where users declare a custom flavour: name, target namespace pattern, type/member translation table.
- The generator picks up custom flavours and treats them on equal footing with built-ins.
- Documentation: `docs/extending.md` walks through adding a flavour, with Unity's new `Awaitable` as a worked example.

---

## 4. Translation Tables — Maintenance Notes

Keep the tables in code, one class per flavour, fields/properties (not dictionaries with string keys — use `INamedTypeSymbol` / `ISymbol` matching).

Suggested shape (illustrative, not prescriptive — the implementer should refine):

```csharp
internal sealed class GDTaskFlavour : ITargetFlavour
{
    public string Name => "GDTask";
    public string TargetNamespaceSuffix => ".GDTask";

    public IReadOnlyDictionary<string, string> TypeMappings { get; } =
        new Dictionary<string, string>
        {
            ["System.Threading.Tasks.Task"] = "GodotTask.GDTask",
            ["System.Threading.Tasks.Task`1"] = "GodotTask.GDTask`1",
            // ...
        };

    public IReadOnlyDictionary<string, string> MemberMappings { get; } =
        new Dictionary<string, string>
        {
            ["System.Threading.Tasks.Task.Delay"] = "GodotTask.GDTask.Delay",
            // ...
        };
}
```

Keys are fully-qualified metadata names (use `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` or metadata-name format consistently). Test the lookup at unit level — symbol equality is subtle.

---

## 5. Testing Strategy

Testing is the spine of this project. Generators that ship without snapshot tests rot in days.

### Snapshot tests (`EngineTask.Generator.Tests`)

- Use `Verify.SourceGenerators` + `Verify.Xunit`.
- One test file per scenario: `SimpleTaskMethod.cs` produces `SimpleTaskMethod.verified.cs`.
- Cover, at minimum: signature swap, body rewrite per translation entry, generics, constraints, expression-bodied methods, `CancellationToken` handling, nested types, every diagnostic, `[MirrorIgnore]`, conditional sections.
- Run on every PR. Snapshot diffs are reviewed manually — they're the spec of what the generator produces.

### Per-flavour integration tests (`EngineTask.GDTask.Tests`, `EngineTask.UniTask.Tests`)

Each is a separate xUnit project that:

- References the EngineTask generator package (or project, in dev).
- References the engine task library (GDTask NuGet / UniTask).
- Contains a small library written with `[GenerateMirror]`.
- Contains tests that call the *generated mirror methods* and assert behavior — return values, cancellation, exception propagation, sequential vs parallel composition.
- Crucially, contains an allocation test (Phase 5) using `GC.GetAllocatedBytesForCurrentThread()` that asserts the mirror is allocation-free.

Why separate projects? Each engine task library has different runtime initialisation requirements (GDTask wants its player-loop runner, UniTask wants its `PlayerLoopHelper`). Isolating them avoids loader weirdness and makes per-flavour CI feasible.

### CI

GitHub Actions, matrix on .NET version. Phases 1–4 run on standard `dotnet test`. Phase 5 benchmark project runs nightly, not per-PR (BenchmarkDotNet is slow).

The Unity sample is not built in CI — Unity in CI is a known pain. Smoke-test it manually before releases. The Godot sample can be CI-built if a Godot CI action is convenient; otherwise also manual.

---

## 6. Implementation Guidance for the Agent

### Start order within Phase 1

1. Solution + project scaffolding. Get `EngineTask.Attributes` compiling first; the generator references it (or duplicates the attribute internally — see below).
2. Decide whether `[GenerateMirror]` is shipped in a separate runtime assembly (`EngineTask.Attributes`) or generated alongside the user's code (the "internal attribute" pattern — generator emits the attribute into the user's compilation). The latter avoids a runtime dependency. **Recommendation: use the internal-attribute pattern.** Lighter for consumers. The generator emits the attribute definition into a `EngineTask.Generated.g.cs` file at the start of each compilation.
3. Build the simplest possible `IIncrementalGenerator` that outputs a hardcoded string when it sees the attribute. Verify the IDE pipeline works.
4. Iterate from there.

### Roslyn gotchas to expect

- **`ForAttributeWithMetadataName`** is the right entry point. It only fires for compilations that reference the attribute, so the internal-attribute pattern requires emitting the attribute via `RegisterPostInitializationOutput` *before* registering the syntax provider.
- **Symbol equality:** always use `SymbolEqualityComparer.Default`. Direct `==` on symbols compiles but is wrong.
- **Generated code must opt out of nullable / analyzers** unless you mean to enforce them. Top of every generated file: `#nullable enable` (or disable) explicitly, and consider `// <auto-generated/>` to suppress IDE warnings.
- **Don't cache `Compilation` or `ISymbol` across incremental steps.** The incremental pipeline relies on equatable data. Project to records of plain primitives in the syntax provider, then materialise in the output stage.
- **Generator authors target `netstandard2.0`** — no `IAsyncEnumerable`, no records, no nullable reference types in the generator code itself unless polyfilled. Add `<LangVersion>latest</LangVersion>` and a `PolySharp` reference if needed for syntactic comforts.

### Reference projects to read

- `Zomp.SyncMethodGenerator` — closest precedent. Read `AsyncToSyncRewriter.cs` carefully; the `CSharpSyntaxRewriter` pattern there is exactly what we need, just pointed at a different axis.
- `Cysharp/UniTask` — read `UniTask.cs` to understand the `AsyncMethodBuilder` integration. We're not reimplementing this; we just need to know what we're producing code that targets.
- `dotnet/roslyn` source-generator cookbook — the canonical reference for incremental-generator patterns.

### Don't yak-shave on

- Custom DSLs, generator-generators, AST transformations beyond the table.
- Optimising generator throughput before it's measurably slow.
- Supporting every conceivable Task API in Phase 2 — start with the table above, add as needed.
- Unity CI. Skip it; document manual test steps.

---

## 7. Definition of Done (for the project as a whole)

- All seven phases complete and tested.
- Published on NuGet as `EngineTask` and `EngineTask.Attributes` (or single package — decide during Phase 6).
- README clearly states the model, constraints (shared subset, no engine-specific awaits in source), and how to extend.
- A worked sample library that ships GDTask and UniTask mirrors, with a small Godot project and a small Unity project consuming it.
- Allocation tests pass (mirror methods allocate zero on the async path beyond what the engine library itself allocates).
- One blog post or README section explaining the technique, for discoverability.

---

## 8. Open Questions to Resolve During Phase 1

These don't need answers before starting, but flag them as decisions to make explicitly during implementation rather than drift into:

1. **One NuGet package or two?** Single `EngineTask` containing both the generator and attributes is simpler for consumers. Split is more conventional. Recommend: single package using the internal-attribute pattern.
2. **`ValueTask` source methods:** treat as equivalent to `Task` for mirroring purposes, or separate? Recommend: equivalent — both map to the flavour's task type. Note in docs.
3. **`async void`:** error or warning? Recommend: error in Phase 3 (`ENGTASK003`). Event handlers shouldn't be in a portable library anyway.
4. **Versioning policy:** lockstep with GDTask/UniTask versions, or independent? Recommend: independent, document compatible ranges.

---

## 9. Quick Reference — What Each Phase Adds

| Phase | Adds | Visible to user |
|---|---|---|
| 1 | Skeleton, attribute, GDTask signature stubs | Generated namespace appears, methods throw |
| 2 | Body rewrite for GDTask | Mirror actually runs |
| 3 | Diagnostics, escape hatches | Errors instead of broken output |
| 4 | UniTask + flavour abstraction | Second engine works |
| 5 | Allocation tests + benchmarks | Proof of the core claim |
| 6 | Configuration + niceties | First public release |
| 7 | User-defined flavours | Anyone can add a target |

Ship each phase. Don't try to skip ahead. Phase 5 in particular is where the project's central claim either lives or dies — don't defer it past where it sits.
