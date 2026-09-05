# Windows local I/O execution boundary handoff — 2026-09-05

Status: informational

## Work item

- Issue: #56
- Branch: `perf/56-windows-io-execution-boundary`
- Decision: ADR-0027
- Invariant: synchronous provider work never runs inline on the UI caller; completion/fault remains owned by the awaiter; gateway serialization and typed cancellation remain unchanged.
- Canonical mechanism: `WindowsLocalIoExecutionBoundary`, shared at the App composition root.

## Verification checkpoint

- Failure-first manual-scheduler proof: mutation inspection completed inline before wiring and failed the new assertion.
- Release locked restore/build: PASS, zero warnings.
- Infrastructure.Windows 66, Application 155, Architecture 4: PASS.
- Conformance and security without negative fixtures: PASS; 18 adversarial cases remained registered.

## Integration steps

1. Review the boundary options and confirm all synchronous Windows local adapter entry points use it.
2. Run conformance/security focused checks and the affected test projects.
3. Commit through the existing Commit-mode hook and push without force.
4. Create a Draft PR that closes #56 and verify its head OID against local HEAD.
5. Mark Ready only for the final candidate, require fresh `canonical-gate` success, then squash merge and synchronize clean `main`.

## Remaining environmental proof

None. The deterministic scheduler proof establishes that provider work is not performed by the invoking UI owner; existing App awaits retain the captured UI context. Lifecycle fault cleanup remains explicitly separated as Issue #59.
