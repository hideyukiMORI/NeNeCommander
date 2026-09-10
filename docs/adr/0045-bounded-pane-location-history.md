# ADR-0045: Retain bounded location history in each pane state

Status: accepted

Date: 2026-09-08

Accepted for Issue #109 on 2026-09-08 by the NeNe Commander design owner under hide's delegated authority.

## Context

The first usable release requires address and history navigation. Address entry, focused-directory
opening, parent navigation, and refresh already converge on `PaneSession.NavigateAsync`, the sole
coordinator of a pane read. A pane nevertheless forgets every location after a successful change,
so the canonical input model cannot provide Back or Forward navigation.

CMD-002 assigns navigation and history transitions to `PaneReducer` over immutable `PaneState`.
ARC-001 prohibits another navigation service, and ARC-004 prohibits a second mutable history owner
inside the host or session. A history request also must not claim a cursor change before its target
has been read successfully. Failed, cancelled, superseded, or stale reads must preserve the exact
candidate so the user can retry.

An unbounded session history would permit avoidable memory growth. Physical filesystem identity is
neither needed nor correct for navigation history: the existing `FileSystemPathIdentityComparer`
already owns Windows, UNC, WSL-distribution, and Linux-segment identity semantics.

## Decision

- **Each `PaneState` carries one immutable `PaneNavigationHistory`.** It contains a sequence of
  validated `FileSystemPath` values and the index of the current location. The sequence contains
  at most 100 locations including current. Each left or right pane state therefore carries its own
  independent history; no process-global, dual-pane, host, persisted, or settings-owned history is
  introduced.
- **`PaneReducer` remains the only transition owner.** A pane's first successful read seeds a
  one-location history. A successful normal navigation to a distinct location truncates the
  Forward suffix, appends the new location, and removes the oldest retained location when the
  100-location limit would be exceeded. A successful Back or Forward read moves only the cursor.
  A successful refresh or same-location read retains the sequence and cursor.
- **All reads keep the existing generation route.** `NavigateBack` and `NavigateForward` ask the
  current history for the adjacent candidate, then call the same private `PaneSession.NavigateAsync`
  path used by every other pane read. The reducer commits the selected history action only after a
  `DirectoryReadSucceeded` result survives the existing latest-generation check. No candidate
  means no read and no state change.
- **Non-success preserves history.** `DirectoryReadFailed` and `DirectoryReadCancelled` retain the
  previous listed content, including its history. A superseded or stale completion is discarded by
  the existing generation identity before reduction, so it cannot append a location or move the
  cursor. The same candidate remains available for retry.
- **History records locations only.** Back and Forward do not retain historical focus, selection,
  scroll position, listing data, or provider metadata. Their successful reads use the existing
  navigation rule: selection is cleared and focus lands on the first visible entry when no
  preferred item is supplied. Parent navigation retains its origin-directory preferred focus, and
  refresh retains its current-item preferred focus.
- **Path identity remains provider-aware and lexical.** A normal navigation is same-location when
  `FileSystemPathIdentityComparer` says both paths identify the same provider location. Windows
  local and UNC identity and WSL distribution names remain case-insensitive; WSL Linux segments
  remain case-sensitive. The feature adds no file-identifier lookup, filesystem probe, parsing
  path, or provider capability.
- **The canonical key map owns both bindings.** `Alt+Left` emits `NavigateBack` and `Alt+Right`
  emits `NavigateForward` in `FileList` and `NavigationSurface` contexts. They use virtual-key
  translation. Address, generic text-entry, modal, and IME input are unchanged, and the existing
  read and operation freeze rules keep priority while work owns interaction.
- **No new visual surface is added.** The current pane address, listing, focus mark, selection mark,
  loading state, and typed read-failure status already render every resulting state. The change
  adds key-label resources for the canonical bindings but no XAML, style, token, or layout.

## Rejected alternatives

- Keeping mutable Back and Forward stacks in `PaneSession`: this would make history a second state
  owner outside `PaneReducer` and allow it to drift from the immutable pane state.
- Keeping history in `DualPaneSession` or the window: this would couple independent panes or place
  navigation decisions in framework code.
- Moving the cursor before starting a read and rolling it back on failure: rollback creates an
  avoidable transient state and complicates failure, cancellation, and stale-completion behavior.
- Caching old listings to avoid reads: history navigation must observe current provider state and
  reuse the existing bounded directory query and typed outcomes.
- Recording refreshes or equal provider identities as visits: this would fill history with entries
  that cannot change location and make Back behavior depend on casing aliases.
- Persisting history or adding a configurable limit: neither is needed for the first usable
  session behavior, and both would expand the settings and migration contract.
- Restoring historical focus or scroll position: location history is the approved focused slice;
  retaining presentation position would require a separate state and UI contract.
- Querying physical file identity: navigation identity is already defined by the canonical path
  comparer and does not require filesystem I/O.

## Consequences

Each pane can move backward and forward across its last 100 successful locations without changing
the directory-read, provider-routing, failure-normalization, cancellation, hidden-item, focus, or
selection mechanisms. Back followed by a successful ordinary location change abandons the Forward
suffix. The oldest visit becomes unavailable after capacity eviction, while every retained path is
an already validated immutable value.

This decision supersedes ADR-0012 only where that ADR listed history as future work. Its sole pane
session, reducer ownership, generation check, and successful-navigation rules remain active.

## Migration and removal

Extend `UserIntent`, `PaneState`, `PaneReducer`, and `PaneSession` atomically with the bounded history
and its two actions. Extend the existing keyboard identities, translator, canonical binding table,
localized key labels, and focused tests in the same change. No persisted data, dependency,
composition, XAML, Domain, Infrastructure, or provider migration exists.

## Executable proof

Reducer tests prove append, Back, Forward, Forward-suffix truncation, provider-aware same-location
comparison, immutable cursor ownership, and eviction at 100 locations. Pane-session tests prove
initial seeding, no-candidate no-op, successful Back and Forward reads, selection clearing, focus
fallback, failed and cancelled retry, refresh and same-location preservation, and stale-result
discard. Dual-pane tests prove that routing an intent moves only the active pane's independent
cursor.

Presentation tests prove virtual-key translation, `Alt+Left` and `Alt+Right` in both approved
contexts, binding uniqueness and labels, and pass-through in text, address, modal, and unmodified
contexts. Focused suites, affected coverage, conformance, and dependency-impact checks run during
implementation. The final candidate requires the one normal Draft-to-Ready canonical CI gate.
This change does not widen a filesystem, process, persistence, destructive-operation, or native
interop boundary, so it adds no feature-specific deep-review or mutation tier. Scheduled,
security-sensitive, and release-environment obligations remain unchanged.
