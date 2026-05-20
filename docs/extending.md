# Adding a custom flavour to EngineTask

Built-in flavours cover Godot's GDTask and Cysharp's UniTask. To target any other task-like — Stride's `MicroThread`, Unity's new `Awaitable`, an in-house custom builder — declare a flavour in an `enginetask.json` file inside your project. No generator fork required.

## How it works

1. Drop an `enginetask.json` at the root of the project consuming EngineTask.
2. Tell MSBuild it's an `AdditionalFile` so the generator can see it.
3. Use `[GenerateMirror("YourFlavourName")]` (string overload) on the source class.

The generator parses the JSON during compilation, looks up the flavour name, and emits the mirror using your mappings.

## Wire it up

```xml
<!-- in your .csproj -->
<ItemGroup>
  <AdditionalFiles Include="enginetask.json" />
</ItemGroup>
```

## Schema

```json
{
  "flavours": [
    {
      "id": "Awaitable",
      "namespaceSuffix": "UnityAwaitable",
      "typeMappings": {
        "<source-metadata-name>": "<target-text>"
      },
      "memberMappings": {
        "<source-metadata-name>": "<target-text>"
      }
    }
  ]
}
```

Fields:

| Field | Required | What it does |
|---|---|---|
| `id` | yes | The string used in `[GenerateMirror("...")]`. Must be unique across the catalog. |
| `namespaceSuffix` | yes | Appended to the source namespace to form the mirror namespace (e.g. source `MyLib.Core` + suffix `UnityAwaitable` → mirror `MyLib.Core.UnityAwaitable`). |
| `typeMappings` | no | Map a System.Threading.Tasks type (using its **metadata name** — `Task`, `Task\`1`, `TaskCompletionSource\`1`) to a target type. Targets should be fully qualified with `global::`. |
| `memberMappings` | no | Map a static factory (`Task.Delay`, `Task.FromResult`, …) to a target static. Member keys are dotted; values are fully qualified text. |

Both mapping tables can be empty. Unmapped source APIs trigger `ENGTASK001` and the mirror for that method is skipped.

The exact metadata-name format used by the built-in flavours is `docs/translation-tables.md` — refer to it when filling in your own.

## Worked example: Unity Awaitable

`UnityEngine.Awaitable` is Unity's built-in async-method-builder type, available from Unity 2023.1 onwards. It mirrors `Task` shape-wise but allocates differently. A consumer wiring it up would write:

```json
{
  "flavours": [
    {
      "id": "Awaitable",
      "namespaceSuffix": "UnityAwaitable",
      "typeMappings": {
        "System.Threading.Tasks.Task":          "global::UnityEngine.Awaitable",
        "System.Threading.Tasks.Task`1":        "global::UnityEngine.Awaitable",
        "System.Threading.Tasks.ValueTask":     "global::UnityEngine.Awaitable",
        "System.Threading.Tasks.ValueTask`1":   "global::UnityEngine.Awaitable"
      },
      "memberMappings": {
        "System.Threading.Tasks.Task.FromResult":    "global::UnityEngine.Awaitable.FromResult",
        "System.Threading.Tasks.Task.CompletedTask": "global::UnityEngine.Awaitable.CompletedTask"
      }
    }
  ]
}
```

```csharp
using System.Threading.Tasks;
using EngineTask;

namespace MyLib.Core;

[GenerateMirror("Awaitable")]
public partial class WorkService
{
    public Task<int> ComputeAsync(int a, int b) => Task.FromResult(a + b);
}
```

The generator emits, alongside, in namespace `MyLib.Core.UnityAwaitable`:

```csharp
public partial class WorkService
{
    public global::UnityEngine.Awaitable<int> ComputeAsync(int a, int b)
        => global::UnityEngine.Awaitable.FromResult(a + b);
}
```

The mirror lives next to the source library and can be referenced directly from Unity code.

## Multiple flavours, one class

The same `[GenerateMirror]` rules from Phase 4 apply to custom flavours — a class can carry any mix of built-in and custom flavour attributes:

```csharp
[GenerateMirror(TaskFlavour.GDTask)]
[GenerateMirror(TaskFlavour.UniTask)]
[GenerateMirror("Awaitable")]
public partial class WorkService { ... }
```

Each attribute produces its own mirror, in its own namespace, with its own translation table applied.

## Errors

| Diagnostic | When |
|---|---|
| `ENGTASK005` | `[GenerateMirror("X")]` references a flavour that no `enginetask.json` declares. |
| `ENGTASK006` | An `enginetask.json` is unparseable (the parser error message is included). |

## Where the built-ins live

If you want to crib from a working example, the in-source tables for the built-ins are in [`src/EngineTask.Generator/Flavours/`](../src/EngineTask.Generator/Flavours/). The format inside those files is the C# equivalent of the JSON schema above.
