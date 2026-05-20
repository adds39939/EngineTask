using System.Collections.Generic;
using System.Linq;
using EngineTask.Generator.CustomFlavours;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EngineTask.Generator;

internal readonly record struct MirrorTarget(
    string SourceNamespace,
    string ClassName,
    string FlavourId,
    string TargetNamespaceSuffix,
    string? NamespaceOverride,
    string ClassSuffix,
    EquatableArray<string> ContainingTypes,
    EquatableArray<string> Usings,
    EquatableArray<MirrorMethod> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public string MirrorNamespace
    {
        get
        {
            if (!string.IsNullOrEmpty(NamespaceOverride)) return NamespaceOverride!;
            return string.IsNullOrEmpty(SourceNamespace)
                ? TargetNamespaceSuffix
                : $"{SourceNamespace}.{TargetNamespaceSuffix}";
        }
    }

    public string MirrorClassName => ClassName + ClassSuffix;

    public string HintName
    {
        get
        {
            var prefix = !string.IsNullOrEmpty(NamespaceOverride)
                ? NamespaceOverride + "."
                : string.IsNullOrEmpty(SourceNamespace) ? string.Empty : SourceNamespace + ".";
            var typePath = ContainingTypes.Length > 0
                ? string.Join(".", ContainingTypes) + "." + MirrorClassName
                : MirrorClassName;
            return $"{prefix}{typePath}.{TargetNamespaceSuffix}.g.cs";
        }
    }

    public static IReadOnlyList<MirrorTarget> AllFromContext(
        GeneratorAttributeSyntaxContext ctx,
        FlavourCatalog catalog)
    {
        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var classSyntax = (ClassDeclarationSyntax)ctx.TargetNode;

        var classLevelDiagnostics = new List<DiagnosticInfo>();
        // ENGTASK002: source class should be partial. Reported once per class
        // even when multiple [GenerateMirror] attributes are present.
        if (!classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            classLevelDiagnostics.Add(DiagnosticInfo.Create(
                Generator.Diagnostics.NonPartialClass.Id,
                classSyntax.Identifier.GetLocation(),
                classSymbol.Name));
        }

        var results = new List<MirrorTarget>(ctx.Attributes.Length);
        var includeClassLevel = true;
        for (var i = 0; i < ctx.Attributes.Length; i++)
        {
            var attr = ctx.Attributes[i];
            var flavourId = ReadFlavourId(attr);
            if (flavourId is null) continue;

            var flavour = MirrorFlavour.Find(flavourId, catalog);
            if (flavour is null)
            {
                // ENGTASK005 — flavour not found in built-ins or catalog
                classLevelDiagnostics.Add(DiagnosticInfo.Create(
                    Generator.Diagnostics.UnknownCustomFlavour.Id,
                    GetAttributeLocation(attr) ?? classSyntax.Identifier.GetLocation(),
                    flavourId));
                continue;
            }

            var namespaceOverride = ReadNamedString(attr, "Namespace");
            var classSuffix = ReadNamedString(attr, "ClassSuffix") ?? string.Empty;

            var target = BuildOne(
                ctx,
                classSymbol,
                classSyntax,
                flavour,
                namespaceOverride,
                classSuffix,
                includeClassLevel ? classLevelDiagnostics : null);
            results.Add(target);
            includeClassLevel = false;
        }

        // Edge case: if no targets were produced (every attribute failed
        // validation), still surface the class-level diagnostics by
        // attaching them to a no-method placeholder. Without this the
        // ENGTASK005/ENGTASK002 wouldn't fire because nothing flows
        // downstream of the transform.
        if (results.Count == 0 && classLevelDiagnostics.Count > 0)
        {
            results.Add(new MirrorTarget(
                string.Empty, string.Empty, string.Empty, string.Empty,
                null, string.Empty,
                EquatableArray<string>.Empty,
                EquatableArray<string>.Empty,
                EquatableArray<MirrorMethod>.Empty,
                new EquatableArray<DiagnosticInfo>(classLevelDiagnostics.ToArray())));
        }

        return results;
    }

    private static string? ReadFlavourId(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0) return null;
        var arg = attr.ConstructorArguments[0];

        if (arg.Value is string customName && !string.IsNullOrEmpty(customName))
            return customName;

        if (arg.Value is int enumValue)
        {
            return enumValue switch
            {
                0 => Flavours.GDTaskFlavour.Id,
                1 => Flavours.UniTaskFlavour.Id,
                _ => null,
            };
        }
        return null;
    }

    private static string? ReadNamedString(AttributeData attr, string name)
    {
        foreach (var pair in attr.NamedArguments)
        {
            if (pair.Key == name && pair.Value.Value is string s && !string.IsNullOrEmpty(s))
                return s;
        }
        return null;
    }

    private static Location? GetAttributeLocation(AttributeData attr) =>
        attr.ApplicationSyntaxReference?.GetSyntax().GetLocation();

    private static MirrorTarget BuildOne(
        GeneratorAttributeSyntaxContext ctx,
        INamedTypeSymbol classSymbol,
        ClassDeclarationSyntax classSyntax,
        MirrorFlavour flavour,
        string? namespaceOverride,
        string classSuffix,
        List<DiagnosticInfo>? classLevelDiagnostics)
    {
        var compilation = ctx.SemanticModel.Compilation;

        var taskType         = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType      = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType    = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        var mirrorIgnoreType = compilation.GetTypeByMetadataName("EngineTask.MirrorIgnoreAttribute");

        var methods = new List<MirrorMethod>();
        var diagnostics = classLevelDiagnostics is not null
            ? new List<DiagnosticInfo>(classLevelDiagnostics)
            : new List<DiagnosticInfo>();

        var sourceNs = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        // Walk outer types (outermost first) so nested classes mirror
        // as `partial class Outer { partial class Inner { ... } }`.
        var containingTypes = new List<string>();
        for (var outer = classSymbol.ContainingType; outer is not null; outer = outer.ContainingType)
            containingTypes.Insert(0, outer.Name);

        var mirrorNs = !string.IsNullOrEmpty(namespaceOverride)
            ? namespaceOverride!
            : string.IsNullOrEmpty(sourceNs)
                ? flavour.TargetNamespaceSuffix
                : $"{sourceNs}.{flavour.TargetNamespaceSuffix}";
        var mirrorClassName = classSymbol.Name + classSuffix;

        // Compose the mirror's metadata name for ENGTASK004 collision
        // lookup. For nested types, outer classes use '+' between them
        // (metadata-name convention).
        var mirrorTypePath = containingTypes.Count > 0
            ? string.Join("+", containingTypes) + "+" + mirrorClassName
            : mirrorClassName;
        var mirrorFullName = string.IsNullOrEmpty(mirrorNs)
            ? mirrorTypePath
            : $"{mirrorNs}.{mirrorTypePath}";
        var existingMirrorType = compilation.GetTypeByMetadataName(mirrorFullName);
        var existingSignatures = new HashSet<(string Name, int Arity)>();
        if (existingMirrorType is not null)
        {
            foreach (var m in existingMirrorType.GetMembers().OfType<IMethodSymbol>())
            {
                if (m.MethodKind != MethodKind.Ordinary) continue;
                existingSignatures.Add((m.Name, m.Parameters.Length));
            }
        }

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            if (method.MethodKind != MethodKind.Ordinary) continue;
            if (HasMirrorIgnore(method, mirrorIgnoreType)) continue;

            if (method.IsAsync && method.ReturnsVoid)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Generator.Diagnostics.AsyncVoidMethod.Id,
                    GetMethodIdentifierLocation(method),
                    method.Name));
                continue;
            }

            if (existingSignatures.Contains((method.Name, method.Parameters.Length)))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Generator.Diagnostics.MirrorMethodCollision.Id,
                    GetMethodIdentifierLocation(method),
                    method.Name,
                    mirrorClassName));
                continue;
            }

            var mirrored = MirrorMethod.TryCreate(
                method,
                ctx.SemanticModel,
                flavour,
                taskType,
                taskOfTType,
                valueTaskType,
                valueTaskOfTType,
                diagnostics);
            if (mirrored.HasValue) methods.Add(mirrored.Value);
        }

        return new MirrorTarget(
            sourceNs,
            classSymbol.Name,
            flavour.Id,
            flavour.TargetNamespaceSuffix,
            namespaceOverride,
            classSuffix,
            new EquatableArray<string>(containingTypes.ToArray()),
            new EquatableArray<string>(ExtractUsings(classSyntax).ToArray()),
            new EquatableArray<MirrorMethod>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static List<string> ExtractUsings(ClassDeclarationSyntax classSyntax)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();

        void Add(UsingDirectiveSyntax u)
        {
            if (u.Alias is null && u.StaticKeyword == default
                && u.Name?.ToString() == "System.Threading.Tasks")
                return;

            var text = u.NormalizeWhitespace().ToFullString().Trim();
            if (seen.Add(text)) result.Add(text);
        }

        var unit = classSyntax.SyntaxTree.GetCompilationUnitRoot();
        foreach (var u in unit.Usings) Add(u);

        SyntaxNode? parent = classSyntax.Parent;
        while (parent is not null)
        {
            if (parent is BaseNamespaceDeclarationSyntax bns)
                foreach (var u in bns.Usings) Add(u);
            parent = parent.Parent;
        }

        return result;
    }

    private static bool HasMirrorIgnore(IMethodSymbol method, INamedTypeSymbol? mirrorIgnoreType)
    {
        if (mirrorIgnoreType is null) return false;
        foreach (var attr in method.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mirrorIgnoreType))
                return true;
        }
        return false;
    }

    private static Location? GetMethodIdentifierLocation(IMethodSymbol method)
    {
        var declRef = method.DeclaringSyntaxReferences.FirstOrDefault();
        if (declRef?.GetSyntax() is MethodDeclarationSyntax mds)
            return mds.Identifier.GetLocation();
        return method.Locations.FirstOrDefault();
    }
}
