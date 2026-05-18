using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace EngineTask.Generator;

internal static class Diagnostics
{
    public const string Category = "EngineTask";

    public static readonly DiagnosticDescriptor NonPartialClass = new(
        id: "ENGTASK002",
        title: "[GenerateMirror] should be applied to a partial class",
        messageFormat: "Class '{0}' is not declared partial; mark it `partial` so consumers can extend the mirror with engine-specific code",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AsyncVoidMethod = new(
        id: "ENGTASK003",
        title: "async void methods cannot be mirrored",
        messageFormat: "Method '{0}' is `async void`; mirrors require a task-like return type and the method will be skipped",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly IReadOnlyDictionary<string, DiagnosticDescriptor> ById =
        new Dictionary<string, DiagnosticDescriptor>
        {
            [NonPartialClass.Id] = NonPartialClass,
            [AsyncVoidMethod.Id] = AsyncVoidMethod,
        };
}
