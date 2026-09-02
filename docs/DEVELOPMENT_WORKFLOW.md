# Development Workflow

Status: normative

## One-time clone setup

Run `pwsh -NoProfile -File ./eng/bootstrap.ps1`. It verifies the exact SDK, configures `core.hooksPath=.githooks` for this repository, and runs the canonical gate. It does not install unpinned tools or alter global Git configuration.

## Before editing

1. Read `AGENTS.md` and `docs/PROJECT_STATE.md` completely.
2. Read the documents governing the affected behavior.
3. Inspect the working tree and preserve unrelated changes.
4. Identify the owning layer, canonical mechanism, invariant, and required proof.
5. If no canonical mechanism exists, write an ADR before production code.

Every implementation change cites one focused GitHub Issue with acceptance criteria. The directly authorized 2026-09-02 policy bootstrap and initial vertical slice in Issue #1 are the sole repository-initialization exception to branching from an existing `main`.

## Change shape

A complete change contains, in one atomic review unit:

- the smallest implementation at the owning layer;
- tests that fail without it;
- conformance or architecture proof when a boundary changes;
- documentation and glossary updates when contracts change;
- lock-file updates when an approved dependency changes;
- design-token or resource updates when UI presentation changes;
- no compatibility wrapper or second path unless a time-bounded migration ADR requires it.

## Decision protocol

Use this order:

1. Find the existing canonical mechanism.
2. Extend it if the invariant remains coherent.
3. If it cannot express the requirement, propose one replacement mechanism in an ADR.
4. Migrate callers and tests atomically.
5. Delete the old mechanism in the same change.

Do not create parallel `V2`, `Legacy`, `New`, `Alternative`, or temporary implementations. Feature flags that keep two architectural paths require an ADR with an expiry and removal proof.

## Verification

Run from the repository root:

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

Do not invoke a subset as final evidence. If an environmental tier cannot run, report it precisely and do not claim it passed. The canonical non-environmental gate must still pass.

## Review evidence

Every handoff states:

- the invariant and canonical mechanism affected;
- files changed;
- behavior and negative cases proved;
- exact gate command and exit result;
- environmental tests run or still required;
- ADR or waiver identifier, if applicable.

## Git lifecycle and generated output

Issue, branch, commit, and pull-request rules are defined only in `docs/COMMIT_CONVENTIONS.md`. Do not commit build output, secrets, local paths, signing material, generated intermediates, or editor state. Regeneration must be deterministic and leave a clean working tree.
