# ADR-0032: Use mounted-volume identity for Windows local atomic move

Status: accepted

Date: 2026-09-06

## Context

FS-005 permits an atomic provider move only after an explicit capability decision. Windows local moves currently always use copy, verify, then delete, even when source and destination share a volume and `File.Move` or `Directory.Move` can perform one non-overwriting rename. Drive letters and path prefixes are not volume identity because mount points and volume mappings can cross those textual boundaries. Capability discovery must complete for the whole batch before mutation, and the actual effect must revalidate source identity, containment, target absence, link policy, and volume.

## Decision

Keep `FileOperationGateway.ExecuteTransferAsync` as the sole transfer mechanism. Before a move mutates anything, the gateway asks `IFileOperationPort.GetAtomicMoveCapabilityAsync` for every preflighted source. The closed answer is supported, unsupported, or failed. Any failed query rejects the whole batch; cancellation starts no further query or effect. Supported items invoke the provider's single `MoveAsync` step and report one `AtomicallyMoved` effect. Unsupported items retain the existing copy-verify-delete path.

`WindowsLocalFileOperationAdapter` supports atomic move only for existing, non-reparse Windows local source and destination entries whose mounted-volume GUIDs match. `WindowsLocalVolumeIdentity` obtains those identities through source-generated imports of `GetVolumePathNameW` and `GetVolumeNameForVolumeMountPointW`; no drive-string heuristic exists. Immediately before `File.Move` or `Directory.Move`, the adapter again validates the frozen metadata identity, destination existence, reparse status, mounted volume, containment, and target absence. Overwrite is never enabled.

The interop generator requires `AllowUnsafeBlocks`; SEC-014 constrains that setting to Infrastructure.Windows and prohibits handwritten `unsafe` code throughout owned source and tests.

## Rejected alternatives

- Infer sameness from drive letters or rooted path text: mount points make that result unsound.
- Attempt the atomic call and fall back to copy after any failure: an ambiguous failure could follow a completed effect and would create a second mutation path.
- Query capability lazily per source: an unavailable later source could allow an earlier batch item to mutate before complete preflight.
- Replace every move with the atomic call: cross-volume and link cases need the established composite path.

## Consequences

- Same-volume, non-reparse Windows local moves have one reported effect and avoid copying bytes.
- Capability is advisory only; the effect boundary repeats every safety check and normalizes expected platform failures.
- A mixed batch can use atomic and composite steps after all capability answers succeed. As with existing batch operations, a later per-item failure reports already completed effects and does not claim rollback.
- ADR-0015's statement that Windows local moves always use copy-verify-delete is superseded by this decision; its session ownership remains unchanged.

## Migration and removal

The gateway's one transfer path gains the capability phase and atomic step in the same change. The composite path remains the required unsupported/cross-volume mechanism, so no compatibility path or deferred removal exists.

## Executable proof

`FileOperationGatewayTests` proves failure-first complete capability preflight, mixed atomic/composite execution, cancellation, copy-path isolation, and exact effects. `WindowsLocalFileOperationAdapterTests` proves real test-owned file and directory moves plus identity change, late collision, and reparse refusal. SEC-014 and the `unsafe-enabled-outside-interop-project` and `handwritten-unsafe-interop` negative fixtures constrain native interop. The security deep review and final canonical CI gate prove the integration candidate.
