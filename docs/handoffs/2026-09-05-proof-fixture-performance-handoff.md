# Proof fixture materialization performance handoff — 2026-09-05

Status: informational

## Work item

- Issue: #54
- Branch: `build/54-prune-proof-fixtures`
- Decision: ADR-0026
- Invariant: current-tree inspection inputs are complete; generated/reparse subtrees are pruned before traversal; cleanup remains inside a resolved unique OS-temporary root.
- Canonical mechanism: `eng/repository-tree.ps1` (`Get-RepositoryTreeFile` and `Copy-ProofFoundation`).

## Verification checkpoint

- Previous/new materialization: 10,662.0 / 1,251.5 ms; 2,555 / 356 files; 594,771,574 / 1,321,971 bytes.
- Parser, conformance, security without fixtures: PASS.
- Gate negative proofs: PASS, all 15 cases plus commit-message proofs and materialization proof.
- Security negative proofs: PASS, all 6 cases.
- Thresholds and case registries were not changed.

## Integration steps

Completed: PR #55 passed canonical CI run `33960075774` on final head `25c3148`, squash merged as `9f0eac9`, and local `main` was synchronized cleanly.

## Remaining environmental proof

None. This change is repository tooling only and its destructive activity is confined to validated test-owned OS-temporary roots.
