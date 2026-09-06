# ADR-0037: Copy and composite-move within one WSL distribution

Status: accepted

Date: 2026-09-06

## Context

ADR-0036 routes WSL mutations through `FileOperationGateway` but deliberately leaves transfer operations unavailable. The gateway already owns one copy algorithm and one non-atomic move algorithm: complete batch preflight, copy, verify, and for move only, delete the source. The WSL provider needs to implement those existing port steps without adding shell commands, collision defaults, rollback deletion, or a second orchestrator.

## Decision

Extend `WslFileOperationAdapter` and its Windows-side filesystem boundary for copy and verification only when every source and destination belong to the same WSL distribution. `FileOperationGateway` remains the sole transfer owner. WSL atomic-move capability reports `Unsupported`, so the gateway performs move as copy, verify, then permanent source deletion.

Preflight validates the complete batch before effects. It requires an existing non-link destination directory, revalidates every source identity, rejects a destination contained by a source, scans each source tree without following reparse points, derives the exact target with `FileSystemPath.Child`, and rejects an existing target as `Conflict`. Copy repeats source identity, destination, link, and collision checks at its side-effect boundary. Verification repeats source identity and destination checks and compares kind, direct entry set, and byte count through the existing tree-copy mechanism.

If expected copy failure leaves the top-level target, the adapter returns `CopyTargetCreated`; the gateway reports `PartiallyCompleted` and never deletes the source. It does not roll back the target. Verification failure and source identity change likewise stop before source deletion.

Windows local to WSL, WSL to Windows local, and cross-distribution transfer remain `ProviderUnavailable`. They require a later provider-routing decision because the current mutation router dispatches each port step by the frozen source provider.

## Rejected alternatives

- Add a WSL-specific transfer gateway: duplicates preflight, progress, cancellation, effects, and move safety.
- Use `wsl.exe cp`, `mv`, or `rm`: creates a second mutation engine and command-input surface.
- Treat same-distribution move as atomic without capability proof: Linux namespace implementation details are not inferred from the path label.
- Delete a partial target after failure: rollback can race, fail, or remove evidence the operation no longer owns.
- Enable overwrite, skip, or keep-both now: Issue #73 owns that product policy; the current conflict rejection remains safe and complete.

## Consequences

- Existing F5/F6 application and UI behavior works within one WSL distribution through the same effects, progress, cancellation, and result model as Windows local.
- Composite WSL move performs permanent source deletion only after successful verification; it is not described as atomic.
- Tree traversal occurs through the canonical Windows WSL namespace on the shared I/O execution boundary and can occupy one worker for the duration of a provider step.
- Verification retains the existing kind/entry-set/byte-count contract and does not claim hashes or metadata fidelity not represented by that contract.

## Migration and removal

The transfer members of `WslFileOperationAdapter` change from `ProviderUnavailable` to the accepted same-distribution subset. No compatibility route remains. ADR-0036's create, rename, delete, identity, and provider-routing decisions remain active; only its statement that all WSL transfer is unavailable is superseded.

## Executable proof

Gateway-level WSL tests prove copy and composite move effects, copy-before-delete ordering, identity replacement after copy, verification failure, partial target reporting, full-batch preflight, collision, containment, distribution, destination, and link boundaries. `WindowsWslFileSystemTests` copy and verify real file and directory trees under `TestOwnedTemporaryRoot`. Infrastructure coverage and focused complete mutation prove the provider implementation; Architecture, conformance, security, deep review, and final canonical CI prove the integration candidate.
