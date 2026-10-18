# Entry visibility handoff — 2026-10-18

Issue [#320](https://github.com/hideyukiMORI/NeNeCommander/issues/320) corrects only the glossary's provider-wide name prohibition. Windows local/UNC use Hidden/System attributes; WSL uses Linux dot-prefixed names and ignores projected Windows attributes. Upper layers consume the reported classification; `PaneReducer` remains the sole visible-set owner.

ADR-0024/0035/0057, both classification implementations and the three provider test sources agree. See the [daily report](../reports/2026-10-18-visibility-glossary-daily-report.md). ADR-0061 selects text/link/diff review and the normal Commit hook; no build/test/CI is required or claimed. No waiver, dependency, schema or runtime behavior changes. Prior #318 integration and cleanup completion are recorded in its own report/handoff.

The existing checkout is used, with no new worktree/output to clean. Evidence and final integration/main-sync results reside in `D:/NeNeCommander/evidence/visibility-glossary-20261018/`. Next: the already-authorized CS-013 census and responsibility-split/gate adoption plan, without silently enabling a global gate.

PR #321 was squash merged at 19:55:57 JST as `9a6d45fb474bd8f017e9668761e9de0b4e05449e`.
Main synchronization, clean state and task-tree equality are confirmed. No task cleanup remains.
