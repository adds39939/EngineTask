using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EngineTask.Generator;

internal sealed class MirrorRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly MirrorFlavour _flavour;
    private readonly HashSet<string> _seenUnmapped = new();
    private readonly List<UnmappedApi> _unmapped = new();

    private MirrorRewriter(SemanticModel semanticModel, MirrorFlavour flavour)
    {
        _semanticModel = semanticModel;
        _flavour = flavour;
    }

    public static RewriteResult RewriteMethod(
        MethodDeclarationSyntax decl,
        SemanticModel semanticModel,
        MirrorFlavour flavour)
    {
        var rewriter = new MirrorRewriter(semanticModel, flavour);
        var rewritten = (MethodDeclarationSyntax?)rewriter.Visit(decl);
        var source = rewritten?.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString()
            ?? string.Empty;
        return new RewriteResult(source, new EquatableArray<UnmappedApi>(rewriter._unmapped.ToArray()));
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (node.IsVar) return base.VisitIdentifierName(node);

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

            // Only flag *static* unmapped accesses. Instance accesses on
            // Task-related types (e.g. `tcs.Task` on TaskCompletionSource)
            // often resolve correctly on the rewritten target type
            // (GDTaskCompletionSource here also has a `.Task` property).
            // Flagging them would skip more methods than necessary.
            if (symbol.IsStatic && IsUnmappableTaskMember(symbol))
                RecordUnmapped(symbol.ToDisplayString(), node.GetLocation());
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
            if (IsUnmappableTaskType(named))
                RecordUnmapped(named.ToDisplayString(), typeNode.GetLocation());
        }
        mappedText = string.Empty;
        return false;
    }

    private void RecordUnmapped(string display, Location? location)
    {
        if (_seenUnmapped.Add(display))
            _unmapped.Add(new UnmappedApi(display, LocationInfo.Create(location)));
    }

    private static bool IsUnmappableTaskType(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace?.ToDisplayString();
        if (ns == "System.Threading.Tasks") return true;
        if (ns == "System.Collections.Generic")
        {
            var name = type.MetadataName;
            if (name is "IAsyncEnumerable`1" or "IAsyncEnumerator`1") return true;
        }
        return false;
    }

    private static bool IsUnmappableTaskMember(ISymbol member) =>
        member.ContainingType is INamedTypeSymbol c && IsUnmappableTaskType(c);
}

internal readonly record struct UnmappedApi(string Display, LocationInfo? Location);

internal readonly record struct RewriteResult(string Source, EquatableArray<UnmappedApi> Unmapped);
