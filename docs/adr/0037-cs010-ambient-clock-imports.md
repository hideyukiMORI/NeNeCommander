# ADR-0037: Close known ambient clock import escape routes in CS-010

Status: accepted

Date: 2026-09-06

## Context

Issue #67 showed that the existing CS-010 forbidden-API scan rejected direct `DateTime.Now` but accepted four equivalent ambient access shapes: a type alias, a `using static` directive, `TimeProvider.System`, and `Stopwatch` entry points. The project has a named `IClock` boundary and no approved dependency for Roslyn semantic analysis. A second parser would therefore be both an architectural dependency and a false claim about the guarantee provided by the gate.

The design decision was delegated by hide to design Sana together with the remaining issue work. This ADR records that delegated decision; it does not claim a prior individual approval by hide.

## Decision

- Keep one `sourcePatterns` forbidden-API table in `eng/conformance.ps1`. Preserve the direct wall-clock, identifier-generation, and environment checks, including their existing text-scan behavior and interpolation coverage.
- Reject ambient clock-bearing type aliases, including aliases using `global::`, escaped alias identifiers, whitespace around member separators, inline namespace declarations, and `global using` declarations. Reject aliases to `System` and `System.Diagnostics` because they provide a cross-file route to the ambient types.
- Reject `using static` for `DateTime`, `DateTimeOffset`, `TimeProvider`, and `System.Diagnostics.Stopwatch`; reject `TimeProvider.System`; and reject ambient `Stopwatch` static entry points and construction.
- `src/NeNeCommander.Infrastructure.Windows/Time/StopwatchClock.cs` is the sole exact repository-relative exception for the stopwatch concern. No settings-location or other adapter exception grants clock access.
- The scan intentionally rejects safe-looking date arithmetic reached through an alias. It does not resolve symbols, detect identifier shadowing, follow arbitrary namespace aliases beyond the enumerated declarations, or claim complete comment/string parsing. Those limits are recorded and are not a reason to weaken the existing direct scan.

## Rejected alternatives

- A hand-written C# lexer or interpolated-string scrubber would add a second parsing mechanism and could weaken detection inside interpolation expressions.
- Roslyn or `BannedApiAnalyzers` would provide stronger semantic resolution, but adds an unapproved dependency and SDK/path coupling. It requires a separate dependency ADR.
- File-level alias collection followed by cross-file resolution is not a bounded text scan and would still fail to model shadowing and generated/compiler semantics.

## Consequences

The known four escape routes fail the existing CS-010 gate without adding a dependency. Safe date arithmetic through a type alias is rejected as the cost of keeping the boundary explicit. The protection remains lexical and conservative, so semantic guarantees such as shadowing resolution remain outside this ADR.

## Migration and removal

The existing table, gate proof fixtures, this ADR, the rule text, and the proof registry change together. No suppression, waiver, generated artifact, or dependency is added.

## Executable proof

`eng/prove-gates.ps1` materializes isolated temporary roots and runs the real conformance script against negative fixtures for ambient type aliases (including inline namespace and `global::` forms), static imports, `TimeProvider.System`, and `Stopwatch.StartNew`. The existing source tree is the positive proof, including the exact named `StopwatchClock` adapter. The old direct wall-clock positive and negative behavior remains covered by the same conformance gate. The pre-change four fixtures reproduce the Issue #67 miss; the post-change fixtures fail with CS-010.
