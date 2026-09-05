# Task lifecycle ownership handoff — 2026-09-05

Status: informational

## Work item

- Issue: #59
- Branch: `fix/59-task-lifecycle-ownership`
- Decision: ADR-0030
- Invariant: framework-started work is observed by one owner, and shutdown cancels, awaits, then disposes.
- Canonical mechanism: `AsyncWorkOwner` for launch/window tasks; `DualPaneSession.StartAsync` for operation state and its cancellation delegate.

## Verification checkpoint

- Failure-first session proof reproduced stale `OperationRunning` and `ObjectDisposedException` on the next Escape.
- Locked restore and solution build: PASS, zero warnings and errors.
- Full Application 158, Presentation.WinUI 73, and Architecture 5: PASS, including the no-raw-Task-field ownership proof.
- Targeted branch coverage: Application 100%; Presentation.WinUI 94.94%.
- Targeted Presentation mutation: PASS, 91.34% overall and 100% for `AsyncWorkOwner`.
- Conformance and security without negative fixtures: PASS; all 18 adversarial cases remain registered.
- The owner tests cover asynchronous defect observation, immediate replacement races, synchronous factory-defect propagation and token cleanup, overlap rejection, successful replacement, null boundaries, and cancel → await → dispose ordering.

## Integration steps

1. Review the owner race boundaries, exact-once defect publication, and nested close cleanup.
2. Run conformance/security and the affected full test suites.
3. Run one integration deep review because cancellation and shutdown ownership are safety-sensitive.
4. Commit through the existing Commit-mode hook, push, and open a Draft PR closing #59.
5. Verify final remote head/latest base, mark Ready, require fresh canonical CI, then squash merge and synchronize clean `main`.

## Remaining environmental proof

No keyboard or focus injection is needed. A packaged/runtime close smoke remains environmental evidence for the WinUI framework event itself; deterministic tests own the lifecycle decisions and ordering.
