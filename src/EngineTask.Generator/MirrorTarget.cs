using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace EngineTask.Generator;

internal readonly record struct MirrorTarget(
    string SourceNamespace,
    string ClassName,
    EquatableArray<MirrorMethod> Methods)
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
        var compilation = ctx.SemanticModel.Compilation;
        var flavour = MirrorFlavour.GDTask;

        var taskType         = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType      = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType    = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        var methods = new List<MirrorMethod>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            if (method.MethodKind != MethodKind.Ordinary) continue;

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

        return new MirrorTarget(ns, classSymbol.Name, new EquatableArray<MirrorMethod>(methods.ToArray()));
    }
}
