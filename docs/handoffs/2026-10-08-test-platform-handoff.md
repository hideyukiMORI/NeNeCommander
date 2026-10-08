# Handoff — Test-platform pin alignment — 2026-10-08

Status: informational

## Start here

1. Read the PR closing [Issue #165](https://github.com/hideyukiMORI/NeNeCommander/issues/165) for its final head/base, dependency review, canonical Ready CI, squash commit, and cleanup. If matching success is recorded, reuse it; do not repeat the full gate, mutation, or completed UI evidence because the operator changed.
2. PR #163 is integrated as `ccf4423` after canonical run `37784392752`; its Issue #162 worktree and body files are removed, with branch and commit retained. Issue #160's verified CodeQL baseline `2ef9cb0`, analysis `1909436445`, and fixed alerts #104/#105 remain recorded in the previous handoff. No new CodeQL remediation is pending.
3. Issue #165 aligns MSTest.Sdk 4.5.1 and Microsoft.NET.Test.Sdk 18.10.1, CFG-002 and its negative fixture, ADR-0006/0048, and all five test lock files. ADR-0006 accepts the coverage graph's single new locked package, Mono.Cecil 0.11.6; three other declared System dependencies are framework-pruned. Production dependencies and behavior do not change.
4. Canonical testing remains MTP/Default profile; mutation remains VSTest under ADR-0048. The limited mutation checks in the paired report do not claim a full deep-review pass. The remaining layers stay in the scheduled tier.
5. The next product task is [Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94). hide explicitly confirmed on 2026-10-08 that its dedicated interactive Windows environment is not prepared. Do not mistake another local profile or a Hyper-V worker for an identified test environment.

## Issue #94 preparation boundary

Identify a dedicated Windows guest or machine, connection method, test account/SID, interactive session, test-owned filesystem root, and evidence output path. Confirm the production-resolved settings location within that profile. Windows-reported DPI, high contrast, and display topology must be preconfigured in the isolated environment; do not change hide's shared desktop to supply a missing cell.

The Issue #94 body and its Issue #100 follow-up comment remain the execution contract: real 100/150/200/300 percent, normal and 900-by-600-DIP narrow layouts, eight schemes, high contrast, independent positive/negative keyboard-modal cases, settings restoration, helper and Narrator evidence, the Windows App SDK 2.5.1 cross-scale move, and the refused taskbar-gap seam. Narrator requires speech evidence. Missing cells keep the Issue open and release readiness unmet.

Before any synthetic input, verify the owned PID/session/root HWND plus the expected mode/modal and focused control. On focus loss, stop without sending blind cleanup key events. `D:/NeNeCommander/evidence-100/Invoke-WindowAdjustmentEvidence.ps1` is an observation reference only: its default binary is stale, its checks are insufficient, and its `Stop-Run` sends held-key releases after ownership loss. Do not run it unchanged. A `LOCALAPPDATA` environment override does not isolate production settings, which use `Environment.GetFolderPath(LocalApplicationData)`.

Record commit and binary/driver hashes, real environment values, physical/DIP bounds, owned PID/HWND, UIA focus/bounds, screenshot hashes and visual review, plus each result and failure/skip reason. The successful existing 125-percent window-adjustment evidence need not be repeated.

## Work locations and cleanup

- Primary checkout: `C:/Users/info/WORKS/NeNeCommander`; keep main clean and synchronized.
- Issue #165 worktree: `D:/NeNeCommander/wt-165`, branch `build/165-mstest-sdk-4-5-1`. Remove it only after integration, durable evidence, clean/untracked/ignored inspection, absolute-path and link checks, and no running task references. Keep branch and commits.
- Current task output: `D:/NeNeCommander/outputs/resume-20261008/`, including downloaded package evidence, command logs, proof fixtures, and mutation reports. Once the final results are retained in the PR/report and no task refers to them, remove disposable outputs with the same safety checks. Read the PR for the actual cleanup result rather than assuming a directory still exists.
- Earlier retained directories `run-37499633218`, `run-37597191129`, and `evidence-codeql-window` under `D:/NeNeCommander/` had policy-rejected cleanup in the previous session and were left untouched. Existing `evidence-100`, `mutation-100`, `run-37189427536`, and `closure-158` also remain outside this task's cleanup scope.
