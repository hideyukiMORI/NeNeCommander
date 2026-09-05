# WSL distribution catalog handoff — 2026-09-06

Status: informational

## Work item

- Issue: #79
- Branch: `feat/79-wsl-distribution-catalog`
- Decision: ADR-0034
- Invariant: fixed non-shell invocation, bounded complete output, exact WSL-root parsing, no partial snapshot, and owned cancellation.
- Canonical mechanism: Application `IWslDistributionCatalog`, implemented once by Infrastructure.Windows `WslDistributionCatalog`.

## Verification checkpoint

- Failure-first stub parser produced the expected three catalog failures; pre-cancellation already passed without invocation.
- Release build passes with zero warnings and errors.
- Application 186, Infrastructure.Windows 100, and Architecture 5 tests pass with zero skips.
- Conformance/security pass with all 18 adversarial mappings.
- Final branch coverage is Application 100.00% and Infrastructure.Windows 93.20%.
- Focused mutation is Application 100.00% and Infrastructure.Windows 90.38%, without threshold reduction.

## Integration steps

1. Review fixed arguments, UTF-16 boundaries, root parsing, and cancel/kill/await ordering.
2. Commit through the existing Commit-mode hook, push, and create a Draft PR closing #79.
3. Confirm remote head and latest base, run one exact-head security deep review because process execution and cancellation ownership changed.
4. If unchanged, mark Ready and require fresh canonical CI before squash merge and clean main synchronization.
5. Create the next focused Issue for WSL directory access through the canonical `\\wsl.localhost` provider path; do not add shell mutation commands.

## Remaining environmental proof

Local read-only discovery exited 0 and confirmed redirected UTF-16 characteristics without recording distribution names. Live WSL directory/mutation proof still requires an explicitly configured safe `NENE_COMMANDER_WSL_TEST_ROOT` and belongs to later Issues.
