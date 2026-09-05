# Development Workflow

Status: normative

## One-time clone setup

Run `pwsh -NoProfile -File ./eng/bootstrap.ps1`. It verifies the exact SDK, configures `core.hooksPath=.githooks` for this repository, and runs lightweight commit checks. It does not run the full integration gate, install unpinned tools, or alter global Git configuration.

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

During implementation, test the changed behavior first, then its owning layer and affected consumers. Use project selection and test filters for diagnostic runs; build/restore the changed code before testing it, rather than trusting stale `--no-build` output. Record the scope and outcome. Changes to shared contracts, dependencies, test infrastructure, or policy need wider impact checks. Unknown impact must be investigated, not called covered. Do not postpone all testing until merge.

Commits run `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`; this is a lightweight policy/security/whitespace check. Keep commits small and coherent, and keep the PR draft while implementing or reviewing. Do not run the full suite merely because a commit, push, handoff, or draft PR was created.

At merge readiness, transition the final draft PR to Ready (`gh pr ready <number>`). This is the explicit request for the required CI `canonical-gate`, which runs from the repository root:

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

Wait for this full check on the final PR merge candidate, then squash merge through the protected PR path. Do not repeat it locally just to reproduce the same successful evidence. If the head changes, or main changes and the branch must be updated, return to Draft (`gh pr ready <number> --undo`), finish the work, then mark Ready again. A directly opened non-draft PR also needs this Draft to Ready transition. Never bypass a missing check or substitute a success for a skipped full job. Preserve strict main ruleset checks.

Do not invoke a subset as final evidence. If an environmental tier cannot run, report it precisely and do not claim it passed. Scheduled deep review, security-sensitive integration review, and release proof remain separate mandatory tiers. They are not required after every intermediate edit. Interrupted work may be handed off with scoped evidence and explicitly pending integration checks; it must not be described as merge-ready.

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
