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

1. Review the branch diff for exact filter parity and test-owned-root containment.
2. Commit through the existing Commit-mode hook and push without force.
3. Create a Draft PR that closes #54 and includes the measurement above.
4. Confirm `gh pr view` reports a head OID equal to local `git rev-parse HEAD`.
5. Mark Ready once the focused candidate is final, watch required `canonical-gate`, and squash merge only when the final head/base candidate is green.
6. Synchronize a clean `main` and record the CI run in the PR body or final report without creating a result-only commit.

## Remaining environmental proof

None. This change is repository tooling only and its destructive activity is confined to validated test-owned OS-temporary roots.
