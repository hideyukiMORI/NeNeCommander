# ADR-0030: Own framework asynchronous work through one lifecycle mechanism

Status: accepted

Date: 2026-09-05

## Context

WinUI launch and input events cannot directly await their asynchronous work. `CommanderApplication` and `CommanderWindow` retained raw `Task` fields, but neither field observed a completed fault or closed in-flight work. A gateway defect also left `DualPaneSession` in `OperationRunning` with a cancellation delegate targeting a disposed `CancellationTokenSource`. The next Escape then threw `ObjectDisposedException`, and later work remained frozen.

Expected provider failures are already closed outcomes. This decision addresses unexpected defects and framework lifetime only; it does not turn expected failures into exceptions or invent recovery behavior.

## Decision

Add one framework-neutral `AsyncWorkOwner` in Presentation and use it for both application startup and window pane work. It:

- owns exactly one `CancellationTokenSource` and task at a time;
- rejects overlapping work and any replacement after an observed defect;
- observes the exact task defect once and publishes it through the composition root's defect callback;
- closes in-flight work in cancel, await, dispose order; and
- permits replacement only after successful completion, disposing the prior token owner first.

The App composition root supplies one defect callback that rethrows the preserved exception through the framework context. `CommanderWindow` passes the owner's token through `DualPaneSession`, instead of `CancellationToken.None`, and exposes only `StopAsync` for the application close handler. The close handler awaits window work, then startup work, and releases the gateway in nested `finally` blocks.

`DualPaneSession.StartAsync` separately owns operation state. Its gateway await now resets the cancellation delegate in `finally`; an unexpected defect returns operation state to `Idle` before rethrowing. Successful, expected-failure, confirmation, cancellation, progress, and refresh behavior remain on their existing canonical paths.

## Consequences

- Launch and pane tasks have the same ownership and defect-publication mechanism.
- A fault cannot be mislabeled as success, silently replaced by later work, or leave a disposed cancellation callback reachable.
- Closing a window waits for pane cleanup before the mutation gateway is disposed.
- Input received while pane work is active is rejected by the owner, matching the session's existing freeze invariant without starting overlapping render continuations.
- Framework event forwarding remains the only permitted `async void` boundary.

## Rejected alternatives

- Retain raw task fields and inspect them only at process shutdown: defects could remain unobserved for the entire session.
- Add `ContinueWith` calls at each event: this creates unowned continuation tasks and multiple lifecycle mechanisms.
- Catch defects and convert them to typed provider failures: defects are not expected business outcomes.
- Add cancellation sources independently to App and Window handlers: cancellation, await, and disposal ordering would remain duplicated and inconsistent.

## Verification

`DualPaneSessionTests` injects a gateway defect and proves the exact exception is rethrown while operation state and cancellation ownership are released. `AsyncWorkOwnerTests` deterministically prove immediate asynchronous fault observation, synchronous factory-defect propagation and token cleanup, overlap rejection, successful replacement, and cancel → await → dispose shutdown ordering. Application, Presentation, Architecture, conformance, security, and the canonical gate remain required.
