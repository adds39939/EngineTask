using System;
using System.Collections.Generic;

namespace EngineTask.Generator.Flavours;

internal static class UniTaskFlavour
{
    public const string Id = "UniTask";

    public static MirrorFlavour Instance { get; } = new(
        id: Id,
        targetNamespaceSuffix: "UniTask",
        typeMappings: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.Threading.Tasks.Task"]                       = "global::Cysharp.Threading.Tasks.UniTask",
            ["System.Threading.Tasks.Task`1"]                     = "global::Cysharp.Threading.Tasks.UniTask",
            ["System.Threading.Tasks.ValueTask"]                  = "global::Cysharp.Threading.Tasks.UniTask",
            ["System.Threading.Tasks.ValueTask`1"]                = "global::Cysharp.Threading.Tasks.UniTask",
            ["System.Threading.Tasks.TaskCompletionSource`1"]     = "global::Cysharp.Threading.Tasks.UniTaskCompletionSource",
            // UniTask exposes its own async-enumerable equivalents
            // (Phase 3 deferred this — Phase 6 wires it through). GDTask
            // 3.0.0 has no equivalent, so the GDTask flavour keeps
            // flagging IAsyncEnumerable via ENGTASK001.
            ["System.Collections.Generic.IAsyncEnumerable`1"]     = "global::Cysharp.Threading.Tasks.IUniTaskAsyncEnumerable",
            ["System.Collections.Generic.IAsyncEnumerator`1"]     = "global::Cysharp.Threading.Tasks.IUniTaskAsyncEnumerator",
        },
        memberMappings: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.Threading.Tasks.Task.Delay"]         = "global::Cysharp.Threading.Tasks.UniTask.Delay",
            ["System.Threading.Tasks.Task.WhenAll"]       = "global::Cysharp.Threading.Tasks.UniTask.WhenAll",
            ["System.Threading.Tasks.Task.WhenAny"]       = "global::Cysharp.Threading.Tasks.UniTask.WhenAny",
            ["System.Threading.Tasks.Task.FromResult"]    = "global::Cysharp.Threading.Tasks.UniTask.FromResult",
            ["System.Threading.Tasks.Task.CompletedTask"] = "global::Cysharp.Threading.Tasks.UniTask.CompletedTask",
            ["System.Threading.Tasks.Task.FromException"] = "global::Cysharp.Threading.Tasks.UniTask.FromException",
            ["System.Threading.Tasks.Task.FromCanceled"]  = "global::Cysharp.Threading.Tasks.UniTask.FromCanceled",
        });
}
