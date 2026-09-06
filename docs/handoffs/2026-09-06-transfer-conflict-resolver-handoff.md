# Handoff — transfer conflict resolver — 2026-09-06

Status: Draft implementation

Worktree: `C:\Users\info\WORKS\NeNeCommander-73`
Branch: `feat/73-transfer-conflict-resolver`
Base: verified `origin/main` / `991d7ffd24ba0e33a2f9a09a8b9c88d4eb2717cf`
Draft PR: [#91](https://github.com/hideyukiMORI/NeNeCommander/pull/91)

## Invariant and canonical mechanism

Complete-batch inspection and preflight precede every transfer effect. Conflict resolution retains the original frozen identity and never replaces it with a later entry. The sole path is `UserIntent` through `DualPaneSession.OperationAwaitingConflict` and `FileOperationGateway.ResumeAsync` to `IFileOperationPort`; no second copy/move engine exists.

The gateway-issued continuation owns the pending immutable `ConflictSet` and is a one-shot reference capability bound to the issuing gateway. Skip records `NotTransferred` without a filesystem effect. KeepBoth uses one validated candidate consistently for copy and verification, and a move deletes its source only after verification. Provider plan/conflict output must match every frozen source in order, identity, choice, disposition, and destination parent before any effect.

## Changed areas

- ADR-0039 and typed Application transfer outcomes, conflicts, choices, plans, continuation, gateway validation, operation state, and results.
- Windows-local collision discovery, provider-comparison-aware reservations, KeepBoth naming, containment/reparse/path-length rejection, and candidate-race re-conflict.
- WSL preflight planning from one revalidated entry while preserving same-distribution non-conflict behavior and existing cross-provider rejection.
- Existing session, presenter, command path, localized operation results, and one conflict modal with Cancel initial focus and Apply-to-all operation scope.
- Behavioral, adversarial, infrastructure, presentation, architecture, coverage, and focused mutation proof described in the matching [daily report](../reports/2026-09-06-transfer-conflict-resolver-daily-report.md).

The UI follows the approved semantic shell composition and keeps the two panes, one operation bar, and generated hints. Canvas input is directional; no final style, font, color, or new design token is claimed as approved. The conflict modal passes Enter and Space to the focused native decision button, starts focus on Cancel, and keeps Escape on the canonical mapped cancel path. Issue #72's active-pane session visibility is preserved independently from startup settings. Issue #74 settings persistence is not implemented.

## Saved proof

On base `991d7ffd`, the Release solution build passes with zero warnings and errors. Application passes 220/220, Infrastructure.Windows 144/144, Presentation.WinUI 79/79, and Architecture 5/5. Protected branch coverage passes at Domain 100.00%, Application 100.00%, Infrastructure.Windows 93.56%, and Presentation.WinUI 96.56%. Commit validation passes. Draft PR dependency review `34022555540` passes. Focused WSL adapter mutation scored 98.04%, and focused conflict-presentation mutation scored 94.64%. The optional `KeyboardIntentMapper.cs` diagnostic scored 93.33% overall while killing every mutant in the new native-control deferral method; four survivors remain in pre-existing constructor and context-reset code.

The Application three-file focused mutation diagnostic reported 89.47%, but this is not the normative project-level threshold run. A reported surviving KeepBoth disposition mutant was manually applied and caused the targeted adversarial test to fail. The inconsistent diagnostic is retained rather than retried or suppressed; the final full Application project mutation remains mandatory in exact-head deep review.

## Remaining integration work and release proof

1. Review the final documentation-sync head while the PR remains Draft. If any code or base changes, repeat affected focused checks.
2. Run the security-sensitive exact-head deep workflow, including the full Application project mutation threshold, dependency audit, repeated adversarial proof, and CodeQL. Resolve findings without exclusions, suppressions, waivers, or retry-based acceptance.
3. Separately from Issue #73 integration readiness, live WSL copy and deletion remain an unexecuted release environmental tier because `NENE_COMMANDER_WSL_TEST_ROOT` is unset and no live harness exists. Do not substitute unit tests for it.
4. Actual-window conflict keyboard interaction is also unexecuted release environmental proof. Issue #70 deliberately sent no keyboard input, and the current tree has no executable canonical test-owned conflict fixture or sanctioned runtime interaction harness; UIA exposes no invokable command route. The focused Presentation route test is not a claim of real WinUI interaction, and this Issue does not invent a synthetic harness.
5. Once deep review and review findings are complete, transition PR #91 from Draft to Ready to request the canonical CI command `pwsh -NoProfile -File ./eng/check.ps1`. Do not merge without fresh exact-head evidence.

Issue #85 / PR #86 is integrated in the base, so its dependency is resolved. Replace, directory merge, cross-provider Windows/WSL transfer, and settings persistence remain outside Issue #73.
