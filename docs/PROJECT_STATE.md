# Project State

Status: normative

- Stage: `implementation`
- Production code: `permitted`
- Canonical gate: `pwsh -NoProfile -File ./eng/check.ps1`
- Verification cadence: ADR-0025, authorized by hide on `2026-09-05`: targeted tests during implementation; lightweight commit checks; full required CI gate at Draft-to-Ready immediately before Issue/PR integration. Scheduled/security/release deep review remains mandatory.
- Product specification source reviewed: external `NeNe_Commander_Product_Spec.md`
- Public repository: `https://github.com/hideyukiMORI/NeNeCommander`
- Active initial work item: `#1` (closed)
- Completed vertical slice: [Issue #3](https://github.com/hideyukiMORI/NeNeCommander/issues/3) (closed by PR #4)
- Completed vertical slice: [Issue #6](https://github.com/hideyukiMORI/NeNeCommander/issues/6) (closed by PR #7)
- Completed vertical slice: [Issue #9](https://github.com/hideyukiMORI/NeNeCommander/issues/9) (closed by PR #10)
- Completed vertical slice: [Issue #12](https://github.com/hideyukiMORI/NeNeCommander/issues/12) (closed by PR #13)
- Completed vertical slice: [Issue #15](https://github.com/hideyukiMORI/NeNeCommander/issues/15) (closed by PR #16)
- Completed fix: [Issue #18](https://github.com/hideyukiMORI/NeNeCommander/issues/18) (closed by PR #19)
- Completed vertical slice: [Issue #21](https://github.com/hideyukiMORI/NeNeCommander/issues/21) (closed by PR #22)
- Completed vertical slice: [Issue #24](https://github.com/hideyukiMORI/NeNeCommander/issues/24) (closed by PR #25)
- Completed vertical slice: [Issue #28](https://github.com/hideyukiMORI/NeNeCommander/issues/28) (closed by PR #29)
- Completed vertical slice: [Issue #31](https://github.com/hideyukiMORI/NeNeCommander/issues/31) (closed by PR #32)
- Completed vertical slice: [Issue #34](https://github.com/hideyukiMORI/NeNeCommander/issues/34) (closed by PR #35)
- Completed vertical slice: [Issue #37](https://github.com/hideyukiMORI/NeNeCommander/issues/37) (closed by PR #38)
- Completed design pass: [Issue #40](https://github.com/hideyukiMORI/NeNeCommander/issues/40) (closed by PR #41, PR #42)
- Completed vertical slice: [Issue #43](https://github.com/hideyukiMORI/NeNeCommander/issues/43) (closed by PR #44)
- Completed vertical slice: [Issue #46](https://github.com/hideyukiMORI/NeNeCommander/issues/46) (closed by PR #47)
- In-progress vertical slice: [Issue #49](https://github.com/hideyukiMORI/NeNeCommander/issues/49) in Draft PR #53 at `48d0b8c`: focused tests and deep review pass; interactive-desktop runtime verification is still owed
- Completed gate-performance work: [Issue #54](https://github.com/hideyukiMORI/NeNeCommander/issues/54) (closed by PR #55); ADR-0026 centralizes generated-directory pruning without dropping current-tree inputs
- Completed I/O responsiveness work: [Issue #56](https://github.com/hideyukiMORI/NeNeCommander/issues/56) (closed by PR #60); ADR-0027 schedules synchronous Windows local provider work through one shared boundary
- In-progress presentation-performance work: [Issue #57](https://github.com/hideyukiMORI/NeNeCommander/issues/57) on branch `perf/57-incremental-pane-projection`; ADR-0028 retains row sources and replaces only affected rows
- Policy foundation authorized by hide on: `2026-09-02`

## Current checkpoint

- Verified implementation baseline: `9f0eac9f073d4948eaec3c0b429558fb777e6058`
- Daily report: [`docs/reports/2026-09-05-hidden-item-visibility-daily-report.md`](reports/2026-09-05-hidden-item-visibility-daily-report.md)
- Handoff: [`docs/handoffs/2026-09-05-hidden-item-visibility-handoff.md`](handoffs/2026-09-05-hidden-item-visibility-handoff.md)
- Gate-performance report: [`docs/reports/2026-09-05-proof-fixture-performance-daily-report.md`](reports/2026-09-05-proof-fixture-performance-daily-report.md)
- Gate-performance handoff: [`docs/handoffs/2026-09-05-proof-fixture-performance-handoff.md`](handoffs/2026-09-05-proof-fixture-performance-handoff.md)
- I/O execution report: [`docs/reports/2026-09-05-windows-io-execution-daily-report.md`](reports/2026-09-05-windows-io-execution-daily-report.md)
- I/O execution handoff: [`docs/handoffs/2026-09-05-windows-io-execution-handoff.md`](handoffs/2026-09-05-windows-io-execution-handoff.md)
- Incremental projection report: [`docs/reports/2026-09-05-incremental-pane-projection-daily-report.md`](reports/2026-09-05-incremental-pane-projection-daily-report.md)
- Incremental projection handoff: [`docs/handoffs/2026-09-05-incremental-pane-projection-handoff.md`](handoffs/2026-09-05-incremental-pane-projection-handoff.md)
- Design brief: [`docs/design/2026-09-04-design-brief.md`](design/2026-09-04-design-brief.md)
- Previous checkpoints: [`docs/reports/2026-09-05-direction-c-layout-daily-report.md`](reports/2026-09-05-direction-c-layout-daily-report.md), [`docs/handoffs/2026-09-05-direction-c-layout-handoff.md`](handoffs/2026-09-05-direction-c-layout-handoff.md), [`docs/reports/2026-09-05-color-scheme-daily-report.md`](reports/2026-09-05-color-scheme-daily-report.md), [`docs/handoffs/2026-09-05-color-scheme-handoff.md`](handoffs/2026-09-05-color-scheme-handoff.md), [`docs/reports/2026-09-04-rename-daily-report.md`](reports/2026-09-04-rename-daily-report.md), [`docs/handoffs/2026-09-04-rename-handoff.md`](handoffs/2026-09-04-rename-handoff.md), [`docs/reports/2026-09-04-create-directory-daily-report.md`](reports/2026-09-04-create-directory-daily-report.md), [`docs/handoffs/2026-09-04-create-directory-handoff.md`](handoffs/2026-09-04-create-directory-handoff.md), [`docs/reports/2026-09-04-operation-progress-daily-report.md`](reports/2026-09-04-operation-progress-daily-report.md), [`docs/handoffs/2026-09-04-operation-progress-handoff.md`](handoffs/2026-09-04-operation-progress-handoff.md), [`docs/reports/2026-09-04-cancel-operation-daily-report.md`](reports/2026-09-04-cancel-operation-daily-report.md), [`docs/handoffs/2026-09-04-cancel-operation-handoff.md`](handoffs/2026-09-04-cancel-operation-handoff.md), [`docs/reports/2026-09-04-copy-daily-report.md`](reports/2026-09-04-copy-daily-report.md), [`docs/handoffs/2026-09-04-copy-handoff.md`](handoffs/2026-09-04-copy-handoff.md), [`docs/reports/2026-09-04-confirmed-delete-daily-report.md`](reports/2026-09-04-confirmed-delete-daily-report.md), [`docs/handoffs/2026-09-04-confirmed-delete-handoff.md`](handoffs/2026-09-04-confirmed-delete-handoff.md), [`docs/reports/2026-09-04-space-selection-daily-report.md`](reports/2026-09-04-space-selection-daily-report.md), [`docs/handoffs/2026-09-04-move-handoff.md`](handoffs/2026-09-04-move-handoff.md), [`docs/reports/2026-09-04-move-daily-report.md`](reports/2026-09-04-move-daily-report.md), [`docs/reports/2026-09-04-file-operation-adapter-daily-report.md`](reports/2026-09-04-file-operation-adapter-daily-report.md), [`docs/handoffs/2026-09-04-file-operation-adapter-handoff.md`](handoffs/2026-09-04-file-operation-adapter-handoff.md), [`docs/reports/2026-09-03-dual-pane-daily-report.md`](reports/2026-09-03-dual-pane-daily-report.md), [`docs/handoffs/2026-09-03-dual-pane-handoff.md`](handoffs/2026-09-03-dual-pane-handoff.md), [`docs/reports/2026-09-03-pane-navigation-daily-report.md`](reports/2026-09-03-pane-navigation-daily-report.md), [`docs/handoffs/2026-09-03-pane-navigation-handoff.md`](handoffs/2026-09-03-pane-navigation-handoff.md), [`docs/reports/2026-09-03-directory-listing-daily-report.md`](reports/2026-09-03-directory-listing-daily-report.md), [`docs/handoffs/2026-09-03-directory-listing-handoff.md`](handoffs/2026-09-03-directory-listing-handoff.md), [`docs/reports/2026-09-03-daily-report.md`](reports/2026-09-03-daily-report.md), [`docs/handoffs/2026-09-03-initial-foundation-handoff.md`](handoffs/2026-09-03-initial-foundation-handoff.md)
- Completed product vertical slices: one Windows local directory read projected onto the left pane (ADR-0010, ADR-0011); keyboard focus movement and directory entry/parent navigation in the left pane (ADR-0012); the right pane as a second pane session with `Tab` switching the active pane (ADR-0013); the Windows local production adapter for `IFileOperationPort` (ADR-0014); `F6` moving the active pane's item to the passive pane through `FileOperationGateway` (ADR-0015); `F8` permanently deleting the active pane's items after a modal confirmation state (ADR-0016); `F5` copying the active pane's items to the passive pane through the shared transfer path (ADR-0017); `Escape` cancelling a running file operation through a session-owned cancellation token (ADR-0018); typed per-source progress of a running operation reported through the session and rendered beside the status (ADR-0019); `F7` creating a directory in the active pane's location through a session-owned name entry (ADR-0020); `F2` renaming the focus item through the same name entry (ADR-0021); the color scheme selected through the settings document and applied as one scheme resource dictionary at the composition root (ADR-0022); the approved Direction C shell layout with tokenized values, closed row marks, one operation bar with a closed tone, and key hints generated from the canonical key map (ADR-0023).
- Next vertical slice: finish Draft PR #53 for hidden-item visibility after non-interfering interactive-desktop proof becomes possible. Its focused tests and deep review pass, but it must absorb the latest `main`, return to a final Ready candidate, and obtain fresh canonical CI evidence before merge. The hidden-visibility keystroke, which needs a keyboard-model revision, and the collision resolver (FS-007) follow.
- Open security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)

## Implementation transition

The implementation stage was activated on 2026-09-02 by the same change that:

1. records an accepted ADR for the final project graph;
2. creates every project declared by `eng/architecture.json`;
3. activates solution restore, format, build, test, architecture, and conformance checks in `eng/check.ps1`;
4. adds at least one positive and one negative proof for every custom conformance rule used by production code;
5. maps every case in `eng/adversarial-cases.json` to an executable adversarial test;
6. activates coverage, dependency audit, CodeQL, and mutation execution without exclusions or baselines;
7. changed this file to `Stage: implementation` and `Production code: permitted` only after both the canonical and deep-review gates passed.

The stage marker remains a safety interlock, not a roadmap label. It may never be changed alone or reverted to hide failing implementation gates.
