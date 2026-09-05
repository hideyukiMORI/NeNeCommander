# Daily report — Windows local atomic move — 2026-09-06

Status: informational

## Scope

Issue #75 implements the FS-005 same-volume atomic move capability for Windows local files and directories. It does not add overwrite, collision resolution, rollback, cross-provider support, Win32 file identifiers, or link traversal. Unsupported cases retain the existing copy-verify-delete path.

## Invariant and canonical mechanism

Every source is inspected, batch-preflighted, and capability-resolved before any mutation. The provider decides capability from actual mounted-volume identity rather than path text. Source identity, destination existence and volume, reparse policy, containment, and target absence are revalidated at the effect boundary. `FileOperationGateway.ExecuteTransferAsync` remains the sole transfer mechanism.

## Failure-first proof

The existing real-adapter gateway test was changed first to require one atomic effect for a same-volume move. Before implementation it failed because the gateway returned three composite effects (`Copied`, `Verified`, and `SourceDeleted`). The implemented test now observes one `AtomicallyMoved` effect and the expected source/target filesystem state.

## Changes

- Added the closed supported/unsupported/failed atomic capability outcome and provider query.
- Resolved Windows mounted-volume GUID identity through source-generated `GetVolumePathNameW` and `GetVolumeNameForVolumeMountPointW` imports.
- Added a non-overwriting atomic provider step for regular files and directories with complete effect-boundary revalidation.
- Kept mixed batches in the one gateway path: supported items move atomically; unsupported items use copy-verify-delete.
- Reported exactly one atomic effect and unchanged source-unit progress; a failed atomic step never falls back to a second mutation attempt.
- Added SEC-014 plus two negative fixtures to confine generated interop support to Infrastructure.Windows and reject handwritten unsafe code.

## Focused verification

- `eng/conformance.ps1 -Quiet`: PASS, including SEC-014 positive state.
- `eng/prove-gates.ps1`: PASS, including both new SEC-014 negative fixtures and all existing negative proofs.
- Release solution build: PASS, zero warnings and errors.
- Application tests: 182/182 PASS, skip 0.
- Infrastructure.Windows tests: 84/84 PASS, skip 0.
- Presentation.WinUI impact tests: 76/76 PASS, skip 0.
- Architecture impact tests: 5/5 PASS, skip 0.
- Application branch coverage: 100.00%.
- Infrastructure.Windows branch coverage: 93.55% (minimum 90%).
- The first exact-head deep run `33974827825` passed its canonical gate, 409 tests, coverage, adversarial repeats, and CodeQL, but correctly failed Infrastructure.Windows mutation at 87.38%. Missing-destination, effect-boundary reparse, native-buffer termination, and null-guard proofs raised the focused score to 90.77% without lowering the threshold. A fresh exact-head deep run remains required after commit.

## Pending integration evidence

Because this changes filesystem safety and native interop, the committed candidate requires one security deep-review run. The final head/latest-base candidate then requires the canonical Ready CI gate. Run identifiers belong in the PR body without a result-only documentation commit.

## Remaining environmental proof

No controlled second mounted volume is available in the current test-owned root, so a live cross-volume capability result is not claimed. The unsupported outcome and unchanged composite route are covered at the gateway boundary; cross-volume live proof remains an environmental item and must not be inferred from drive letters.
