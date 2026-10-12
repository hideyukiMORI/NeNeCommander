# ADR-0061: Run only the verification justified by the change

Status: accepted

Date: 2026-10-12

## Context

Issue #218. hide explicitly requested that documentation changes start no CI,
ordinary changes run only the necessary tests, and expensive validation run when
merging a substantial feature or when otherwise justified. hide also explicitly
chose to stop the three-day deep-review schedule. The preceding #190 session ran
a deep review containing the full gate and then repeated that gate for the daily
integration. A full gate took about twelve minutes; deep review took about
forty-five minutes. Neither cost is justified by a documentation-only update.

This decision supersedes ADR-0025's automatic readiness trigger, mandatory full
gate for every main integration, required-status ruleset, and daily integration
requirement, and the periodic execution requirements of QLT-013, SEC-008/009 and
TST-008. It changes execution cadence, not the protected compiler, architecture,
coverage, mutation, dependency-audit or destructive-operation safety rules.

## Decision

Choose the verification scope before executing it and record the reason and the
commands/results in the PR:

| Change | Required evidence |
|---|---|
| Documentation or comments only | Review the changed text, links and diff; no automatic CI, build, product tests, coverage or mutation. Commit checks remain local. |
| Small behavior fix or isolated feature | Build the affected code and run its behavior tests plus directly affected boundary/consumer tests. No automatic full suite. |
| Substantial feature, shared execution path or cross-layer contract | Run the full canonical gate once on the integration candidate when focused proof cannot establish the wider impact; explain that impact in the PR. |
| Security-sensitive behavior | Run the affected threat cases. Request deep review when identity, containment, deletion, credentials or another trust boundary needs broader proof. |
| Tooling, CI or policy only | Run the affected tool's positive/negative proofs and configuration checks. This is not by itself a reason to run unrelated product tests or mutation. |
| Release | Run the required release/environmental proof and obtain deep-review evidence within the existing 96-hour freshness bound. |

`eng/check.ps1` remains the sole full orchestrator, with full `Merge` mode as its
default and lightweight `Commit` mode for local hooks/bootstrap. `quality.yml`
has only `workflow_dispatch` and always executes the full command when requested.
`security-deep-review.yml` also has only `workflow_dispatch`; there is no cron or
PR-triggered deep run. Dependency review starts only for dependency manifests,
lock files, SDK/tool pins, build property/target files or workflow files.

Draft/Ready describes review state and does not request CI. Use
`gh workflow run quality.yml --ref <candidate-branch>` or
`gh workflow run security-deep-review.yml --ref <candidate-branch>` only when the
selected scope requires it. Before a merge-level run, include the intended base
in the candidate and record its commit. Before merging, compare the final inputs
with the evidence; a changed commit identifier alone does not invalidate results.
Reuse successful proof when the code, tests, dependencies and relevant tooling
inputs are unchanged. Re-run only the affected proof when they change. A deep
review already includes the full canonical gate, so do not also request quality
for the same inputs. An unrelated failing test is recorded separately instead of
starting repeated broad runs or unrelated repairs.

Issue PRs may target main directly. An integration branch remains useful for
related work, but a daily umbrella PR is not mandatory. Keep PRs and squash-only
merging, resolved review threads, no bypass actors, and protection against force
pushes and branch deletion. Remove the unconditional required-status-check rule:
GitHub cannot merge a PR requiring a check whose workflow does not run. The PR
reviewer/agent must verify the selected evidence before merging; GitHub no longer
enforces an always-present canonical check. Never manufacture a success status or
describe unexecuted tiers as passed.

## Alternatives rejected

- A tiny always-running CI job for documents still violates the requested zero-CI behavior.
- A skipped required job or synthetic success misrepresents evidence.
- A path-based required check stays pending when its workflow is filtered out.
- Keeping the three-day full/mutation schedule contradicts hide's explicit choice.
- Lower thresholds or source exclusions do not address unnecessary execution.

## Migration and proof

Update workflows, their conformance/negative proofs, the security policy and the
normative guidance atomically. Preserve the pre-commit hook and both gate modes.
Provide bounded `-Rule QLT-015` and `-Rule SEC-009` diagnostics in the existing
proof tools so this change does not require unrelated fixtures or product tests.
Verify valid configuration, rejected automatic full/deep triggers, rejected
missing dependency path filters, and unchanged full-gate/default protections.
Run Commit mode and read back the remote ruleset after changing it. This migration
uses that focused proof under hide's new policy; it does not repeat the old/full
product gate merely because CI policy changed. After integration, a documentation
closure PR demonstrates that zero workflow runs still permits normal squash merge.

The existing #190/main full and deep evidence remains valid for unchanged product
code; it is not claimed as proof of the new workflow configuration.
