# ADR-0046: Launch Windows local files through the Shell association

Status: accepted

Date: 2026-09-08

Accepted for Issue #111 on 2026-09-08 by the NeNe Commander design owner under hide's delegated authority.

## Context

The first usable release requires `l` and `Enter` to open the focus item. `PaneSession` already
owns `OpenFocused`, resolves the focus entry once, and routes a directory through the sole pane
navigation path. A focused file nevertheless does nothing. The canonical mechanism registry
reserves `IFileLauncher` for this concern, but its provider contract and Windows implementation
have not existed.

A Windows association handoff is different from a filesystem mutation or an identity-sensitive
query. The user asks Windows to apply its current association semantics to one validated path at
that moment. A preliminary existence, attributes, reparse-point, or file-identifier probe would
add a time-of-check/time-of-use race without proving what the Shell later receives. The handoff
must also preserve executable, shortcut, removable-drive, and file reparse-point behavior rather
than adding an extension or entry-kind policy outside Windows.

`Process.Start` exposes arguments, verbs, process wrappers, and platform exceptions that do not
belong in Application. Cancellation can prevent a queued handoff, but it cannot revoke a handoff
that has begun or make NeNe Commander the owner of an external process.

## Decision

- **`PaneSession` remains the sole `OpenFocused` decision owner.** It resolves the current focus
  entry once. `DirectoryEntryKind.Directory` keeps the existing `NavigateAsync` path. Only
  `DirectoryEntryKind.File` with a `WindowsLocalPath` reaches `IFileLauncher`; Windows UNC and WSL
  files produce `ProviderUnavailable` before the port is called.
- **`IFileLauncher` is the only launch boundary.** It accepts one typed `WindowsLocalPath` and a
  cancellation token, and returns the closed `FileLaunchOutcome`: `Accepted`, `Cancelled`, or
  `Failed` with `NotFound`, `AccessDenied`, `AssociationUnavailable`, `ProviderUnavailable`, or
  the `ShellRejected` fallback. Acceptance means only that the Shell API accepted the path
  handoff. It does not claim a new process, successful file opening, or application completion.
- **`WindowsShellFileLauncher` performs the one Windows association handoff.** It creates one
  `ProcessStartInfo` whose `FileName` is the target's exact canonical text, whose
  `WorkingDirectory` is the canonical parent, and whose `UseShellExecute` is `true`. It supplies
  no verb or arguments. It does not probe existence, attributes, entry kind, reparse state, file
  identity, extension, drive type, or association before the handoff.
- **The existing Windows execution boundary schedules the synchronous call.** The App composition
  root gives the launcher the same `WindowsLocalIoExecutionBoundary` used by other synchronous
  Windows-side adapters. Cancellation is checked before scheduling and again in the queued work
  immediately before `Process.Start`. Once that call begins, its accepted result is never changed
  to cancellation.
- **External process lifetime stays outside NeNe Commander.** A null `Process.Start` return is an
  accepted handoff. A returned wrapper is disposed immediately; the launcher never waits for,
  terminates, tracks, or otherwise owns the external process.
- **Pane content stays immutable across the handoff.** The pane reports `PaneLaunching`, then idle
  on acceptance or a typed cancelled/failed activity. Content, focus, selection, listing, and
  navigation history retain the same object. While `PaneLaunching` is current, every intent and
  public pane navigation or refresh entry point is frozen, so no duplicate handoff or overlapping
  read can replace or later resurrect pane state.

## Rejected alternatives

- Launch directly from the WinUI event or presentation layer: this would duplicate `OpenFocused`
  decisions outside the pane session and expose platform behavior above Infrastructure.Windows.
- Route launch through `FileOperationGateway`: an association handoff is not a filesystem mutation
  and has no preflight, conflict, progress, or partial-effect contract.
- Pass raw text, a verb, arguments, or a caller-selected working directory through the port: this
  would weaken the validated path boundary and expose Shell policy to Application.
- Probe with `File.Exists`, attributes, handles, or file identifiers before launch: the later Shell
  handoff remains path-based, and the extra observation would reject supported Shell semantics or
  create false assurance across a race.
- Use `cmd.exe`, PowerShell, `explorer.exe`, or string-built command lines: each creates a second
  parser and command-injection surface and does not represent the default association API.
- Wait for or terminate the returned process: association dispatch may reuse an existing process,
  return no wrapper, or start an application whose lifetime the pane does not own.
- Add WSL or UNC translation: provider-specific launch semantics require their own future decision;
  silently translating paths would widen the supported trust boundary.

## Consequences

Focused Windows local files, including executable, shortcut, removable-drive, and file
reparse-point entries reported as files, use the current Windows association without NeNe
Commander interpreting their contents or identity. The exact canonical path is a single
`ProcessStartInfo.FileName`, so metacharacters do not become command syntax. Expected Shell
rejections are stable Application outcomes and localized pane statuses.

The contract deliberately retains the normal path race between focus resolution and Shell use.
The target may disappear, change, or resolve differently under Windows association behavior;
the launcher reports only the result of that one handoff attempt. Actual desktop association and
removable-drive behavior remain controlled Windows environmental proof under Issue #94.

This decision supersedes ADR-0012 only where that ADR left file launch as future work. Its pane
session, reducer, directory navigation, generation, content-preservation, and cancellation
decisions remain active. It extends ADR-0027 by applying its shared scheduling boundary to this
synchronous Windows-side adapter.

## Migration and removal

Add the Application launch contract and closed outcomes, extend `PaneSession` and its closed
activity, add the Infrastructure.Windows Shell adapter, and compose one shared launcher into both
panes. Extend the existing pane presenter and localized resources for launch activity. No old
launcher, command path, dependency, persisted data, or external-process owner exists to migrate
or remove.

## Executable proof

Application tests prove exact single dispatch, directory preservation, unsupported-provider
rejection, content/focus/selection/history preservation, frozen intent and direct-read entry
points, cancellation and failure activities, and exhaustive outcome handling. Infrastructure.Windows
tests use an injected start delegate and deterministic scheduler to prove the exact
`ProcessStartInfo`, no Shell call before cancellation, cancellation after handoff, null acceptance,
wrapper disposal, parent validation, and Win32 failure normalization without launching a desktop
application. Presentation tests prove every closed status and localized resource.

`ADV-019` maps unsupported providers and path text that resembles command syntax to owner tests.
Focused suites, affected coverage, architecture, and conformance checks run during implementation.
Because this change opens a process and Shell trust boundary, its final exact head requires the
security deep-review workflow before the one normal Draft-to-Ready canonical CI gate. Controlled
desktop interaction remains release environmental proof.
