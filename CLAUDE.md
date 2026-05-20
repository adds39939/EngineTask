# EngineTask — agent guidelines

For AI assistants working in this repo. Read before making changes.

## What this project is

EngineTask is a C# source generator that emits parallel, allocation-free mirrors of `Task`-based async code targeting engine-specific task-likes — `GDTask` for Godot, `UniTask` for Unity, and user-defined flavours declared via `enginetask.json`. The central technical claim is **zero `Task` allocation on the mirror path**; that claim is verified by BenchmarkDotNet in `tests/EngineTask.Benchmarks/` and asserted as a regression test in each flavour integration project.

The user-facing description lives in `README.md`; the user-facing extension and configuration docs live under `docs/`.

## Non-negotiable constraints

- `src/EngineTask.Generator/` targets **netstandard2.0**. Don't bump it — Roslyn analyzers load there.
- The generator is an `IIncrementalGenerator`. Don't use the legacy `ISourceGenerator`.
- Match types by **symbol identity** via `SymbolEqualityComparer.Default`. Never string-match on type names — consumers alias.
- The incremental pipeline must carry only **equatable** data. Use `EquatableArray<T>` for collections, `record struct`s with value-typed fields elsewhere. The single exception is `EngineTaskGenerator.ContextHolder`, a deliberate non-equatable wrapper used to defer transform-time work until after the AdditionalFiles catalog is `Combine`d in — and that costs the transform-stage cache.
- Diagnostic IDs `ENGTASK001`–`ENGTASK006` are catalogued in `src/EngineTask.Generator/AnalyzerReleases.Shipped.md` under "Release 0.1". Adding a new diagnostic requires an entry in `AnalyzerReleases.Unshipped.md`; the `RS2008` analyzer enforces this at build time.

## Workflow

- After any change in `src/EngineTask.Generator/`: run `dotnet test tests/EngineTask.Generator.Tests/` and review every snapshot diff manually. Verify writes a `.received.cs` next to each `.verified.cs`; the diff between them IS the spec change. Don't blindly promote — read what changed.
- After any change touching the rewriter, the flavour translation tables, or `MirrorTarget.AllFromContext`: also run the GDTask and UniTask integration test projects.
- The allocation tests in `tests/EngineTask.GDTask.Tests/AllocationTests.cs` and `tests/EngineTask.UniTask.Tests/AllocationTests.cs` are the regression boundary for the central no-alloc claim. The async case is gated on `#if !DEBUG` because the C# compiler emits async state machines as classes in Debug builds.
- Commit per logical unit of work with a clear message.

## What to flag (not silently change)

Stop and confirm with the user before:

- Adding a built-in translation-table entry. Every new mapping is a public-API decision and a snapshot regression — talk through the change first.
- Changing attribute shape — `[GenerateMirror]` constructor signature, named parameters, or the `TaskFlavour` enum.
- Changing the default mirror-namespace convention (`{SourceNamespace}.{TargetSuffix}`) or the hint-name shape. Roslyn keys generated source by hint name; renaming breaks downstream tooling.
- Touching `src/EngineTask.Generator/EngineTask.Generator.csproj` packaging metadata (`PackageId`, license expression, README path, `analyzers/dotnet/cs` packaging directive).

## Repository layout

```
src/EngineTask.Generator/        IIncrementalGenerator (netstandard2.0)
  Flavours/                      per-flavour translation tables
  CustomFlavours/                user-defined flavours (enginetask.json + parser)
  AnalyzerReleases.*.md          RS2008 analyzer release tracking
samples/                         sample consumers (GDTask + Unity manual)
tests/
  EngineTask.Generator.Tests/    Verify snapshot tests
  EngineTask.GDTask.Tests/       runtime integration + allocation
  EngineTask.UniTask.Tests/      runtime integration + allocation
  EngineTask.Benchmarks/         BenchmarkDotNet allocation measurements
docs/                            user-facing documentation
.github/workflows/               CI + release automation
```

## Tooling

- The generator targets `netstandard2.0`. Every other project targets **`net10.0`**.
- LF line endings throughout (`.gitattributes`).
- CI runs `dotnet test -c Release` on every push to main and every PR.
- Releases are tag-driven (`v*`) via `.github/workflows/release.yml`; this is a user-initiated step.
