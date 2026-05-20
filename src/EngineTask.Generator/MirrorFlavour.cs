using System;
using System.Collections.Generic;
using EngineTask.Generator.CustomFlavours;
using Microsoft.CodeAnalysis;

namespace EngineTask.Generator;

// Public to allow the test project to render the in-code translation
// tables as a markdown doc (see TranslationTablesMarkdown.cs in the
// Generator.Tests project). The generator is distributed as an
// analyzer assembly — consumers never directly reference this type.
public sealed class MirrorFlavour
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

    public static MirrorFlavour For(string id) => Find(id, FlavourCatalog.Empty)
        ?? throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown flavour id");

    public static MirrorFlavour? Find(string id, FlavourCatalog catalog)
    {
        if (id == Flavours.GDTaskFlavour.Id)  return Flavours.GDTaskFlavour.Instance;
        if (id == Flavours.UniTaskFlavour.Id) return Flavours.UniTaskFlavour.Instance;
        var custom = catalog.Find(id);
        if (custom.HasValue) return FromCustom(custom.Value);
        return null;
    }

    private static MirrorFlavour FromCustom(CustomFlavourData data)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var m in data.TypeMappings) types[m.From] = m.To;
        var members = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var m in data.MemberMappings) members[m.From] = m.To;
        return new MirrorFlavour(data.Id, data.NamespaceSuffix, types, members);
    }
}
