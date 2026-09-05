# Runtime UI proof handoff — 2026-09-05

Status: informational

## Work item

- Issue: #70
- Branch: `test/70-runtime-ui-proof`
- Invariant: no synthetic keyboard input, machine-wide display/accessibility mutation, unrelated process termination, or non-test-owned filesystem mutation.
- Canonical mechanism: the shipped keyboard mapper, pane session, presenter, and window rendering path; no test backdoor.

## Completed evidence

At 125% DPI, all eight configured color schemes rendered at 900 × 600 dip. UIA found both panes visible, equal-height, and non-overlapping, and visual review found no clipping or unreadable reached state. Eight distinct screenshots and the detailed UIA bounds are retained under the local ignored `artifacts/implementation-sana/visual-runtime-proof` directory. The settings document's before/after hash matched. UIA exposed 91 elements and zero `InvokePattern` elements.

The exact source had already passed 400 tests with zero skips, including owner tests for F2 rename, F7 directory creation, F8 confirmation, and operation progress state/projection. This handoff does not relabel those automated tests as runtime UI proof.

## Remaining release-matrix evidence

1. Run on desktops configured for high contrast and the untested 100%, 150%, 200%, and 300% DPI points; record each environment rather than simulating it globally on hide's active desktop.
2. Observe F2/F7/F8 and copy/move progress in the actual window when an authorized non-synthetic interaction path is available. UIA alone cannot invoke them because the app intentionally has no button command surface.
3. Do not add an alternate production command path solely to make the proof easier.

No hide product decision is required for these gaps. They remain named release environmental proof under QLT-009 and QLT-011.

## Integration steps

Commit through the repository hook, open a Draft PR closing #70, confirm exact head/base, mark Ready, require canonical CI, squash merge, and synchronize clean `main`.
