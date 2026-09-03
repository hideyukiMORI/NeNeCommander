# Project State

Status: normative

- Stage: `implementation`
- Production code: `permitted`
- Canonical gate: `pwsh -NoProfile -File ./eng/check.ps1`
- Product specification source reviewed: external `NeNe_Commander_Product_Spec.md`
- Public repository: `https://github.com/hideyukiMORI/NeNeCommander`
- Active initial work item: `#1` (closed)
- Completed vertical slice: [Issue #3](https://github.com/hideyukiMORI/NeNeCommander/issues/3) (closed by PR #4)
- Completed vertical slice: [Issue #6](https://github.com/hideyukiMORI/NeNeCommander/issues/6) (closed by PR #7)
- Completed vertical slice: [Issue #9](https://github.com/hideyukiMORI/NeNeCommander/issues/9) (closed by PR #10)
- Completed vertical slice: [Issue #12](https://github.com/hideyukiMORI/NeNeCommander/issues/12) (closed by PR #13)
- Active work item: [Issue #15](https://github.com/hideyukiMORI/NeNeCommander/issues/15)
- Policy foundation authorized by hide on: `2026-09-02`

## Current checkpoint

- Verified implementation baseline: `f8a500b6db215ee312571b0b3fb51bac88bbde40`
- Daily report: [`docs/reports/2026-09-04-file-operation-adapter-daily-report.md`](reports/2026-09-04-file-operation-adapter-daily-report.md)
- Handoff: [`docs/handoffs/2026-09-04-file-operation-adapter-handoff.md`](handoffs/2026-09-04-file-operation-adapter-handoff.md)
- Previous checkpoints: [`docs/reports/2026-09-03-dual-pane-daily-report.md`](reports/2026-09-03-dual-pane-daily-report.md), [`docs/handoffs/2026-09-03-dual-pane-handoff.md`](handoffs/2026-09-03-dual-pane-handoff.md), [`docs/reports/2026-09-03-pane-navigation-daily-report.md`](reports/2026-09-03-pane-navigation-daily-report.md), [`docs/handoffs/2026-09-03-pane-navigation-handoff.md`](handoffs/2026-09-03-pane-navigation-handoff.md), [`docs/reports/2026-09-03-directory-listing-daily-report.md`](reports/2026-09-03-directory-listing-daily-report.md), [`docs/handoffs/2026-09-03-directory-listing-handoff.md`](handoffs/2026-09-03-directory-listing-handoff.md), [`docs/reports/2026-09-03-daily-report.md`](reports/2026-09-03-daily-report.md), [`docs/handoffs/2026-09-03-initial-foundation-handoff.md`](handoffs/2026-09-03-initial-foundation-handoff.md)
- Completed product vertical slices: one Windows local directory read projected onto the left pane (ADR-0010, ADR-0011); keyboard focus movement and directory entry/parent navigation in the left pane (ADR-0012); the right pane as a second pane session with `Tab` switching the active pane (ADR-0013); the Windows local production adapter for `IFileOperationPort` (ADR-0014).
- Current product vertical slice: `F6` moving the active pane's item to the passive pane through `FileOperationGateway` ([Issue #15](https://github.com/hideyukiMORI/NeNeCommander/issues/15), ADR-0015).
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
