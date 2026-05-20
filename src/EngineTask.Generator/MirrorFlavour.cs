using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace EngineTask.Generator;

internal sealed class MirrorFlavour
{
    public string Id { get; }
    public string TargetNamespaceSuffix { get; }
    public IReadOnlyDictionary<string, string> TypeMappings { get; }
    public IReadOnlyDictionary<string, string> MemberMappings { get; }

    public MirrorFlavour(
        string id,
        string targetNamespaceSuffix,
        IReadOnlyDictionary<string, string> typeMappings,
        IReadOnlyDictionary<string, string> memberMappings)
    {
        Id = id;
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

    public static MirrorFlavour For(string id) => id switch
    {
        Flavours.GDTaskFlavour.Id   => Flavours.GDTaskFlavour.Instance,
        Flavours.UniTaskFlavour.Id  => Flavours.UniTaskFlavour.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown flavour id"),
    };
}
