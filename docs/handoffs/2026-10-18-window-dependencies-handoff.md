# Window dependency handoff — 2026-10-18

- Issue: [#318](https://github.com/hideyukiMORI/NeNeCommander/issues/318).
- Canonical path: `CommanderApplication.CreateWindow` → `CommanderWindowDependencies.Create` → the sole `CommanderWindow` constructor. The record carries references; existing sessions and async owners retain all state and lifetime duties.
- Changes: App composition root, window constructor, new App.Views dependency record, two focused architecture tests and glossary definition. The public six-parameter constructor becomes three parameters; no compatibility overload remains.
- Proof: the old six-parameter constructor fails the focused assertion (exit 2). The final affected Release build has zero warnings/errors; two parameter-boundary tests and the existing lifecycle ownership test pass (3/3, no skip). Changed-file whitespace formatting exits 0 with a workspace-load warning. Exact commands and limits are in the [daily report](../reports/2026-10-18-window-dependencies-daily-report.md).
- Scope: bounded App refactor under ADR-0061; no dependency/schema/security-boundary change or waiver. No full/deep CI, coverage/mutation, broad regression, live desktop/provider or release proof. This is not the global CS-013 gate.
- Evidence: `D:/NeNeCommander/evidence/issue-318/` retains RED/GREEN/build/format logs. The Issue PR and `completion.json` own final Commit/squash/main/cleanup results. Branches and commits remain after task cleanup.
- Authorized next work: provider-specific entry visibility glossary correction, then a current CS-013 census and responsibility-split/gate implementation order, each in its own Issue/PR. Do not start unrelated release #94 work.

PR #319 was squash merged at 19:52:27 JST as `839936e1a1e7e471b2c80df28087f1d223f4bf91`.
Main was synchronized, clean and tree-equal to the task. Cleanup preflight found only regenerable ignored build output, no unsaved/untracked work, no links and no active task process references. The archived script exited 0 and removed both `D:/NeNeCommander/worktrees/issue-318` and `D:/NeNeCommander/outputs/issue-318`; worktree registration removal was confirmed. Permanent evidence, branches/commits and the shared SDK remain. No cleanup request is pending.
