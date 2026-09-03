# ADR-0016: Resolve permanent-deletion confirmation as a modal operation state

Status: accepted

Date: 2026-09-04

## Context

`FileOperationGateway` refuses a permanent deletion whose exact source set has not been confirmed (`ConfirmationRequired`, ADV-008, FS-006). The Windows local adapter reports `PermanentOnly`, so every `F8` on a Windows local path needs that confirmation. KBD-002 requires a modal to own only its declared keys, and CS-022 forbids the window from deciding what a confirmation means.

## Decision

`DualPaneSession` starts deletion on `UserIntent.Delete` with an unconfirmed `DeleteRequest`. When the gateway answers `ConfirmationRequired`, the session records `OperationAwaitingConfirmation` holding that request; its frozen sources are the only set a confirmation may name.

- While a confirmation is pending, every intent except `Confirm` and `Escape` returns the current snapshot unchanged, and navigation into either pane is refused.
- `Confirm` re-runs a `DeleteRequest` over the same sources with `PermanentDeletionConfirmation.CreateFor(request)`, so the gateway's exact-set check applies; `Escape` returns the session to `Idle` without touching the filesystem.
- Every operation activity now carries a closed `OperationKind` (`Move` or `Delete`) so the presentation can name the operation.
- `UserIntent.Confirm` is new. In the `Modal` keyboard context the mapper maps `Enter` to `Confirm`, `Escape` to `Escape`, and passes every other key through. The text-entry context is unchanged.
- `DualPanePresenter` projects the pending confirmation as `OperationStatus.DeleteAwaitingConfirmation`, exposes the item count separately, and reports `KeyboardContext.Modal` as the input context. The App host feeds that context to the mapper unless a text control owns focus, and shows the count next to the status text.

## Rejected alternatives

- A framework dialog for the confirmation: moves the decision and the key handling into the window boundary and outside the mapper's single key map (KBD-005, CS-022).
- Passing a pre-built confirmation with the first request: bypasses the gateway's rule that confirmation names a request the user has seen.
- Formatting the count into the localized sentence in code: CS-025 prohibits assembling user-facing text; the count is rendered as a number in its own control.

## Consequences

- A pending confirmation freezes both panes until resolved; there is no timeout.
- The confirmation text is a placeholder sentence in resources until the design handoff defines the modal presentation.
- A provider that reports recycle capability will delete without confirmation through the same path; none exists yet.

## Migration and removal

`OperationRunning`, `OperationCompleted`, and `OperationRequestRejected` gain the kind; the kind-less constructors are removed in the same change.

## Executable proof

`DualPaneSessionTests` (unconfirmed deletion never deletes, confirm executes the exact set, escape abandons, intents and navigation frozen while pending, recycle-capable provider deletes without confirmation), `KeyboardIntentMapperTests` for the modal keys, `DualPanePresenterTests`, and the canonical gate.
