using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EngineTask.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class EngineTaskGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource("EngineTask.Attributes.g.cs", AttributeSource.Text));

        var targets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "EngineTask.GenerateMirrorAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => MirrorTarget.AllFromContext(ctx).ToImmutableArray())
            .SelectMany(static (arr, _) => arr);

        context.RegisterSourceOutput(targets, static (spc, target) =>
        {
            foreach (var d in target.Diagnostics)
            {
                if (!Diagnostics.ById.TryGetValue(d.DescriptorId, out var descriptor)) continue;
                var args = new object?[d.Arguments.Length];
                for (var i = 0; i < args.Length; i++) args[i] = d.Arguments[i];
                spc.ReportDiagnostic(Diagnostic.Create(
                    descriptor,
                    d.Location?.ToLocation() ?? Location.None,
                    args));
            }

            spc.AddSource(target.HintName, MirrorEmitter.Emit(target));
        });
    }
}
