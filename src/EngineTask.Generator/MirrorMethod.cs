using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EngineTask.Generator;

internal readonly record struct MirrorMethod(string Source)
{
    public static MirrorMethod? TryCreate(
        IMethodSymbol method,
        SemanticModel semanticModel,
        MirrorFlavour flavour,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        if (!IsEligibleReturn(method.ReturnType, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
            return null;

        var declRef = method.DeclaringSyntaxReferences.FirstOrDefault();
        if (declRef is null) return null;
        if (declRef.GetSyntax() is not MethodDeclarationSyntax decl) return null;

        var tree = decl.SyntaxTree;
        var model = ReferenceEquals(tree, semanticModel.SyntaxTree)
            ? semanticModel
            : semanticModel.Compilation.GetSemanticModel(tree);

        var source = MirrorRewriter.RewriteMethod(decl, model, flavour);
        return new MirrorMethod(source);
    }

    private static bool IsEligibleReturn(
        ITypeSymbol returnType,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        if (returnType is not INamedTypeSymbol named) return false;
        var def = named.OriginalDefinition;
        return (taskType is not null && SymbolEqualityComparer.Default.Equals(def, taskType))
            || (taskOfTType is not null && SymbolEqualityComparer.Default.Equals(def, taskOfTType))
            || (valueTaskType is not null && SymbolEqualityComparer.Default.Equals(def, valueTaskType))
            || (valueTaskOfTType is not null && SymbolEqualityComparer.Default.Equals(def, valueTaskOfTType));
    }
}
