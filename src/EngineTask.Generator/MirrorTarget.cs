using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EngineTask.Generator;

internal readonly record struct MirrorTarget(
    string SourceNamespace,
    string ClassName,
    EquatableArray<MirrorMethod> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public string MirrorNamespace =>
        string.IsNullOrEmpty(SourceNamespace) ? "GDTask" : $"{SourceNamespace}.GDTask";

    public string HintName =>
        string.IsNullOrEmpty(SourceNamespace)
            ? $"{ClassName}.GDTask.g.cs"
            : $"{SourceNamespace}.{ClassName}.GDTask.g.cs";

    public static MirrorTarget FromContext(GeneratorAttributeSyntaxContext ctx)
    {
        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var classSyntax = (ClassDeclarationSyntax)ctx.TargetNode;
        var compilation = ctx.SemanticModel.Compilation;
        var flavour = MirrorFlavour.GDTask;

        var taskType         = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType      = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType    = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        var mirrorIgnoreType = compilation.GetTypeByMetadataName("EngineTask.MirrorIgnoreAttribute");

        var methods = new List<MirrorMethod>();
        var diagnostics = new List<DiagnosticInfo>();

        // ENGTASK002: source class should be partial
        if (!classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Generator.Diagnostics.NonPartialClass.Id,
                classSyntax.Identifier.GetLocation(),
                classSymbol.Name));
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

            var mirrored = MirrorMethod.TryCreate(
                method,
                ctx.SemanticModel,
                flavour,
                taskType,
                taskOfTType,
                valueTaskType,
                valueTaskOfTType);
            if (mirrored.HasValue) methods.Add(mirrored.Value);
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new MirrorTarget(
            ns,
            classSymbol.Name,
            new EquatableArray<MirrorMethod>(methods.ToArray()),
            new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
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
