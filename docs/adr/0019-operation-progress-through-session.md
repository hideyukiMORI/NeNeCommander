# ADR-0019: Report operation progress through the session to the host

Status: accepted

Date: 2026-09-04

## Context

While a move, copy, or deletion ran, the status line showed only the running verb. `FileOperationGateway` reported completed effects only after the whole request finished, and the App host rendered only when `DualPaneSession.HandleAsync` completed, so no intermediate state could reach the screen. The keyboard model and CS-025 forbid the host from computing or formatting that state itself.

## Decision

- `FileOperationProgress.Create(completed, total)` is the closed progress value; it enforces `1 ≤ total` and `0 ≤ completed ≤ total` and throws on violation because only the gateway produces it.
- `FileOperationGateway.ExecuteAsync` takes an `IFileOperationProgressObserver` and calls `Report` exactly once per source whose every step completed, for transfers and deletions alike. Adapters never see the observer; a source that fails or is cancelled is never reported.
- `OperationRunning` carries `Progress`. `DualPaneSession` starts it at `0 / sources.Count` and, through a private relay, replaces the running activity on each report and hands the new snapshot to the `IDualPaneProgressObserver` the caller passed to `HandleAsync`. Freezing and cancellation are unchanged.
- `DualPanePresentation.Detail` is the closed `OperationDetail`: `None`, `OperationItemCountDetail` for a pending confirmation, or `OperationProgressDetail` for a running operation. It replaces `ConfirmationItemCount`.
- The App window implements `IDualPaneProgressObserver` by rendering the reported snapshot, passes itself to every `HandleAsync`, and renders the numbers in their own controls; the separator between them is a resource.

## Rejected alternatives

- A constructor-injected observer on `DualPaneSession`: the window needs the session to forward intents and the session would need the window, so composition would require mutable wiring.
- `System.Progress<T>`: it posts through the synchronization context asynchronously, making the order of snapshots and completion untestable and letting a report arrive after completion.
- Formatting `completed / total` into one string in the host: CS-025 forbids assembling user-facing text in code.

## Consequences

- Progress is per source, not per byte; a single large entry shows no movement until it completes.
- A host that ignores intermediate snapshots passes a no-op observer; tests use a recording one.
- The observer runs on the gateway's continuation, on the UI thread in the App because every provider step completes synchronously.

## Migration and removal

`FileOperationGateway.ExecuteAsync(request, cancellationToken)`, `DualPaneSession.HandleAsync(intent, cancellationToken)`, `OperationRunning(kind)`, and `DualPanePresentation.ConfirmationItemCount` are removed in the same change.

## Executable proof

`FileOperationProgressTests`, `FileOperationGatewayTests` (one report per completed source, none for a failed one), `DualPaneSessionTests` (zero progress at start, observer sees each completed source), `DualPanePresenterTests` (running detail, reported detail, confirmation count), and the canonical gate.
