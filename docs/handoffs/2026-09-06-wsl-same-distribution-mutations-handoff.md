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

1. Run final targeted conformance/security checks after documentation review and commit through the existing Commit-mode hook.
2. Push and create a Draft PR closing #83, then confirm the remote head equals the local head and the base is current.
3. Obtain exact-head dependency and deep-review evidence because provider mutation and path handling changed.
4. Resolve findings while Draft; only the final head may be marked Ready for a fresh canonical CI gate.
5. Squash merge, synchronize clean main, and create the separate WSL transfer Issue without assuming Issue #73's collision policy.

## Remaining environmental proof

No live WSL path was mutated. A future opt-in proof must use a validated `NENE_COMMANDER_WSL_TEST_ROOT`; distribution roots, home, `/mnt`, the repository, and their ancestors remain prohibited. WSL copy, move, verification, and cross-provider transfer are deliberately unavailable.
