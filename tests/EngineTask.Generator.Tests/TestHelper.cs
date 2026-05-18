using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    public static Task VerifyMirrorAsync(string source)
    {
        var driver = RunGenerator(source);
        var result = driver.GetRunResult();
        var mirrorTree = result.GeneratedTrees.FirstOrDefault(t =>
            t.FilePath.EndsWith(".GDTask.g.cs", StringComparison.Ordinal)
            && !t.FilePath.Contains(".Attributes."));

        var content = mirrorTree?.ToString() ?? "<no mirror emitted>";
        return Verifier.Verify(content, "cs").UseDirectory("Snapshots");
    }

    public static Task VerifyEntryAsync(string memberSource) =>
        VerifyMirrorAsync($$"""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public partial class C
            {
                {{memberSource}}
            }
            """);

    private static GeneratorDriver RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Sample",
            syntaxTrees: new[] { syntaxTree },
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new EngineTaskGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation);
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
