# WSL same-distribution mutations handoff — 2026-09-06

Status: informational

## Work item

- Issue: #83
- Branch: `feat/83-wsl-local-mutations`
- Decision: ADR-0036
- Invariant: gateway-only mutation, effect-boundary identity/provider revalidation, links not followed, collision remains explicit, and outcomes describe only completed effects.
- Canonical mechanism: App-composed `ProviderFileOperationPort`; provider-owned `WslFileOperationAdapter`; shared `WindowsLocalIoExecutionBoundary`; Windows-side `\\wsl.localhost` namespace without shell invocation.

## Verification checkpoint

- Failure-first router compilation and direct-child create test demonstrated the missing route and caught an invalid shared predicate.
- Release build passes with zero warnings and errors.
- Infrastructure.Windows tests pass 124/124; Architecture tests pass 5/5.
- Infrastructure.Windows branch coverage is 93.70%.
- Focused WSL mutation passes at 100.00% (88/88 killed); focused router mutation passes at 100.00% (23/23 killed).
- Focused shared `WindowsFileIdentifier` mutation passes at 100.00% (7/7 killed).
- Conformance passes 112 unique normative rules; security conformance passes all 18 adversarial mappings plus secret and workflow supply-chain checks.

## Integration steps

Completed: PR #84 passed dependency run `33988561920`, exact-head deep run `33988574140`, and Ready canonical run `33989351752`, then squash-merged as `4b01d286cea2a823dddddd87aca0c611bbb2d0ff`. Issue #83 is closed. Continue with Issue #85 for same-distribution WSL transfer without repeating #83's successful gates or assuming Issue #73's collision policy.

## Remaining environmental proof

No live WSL path was mutated. A future opt-in proof must use a validated `NENE_COMMANDER_WSL_TEST_ROOT`; distribution roots, home, `/mnt`, the repository, and their ancestors remain prohibited. WSL copy, move, verification, and cross-provider transfer are deliberately unavailable.
