# ADR-0001: Strictness is mechanically enforced

Status: accepted

Date: 2026-09-02

## Context

NeNe Commander must converge toward the same implementation regardless of which competent human or AI model writes it. Prose guidance alone permits silent variation and drift.

## Decision

Repository law is encoded in normative rule IDs and projected into one `eng/check.ps1` gate used locally and in CI. Compiler warnings, nullable analysis, adopted style diagnostics, conformance checks, tests, architecture checks, package allowlists, and lock files fail closed. The policy-foundation stage prohibits production code until every implementation-stage gate is activated atomically.

## Rejected alternatives

- Review-only conventions: inconsistent and discovered too late.
- Warning baselines: legitimize greenfield debt.
- Tool-specific developer commands: create divergent definitions of done.
- Inline suppressions: hide local exceptions from architectural review.

## Consequences

Initial setup is heavier and legitimate rule changes require ADRs and negative proofs. In return, violations are immediate, review is narrower, and implementation choice is deliberately constrained.

## Migration and removal

None. This is the initial foundation.

## Executable proof

`eng/prove-gates.ps1`, `eng/conformance.ps1`, and `.github/workflows/quality.yml` prove and invoke the policy.
