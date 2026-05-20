using System;
using System.Collections.Generic;

namespace EngineTask.Generator.Flavours;

public static class GDTaskFlavour
{
    public const string Id = "GDTask";

    public static MirrorFlavour Instance { get; } = new(
        id: Id,
        targetNamespaceSuffix: "GDTask",
        typeMappings: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.Threading.Tasks.Task"]                   = "global::GodotTask.GDTask",
            ["System.Threading.Tasks.Task`1"]                 = "global::GodotTask.GDTask",
            ["System.Threading.Tasks.ValueTask"]              = "global::GodotTask.GDTask",
            ["System.Threading.Tasks.ValueTask`1"]            = "global::GodotTask.GDTask",
            ["System.Threading.Tasks.TaskCompletionSource`1"] = "global::GodotTask.GDTaskCompletionSource",
        },
        memberMappings: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.Threading.Tasks.Task.Delay"]         = "global::GodotTask.GDTask.Delay",
            ["System.Threading.Tasks.Task.WhenAll"]       = "global::GodotTask.GDTask.WhenAll",
            ["System.Threading.Tasks.Task.WhenAny"]       = "global::GodotTask.GDTask.WhenAny",
            ["System.Threading.Tasks.Task.FromResult"]    = "global::GodotTask.GDTask.FromResult",
            ["System.Threading.Tasks.Task.CompletedTask"] = "global::GodotTask.GDTask.CompletedTask",
            ["System.Threading.Tasks.Task.FromException"] = "global::GodotTask.GDTask.FromException",
            ["System.Threading.Tasks.Task.FromCanceled"]  = "global::GodotTask.GDTask.FromCanceled",
        });
}
