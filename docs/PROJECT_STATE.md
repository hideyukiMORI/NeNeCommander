# Project State

Status: normative

- Stage: `implementation`
- Production code: `permitted`
- Canonical gate: `pwsh -NoProfile -File ./eng/check.ps1`
- Product specification source reviewed: external `NeNe_Commander_Product_Spec.md`
- Public repository: `https://github.com/hideyukiMORI/NeNeCommander`
- Active initial work item: `#1`
- Policy foundation authorized by hide on: `2026-09-02`

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
