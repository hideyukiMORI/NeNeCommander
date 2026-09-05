# ADR-0015: Start file operations from the dual-pane session

Status: accepted

Date: 2026-09-04

## Context

`FileOperationGateway` and its Windows local adapter existed without a caller. `F6` maps to `UserIntent.Move`, whose product meaning is fixed by the keyboard model: move the selection, or the focus item when the selection is empty, to the passive pane. The operation must be preflighted atomically (CMD-004), reported as one closed outcome (CMD-005), and must not interleave with pane navigation or activation while it runs (ADV-014, ADV-016).

## Decision

`DualPaneSession` receives the sole `FileOperationGateway` and becomes the only starter of file operations. On `UserIntent.Move` it:

- requires both panes to be listed and the active pane to have a focus item, otherwise does nothing;
- takes the active pane's selection, or its focus item, as sources and the passive pane's listed location as destination;
- builds a `MoveRequest`; a typed request rejection is recorded as `OperationRequestRejected` and never reaches the gateway;
- records `OperationRunning`, awaits `FileOperationGateway.ExecuteAsync`, records `OperationCompleted` with the full outcome, then refreshes both panes.

`DualPaneSnapshot` carries the closed `OperationActivity` (`Idle`, `OperationRunning`, `OperationCompleted`, `OperationRequestRejected`). While an operation runs, `HandleAsync` and `NavigateAsync` return the current snapshot unchanged, so no intent, activation, or read can change the identities the operation captured.

`PaneSession.RefreshAsync` re-reads the listed location through the same navigation path, preferring the previous focus item and clearing selection (KBD-004). `UserIntent.Refresh` uses it.

`DualPanePresenter` projects the activity onto a closed `OperationStatus` resource key; the App host assigns it to one status line and disposes the gateway with the window.

## Rejected alternatives

- A separate operation coordinator beside `DualPaneSession`: both would need the same freeze over both panes, creating two owners of one lifecycle (ARC-004).
- Refreshing only the panes that changed: the gateway reports effects per source, but the passive pane also changed; re-reading both is the single mechanism.
- Cancelling a running operation from the keyboard: cancellation UI, progress, and a cancellation token owner are a later slice.

## Consequences

- Only move exists; copy needs a copy request in the application layer and delete needs confirmation UI.
- The status line shows the last outcome until the next operation; per-effect detail is not presented yet.
- At the time of this decision, Windows local moves always ran copy, verify, and delete. ADR-0032 later replaced that provider strategy for supported same-volume moves while preserving this ADR's session ownership.

## Migration and removal

`DualPaneSession` gains a required gateway parameter; the two-parameter constructor is removed in the same change.

## Executable proof

`DualPaneSessionTests` (focus item and selection moves, no-destination and no-source cases, request rejection, failed outcome, freeze under ADV-014 and ADV-016), `PaneSessionTests` refresh, `DualPanePresenterTests`, and the canonical gate.
