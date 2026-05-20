# EngineTask.Sample.Unity

Folder-layout sample showing how to consume the EngineTask UniTask mirror from a Unity project. **This is not built in CI** — Unity in CI is a known pain (see [`EngineTask-Plan.md`](../../EngineTask-Plan.md) §5). Smoke-test it manually before each public release.

## Layout

```
Assets/Scripts/
  WorkService.cs       library author's Task-based code,
                       marked [GenerateMirror(TaskFlavour.UniTask)]
  WorkBehaviour.cs     Unity-side consumer awaiting the UniTask mirror
Packages/manifest.json UniTask installed via Cysharp's UPM git URL
```

## How to wire this up to a real Unity project

1. Create a new Unity 2022.3 LTS (or newer) project, .NET Standard 2.1.
2. Replace `Packages/manifest.json` with the one in this folder (or merge dependencies). Unity pulls UniTask from Cysharp's git URL on first open.
3. Build the EngineTask generator into a NuGet package (Phase 6 work) or, while still pre-release, drop a copy of `EngineTask.Generator.dll` (the `netstandard2.0` build output) into `Assets/Plugins/` with `.asmdef` set to `noEngineReferences = true` and label it a `RoslynAnalyzer` in the inspector.
4. Drop `Assets/Scripts/` from this folder into the Unity project.
5. Open any scene, add an empty GameObject, attach `WorkBehaviour`. Press Play. The console should print `compute => 5` and `sum => 10`.

## What this proves manually

`WorkService` is authored against `System.Threading.Tasks.Task` exactly once. The generator emits a parallel `MyGame.Core.UniTask.WorkService` whose `ComputeAsync` returns `Cysharp.Threading.Tasks.UniTask<int>`. The `await` in `WorkBehaviour.Start` resolves against the UniTask mirror — the C# compiler builds the awaiter chain with `AsyncUniTaskMethodBuilder`, allocating no `Task`.

The corresponding *automated* coverage of "does the generator emit valid UniTask code" lives in `tests/EngineTask.UniTask.Tests/`, which uses a minimal Cysharp.Threading.Tasks shim to run end-to-end without Unity.
