# Handoff — Window-adjustment CodeQL remediation — 2026-10-07

Status: informational

Updated through the session closure on 2026-10-08 JST (Issue #162).

## Start here

1. Resume the draft PR closing [Issue #162](https://github.com/hideyukiMORI/NeNeCommander/issues/162), branch `docs/162-session-closure`, worktree `D:/NeNeCommander/wt-162`. The final report, this handoff, and the state update are committed and pushed there. Documentation integration is pending; the primary checkout stays clean on `main` at `2ef9cb0` until that PR merges.
2. Inspect the closure PR head/base and checks. When ready, use `gh pr ready <number>` to request the required CI command `pwsh -NoProfile -File ./eng/check.ps1`; squash merge only after a successful final-candidate gate, then synchronize main. Do not repeat a successful matching CI gate locally. No new deep review or runtime test is required for these three Markdown files.
3. After the documentation is integrated and all unique evidence is retained, remove `wt-162` and its disposable body files only after the standard path, integration, dirty/untracked/ignored-file, unique-artifact, process, and link checks. Keep the branch and commits. Leave any policy-rejected cleanup explicit; do not bypass it.
4. The implementation work is complete: PR #159 merged as `e395039`; [PR #161](https://github.com/hideyukiMORI/NeNeCommander/pull/161) merged as `2ef9cb0` and closed Issue #160. Its final [verification comment](https://github.com/hideyukiMORI/NeNeCommander/pull/161#issuecomment-6041283963) supersedes the PR body's pending text. Do not restart the correction or its completed main analysis.
5. The next product task is [Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94). First identify a dedicated interactive Windows environment and test profile. Do not reimplement #100 or rerun its successful 125-percent evidence merely because the operator changed.

## Invariant, changes, and completed proof

Issue #160 made only the equivalent projection in `KeyHintWrapPanel.Place` and in the Control+W character-translation test explicit. Child order, lazy traversal, one DesiredSize read per child, wrapping arithmetic, `w`/U+0017 cases, and both assertions are unchanged. Existing layout and keyboard mechanisms remain canonical (ARC-001, SEC-008, CS-014, TST-001). No ADR or waiver was needed.

The paired report gives the exact local commands: locked Release restore, App build with zero warnings/errors, 38 affected Presentation tests with no failures/skips, changed-file Release formatting, Commit mode, and whitespace checks. All final commands exited 0. Dependency run `37639790444` and canonical Ready run `37639819472` then succeeded at head `1c7bc93e90245a1330b46ea03b7d1e053869d395` and base `e39503988896445fce1780137fc33779f3adbf27`. Canonical CI recorded 998 passed, zero failed, and six live-WSL `RootParameterAbsent` skips out of 1004 tests, with branch coverage 100.00% / 100.00% / 92.88% / 92.57% in Domain / Application / Infrastructure.Windows / Presentation.WinUI order.

[Main deep run 37641629223](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/37641629223) succeeded on integrated commit `2ef9cb0a2cd36408500e3742fd1eb860f351dd11`; mutation scores in the same order were 95.52% / 96.24% / 90.86% / 94.57%. CodeQL analysis `1909436445` matched that commit and `refs/heads/main`, with zero results/errors/warnings and all 511 C# files scanned. Alerts #104/#105 were read back as `fixed`; a separate main open-alert query returned zero. These results replace the two findings in older runs `37499633218` and `37597191129`. No further Issue #160 verification is pending.

Issue #162 changes only `docs/PROJECT_STATE.md`, `docs/reports/2026-10-07-window-adjustment-codeql-daily-report.md`, and this handoff. Its invariant is truthful separation of completed implementation evidence and outstanding environmental proof; the state file remains the single current checkpoint. Scope is documentation, with no behavior/schema changes or ADR/waiver. Its draft PR records the final Commit-mode, whitespace, local-link, and encoding results; the full integration gate remains pending as described above.

## Issue #94 execution prerequisites

No dedicated Windows guest, interactive session, or test profile was verified. The current console belongs to hide; Hyper-V worker presence alone proves no usable test environment. VM enumeration was denied. The missing information for the next session is the dedicated guest/session and account to use; resolve that before launching an input harness. No UI was launched and no host setting or input was changed in this continuation.

The execution contract remains the Issue #94 body and its Issue #100 follow-up comment:

- Real Windows-reported 100/150/200/300 percent, normal and 900-by-600-DIP narrow layouts, eight schemes, and configured high-contrast environments. A 125-percent run or simulated scale does not fill another cell.
- Separate keyboard cases for name entry, deletion, progress, conflict, settings, and window adjustment, including positive and negative routes, initial/returned focus, freeze, and filesystem effects confined to test-owned entries.
- A dedicated Windows user profile: verify the production-resolved settings location, preserve original bytes or absence, and restore only that owned profile. Production uses `Environment.GetFolderPath(LocalApplicationData)`; an environment-variable override is not isolation. The old #70 driver writes host settings and must not run on the shared desktop.
- Before each synthetic input, verify the owned process, session and root HWND plus the expected mode/modal and focused control. On focus loss, stop sending input; do not send blind cleanup key events into another application. The #100 driver checks HWND but lacks these per-input containment checks and points by default to a removed worktree; redesign these boundaries before reusing its observation routines.
- Record commit, binary/driver hashes, real DPI/high-contrast and topology, scheme, physical/DIP bounds, owned PID/HWND, UIA focus/bounds, screenshot hash and visual review, result, and reason for every FAIL/SKIP. Narrator requires actual speech evidence, not only UIA attributes.
- Keep #94 open while any required cell is missing. Do not alter host DPI, high contrast, existing settings, or the shared desktop to manufacture a pass.

The 125-percent #100 reactivation proof is already complete in its second run. Remaining helper cells include other scales, high contrast, Narrator and eight schemes; the Windows App SDK 2.5.1 cross-scale move and runtime taskbar seam also remain. The approved seam behavior itself does not change. Earlier live-WSL proof remains valid for its unchanged implementation and is separate from this UI matrix.

## Shutdown state and retained artifacts

- Source checkout: `C:/Users/info/WORKS/NeNeCommander`, clean and synchronized at `2ef9cb0` before the documentation closure is integrated.
- Completed worktrees `D:/NeNeCommander/wt-158` and `D:/NeNeCommander/wt-160`: removed after verified integration, identical committed trees, dirty/untracked/ignored-file inspection, evidence preservation, and process/link checks. Branches `docs/158-session-closure` and `fix/160-window-adjustment-codeql`, including their commits, remain.
- Unfinished documentation worktree: `D:/NeNeCommander/wt-162`, branch `docs/162-session-closure`; retained because the draft PR is not integrated. Its task body files are in `D:/NeNeCommander/closure-2026-10-08/` and are retained with it.
- `D:/NeNeCommander/run-37499633218/` and `D:/NeNeCommander/run-37597191129/`: downloaded artifacts from the two older main deep runs. Automatic approval review rejected recursive cleanup with `blocked by policy`.
- `D:/NeNeCommander/evidence-codeql-window/`: scoped verification logs and Issue/PR/comment body files. Automatic approval review also rejected an explicit non-recursive file-list cleanup with `blocked by policy`. No workaround deletion was attempted. The durable results are in the paired report and [PR #161 comments](https://github.com/hideyukiMORI/NeNeCommander/pull/161#issuecomment-6041374405).
- Existing `D:/NeNeCommander/evidence-100/`, `D:/NeNeCommander/mutation-100/`, `D:/NeNeCommander/run-37189427536/`, and `D:/NeNeCommander/closure-158/` were not changed. They belong to earlier work; inspect their evidence and ownership before any later cleanup.
- The bounded implementation and investigation agents completed their tasks. No local build, test, UI harness, or background work from this session remains running. No desktop interaction or shutdown operation is needed to preserve the committed handoff.
