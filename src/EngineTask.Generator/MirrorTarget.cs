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
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

        var methods = new List<MirrorMethod>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            if (method.MethodKind != MethodKind.Ordinary) continue;

            var mirrored = MirrorMethod.TryCreate(method, taskType, taskOfTType);
            if (mirrored.HasValue) methods.Add(mirrored.Value);
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new MirrorTarget(ns, classSymbol.Name, new EquatableArray<MirrorMethod>(methods.ToArray()));
    }
}
