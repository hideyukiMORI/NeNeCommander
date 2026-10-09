# Handoff — Test-platform pin alignment — 2026-10-08

Status: informational

Updated through hide's session-closure request on 2026-10-08 JST (Issue #167).

## Start here

1. Resume the documentation-only draft PR closing [Issue #167](https://github.com/hideyukiMORI/NeNeCommander/issues/167), branch `docs/167-session-closure`, worktree `D:/NeNeCommander/wt-167`. These final documents are committed and pushed there; the primary checkout remains clean on implementation main `55d911b` until this closure is integrated. Read the PR for its exact head and verification results.
2. Inspect that documentation PR's head/base and checks, transition Draft to Ready, and require its successful `pwsh -NoProfile -File ./eng/check.ps1` CI result before squash merge. Then synchronize main and safely remove the closure worktree/body files, retaining branch and commits. No new runtime or deep review is needed for the three Markdown files, and a matching successful full CI is not repeated locally.
3. [PR #166](https://github.com/hideyukiMORI/NeNeCommander/pull/166) completed Issue #165 as `55d911b7670164ca336f882312629136b2ce4dd1` on 2026-10-08 at 22:59:59 JST. Dependency run `37787074054` and canonical Ready run `37787104257` succeeded at head `f3f6b0f13af3f8a04be35ea5fcb0dc2398cf58af`, base `ccf4423b2c883acb3ab82684d4e5f1022076cae3`. The canonical gate recorded 998 passed, zero failed, and six live-WSL `RootParameterAbsent` skips out of 1004; branch coverage was 100.00% / 100.00% / 92.88% / 92.57% in Domain / Application / Infrastructure.Windows / Presentation.WinUI order. PR #164 is closed as replaced. No Issue #165 integration check is pending; reuse this evidence.
4. PR #163 is integrated as `ccf4423` after canonical run `37784392752`; its Issue #162 worktree and body files are removed, with branch and commit retained. Issue #160's verified CodeQL baseline `2ef9cb0`, analysis `1909436445`, and fixed alerts #104/#105 remain recorded in the previous handoff. No new CodeQL remediation is pending.
5. Issue #165 aligned MSTest.Sdk 4.5.1 and Microsoft.NET.Test.Sdk 18.10.1, CFG-002 and its negative fixture, ADR-0006/0048, and all five test lock files. ADR-0006 accepts the coverage graph's single new locked package, Mono.Cecil 0.11.6; three other declared System dependencies are framework-pruned. Production dependencies and behavior did not change. Canonical testing remains MTP/Default profile; mutation remains VSTest under ADR-0048. The limited mutation checks in the paired report do not claim a full deep-review pass; the remaining layers stay in the scheduled tier.
6. The next product task is [Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94). hide explicitly confirmed on 2026-10-08 that its dedicated interactive Windows environment is not prepared. Do not mistake another local profile or a Hyper-V worker for an identified test environment. No local build, test, UI harness, or background agent task from this session remains running.

## Issue #94 preparation boundary

Identify a dedicated Windows guest or machine, connection method, test account/SID, interactive session, test-owned filesystem root, and evidence output path. Confirm the production-resolved settings location within that profile. Windows-reported DPI, high contrast, and display topology must be preconfigured in the isolated environment; do not change hide's shared desktop to supply a missing cell.

The Issue #94 body and its Issue #100 follow-up comment remain the execution contract: real 100/150/200/300 percent, normal and 900-by-600-DIP narrow layouts, eight schemes, high contrast, independent positive/negative keyboard-modal cases, settings restoration, helper and Narrator evidence, the Windows App SDK 2.5.1 cross-scale move, and the refused taskbar-gap seam. Narrator requires speech evidence. Missing cells keep the Issue open and release readiness unmet.

Before any synthetic input, verify the owned PID/session/root HWND plus the expected mode/modal and focused control. On focus loss, stop without sending blind cleanup key events. `D:/NeNeCommander/evidence-100/Invoke-WindowAdjustmentEvidence.ps1` is an observation reference only: its default binary is stale, its checks are insufficient, and its `Stop-Run` sends held-key releases after ownership loss. Do not run it unchanged. A `LOCALAPPDATA` environment override does not isolate production settings, which use `Environment.GetFolderPath(LocalApplicationData)`.

Record commit and binary/driver hashes, real environment values, physical/DIP bounds, owned PID/HWND, UIA focus/bounds, screenshot hashes and visual review, plus each result and failure/skip reason. The successful existing 125-percent window-adjustment evidence need not be repeated.

## Work locations and cleanup

- Primary checkout: `C:/Users/info/WORKS/NeNeCommander`, clean and synchronized at `55d911b` before this documentation closure is integrated.
- Completed worktrees `D:/NeNeCommander/wt-162` and `D:/NeNeCommander/wt-165`: removed after integration and safety checks. Branches `docs/162-session-closure` and `build/165-mstest-sdk-4-5-1`, with their commits, remain.
- Retained implementation output: `D:/NeNeCommander/outputs/resume-20261008/`, approximately 132 MB, contains downloaded package evidence, command logs, proof fixtures, and mutation reports. Automatic approval review rejected the operation containing its recursive deletion with `blocked by policy`. No alternative deletion of that output was attempted. Results are durably summarized in PR #166 and the paired report; retention is due to the rejection, not unfinished implementation or a running job.
- Unfinished documentation worktree: `D:/NeNeCommander/wt-167`, branch `docs/167-session-closure`; body files: `D:/NeNeCommander/outputs/closure-20261008/`. Retain both while Issue #167's draft PR is not integrated. After successful integration, inspect absolute paths, tree equality, untracked/ignored files, unique evidence, running references, and links before removing them. Keep branch and commits.
- Earlier retained directories `run-37499633218`, `run-37597191129`, and `evidence-codeql-window` under `D:/NeNeCommander/` had policy-rejected cleanup in the previous session and were left untouched. Existing `evidence-100`, `mutation-100`, `run-37189427536`, and `closure-158` also remain outside this task's cleanup scope.
