using System;
using System.IO;
using Xunit;

namespace EngineTask.Generator.Tests;

public class TranslationTablesDocTests
{
    // Sentinel — docs/translation-tables.md should be the markdown
    // rendering of the in-code translation tables. When this test fails,
    // the developer is meant to inspect the .received file (which the
    // failure path writes next to the doc) and replace
    // docs/translation-tables.md with it if the change is intended.
    [Fact]
    public void DocFile_StaysInSyncWithCode()
    {
        var generated = TranslationTablesMarkdown.Build().Replace("\r\n", "\n");
        var docPath = Path.Combine(SolutionRoot(), "docs", "translation-tables.md");

        var committed = File.Exists(docPath)
            ? File.ReadAllText(docPath).Replace("\r\n", "\n")
            : string.Empty;

        if (generated != committed)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(docPath)!);
            File.WriteAllText(docPath + ".received", generated);
            Assert.Fail(
                $"docs/translation-tables.md is out of sync with the in-code translation tables. " +
                $"Inspect {docPath}.received and replace docs/translation-tables.md with it if the change is intended.");
        }
    }

    private static string SolutionRoot()
    {
        // The test executable runs in tests/EngineTask.Generator.Tests/bin/Debug/net8.0 —
        // walk up to the directory that holds the .slnx.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EngineTask.slnx")))
        {
            dir = dir.Parent;
        }
        if (dir is null)
            throw new InvalidOperationException("Could not locate the repository root (EngineTask.slnx).");
        return dir.FullName;
    }
}
