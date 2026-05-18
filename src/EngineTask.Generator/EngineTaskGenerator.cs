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
                transform: static (ctx, _) => MirrorTarget.FromContext(ctx));

        context.RegisterSourceOutput(targets, static (spc, target) =>
            spc.AddSource(target.HintName, MirrorEmitter.Emit(target)));
    }
}
