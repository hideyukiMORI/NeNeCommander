# Handoff — Window-adjustment CodeQL remediation — 2026-10-07

Status: informational

## Start here

1. Read the PR closing [Issue #160](https://github.com/hideyukiMORI/NeNeCommander/issues/160). Integration results are recorded there after this document's commit. Do not assume that a pending statement in this document means a completed remote check must be rerun.
2. Confirm local main is clean and synchronized. PR #159 was integrated as `e395039`, after Issue #100's `41a682c`; the new correction is on `fix/160-window-adjustment-codeql` until its PR merges.
3. Require a successful canonical Ready run for the final integration candidate. After merge, require a main CodeQL analysis at the new main commit with zero results/errors and both alerts #104/#105 fixed, and read the main open-alert count separately. Workflow success alone is insufficient.
4. If those results are already recorded and match, continue with Issue #94's dedicated-environment preparation. Do not reimplement #100 or rerun its successful 125-percent evidence merely because the operator changed.

## Invariant and correction

The only code change is the equivalent projection in `KeyHintWrapPanel.Place` and in the Control+W character-translation test. Child order, lazy traversal, one DesiredSize read per child, wrapping arithmetic, `w`/U+0017 cases, and both assertions are unchanged. Existing layout and keyboard mechanisms remain canonical (ARC-001, SEC-008, CS-014, TST-001). No ADR or waiver is necessary.

Focused verification is recorded in the paired report: locked Release restore, App build with zero warnings/errors, 38 affected Presentation tests with no failures/skips, changed-file Release formatting, Commit mode, and whitespace checks. All final commands exited 0. The full local suite and mutation were not repeated. The main deep workflow remains necessary to verify CodeQL on the integrated source; it is the existing repository-owned analysis path, not an additional local test run.

## Confirmed previous main evidence

Main deep runs `37499633218` and `37597191129` on `41a682c` both passed their executable tiers, but CodeQL analyses `1902667923` and `1907175157` each reported two findings. Alert #104 names the layout loop; #105 names the character-route test loop. Both are note-level `cs/linq/missed-select`. They must be fixed in source, not dismissed. The exact four mutation scores for both runs are in the paired report.

## Issue #94 execution prerequisites

No dedicated Windows guest, interactive session, or test profile was verified. The current console belongs to hide; Hyper-V worker presence alone proves no usable test environment. VM enumeration was denied. Request or identify the dedicated guest and account before launching an input harness.

The execution contract remains the Issue #94 body and its Issue #100 follow-up comment:

- Real Windows-reported 100/150/200/300 percent, normal and 900-by-600-DIP narrow layouts, eight schemes, and configured high-contrast environments. A 125-percent run or simulated scale does not fill another cell.
- Separate keyboard cases for name entry, deletion, progress, conflict, settings, and window adjustment, including positive and negative routes, initial/returned focus, freeze, and filesystem effects confined to test-owned entries.
- A dedicated Windows user profile: verify the production-resolved settings location, preserve original bytes or absence, and restore only that owned profile. An environment-variable override is not isolation.
- Before each synthetic input, verify the owned process, session and root HWND plus the expected mode/modal and focused control. On focus loss, stop sending input; do not send blind cleanup key events into another application.
- Record commit, binary/driver hashes, real DPI/high-contrast and topology, scheme, physical/DIP bounds, owned PID/HWND, UIA focus/bounds, screenshot hash and visual review, result, and reason for every FAIL/SKIP. Narrator requires actual speech evidence, not only UIA attributes.
- Keep #94 open while any required cell is missing. Do not alter host DPI, high contrast, existing settings, or the shared desktop to manufacture a pass.

The 125-percent #100 reactivation proof is already complete in its second run. Remaining helper cells include other scales, high contrast, Narrator and eight schemes; the 2.5.1 cross-scale move and runtime taskbar seam also remain. The approved seam behavior itself does not change.

## Artifacts and cleanup

- Source checkout: `C:/Users/info/WORKS/NeNeCommander`.
- Issue #160 worktree: `D:/NeNeCommander/wt-160`; branch `fix/160-window-adjustment-codeql`. Remove the extra worktree only after integration, clean/untracked/ignored-file inspection, evidence preservation, and process/link checks. Keep its branch and commits.
- Current diagnostic logs: `D:/NeNeCommander/evidence-codeql-window/`; final durable verification goes in the Issue #160 PR. Remove disposable logs after their durable evidence is recorded and no running job references them.
- Previous main deep artifacts: `D:/NeNeCommander/run-37499633218/` and `D:/NeNeCommander/run-37597191129/`. These remain the evidence for the discovered findings. An automatic approval review rejected the investigator's recursive cleanup attempt; it was not bypassed. No task depends on deleting these files.
- Existing #100 runtime/driver, mutation, and older scheduled-run evidence is untouched. Its retention/cleanup is separate from the new correction.
- `wt-158` was removed after verified integration; `docs/158-session-closure` and its commit were retained under hide's current cleanup rule.
