# ADR-0025: Run the full gate at merge readiness

Status: superseded by [ADR-0061](0061-impact-scoped-verification.md) on 2026-10-12; retained below as historical rationale.

Revised: 2026-10-12 (hide: the full gate runs only when an integration candidate enters `main`, and a document-only change never runs it)

Date: 2026-09-05

## Context

Issue #51. hide explicitly authorized replacing full validation after every commit/push/PR update with focused development tests and one full integration check. A measured full local gate took 255.7 seconds, including 206.2 seconds of policy proof/security stages; the 351-test stage took 2.4 seconds. Repeating full validation at every development boundary delays feedback without changing the candidate being proved.

## Decision

Keep `eng/check.ps1` as the sole orchestrator. Its default `Merge` mode remains the unchanged full validation sequence and all protected thresholds. Its explicit `Commit` mode performs SDK, conformance, security scans without negative fixtures, and staged/unstaged whitespace checks, then reports incomplete integration evidence. Bootstrap and pre-commit use Commit mode. Developers run scoped behavior and dependency-impact tests while working.

Use the PR's Draft-to-Ready transition (`pull_request: ready_for_review`) as the sole normal full-CI request. Keep work draft until integration readiness. The required `canonical-gate` job always executes the full command on the PR merge candidate; it has no conditional success/skip shortcut. No push, PR opened/synchronize, or post-merge trigger runs this full workflow. After a head change, return to Draft then Ready. After a base change, update the branch and request validation again. A directly created ready PR needs the same transition. The existing main ruleset retains strict required GitHub Actions checks, no bypass actors, and squash-only PRs.

This supersedes earlier every-change/session/commit full-run wording in the constitution, workflow, and quality documents. It does not supersede required tests, coverage/mutation thresholds, the full merge gate, or scheduled/security-sensitive integration/release deep review. A successful CI integration proof need not be repeated locally. Intermediate handoffs record incomplete proof honestly.

## Revision of 2026-10-12

hide decided that the full validation sequence runs once per integration into `main`, not once per Issue PR, and that a change touching only documents (`docs/**`, `README.md`, `AGENTS.md`, `AGENT.md`, `CLAUDE.md`, `.github/copilot-instructions.md`) never runs the build and test tiers. The `canonical-gate` job keeps its name, its sole trigger (`ready_for_review`), and the main ruleset; inside the job the depth is chosen only from the PR base and the changed paths: full `Merge` mode for a PR into `main` with code changes, `Commit` mode otherwise. Issue PRs target the day's integration branch `integrate/<yyyy-mm-dd>` (merged after the `Commit`-mode gate and the focused evidence recorded in the PR); one PR a day carries that branch into `main` under the full gate and squash merge. The rejected alternative "conditional skipped canonical jobs" stays rejected: the job always runs one of the two canonical modes and never reports success without running it.

## Rejected alternatives

- Every-commit full execution: repeats high-cost proofs during small changes.
- Delaying all tests: loses early feedback; scoped tests remain expected throughout development.
- Conditional skipped canonical jobs: skipped jobs can look successful without proving the candidate.
- Merge-queue-only implementation: this is a personal-account repository; the documented hosted merge-queue availability is for organization-owned repositories. Use the existing protected PR mechanism instead.
- Lower thresholds, exclusions, or mutation baselines: unnecessary for this scheduling change.

## Consequences

Readiness is an explicit final validation request, not merely a review invitation. Missing/stale checks block integration until another readiness transition. Ruleset freshness is essential and must not be relaxed. Existing unrelated PRs need the new transition once they adopt the workflow. Scheduled deep remains a separate cost. This change does not optimize fixture copying or application code.

## Migration and removal

Atomically update AGENTS, current state, normative workflow/testing/quality/commit documents, pre-commit, bootstrap, CI, conformance, and negative fixtures. Preserve the check context and existing remote ruleset. Validate this migration itself through a draft PR readiness transition before merging it. ADR-0024 remains reserved by Issue #49.

## Executable proof

QLT-015 conformance validates the exact quality workflow and lightweight hook/bootstrap wiring, plus the explicit mode/default declarations. Negative fixtures introduce a push trigger, a full pre-commit invocation, a skipped full job, and a lightweight default; each must fail QLT-015. Commit mode is exercised directly and through the actual hook; logs must contain no full build/tests/coverage stage. Final CI runs the full default gate. Remote readback must show strict required `canonical-gate` with no bypass actors before integration.
