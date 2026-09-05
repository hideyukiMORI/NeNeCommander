# ADR-0033: Strengthen Windows local identity with Win32 file identifiers

Status: accepted

Date: 2026-09-06

## Context

`WindowsLocalEntryIdentity` originally identified an operation snapshot only by entry kind, byte length, creation time, and last-write time. A different file can replace the inspected entry and restore all four values, causing revalidation to accept an entry that was never inspected. ADR-0032 already established source-generated Win32 interop in Infrastructure.Windows and SEC-014 confines its required unsafe compilation support. The identity query must identify a reparse point as an entry rather than silently following its target.

## Decision

Keep `WindowsLocalEntryIdentity.Describe` and `Revalidate` as the single Windows local snapshot mechanism. Prefix its opaque token with `windows-v2` and include the 64-bit volume serial plus 128-bit file identifier returned by `GetFileInformationByHandleEx(FileIdInfo)`, followed by the existing kind, byte length, creation time, and last-write time. The file identifier detects replacement while the metadata detects rewrites to the same entry.

`WindowsFileIdentifier` owns the native query. It opens the path with zero requested data access, read/write/delete sharing, `OPEN_EXISTING`, `FILE_FLAG_BACKUP_SEMANTICS`, and `FILE_FLAG_OPEN_REPARSE_POINT`; it therefore supports directories and identifies a link entry without traversing it. A failed open or identifier query throws a normalized `IOException`, which the adapter's existing provider-failure boundary closes. No fallback to metadata-only identity is permitted. ADR-0036 renames and shares this low-level Windows namespace mechanism with the WSL adapter without changing the Windows-local `windows-v2` snapshot contract.

## Rejected alternatives

- Retain metadata-only identity: a same-size replacement can restore both timestamps and impersonate the inspected source.
- Use a path, drive letter, or mounted-volume identity alone: those identify neither an individual entry nor a replacement at the same name.
- Follow a reparse point for the query: the target would impersonate the link entry and widen the snapshot boundary.
- Remove metadata once file identifiers are present: rewriting the same entry can retain its identifier, so the tuple remains necessary.
- Add a second identity API to the application gateway: it would split the existing provider-owned revalidation mechanism.

## Consequences

- A replacement that preserves the legacy metadata tuple changes identity, and a rewrite of the same entry still changes metadata identity.
- Files and directories share one fixed-width opaque identifier representation; consumers never parse its fields.
- Each description opens and closes one native handle. Revalidation still reopens by path immediately before a side effect, so a residual path-replacement race remains between the query and mutation. This ADR does not claim handle-relative mutation or eliminate all TOCTOU windows.
- The token version changes from the unversioned metadata tuple to `windows-v2`; tokens are process workflow snapshots, not a persisted public format.

## Migration and removal

The metadata-only definition in ADR-0014 is superseded. Its adapter ownership, closed revalidation outcome, and failure normalization remain in force. There is no compatibility fallback or second implementation to remove.

## Executable proof

`WindowsLocalFileOperationAdapterTests` proves that a different entry preserving size, creation time, and last-write time receives a different identity and is rejected by transfer preflight. `WindowsFileIdentifierTests` proves the shared `WindowsFileIdentifier` returns stable fixed-width identifiers, closes missing-query failure, rejects null, and gives a junction an identifier different from its target. Existing rewrite, file, directory, reparse, and effect-boundary tests retain the surrounding contract. The security deep review and final canonical CI gate prove the integration candidate.
