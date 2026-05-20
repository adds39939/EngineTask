# EngineTask translation tables

This file is generated from the in-code translation tables — see
`src/EngineTask.Generator/Flavours/`. The test in
`tests/EngineTask.Generator.Tests/TranslationTablesDocTests.cs` will
fail if this file falls out of sync.

## GDTask

Mirror namespace: `{SourceNamespace}.GDTask`

### Type mappings

| Source | Target |
|---|---|
| `System.Threading.Tasks.Task` | `global::GodotTask.GDTask` |
| `System.Threading.Tasks.TaskCompletionSource<T>` | `global::GodotTask.GDTaskCompletionSource` |
| `System.Threading.Tasks.Task<T>` | `global::GodotTask.GDTask` |
| `System.Threading.Tasks.ValueTask` | `global::GodotTask.GDTask` |
| `System.Threading.Tasks.ValueTask<T>` | `global::GodotTask.GDTask` |

### Member mappings

| Source | Target |
|---|---|
| `System.Threading.Tasks.Task.CompletedTask` | `global::GodotTask.GDTask.CompletedTask` |
| `System.Threading.Tasks.Task.Delay` | `global::GodotTask.GDTask.Delay` |
| `System.Threading.Tasks.Task.FromCanceled` | `global::GodotTask.GDTask.FromCanceled` |
| `System.Threading.Tasks.Task.FromException` | `global::GodotTask.GDTask.FromException` |
| `System.Threading.Tasks.Task.FromResult` | `global::GodotTask.GDTask.FromResult` |
| `System.Threading.Tasks.Task.WhenAll` | `global::GodotTask.GDTask.WhenAll` |
| `System.Threading.Tasks.Task.WhenAny` | `global::GodotTask.GDTask.WhenAny` |

## UniTask

Mirror namespace: `{SourceNamespace}.UniTask`

### Type mappings

| Source | Target |
|---|---|
| `System.Collections.Generic.IAsyncEnumerable<T>` | `global::Cysharp.Threading.Tasks.IUniTaskAsyncEnumerable` |
| `System.Collections.Generic.IAsyncEnumerator<T>` | `global::Cysharp.Threading.Tasks.IUniTaskAsyncEnumerator` |
| `System.Threading.Tasks.Task` | `global::Cysharp.Threading.Tasks.UniTask` |
| `System.Threading.Tasks.TaskCompletionSource<T>` | `global::Cysharp.Threading.Tasks.UniTaskCompletionSource` |
| `System.Threading.Tasks.Task<T>` | `global::Cysharp.Threading.Tasks.UniTask` |
| `System.Threading.Tasks.ValueTask` | `global::Cysharp.Threading.Tasks.UniTask` |
| `System.Threading.Tasks.ValueTask<T>` | `global::Cysharp.Threading.Tasks.UniTask` |

### Member mappings

| Source | Target |
|---|---|
| `System.Threading.Tasks.Task.CompletedTask` | `global::Cysharp.Threading.Tasks.UniTask.CompletedTask` |
| `System.Threading.Tasks.Task.Delay` | `global::Cysharp.Threading.Tasks.UniTask.Delay` |
| `System.Threading.Tasks.Task.FromCanceled` | `global::Cysharp.Threading.Tasks.UniTask.FromCanceled` |
| `System.Threading.Tasks.Task.FromException` | `global::Cysharp.Threading.Tasks.UniTask.FromException` |
| `System.Threading.Tasks.Task.FromResult` | `global::Cysharp.Threading.Tasks.UniTask.FromResult` |
| `System.Threading.Tasks.Task.WhenAll` | `global::Cysharp.Threading.Tasks.UniTask.WhenAll` |
| `System.Threading.Tasks.Task.WhenAny` | `global::Cysharp.Threading.Tasks.UniTask.WhenAny` |

