# ADR-0058: Abandon a loading pane read from the keyboard

Status: accepted

Date: 2026-10-10

## Context

While a pane is `PaneLoading`, `PaneSession.HandleAsync` refuses every intent, `RefreshAsync` is
refused, and `CommanderSession` keeps the bookmarks, palette, Locations, and address scopes closed.
Nothing lets the user leave that state; the read ends when the provider returns. For local paths
that is milliseconds. For an unreachable UNC host (ADR-0057) or a disconnected mapped network drive
the first enumeration call blocks inside the SMB client until its timeout, which is tens of seconds,
and the ADR-0027 boundary observes cancellation only before enumeration and between entries, so a
token cannot shorten the block. `PaneSession` already discards a superseded read: `NavigateAsync`
records each navigation in `_latestNavigation` and ignores a result whose navigation is no longer
the latest. `Escape` is ordered by the keyboard model as cancel a running file operation, then the
pending chord, then transient UI, then selection (ADR-0018 for the first step).

## Decision

- **`Escape` abandons the active pane's loading read.** While the active pane's activity is
  `PaneLoading`, `UserIntent.Escape` is the one intent `PaneSession.HandleAsync` accepts. It marks
  the current navigation as superseded through the existing `_latestNavigation` mechanism, cancels
  the session-owned token source of that read, and sets the activity to the closed
  `PaneReadAbandoned(target)` over the pane's previous content. The listing, focus, selection, and
  history the pane showed before the read are unchanged; a pane that had no listing yet shows the
  abandoned state alone. Every other intent stays frozen while loading, exactly as today.
- **One token source per read, owned by the session.** `PaneSession.NavigateAsync` links a
  `CancellationTokenSource` to the caller's token for each read, as `DualPaneSession` does for an
  operation (ADR-0018), and disposes it when the read returns. Cancellation lands at the provider's
  existing observation points; a read blocked inside the operating system is not interrupted and
  its eventual result is discarded by the supersession check. The boundary's contract is restated
  rather than changed: ADR-0027 gains the sentence that an abandoned provider step runs to
  completion on its pool thread and its result is dropped, and that no thread, timeout, or
  `LongRunning` option is added for it.
- **The abandoned state is refreshable and typed.** `PaneReadAbandoned` carries the target and is
  presented through the existing status presenter with its own localized text; `Refresh`,
  navigation, and every other intent are accepted again immediately. FS-010's rule that a failed or
  disconnected location yields a typed, refreshable pane state applies to the abandoned state too.
- **Precedence is preserved.** Escape's order becomes: cancel a running file operation, abandon the
  active pane's loading read, cancel the pending chord, close transient UI, clear selection. In the
  session a read started before an operation can still be loading while the operation runs (the
  refresh after the operation skips a loading pane), so the two first steps can meet: the running
  operation takes `Escape` and the read keeps loading. A pending confirmation, name entry, or
  conflict decision is modal (KBD-002) and keeps its own `Escape`. The mapper is unchanged: a
  pending `gg` chord is cancelled by `Escape`, which is then mapped to the one `Escape` intent, so a
  loading pane is abandoned and the chord is gone in one key press.
- **Scope freezes lift with the state.** `CommanderSession` keeps deriving its scope admissions from
  the panes' activities; because the abandoned pane is no longer `PaneLoading`, bookmarks, palette,
  Locations, and address editing admit again without a new predicate. The passive pane's read, if
  any, is not affected by the active pane's `Escape`; `Tab` to the other pane and `Escape` there
  abandons it separately.
- **Out of this decision.** The Locations picker's own `LocationsLoading` state (ADR-0055) is owned
  by `LocationsSession`, not by a pane, and is unchanged. No operating-system timeout is shortened
  and no credential prompt is added.

## Rejected alternatives

- Interrupting the blocked Win32 call with a dedicated thread and `Thread.Interrupt` or process
  tricks: not supported for file I/O and would leave the SMB client in an undefined state.
- A timeout inside the execution boundary: it changes ADR-0027's semantics for every provider
  step, including mutations that must not be abandoned midway, and still cannot unblock the call.
- Automatically restoring the pane after the operating system timeout only: it leaves the user
  frozen for the whole wait with no way out.
- Allowing all intents while loading and racing the read: focus, selection, and history would
  diverge from the listing that eventually arrives.
- Reusing `PaneReadFailed` for the abandoned state: the user chose to stop; it is not a provider
  failure and must not be reported as one.

## Consequences

- An abandoned read keeps one pool thread busy until the operating system returns; repeated
  attempts against an unreachable host can hold several threads. This is accepted and recorded;
  the next step, if it is ever needed, is a per-provider concurrency limit through a new ADR.
- `PaneSession` gains one activity record and a token source per read. Only the latest navigation
  releases the source's cancel delegate when its read returns, so a superseded read that returns
  late never detaches the delegate of the read that replaced it, and a read whose provider faults
  leaves no delegate over a disposed source.
- `DualPaneSession` needs no new route: when no operation is running or awaiting a decision, its
  existing route already hands `Escape` to the active pane's session. `CommanderSession` is
  unchanged.
- A read started by a bookmark is not abandoned by `Escape`: while the manager's navigation is
  pending, `Escape` belongs to the bookmark manager, and while a direct slot navigation is pending
  `CommanderSession` refuses every intent until the read returns. Both are existing ADR-0041
  rules and are left unchanged by this decision.
- The App host rejects input while pane work is in flight (`AsyncWorkOwner`, ADR-0030), and the
  work that started a read stays in flight until the provider returns, even after the read is
  abandoned. Until a host route for `Escape` during in-flight pane work is decided, the
  abandonment is reachable through the session but not yet from the running window; the same
  rejection applies to ADR-0018's `Escape` during a running operation.
- Presentation gains one status text and the key hints keep showing `Esc` with its existing cap.

## Migration and removal

No stored data changes. Removing the feature removes `PaneReadAbandoned`, the token source, the
keyboard model sentence, the ADR-0027 sentence, and the tests together.

## Executable proof

Application tests prove that `Escape` during `PaneLoading` leaves the previous listing, focus,
selection, and history intact, sets `PaneReadAbandoned` with the target, cancels the read's token,
discards a result that arrives afterwards, re-admits refresh and navigation, and that every other
intent stays refused while loading; that each read's token is linked to the caller's token, that a
superseded read returning first does not detach the latest read's cancellation, and that a faulted
read still leaves the pane abandonable; that the passive pane is unaffected and that a running
operation takes `Escape` before a loading read; and that the palette, bookmark, address, and window
adjustment scopes admit again after abandonment. Mapper tests prove that `Escape` after a pending
`g` emits the one `Escape` intent and cancels the chord. Presentation tests prove the status and its
resource key. Coverage and mutation thresholds are unchanged.
