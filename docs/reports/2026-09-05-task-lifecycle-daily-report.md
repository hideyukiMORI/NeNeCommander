# Daily report — task lifecycle ownership — 2026-09-05

Status: informational

## Scope

Issue #59 closes fault observation and shutdown ownership for application launch, window pane work, and the session-owned running-operation cancellation delegate. It does not change expected provider outcomes, add recovery UI, or widen cancellation inside an individual provider step.

## Invariant and canonical mechanism

Every asynchronous task started by a framework event is observed by its owner. Shutdown has one cancel → await → dispose order. `AsyncWorkOwner` is the sole framework-task lifecycle mechanism shared by launch and pane work; `DualPaneSession.StartAsync` remains the sole owner of operation activity and its cancel delegate.

## Failure-first proof

`HandleAsyncWhenGatewayFaultsReleasesRunningStateAndCancellationOwnership` injected an unexpected gateway defect after inspection. Before the fix, the exact defect escaped but `OperationRunning` and the disposed source's cancel delegate remained. The next Escape failed with `ObjectDisposedException`, proving both stale state and stale ownership.

## Changes

- Added the Presentation-owned, framework-neutral `AsyncWorkOwner` with immediate exact-defect observation, overlap rejection, and ordered shutdown.
- Routed both `CommanderApplication` startup and `CommanderWindow` pane work through that mechanism.
- Passed the owner token to initial navigation and pane intents.
- Made window close await pane work and startup work before gateway disposal.
- Reset session operation state on a gateway defect and reset the cancellation delegate in `finally`.
- Added deterministic owner tests for asynchronous fault observation, synchronous factory-defect propagation and token cleanup, overlap, successful replacement, null boundaries, and cancel/await/dispose order.

## Focused verification

- Locked solution restore: PASS.
- Solution build: PASS, zero warnings and errors.
- Application tests: PASS, 158/158.
- Presentation.WinUI tests: PASS, 73/73.
- Architecture tests: PASS, 5/5, including the no-raw-Task-field ownership proof.
- Targeted branch coverage: Application 100%; Presentation.WinUI 94.94%.
- Targeted Presentation mutation: PASS, 91.34% overall and 100% for `AsyncWorkOwner`.
- `eng/conformance.ps1 -Quiet`: PASS.
- `eng/security-check.ps1 -SkipProof`: PASS; all 18 adversarial cases remain registered.

Conformance/security and final CI evidence are recorded before integration. The PR body records the final CI identifiers without a result-only documentation commit.
