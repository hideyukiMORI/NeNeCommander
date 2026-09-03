# ADR-0018: Cancel a running file operation from the dual-pane session

Status: accepted

Date: 2026-09-04

## Context

`FileOperationGateway` observes its `CancellationToken` before each inspection, after preflight, and before every copy, verify, and delete step, and reports the exact completed effects as `Cancelled` (ADV-005). Nothing owned a token that could be cancelled while an operation ran: `DualPaneSession` froze every intent under `OperationRunning` and the App host passed `CancellationToken.None`. The keyboard model reserves `Escape` for cancelling the most transient state first.

## Decision

- `DualPaneSession` creates one `CancellationTokenSource` per started operation, linked to the caller's token, and passes its token to the gateway. The source is disposed when the gateway returns; the pane refresh that follows uses the caller's token, so a cancelled operation still re-reads both panes.
- While `OperationRunning` is current, `UserIntent.Escape` cancels that source and returns the current snapshot. Every other intent and `NavigateAsync` stay frozen (ADV-014, ADV-016).
- The gateway gains no new observation point; cancellation lands at the next existing one, and the outcome is recorded as `OperationCompleted(kind, Cancelled)` with the effects completed so far.
- The presentation and the App host are unchanged: the existing `*Cancelled` statuses already name the outcome, and the window forwards each intent to the session independently.

## Rejected alternatives

- Cancelling from the App host with its own token source: the window would own operation lifecycle state, which belongs to the sole coordinator (ARC-004, CS-022).
- A dedicated `Cancel` intent: `Escape` already means "cancel the most transient state"; a second key for the same meaning would split the single key map (KBD-005).
- Aborting the provider step in flight: adapter steps are atomic per entry; interrupting them would make the reported effects untrue (CMD-005).

## Consequences

- Cancellation takes effect between steps, so a large single entry finishes its current copy before the operation stops.
- Effects completed before cancellation remain on disk; the status line names the cancelled outcome and the panes show the result.
- Progress reporting and a visible cancel affordance remain later slices.

## Migration and removal

No public signature changes.

## Executable proof

`DualPaneSessionTests` (escape during a running copy cancels at the next observation point and the next operation starts with a fresh token; another intent during a running copy does not cancel it) and the canonical gate.
