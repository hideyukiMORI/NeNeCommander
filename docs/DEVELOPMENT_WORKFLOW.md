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

Select the scope from ADR-0061 before running tests. Documentation/comment-only work receives text/link/diff review and no automatic CI, build, product test, coverage or mutation. For a behavior change, build and test its owning code and directly affected consumers; use selected projects and filters. A substantial feature or shared contract requires the full gate only when focused evidence cannot establish the wider impact. Explain that impact in the PR. Tool/CI/policy changes use the affected tool's positive/negative proof rather than unrelated product suites.

Commits keep `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`, a local policy/security/whitespace check. Draft/Ready controls review state, not full-test execution. Dependency-review CI runs at readiness only for dependency inputs. A relevant dependency change after readiness needs a fresh Draft-to-Ready transition; documentation-only updates do not. Keep work coherent and use squash PR integration; a daily integration branch is optional.

When wider validation is necessary, include the intended base in the candidate and explicitly request one existing workflow:

```powershell
gh workflow run quality.yml --ref <candidate-branch>
gh workflow run security-deep-review.yml --ref <candidate-branch>
```

Choose quality for full validation, or deep review when security/mutation proof is necessary. Deep review already executes `pwsh -NoProfile -File ./eng/check.ps1`; do not run both for the same inputs. Both workflows are manual-only, with no schedule. The default full gate and protected thresholds remain unchanged. Release/environmental proof remains separately applicable.

Before merge, inspect the final diff, the selected evidence and all applicable check failures. Main keeps PR/squash and force-push/deletion protections but does not require an always-present CI status, because documents intentionally have none. A relevant code, test, dependency or tooling input change invalidates the affected proof; a new commit hash, docs-only update, push or merge does not invalidate unchanged proof. Do not rerun a successful suite merely to reproduce it on another machine or at another workflow stage. Record unrelated existing failures separately instead of starting broad retries or unrelated fixes. Never claim skipped/unexecuted tiers passed.

For verification-policy diagnostics, `eng/prove-gates.ps1 -Rule QLT-015` and `eng/prove-security.ps1 -Rule SEC-009` execute only those changed rules' negative fixtures. The default `All` still executes every fixture when the full gate is requested.

## Verification units and reuse

hide clarified on 2026-11-28 that impact selection must work within a layer, not
only between projects. Use this procedure under ADR-0061 before an expensive run:

1. Select the changed behavior and its direct contract/dependency boundaries.
   Being in the same project is not, by itself, a reason to rerun other features.
   For example, overwrite, settings persistence and WSL operations are separate
   candidates unless their shared changes establish an actual dependency.
2. Separate behavior tests, native failure/race proof, coverage, security analysis
   and mutation into reviewable units. Record each selected unit's purpose,
   exact command, relevant source/test/dependency/tool inputs and original result.
   A common contract or tool change invalidates every unit that actually consumes
   it; do not infer independence from directory names alone.
3. Reuse completed successful units whose relevant inputs remain unchanged.
   Repair a failure through focused cases first. Do not automatically restart a
   whole project or full/deep gate after each correction, commit or handoff.
4. Request a whole integration-level run only when the remaining wider impact
   cannot be proved by the selected units. State that gap before starting it,
   and consolidate local corrections before the expensive final acceptance run.
5. Preserve results when each unit completes so an interruption does not discard
   other completed units. An unfinished unit is unproved; never mark it passed
   or imply the current tool can resume an unrecorded partial run.
6. Record each unit's start/end time and elapsed duration, and expose completed
   versus selected unit counts during long validation. Estimate remaining time
   only from observed representative durations and a known remaining scope.
   A live process or elapsed time alone is not a completion percentage or ETA.
   If the current runner provides no intermediate counts, report that limitation
   explicitly instead of presenting a prior run's duration as a current forecast.

This is an execution and evidence-selection procedure, not a new gate or an
implemented mutation checkpoint facility. The canonical commands, protected
thresholds and required destructive-boundary proof remain unchanged. Focused
mutation is diagnostic evidence for its stated scope, not a whole-project score.
Do not combine incompatible or incomplete mutation reports into synthetic whole
success. Splitting the canonical mutation runner or changing its aggregation
requires its own ADR and focused positive/negative tooling proof; it must not
start unrelated product suites merely to validate the tooling change.

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

## Blocked cleanup and daily reports

hide's decision on 2026-10-13: completed work must not accumulate disposable output silently when automatic approval blocks its cleanup. The existing daily report and handoff own the pending record; this is a session procedure, not a new executable gate or deletion service.

- Before proposing deletion, confirm the exact absolute targets, main integration, saved and untracked/ignored files, permanent evidence, unique artifacts, active references and links. Retain branches, commits, permanent evidence, unfinished work and unrelated data. Put any needed evidence in its permanent location first.
- If automatic approval rejects cleanup, record the rejected action and the returned reason. Do not retry the rejection with another deletion mechanism. At daily-report delivery, explicitly ask hide to execute the prepared command under the literal marker `! hide実行待ち：後片付け`, at the end of both the report and its user-facing summary. Include the absolute target list, the evidence retained, the rejection reason and one copyable PowerShell line. The same pending record belongs in the handoff.
- Make the command concrete before requesting execution. It may call an inspectable script stored outside its deletion targets. Limit it to the verified targets, handle known links without following them, and tolerate already-deleted targets. Prefer one native directory-level removal per target after bounded preflight; do not add a custom per-file validation/deletion loop or rehash unchanged archived evidence without a specific unresolved reason. Directory-level removal still deletes individual filesystem entries internally and makes no promise of instantaneous completion.
- An unidentified item is pending identification, not permission to widen deletion to its parent, an unrestricted wildcard or the whole recycle bin. State the unresolved identity separately and omit it from the executable targets until it can be identified safely.
- Keep cleanup marked pending until hide reports execution and a completion result or read-only absence check confirms the targets are gone. After interruption or failure, record the remaining exact targets; carry them into the next report/handoff and refresh the command as needed. Do not send a duplicate execution request while the known cleanup command is still running. Product integration and cleanup completion are separate facts.

When no cleanup remains, state that briefly; do not emit an empty `!` request. This procedure does not make the agent execute a deletion on hide's behalf after an automatic rejection.
