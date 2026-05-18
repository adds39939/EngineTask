\# EngineTask — Agent Guidelines



\## Read first

\- EngineTask-Plan.md is the source of truth. Re-read it at the 

&#x20; start of each task.

\- Do not work ahead of the current phase.



\## Constraints

\- Generator and Attributes projects target netstandard2.0. 

&#x20; Non-negotiable.

\- Use IIncrementalGenerator, not ISourceGenerator.

\- Match types by symbol (SymbolEqualityComparer), not by string.

\- Snapshot tests use Verify.SourceGenerators. Always run them 

&#x20; after generator changes and review the diff manually.



\## Workflow

\- After any generator change: run `dotnet test` for the 

&#x20; Generator.Tests project and report results.

\- After any code change touching the rewriter: also run the 

&#x20; flavour integration tests.

\- Commit at the end of each completed sub-task with a message 

&#x20; referencing the phase and sub-task.



\## What to flag, not silently do

\- Translation table additions beyond what the plan specifies.

\- Changes to public API shape (attribute parameters, namespace 

&#x20; conventions).

\- Any decision the plan calls out as "open" or "TBD".



