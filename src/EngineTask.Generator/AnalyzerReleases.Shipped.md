; Shipped analyzer releases.
; See https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.1

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------------------------------------------
ENGTASK001 | EngineTask | Warning | Unmapped Task-related API; method skipped
ENGTASK002 | EngineTask | Warning | [GenerateMirror] applied to non-partial class
ENGTASK003 | EngineTask | Error   | async void methods cannot be mirrored
ENGTASK004 | EngineTask | Warning | Mirror method collision with user-written partial
ENGTASK005 | EngineTask | Warning | Unknown custom flavour (not in any enginetask.json)
ENGTASK006 | EngineTask | Warning | Malformed enginetask.json
