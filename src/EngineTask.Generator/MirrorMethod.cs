using System.Linq;
using Microsoft.CodeAnalysis;

namespace EngineTask.Generator;

internal readonly record struct MirrorMethod(string Source)
{
    public static MirrorMethod? TryCreate(
        IMethodSymbol method,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType)
    {
        var returnType = MapReturnType(method.ReturnType, taskType, taskOfTType);
        if (returnType is null) return null;

        var parameters = string.Join(
            ", ",
            method.Parameters.Select(static p =>
                $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}"));

        var accessibility = method.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "private",
        };

        var staticMod = method.IsStatic ? " static" : string.Empty;
        var src =
            $"{accessibility}{staticMod} {returnType} {method.Name}({parameters}) => throw new global::System.NotImplementedException();";
        return new MirrorMethod(src);
    }

    private static string? MapReturnType(
        ITypeSymbol returnType,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType)
    {
        if (returnType is not INamedTypeSymbol named) return null;

        if (taskType is not null && SymbolEqualityComparer.Default.Equals(named, taskType))
            return "global::GodotTask.GDTask";

        if (taskOfTType is not null
            && named.IsGenericType
            && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, taskOfTType))
        {
            var t = named.TypeArguments[0]
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"global::GodotTask.GDTask<{t}>";
        }

        return null;
    }
}
