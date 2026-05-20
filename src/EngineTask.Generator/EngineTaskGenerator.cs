using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using EngineTask.Generator.CustomFlavours;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EngineTask.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class EngineTaskGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource("EngineTask.Attributes.g.cs", AttributeSource.Text));

        // Parse every enginetask.json AdditionalFile and merge into one
        // catalog. Errors are collected, not thrown, so a malformed
        // config surfaces as ENGTASK006 without breaking generation.
        var catalogProvider = context.AdditionalTextsProvider
            .Where(static t => IsCatalogFile(t.Path))
            .Select(static (t, ct) => ParseCatalogFile(t, ct))
            .Collect()
            .Select(static (results, _) => CombineCatalogs(results));

        // Catalog parse errors → ENGTASK006 diagnostics.
        context.RegisterSourceOutput(catalogProvider, static (spc, catalog) =>
        {
            foreach (var err in catalog.Errors)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MalformedFlavourConfig,
                    Location.None,
                    Path.GetFileName(err.FilePath),
                    err.Message));
            }
        });

        // ForAttributeWithMetadataName's transform output must be equatable
        // for downstream cache hits. The catalog is read in a separate
        // tributary and Combine'd in — we therefore have to defer the
        // actual MirrorTarget creation until AFTER the combine, by
        // wrapping ctx in a non-equatable holder. That loses the cache
        // hit ON THE TRANSFORM, but the MirrorTarget output IS equatable,
        // so RegisterSourceOutput still re-caches normally.
        var attrContexts = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "EngineTask.GenerateMirrorAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => new ContextHolder(ctx));

        var targets = attrContexts
            .Combine(catalogProvider)
            .SelectMany(static (pair, _) =>
                MirrorTarget.AllFromContext(pair.Left.Ctx, pair.Right).ToImmutableArray());

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

            // Diagnostics-only sentinel — every attribute on this class
            // failed validation, but we still want the class-level
            // diagnostics to surface.
            if (string.IsNullOrEmpty(target.FlavourId)) return;

            spc.AddSource(target.HintName, MirrorEmitter.Emit(target));
        });
    }

    private static bool IsCatalogFile(string path) =>
        string.Equals(Path.GetFileName(path), "enginetask.json", StringComparison.OrdinalIgnoreCase);

    private static FileParseResult ParseCatalogFile(AdditionalText file, System.Threading.CancellationToken ct)
    {
        var text = file.GetText(ct)?.ToString() ?? string.Empty;
        var flavours = CustomFlavourParser.TryParse(text, out var error);
        if (flavours is null)
        {
            return new FileParseResult(
                file.Path,
                EquatableArray<CustomFlavourData>.Empty,
                error ?? "Unknown parse error");
        }
        return new FileParseResult(
            file.Path,
            new EquatableArray<CustomFlavourData>(flavours.ToArray()),
            null);
    }

    private static FlavourCatalog CombineCatalogs(ImmutableArray<FileParseResult> results)
    {
        if (results.IsDefaultOrEmpty) return FlavourCatalog.Empty;

        var allFlavours = new List<CustomFlavourData>();
        var allErrors = new List<FlavourParseError>();
        foreach (var r in results)
        {
            if (r.Error is not null)
            {
                allErrors.Add(new FlavourParseError(r.FilePath, r.Error));
                continue;
            }
            foreach (var f in r.Flavours) allFlavours.Add(f);
        }
        return new FlavourCatalog(
            new EquatableArray<CustomFlavourData>(allFlavours.ToArray()),
            new EquatableArray<FlavourParseError>(allErrors.ToArray()));
    }

    // Holder so ContextHolder is reference-equal-only — the transform's
    // cache will effectively no-op, but the catalog-aware SelectMany
    // downstream still produces equatable MirrorTargets.
    private sealed class ContextHolder
    {
        public GeneratorAttributeSyntaxContext Ctx { get; }
        public ContextHolder(GeneratorAttributeSyntaxContext ctx) { Ctx = ctx; }
    }

    private readonly record struct FileParseResult(
        string FilePath,
        EquatableArray<CustomFlavourData> Flavours,
        string? Error);
}
