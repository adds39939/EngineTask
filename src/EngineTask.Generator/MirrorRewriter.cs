using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EngineTask.Generator;

internal sealed class MirrorRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly MirrorFlavour _flavour;

    private MirrorRewriter(SemanticModel semanticModel, MirrorFlavour flavour)
    {
        _semanticModel = semanticModel;
        _flavour = flavour;
    }

    public static string RewriteMethod(
        MethodDeclarationSyntax decl,
        SemanticModel semanticModel,
        MirrorFlavour flavour)
    {
        var rewriter = new MirrorRewriter(semanticModel, flavour);
        var rewritten = (MethodDeclarationSyntax?)rewriter.Visit(decl);
        return rewritten?.WithoutLeadingTrivia().ToFullString() ?? string.Empty;
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (TryMapType(node, out var mappedText))
            return SyntaxFactory.ParseTypeName(mappedText).WithTriviaFrom(node);
        return base.VisitIdentifierName(node);
    }

    public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
    {
        if (TryMapType(node, out var mappedText))
        {
            var visitedArgs = (TypeArgumentListSyntax)Visit(node.TypeArgumentList)!;
            return SyntaxFactory
                .ParseTypeName(mappedText + visitedArgs.ToFullString())
                .WithTriviaFrom(node);
        }
        return base.VisitGenericName(node);
    }

    public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
    {
        if (TryMapType(node, out var mappedText))
        {
            if (node.Right is GenericNameSyntax generic)
            {
                var visitedArgs = (TypeArgumentListSyntax)Visit(generic.TypeArgumentList)!;
                return SyntaxFactory
                    .ParseTypeName(mappedText + visitedArgs.ToFullString())
                    .WithTriviaFrom(node);
            }
            return SyntaxFactory.ParseTypeName(mappedText).WithTriviaFrom(node);
        }
        return base.VisitQualifiedName(node);
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol is not null)
        {
            var mapped = _flavour.TryMapMember(symbol);
            if (mapped is not null)
                return SyntaxFactory.ParseExpression(mapped).WithTriviaFrom(node);
        }
        return base.VisitMemberAccessExpression(node);
    }

    private bool TryMapType(SyntaxNode typeNode, out string mappedText)
    {
        if (_semanticModel.GetSymbolInfo(typeNode).Symbol is INamedTypeSymbol named)
        {
            var mapped = _flavour.TryMapType(named);
            if (mapped is not null)
            {
                mappedText = mapped;
                return true;
            }
        }
        mappedText = string.Empty;
        return false;
    }
}
