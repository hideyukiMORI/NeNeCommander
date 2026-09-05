# ADR-0014: Implement the Windows local file-operation adapter with metadata identity

Status: accepted

Date: 2026-09-04

## Context

`FileOperationGateway` owns every mutation but had no production `IFileOperationPort`. The gateway contract requires each provider step to revalidate the preflighted identity (ADV-004), never delete a source whose copy failed verification (ADV-007), refuse permanent deletion without confirmation (ADV-008), and keep links as entries (FS-004). The Windows local provider has no recycle mechanism in the base class library, and no public file-identifier API exists without Win32 interop.

## Decision

Add one adapter, `WindowsLocalFileOperationAdapter`, for `WindowsLocalPath` only. Any other provider is `ProviderUnavailable`.

- Identity is the metadata tuple kind, byte length, creation time, and last write time, owned by `WindowsLocalEntryIdentity`. Every step revalidates the snapshot through the closed `RevalidationOutcome` (`EntryMatched` or `EntryRejected`) before touching the filesystem; a missing entry is `NotFound`, a changed tuple is `IdentityChanged`.
- Inspection reports `DeletionCapability.PermanentOnly` because no recycle implementation exists; `DeletionExecutionMode.Recycle` is refused with `ProviderUnavailable`. The gateway therefore always requires explicit confirmation for Windows local deletion until a shell recycle adapter is added by a later ADR.
- Preflight requires an existing Windows local destination directory, rejects a destination contained by a source (`ProviderPathContainment`), and rejects an existing target name, all as `Conflict`, for every source before any step runs.
- Copy and verify are owned by `WindowsLocalTreeCopy`. A file is copied without overwrite; a directory is copied recursively. A source that is or contains a reparse point is refused with `ProviderUnavailable` before anything is written. Verification compares kind, entry set, and byte count.
- Permanent deletion removes a file or a directory tree; the base class library does not follow reparse points when deleting.
- Platform failures are caught only as `UnauthorizedAccessException` and `IOException`, normalized by `WindowsFileFailureNormalizer`, and fall back to the step's own failure kind (`Inspection`, `Copy`, `Verification`, `Delete`) when the HRESULT is unknown.

## Rejected alternatives

- Win32 file identifiers through `GetFileInformationByHandle`: stronger identity, but requires unsafe interop and directory handles with backup semantics; deferred to a hardening ADR.
- Reporting recycle capability and emulating it by moving to a hidden folder: a second deletion path that lies about provider semantics (FS-006).
- Following or recreating links during copy: recreating symbolic links needs privilege and following them widens the operation root (FS-004, FS-008).
- Asynchronous stream copy with mid-file cancellation: leaves partial targets the port cannot report because a provider step has no cancelled outcome; the gateway observes cancellation between steps.

## Consequences

- Provider steps remain synchronous and atomic, but ADR-0027 schedules them through the single Windows local I/O execution boundary instead of running them on the caller.
- A directory copy is one provider step; cancellation is observed by the gateway between steps only.
- Content is verified by byte count, not by hash; a same-length corruption is not detected.
- Windows local deletion always requires confirmation until recycle support exists.
- The Infrastructure test harness gains `WriteFile` and `CreateJunction` (NTFS junction through `mklink /J`, which needs no privilege).

## Migration and removal

No prior mechanism exists. Wiring `F5`, `F6`, and `F8` to the gateway and the confirmation UI are later slices.

## Executable proof

`WindowsLocalFileOperationAdapterTests` against `TestOwnedTemporaryRoot`, including the ADV-003, ADV-004, ADV-006, ADV-007, ADV-008, and ADV-017 mappings and end-to-end gateway move and delete, plus the canonical gate.
