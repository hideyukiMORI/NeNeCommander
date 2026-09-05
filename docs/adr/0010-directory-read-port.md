# ADR-0010: Read directories through one provider-neutral query port

Status: accepted

Date: 2026-09-03

## Context

The shell had no way to show real directory contents. `FileOperationGateway` owns mutations only, and CMD-007 requires enumeration to be a query that never alters pane or operation state. Reading a directory must honor the same invariants as mutations: parse untrusted names once at the boundary, branch on the validated provider kind, bound hostile enumerations, report cancellation and availability failures as closed outcomes, and never depend on provider enumeration order.

## Decision

Add one Application query boundary, `IDirectoryReadPort`, with exactly one operation: `ReadAsync(DirectoryReadRequest, CancellationToken)` returning the closed `DirectoryReadOutcome` of `DirectoryReadSucceeded`, `DirectoryReadCancelled`, or `DirectoryReadFailed`. Failures reuse the canonical `FileOperationFailureKind` vocabulary so HRESULT normalization stays in `WindowsFileFailureNormalizer`.

- `DirectoryReadRequest` freezes the validated location and a positive entry boundary no larger than `DirectoryListing.EntryBoundaryLimit` (10,000).
- `DirectoryListing.Create` owns ordering and validation: directories first, then name ignoring case, then ordinal name; duplicate provider identity, null entries, and counts above the limit are typed rejections. It records completeness (`Complete` or `Bounded`) and the count of provider entries the path model could not represent.
- `WindowsLocalDirectoryReader` is the sole Windows local adapter. ADR-0035 adds the WSL adapter and the one provider router composed as `IDirectoryReadPort`; both adapters share one bounded direct-enumeration operation. They are non-recursive, report links as entries, never skip hidden or inaccessible content silently, observe cancellation before enumeration and before each entry, and fail closed for unsupported provider variants.
- `PaneListingPresenter` is the sole deterministic projection from a read outcome to pane rows, initial focus, and a status resource key. The App host assigns those values to controls and makes no further decision.

## Rejected alternatives

- Enumerating from the App window or a view model with `System.IO` directly: violates CS-018 and leaves provider policy in untested framework code.
- Adding enumeration to `IFileOperationPort`: mixes queries with mutations and forces the mutation gateway's serialization onto reads.
- A feature-local failure type for reads: duplicates the failure vocabulary and would require a second HRESULT translation.
- Relying on the provider's enumeration order: violates ARC-007 and makes focus identity nondeterministic.

## Consequences

- The provider enumeration itself is synchronous because .NET offers no asynchronous directory enumeration. ADR-0027 now moves that work through the single Windows local I/O execution boundary; the port and listing contract in this decision remain unchanged.
- Hidden and system entries are listed until the hidden-item visibility transition exists in `PaneReducer`.
- Cancellation between two entries cannot be provoked deterministically from a test; the pre-enumeration check is proved and the per-entry check is documented as unproved.
- Entries whose names the path model rejects are counted, not shown, and never dropped silently.

## Migration and removal

No prior mechanism exists. The WSL adapter added by ADR-0035 implements the same port and listing type. A future UNC adapter must join the same provider router; it may not introduce a second read path or a second listing type.

## Executable proof

`DirectoryListingTests`, `DirectoryReadRequestTests`, `WindowsLocalDirectoryReaderTests` against `TestOwnedTemporaryRoot`, `PaneListingPresenterTests`, the ADV-011, ADV-015, and ADV-017 adversarial mappings, and the canonical gate.
