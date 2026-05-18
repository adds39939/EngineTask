using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace EngineTask.Generator;

internal sealed class MirrorFlavour
{
    public string Name { get; }
    public string TargetNamespaceSuffix { get; }
    public IReadOnlyDictionary<string, string> TypeMappings { get; }
    public IReadOnlyDictionary<string, string> MemberMappings { get; }

    public MirrorFlavour(
        string name,
        string targetNamespaceSuffix,
        IReadOnlyDictionary<string, string> typeMappings,
        IReadOnlyDictionary<string, string> memberMappings)
    {
        Name = name;
        TargetNamespaceSuffix = targetNamespaceSuffix;
        TypeMappings = typeMappings;
        MemberMappings = memberMappings;
    }

    public string? TryMapType(INamedTypeSymbol type)
    {
        var key = GetTypeKey(type.OriginalDefinition);
        return TypeMappings.TryGetValue(key, out var value) ? value : null;
    }

    public string? TryMapMember(ISymbol member)
    {
        if (member.ContainingType is not { } container) return null;
        var key = $"{GetTypeKey(container.OriginalDefinition)}.{member.Name}";
        return MemberMappings.TryGetValue(key, out var value) ? value : null;
    }

    private static string GetTypeKey(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace is { IsGlobalNamespace: false }
            ? type.ContainingNamespace.ToDisplayString() + "."
            : string.Empty;
        return ns + type.MetadataName;
    }

    public static MirrorFlavour GDTask { get; } = new(
        name: "GDTask",
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
