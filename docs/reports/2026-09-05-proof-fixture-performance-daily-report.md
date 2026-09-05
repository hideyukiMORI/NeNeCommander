# Daily report — proof fixture materialization performance — 2026-09-05

Status: informational

## Scope

Issue #54 removes generated-output traversal and copying from negative proof fixtures. It does not reduce a negative case, rule, security check, coverage threshold, mutation threshold, or cleanup boundary. ADR-0026 records the shared repository-tree mechanism.

## Invariant and canonical mechanism

Every tracked, modified, untracked, or newly created inspection input remains in a fixture, while generated directories and reparse points are excluded before descent. `eng/repository-tree.ps1` is the single enumerator and fixture materializer used by gate proofs, security proofs, conformance, and security scanning.

## Changes

- Added `Get-RepositoryTreeFile`, which walks validated repository roots one directory at a time, skips the closed generated-directory set before descent, and does not follow reparse points.
- Added `Copy-ProofFoundation`, which copies the current working tree file by file into the caller's already validated OS-temporary destination. It does not consult Git.
- Removed the two local `Copy-Foundation` implementations from `prove-gates.ps1` and `prove-security.ps1`.
- Added a synthetic materialization proof: an untracked-style `.cs` input must be copied; nested `bin` / `obj` markers and junction content must not be copied.
- Replaced recursive-then-filter discovery in conformance and security with the shared pruned traversal. Each scanner still owns its exact extension and file-name filter.

## Same-tree measurement

The comparison used the same working tree and sequentially materialized one fixture into separate unique directories under the OS temporary directory. Both destinations were removed only after resolved-name and temp-parent validation.

| Implementation | Elapsed | Files | Bytes |
|---|---:|---:|---:|
| previous root-only exclusion plus `Copy-Item -Recurse` | 10,662.0 ms | 2,555 | 594,771,574 |
| ADR-0026 pre-descent pruning | 1,251.5 ms | 356 | 1,321,971 |

The selected bytes fell by 593,449,603 (99.78%) and elapsed materialization time fell by 88.26%. The pre-change deep review independently showed about 410 MiB copied per negative fixture after its build state and 8.1 GiB removed after gate-proof cases.

## Focused verification

- PowerShell parser: all five changed scripts passed.
- `pwsh -NoProfile -File ./eng/conformance.ps1 -Quiet`: PASS.
- `pwsh -NoProfile -File ./eng/security-check.ps1 -SkipProof`: PASS; 18 adversarial cases remained registered.
- `pwsh -NoProfile -File ./eng/prove-gates.ps1`: PASS; all 15 conformance cases, commit-message positive/negative checks, and the new materialization assertions passed.
- `pwsh -NoProfile -File ./eng/prove-security.ps1`: PASS; all 6 security negative cases passed.

PR #55 canonical CI run `33960075774` passed on final head `25c3148`; the PR was squash merged as `9f0eac9`. The successful CI full gate was not duplicated locally.
