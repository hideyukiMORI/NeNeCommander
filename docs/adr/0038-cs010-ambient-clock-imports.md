# ADR-0038: Close known ambient clock import escape routes in CS-010

Status: accepted

Date: 2026-09-06

## Context

Issue #67 showed that the existing CS-010 forbidden-API scan rejected direct `DateTime.Now` but accepted equivalent ambient access through a type alias, a `using static` directive, `TimeProvider.System`, `Stopwatch`, and `Environment.TickCount64`. The project has a named `IClock` boundary and no approved dependency for Roslyn semantic analysis. A second parser would therefore be both an architectural dependency and a false claim about the guarantee provided by the gate.

The design decision was delegated by hide to design Sana together with the remaining issue work. This ADR records that delegated decision; it does not claim a prior individual approval by hide.

## Decision

- Keep one `sourcePatterns` forbidden-API table in `eng/conformance.ps1`. Preserve the direct wall-clock, identifier-generation, and environment checks, including their existing text-scan behavior and interpolation coverage.
- Reject ambient clock-bearing type aliases, including aliases using `global::`, escaped `@` identifiers, whitespace around member separators, inline namespace declarations, and compilation-unit `global using` declarations. Reject aliases to `System` and `System.Diagnostics` because they provide a cross-file route to the ambient types.
- Reject `using static` for `DateTime`, `DateTimeOffset`, `TimeProvider`, `Environment`, and `System.Diagnostics.Stopwatch`; reject `TimeProvider.System`; and reject `Environment.TickCount` / `TickCount64` even in the settings-location adapter.
- Reject every `Stopwatch` type-token reference outside `src/NeNeCommander.Infrastructure.Windows/Time/StopwatchClock.cs`, the sole exact repository-relative exception for that concern. This constrained repository language closes static, instance, qualified, target-typed, and object-initializer entrances without attempting data-flow analysis.
- Keep the direct environment-access concern in the same table. Only `src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsLocation.cs` may use that concern, while the separate environment-clock concern still applies there.
- The raw scan intentionally rejects safe-looking date arithmetic, non-ambient `Stopwatch` type use, and matching examples in comments or strings. It does not resolve identifier shadowing, Unicode-escaped identifiers, or comments inserted between tokens. Those forms are outside the permitted repository language; the gate does not claim semantic or lexical completeness.

## Rejected alternatives

- A hand-written C# lexer or interpolated-string scrubber would add a second parsing mechanism and could weaken detection inside interpolation expressions.
- Roslyn or `BannedApiAnalyzers` would provide stronger semantic resolution, but adds an unapproved dependency and SDK/path coupling. It requires a separate dependency ADR.
- File-level alias collection followed by cross-file resolution is not a bounded text scan and would still fail to model shadowing and generated/compiler semantics.

## Consequences

The known escape routes fail the existing CS-010 gate without adding a dependency. Safe date arithmetic through a type alias and any `Stopwatch` type reference outside the named adapter are rejected as the cost of keeping the boundary explicit. The protection remains a conservative raw-text check, so semantic guarantees remain outside this ADR.

## Migration and removal

The existing table, gate proof fixtures, this ADR, the rule text, and the proof registry change together. No suppression, waiver, generated artifact, or dependency is added.

## Executable proof

`eng/prove-gates.ps1` materializes isolated temporary roots and runs the real conformance script against one-source-form negative fixtures. They separately cover header/directive-prefixed aliases, `global using`, `global::`, escaped `@` identifiers, inline namespaces, protected type and namespace aliases, static imports, `TimeProvider.System`, qualified/target-typed/initializer `Stopwatch` construction, interpolation, and environment clocks both outside and inside the settings-location adapter. A positive fixture asserts that the existing settings adapter still exercises and may use `Environment.GetFolderPath` and `Environment.SpecialFolder.LocalApplicationData`. The current source tree includes the exact named `StopwatchClock` adapter. Every negative fixture fails with CS-010.
