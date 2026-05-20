using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using VerifyXunit;

namespace EngineTask.Generator.Tests;

internal static class TestHelper
{
    private static readonly MetadataReference[] References = LoadReferences();

    public static Task RunAsync(string source)
    {
        var driver = RunGenerator(source);
        return Verifier.Verify(driver).UseDirectory("Snapshots");
    }

    public static Task VerifyMirrorAsync(string source, string flavourSuffix = "GDTask")
    {
        var driver = RunGenerator(source);
        var result = driver.GetRunResult();
        var endsWith = "." + flavourSuffix + ".g.cs";
        var mirrorTree = result.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.EndsWith(endsWith, StringComparison.Ordinal)
            && !t.FilePath.Contains(".Attributes."));

        var content = mirrorTree?.ToString() ?? "<no mirror emitted>";
        return Verifier.Verify(content, "cs").UseDirectory("Snapshots");
    }

    public static Task VerifyAllMirrorsAsync(string source)
    {
        var driver = RunGenerator(source);
        var result = driver.GetRunResult();
        var trees = result.GeneratedTrees
            .Where(t => t.FilePath.EndsWith(".g.cs", StringComparison.Ordinal)
                && !t.FilePath.Contains(".Attributes."))
            .OrderBy(t => t.FilePath, StringComparer.Ordinal)
            .ToList();

        var sb = new System.Text.StringBuilder();
        foreach (var tree in trees)
        {
            sb.Append("// === ").Append(System.IO.Path.GetFileName(tree.FilePath)).Append(" ===\n");
            sb.Append(tree.ToString());
            sb.Append('\n');
        }

        return Verifier.Verify(sb.ToString(), "cs").UseDirectory("Snapshots");
    }

    public static Task VerifyEntryAsync(string memberSource, string flavour = "GDTask") =>
        VerifyMirrorAsync($$"""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.{{flavour}})]
            public partial class C
            {
                {{memberSource}}
            }
            """, flavour);

    public static Task VerifyWithDiagnosticsAsync(string source)
    {
        var driver = RunGenerator(source);
        var result = driver.GetRunResult();

        var sb = new System.Text.StringBuilder();

        var mirror = result.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.EndsWith(".GDTask.g.cs", StringComparison.Ordinal)
            && !t.FilePath.Contains(".Attributes."));
        if (mirror is not null)
        {
            sb.Append(mirror.ToString());
        }

        if (result.Diagnostics.Length > 0)
        {
            sb.Append('\n');
            sb.Append("// === Diagnostics ===\n");
            foreach (var d in result.Diagnostics.OrderBy(d => d.Id).ThenBy(d => d.GetMessage()))
            {
                sb.Append("// ").Append(d.Severity).Append(": ").Append(d.Id).Append(": ")
                  .Append(d.GetMessage()).Append('\n');
            }
        }

        return Verifier.Verify(sb.ToString(), "cs").UseDirectory("Snapshots");
    }

    public static Task VerifyMirrorWithCatalogAsync(
        string source,
        string catalogJson,
        string flavourSuffix)
    {
        var driver = RunGenerator(source,
            new[] { (AdditionalText)new InMemoryAdditionalText("enginetask.json", catalogJson) });
        var result = driver.GetRunResult();
        var endsWith = "." + flavourSuffix + ".g.cs";
        var mirrorTree = result.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.EndsWith(endsWith, StringComparison.Ordinal)
            && !t.FilePath.Contains(".Attributes."));

        var content = mirrorTree?.ToString() ?? "<no mirror emitted>";
        return Verifier.Verify(content, "cs").UseDirectory("Snapshots");
    }

    private static GeneratorDriver RunGenerator(
        string source,
        IReadOnlyCollection<AdditionalText>? additionalTexts = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Sample",
            syntaxTrees: new[] { syntaxTree },
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new EngineTaskGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            additionalTexts: additionalTexts);
        return driver.RunGenerators(compilation);
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;
        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            _text = SourceText.From(text);
        }
        public override string Path { get; }
        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default) => _text;
    }

    private static MetadataReference[] LoadReferences()
    {
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return trusted
            .Split(Path.PathSeparator)
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToArray();
    }
}
