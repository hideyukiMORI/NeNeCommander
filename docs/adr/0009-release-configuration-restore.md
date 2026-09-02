# ADR-0009: Restore dependencies under the build configuration

- Status: Accepted
- Date: 2026-09-03

## Context

The App enables ReadyToRun only for Release builds. A default-configuration locked restore followed by a Release `--no-restore` build can therefore omit configuration-conditional runtime packs on a clean machine. A developer cache concealed this mismatch, while the first clean GitHub runner failed with `NETSDK1112` for the `win-x64` runtime pack.

## Decision

The canonical gate performs its one solution-level locked restore with the MSBuild property `Configuration=Release`. Formatting, build, tests, and coverage continue from that restored graph. Local and CI execution use the same `eng/check.ps1` command.

## Rejected alternatives

- Rely on the default restore configuration: it does not evaluate the graph subsequently built by the gate.
- Add an App-only second restore: two restore mechanisms can drift and obscure which graph is authoritative.
- Patch only the GitHub workflow: a CI-only path would violate the one-gate rule and leave local clean-machine verification unsound.
- Depend on a populated developer package cache: cached packs are not reproducibility evidence.

## Consequences

- A clean runner restores every dependency needed by the Release build before `--no-restore` is enforced.
- Restore may download more than a Debug-only workflow, but it downloads the graph that is actually verified.
- Future configuration-conditional package behavior must remain compatible with the single Release-evaluated restore.

## Migration and removal

Replace the default-configuration restore in `eng/check.ps1`; no coexistence period or obsolete restore path remains.

## Executable proof

Rule QLT-014 in `eng/conformance.ps1` requires exactly one Release-evaluated locked restore. `eng/prove-gates.ps1` replaces it with the old default-configuration command and proves that conformance rejects the fixture. The complete canonical gate is also exercised with an empty NuGet package cache.
