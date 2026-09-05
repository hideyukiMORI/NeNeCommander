# Daily report — partial copy effect — 2026-09-05

Status: informational

## Scope

Issue #58 makes copy and move outcomes truthful when an expected provider failure leaves a partial target. It does not add rollback, overwrite, retry, mid-step cancellation, or descendant-level effects. ADR-0029 records the typed provider-to-gateway mechanism.

## Invariant and canonical mechanism

Every completed filesystem change is represented in the returned effects. `ProviderStepOutcome.FailedAfterEffect` is the sole port-level carrier, and `FileOperationGateway.CopyOneAsync` is the sole mapping into `FileOperationOutcome`.

## Failure-first proof

`ExecuteAsyncWhenTreeCopyFailsAfterTargetCreationReportsPartialEffect` created a test-owned source directory with one child held by `FileShare.None`. Preflight and root creation succeeded, then `File.Copy` failed deterministically. Before the fix, `dest\\tree` existed but the gateway returned `Rejected` with an empty effect list. The test failed on the expected `PartiallyCompleted` completion.

## Changes

- Added the closed provider effect `CopyTargetCreated` and a failed-after-effect outcome factory.
- Added the matching operation effect, whose contract explicitly says contents may be incomplete.
- Normalized expected copy exceptions where the adapter still owns exact target identity, then reported target existence without deleting it.
- Mapped the provider effect before gateway failure handling; stopped verification, later sources, progress, and move deletion as before.
- Added real Windows local and scripted copy/move adversarial coverage.

## Focused verification

- Release locked restore and solution build: PASS, zero warnings.
- New Application partial-copy/null-guard tests: PASS, 7/7.
- New Infrastructure.Windows locked-child integration test: PASS, 1/1.
- Application tests: PASS, 157/157.
- Infrastructure.Windows tests: PASS, 68/68.
- Presentation tests: PASS, 64/64.
- Architecture tests: PASS, 4/4.
- `eng/conformance.ps1 -Quiet`: PASS.
- `eng/security-check.ps1 -SkipProof`: PASS; all 18 adversarial cases remained registered.

The exact committed candidate still requires its security-sensitive integration deep review and final canonical CI; those result identifiers are recorded in the PR body without a result-only docs commit.
