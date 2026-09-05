# Partial copy effect handoff — 2026-09-05

Status: informational

## Work item

- Issue: #58
- Branch: `fix/58-partial-copy-effect`
- Decision: ADR-0029
- Invariant: a failed provider step cannot leave a copy target while the operation reports no effect.
- Canonical mechanism: `ProviderStepOutcome.FailedAfterEffect` → `FileOperationGateway.CopyOneAsync` → `FileOperationEffectKind.CopyTargetCreated`.

## Verification checkpoint

- Failure-first real adapter/gateway proof: target root existed while the old outcome was Rejected/effects empty.
- Release build: PASS, zero warnings.
- New Application tests: 7/7 PASS.
- New real adapter test: 1/1 PASS.
- Full Application 157, Infrastructure.Windows 68, Presentation 64, and Architecture 4: PASS.
- Conformance and security without negative fixtures: PASS; 18 adversarial cases remained registered.

## Integration steps

1. Review the exact target-existence boundary, provider-effect vocabulary, and absence of rollback/source deletion.
2. Run full Application and Infrastructure.Windows suites plus Presentation and Architecture impact tests.
3. Run conformance/security and one integration deep review because this is safety-sensitive.
4. Commit through the existing Commit-mode hook, push, and create a Draft PR closing #58.
5. Verify final remote head/latest base, mark Ready, require fresh canonical CI, then squash merge and synchronize clean `main`.

## Remaining environmental proof

None. The destructive scenario is confined to `TestOwnedTemporaryRoot`, and the locked-child boundary deterministically exercises the real Windows local adapter.
