# ADR-0007: Use the NENE2 Issue-driven public repository lifecycle

Status: accepted

## Context

NeNe Commander was initialized locally before a hosted repository existed. hide requires the project to be public and to follow the same traceable commit operation used by NENE2. Multiple informal commit styles or direct-to-main workflows would undermine deterministic human and AI collaboration.

## Decision

Use the public repository `hideyukiMORI/NeNeCommander`. Every post-initialization change follows Issue, typed branch name, Conventional Commit with Japanese description and Issue number, pull request, required checks, squash merge, and synchronized `main`. A repository-owned `commit-msg` hook is the sole local commit-message validator.

The policy bootstrap and Issue #1 initial vertical slice may establish the first `main` commit directly because no remote default branch existed. This exception ends after that commit is pushed.

## Alternatives rejected

- Direct commits to `main` were rejected because they remove the review and required-check boundary.
- English-only commit descriptions were rejected because they diverge from NENE2 operations.
- Multiple commit-linting packages were rejected because a repository-owned PowerShell validator avoids another dependency and remains testable by the existing gate.

## Consequences

All future implementation work requires an Issue and focused branch before editing. Commit messages are locally rejected when their structure, Issue suffix, Japanese description, or length is invalid. GitHub repository settings and rules must be read back after the initial push.

## Proof

`eng/prove-gates.ps1` proves invalid and valid commit messages against `eng/validate-commit-message.ps1`. The canonical conformance scan requires the hook, validator, and normative convention document.
