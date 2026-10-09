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

The App host forwards every intent through `AsyncWorkOwner` (ADR-0030), which rejects input while
pane work is in flight. The work that started a read stays in flight until the provider returns, so
the window never delivered an `Escape` to a loading pane, and for the same reason it never delivered
ADR-0018's `Escape` to a running file operation: that cancellation was reachable through the
session but not from the running window. This decision repairs that latent defect together with the
new abandonment.

## Decision

- **`Escape` abandons the active pane's loading read.** While the active pane's activity is
  `PaneLoading`, `UserIntent.Escape` is the one intent `PaneSession.HandleAsync` accepts. It marks
  the current navigation as superseded through the existing `_latestNavigation` mechanism, sets the
  activity to the closed `PaneReadAbandoned(target)` over the pane's previous content, completes the
  read's waiting caller, and cancels the read's own token. The listing, focus, selection, and
  history the pane showed before the read are unchanged; a pane that had no listing yet shows the
  abandoned state alone. Every other intent stays frozen while loading, exactly as today.
- **One owned read per navigation; the caller returns at once.** Each read is one internal
  `PaneRead`: a `CancellationTokenSource` linked to the caller's token, as `DualPaneSession` does for
  an operation (ADR-0018), the provider task, and a `TaskCompletionSource<PaneSnapshot>` with
  `RunContinuationsAsynchronously`. `NavigateAsync` awaits `Task.WhenAny` of the provider task and
  that completion; abandonment completes it with the abandoned snapshot, so the waiting caller, and
  the host work that awaits it, returns while the provider is still blocked. Cancellation lands at
  the provider's existing observation points; a read blocked inside the operating system is not
  interrupted. ADR-0027 gains the sentence that an abandoned provider step runs to completion on its
  pool thread and its result is dropped, and that no thread, timeout, or `LongRunning` option is
  added for it.
- **The orphaned provider task is still observed.** `PaneRead` registers one completion callback on
  the provider task through its awaiter, in the form `AsyncWorkOwner` already uses; it creates no
  task of its own. The callback disposes the token source when the provider completes, abandoned or
  not. A read that is not abandoned is observed by its waiting caller as before. For an abandoned
  read, a result or a cancellation is discarded, and a fault is captured as an
  `ExceptionDispatchInfo` that `PaneSession` rethrows at the start of its next `HandleAsync`,
  `NavigateAsync`, `RefreshAsync`, or `RefreshFocusingAsync` and then clears, so it reaches the
  owner's existing defect path (ADR-0030) instead of an unobserved task exception. No observer,
  event, or log is added.
- **The host admits one `Escape` interrupt.** `AsyncWorkOwner.TryStartIntent` starts the work of one
  intent: with nothing running it is the ordinary start; while work runs it admits only
  `UserIntent.Escape`, as a single interrupt that runs beside that work with its own token, and
  rejects any other intent, a second interrupt while the first runs, ordinary work while an
  interrupt runs, and every start after an observed defect. `StopAsync` cancels and awaits both.
  The interrupt calls `CommanderSession.HandleAsync(Escape)` like any intent; both continue on the UI
  context, so session state is not raced. `CommanderWindow` forwards every intent through
  `TryStartIntent`; the decision lives in `AsyncWorkOwner`, not in code-behind. The same route makes
  ADR-0018's `Escape` reach a running operation from the window.
- **Reads started by a direct bookmark are abandonable.** While a `Ctrl+1` to `Ctrl+9` navigation
  is pending, `CommanderSession` keeps refusing every intent except `Escape`, which reaches the
  active pane; the abandoned read returns at once and releases the bookmark freeze. While the
  bookmark manager is open its navigation keeps `Escape` for the manager (KBD-002), unchanged.
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
- Keeping the caller waiting for the provider after abandonment: the host work would stay in flight,
  so the window would keep rejecting input until the operating system timeout.
- Letting the host accept every intent while work runs: overlapping intents would race the session
  and reopen the defects ADR-0030 closed.

## Consequences

- An abandoned read keeps one pool thread busy until the operating system returns; repeated
  attempts against an unreachable host can hold several threads. This is accepted and recorded;
  the next step, if it is ever needed, is a per-provider concurrency limit through a new ADR.
- `PaneSession` gains one activity record and one owned read per navigation. A provider fault that
  arrives after its read was abandoned surfaces as a defect at the next pane entry point, not at
  the moment it happens.
- `DualPaneSession` needs no new route: when no operation is running or awaiting a decision, its
  existing route already hands `Escape` to the active pane's session.
- `AsyncWorkOwner` owns at most two runs, the work and one `Escape` interrupt, and the window's
  forwarding call changes from `TryStart` to `TryStartIntent`.
- Presentation gains one status text and the key hints keep showing `Esc` with its existing cap.

## Migration and removal

No stored data changes. Removing the feature removes `PaneReadAbandoned`, `PaneRead`, the orphaned
fault, the `Escape` interrupt of `AsyncWorkOwner`, the bookmark exception, the keyboard model
sentence, the ADR-0018, ADR-0027, and ADR-0030 sentences, and the tests together.

## Executable proof

Application tests prove that `Escape` during `PaneLoading` leaves the previous listing, focus,
selection, and history intact, sets `PaneReadAbandoned` with the target, cancels the read's token,
returns the waiting caller while the provider is still running, discards a result, cancellation, or
failure that arrives afterwards, re-admits refresh and navigation, and that every other intent stays
refused while loading; that each read's token is linked to the caller's token and its source is
disposed when the provider completes, abandoned or not, or at once when the provider throws
synchronously; that an orphaned fault is rethrown once at each of the four entry points; that a
superseded read returning first does not detach the latest read's cancellation and that a faulted
read still leaves the pane abandonable; that the passive pane is unaffected and that a running
operation takes `Escape` before a loading read; that `Escape` abandons a direct bookmark read and
releases its freeze; and that the palette, bookmark, address, and window adjustment scopes admit
again after abandonment. Presentation tests prove the `AsyncWorkOwner` interrupt rules (ordinary
start when idle, one `Escape` interrupt, rejection of other intents, of a second interrupt, and of
ordinary work while it runs, shutdown of both runs, and fault observation), that `Escape` after a
pending `g` emits the one `Escape` intent and cancels the chord, and the status and its resource
key. Coverage and mutation thresholds are unchanged.
