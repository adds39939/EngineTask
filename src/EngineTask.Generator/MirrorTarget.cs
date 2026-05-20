using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EngineTask.Generator;

internal readonly record struct MirrorTarget(
    string SourceNamespace,
    string ClassName,
    string FlavourId,
    EquatableArray<string> Usings,
    EquatableArray<MirrorMethod> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public MirrorFlavour Flavour => MirrorFlavour.For(FlavourId);

    public string MirrorNamespace =>
        string.IsNullOrEmpty(SourceNamespace)
            ? Flavour.TargetNamespaceSuffix
            : $"{SourceNamespace}.{Flavour.TargetNamespaceSuffix}";

    public string HintName =>
        string.IsNullOrEmpty(SourceNamespace)
            ? $"{ClassName}.{Flavour.TargetNamespaceSuffix}.g.cs"
            : $"{SourceNamespace}.{ClassName}.{Flavour.TargetNamespaceSuffix}.g.cs";

    public static IReadOnlyList<MirrorTarget> AllFromContext(GeneratorAttributeSyntaxContext ctx)
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
        for (var i = 0; i < ctx.Attributes.Length; i++)
        {
            var flavourId = ReadFlavourId(ctx.Attributes[i]);
            if (flavourId is null) continue;

            var target = BuildOne(
                ctx,
                classSymbol,
                classSyntax,
                flavourId,
                // Only the first emitted target carries class-level diagnostics
                // so they are not duplicated when a class has multiple attributes.
                i == 0 ? classLevelDiagnostics : null);
            results.Add(target);
        }

        return results;
    }

    private static string? ReadFlavourId(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0) return null;
        var arg = attr.ConstructorArguments[0];
        if (arg.Value is not int enumValue) return null;
        // Mirror the order in AttributeSource.cs: 0 = GDTask, 1 = UniTask.
        return enumValue switch
        {
            0 => Flavours.GDTaskFlavour.Id,
            1 => Flavours.UniTaskFlavour.Id,
            _ => null,
        };
    }

    private static MirrorTarget BuildOne(
        GeneratorAttributeSyntaxContext ctx,
        INamedTypeSymbol classSymbol,
        ClassDeclarationSyntax classSyntax,
        string flavourId,
        List<DiagnosticInfo>? classLevelDiagnostics)
    {
        var compilation = ctx.SemanticModel.Compilation;
        var flavour = MirrorFlavour.For(flavourId);

        var taskType         = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType      = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType    = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        var mirrorIgnoreType = compilation.GetTypeByMetadataName("EngineTask.MirrorIgnoreAttribute");

        var methods = new List<MirrorMethod>();
        var diagnostics = classLevelDiagnostics is not null
            ? new List<DiagnosticInfo>(classLevelDiagnostics)
            : new List<DiagnosticInfo>();

        // ENGTASK004 prep: enumerate signatures already declared in a
        // user-written partial of the target mirror class.
        var sourceNs = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();
        var mirrorFullName = string.IsNullOrEmpty(sourceNs)
            ? $"{flavour.TargetNamespaceSuffix}.{classSymbol.Name}"
            : $"{sourceNs}.{flavour.TargetNamespaceSuffix}.{classSymbol.Name}";
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

            // ENGTASK003: async void
            if (method.IsAsync && method.ReturnsVoid)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Generator.Diagnostics.AsyncVoidMethod.Id,
                    GetMethodIdentifierLocation(method),
                    method.Name));
                continue;
            }

            // ENGTASK004: collision with user-written partial of the mirror
            if (existingSignatures.Contains((method.Name, method.Parameters.Length)))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Generator.Diagnostics.MirrorMethodCollision.Id,
                    GetMethodIdentifierLocation(method),
                    method.Name,
                    classSymbol.Name));
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
            flavourId,
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
            // The mirror rewrites every Task / Task<T> reference to a fully-
            // qualified flavour-specific name, so this using would just become
            // an unused-using warning in the consumer's compilation.
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
