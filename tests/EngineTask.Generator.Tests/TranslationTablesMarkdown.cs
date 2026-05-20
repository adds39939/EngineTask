using System.Collections.Generic;
using System.Linq;
using System.Text;
using EngineTask.Generator;
using EngineTask.Generator.Flavours;

namespace EngineTask.Generator.Tests;

// Builds a Markdown rendering of every in-code translation table.
// The test in TranslationTablesDocTests asserts this output matches
// docs/translation-tables.md, so the doc cannot drift from the code.
internal static class TranslationTablesMarkdown
{
    public static string Build()
    {
        var sb = new StringBuilder();
        sb.Append("# EngineTask translation tables\n");
        sb.Append('\n');
        sb.Append("This file is generated from the in-code translation tables — see\n");
        sb.Append("`src/EngineTask.Generator/Flavours/`. The test in\n");
        sb.Append("`tests/EngineTask.Generator.Tests/TranslationTablesDocTests.cs` will\n");
        sb.Append("fail if this file falls out of sync.\n");
        sb.Append('\n');

        AppendFlavour(sb, "GDTask", GDTaskFlavour.Instance);
        AppendFlavour(sb, "UniTask", UniTaskFlavour.Instance);

        return sb.ToString();
    }

    private static void AppendFlavour(StringBuilder sb, string title, MirrorFlavour flavour)
    {
        sb.Append("## ").Append(title).Append('\n');
        sb.Append('\n');
        sb.Append("Mirror namespace: `{SourceNamespace}.").Append(flavour.TargetNamespaceSuffix).Append("`\n");
        sb.Append('\n');

        AppendTable(sb, "Type mappings", flavour.TypeMappings);
        AppendTable(sb, "Member mappings", flavour.MemberMappings);
    }

    private static void AppendTable(StringBuilder sb, string title, IReadOnlyDictionary<string, string> mappings)
    {
        sb.Append("### ").Append(title).Append('\n');
        sb.Append('\n');
        sb.Append("| Source | Target |\n");
        sb.Append("|---|---|\n");
        foreach (var pair in mappings.OrderBy(p => p.Key, System.StringComparer.Ordinal))
        {
            sb.Append("| `").Append(DisplayName(pair.Key)).Append("` | `").Append(pair.Value).Append("` |\n");
        }
        sb.Append('\n');
    }

    // Convert metadata names like `Task`1` into user-facing generic
    // syntax (`Task<T>`). Member keys (no backtick) pass through. This
    // keeps the markdown tables readable when rendered on GitHub —
    // the raw backtick-N form parses as broken code spans.
    private static string DisplayName(string metadataName)
    {
        var idx = metadataName.IndexOf('`');
        if (idx < 0) return metadataName;
        var name = metadataName.Substring(0, idx);
        if (!int.TryParse(metadataName.Substring(idx + 1), out var arity) || arity < 1)
            return metadataName;
        var args = arity == 1
            ? "T"
            : string.Join(", ", System.Linq.Enumerable.Range(1, arity).Select(i => "T" + i));
        return name + "<" + args + ">";
    }
}
