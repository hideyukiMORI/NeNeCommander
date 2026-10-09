# ADR-0059: Copy from Windows local to a WSL distribution through the routed transfer path

Status: accepted

Date: 2026-10-10

## Context

ADR-0004 defines a cross-provider move as copy, verify, then delete, and ADR-0036 and ADR-0037
keep mixed providers failing closed until capability, metadata, partial-result, and collision
policies exist. The 2026-10-10 survey (Issue #189) established that both adapters already perform
their copies and verifications with the same `WindowsLocalTreeCopy` over Windows paths, the WSL
adapter through `\\wsl.localhost\<distribution>\...` without any process, and that the only
obstacle is `ProviderFileOperationPort`: it chooses an adapter by the frozen source provider alone
and rejects a batch whose sources and destination are not one provider. `FileOperationGateway` and
`IFileOperationPort` do not look at providers. Name rules differ: Windows segments are stricter than
Linux segments, NTFS is case-insensitive and ext4 is not, `Path.GetFullPath` drops a trailing dot
(Issue #190), and the drvfs alias `/mnt/<drive>` of a Windows volume opens as `AccessDenied` over
the share (ADR-0049).

## Decision

- **The router dispatches by the provider pair.** `ProviderFileOperationPort` derives one closed
  `TransferRoute` from the frozen sources' provider and the destination's provider. A same-provider
  pair keeps its existing adapter. The pair Windows local source to WSL destination routes to one
  internal cross transfer inside Infrastructure.Windows. Every other pair, including WSL to Windows,
  WSL to another distribution, and anything involving `WindowsUncPath`, stays `ProviderUnavailable`
  at preflight. The gateway, the port, and the Application layer are unchanged.
- **The cross transfer reuses both ends.** Source-side identity revalidation (`windows-v2`) and the
  reparse rejection come from the Windows local adapter's existing functions; destination-side
  target derivation through `FileSystemPath.Child`, collision detection, the non-link directory
  check, and the partial-target report come from the WSL adapter's existing functions; copying and
  verification are `WindowsLocalTreeCopy.Copy` and `Matches`, reached through two
  `IWslFileSystem` members that only resolve the WSL target to its Windows-side path. No second
  copy implementation, verifier, or identity query is introduced.
- **Copy only, and only in this direction, in this decision.** A move request across the pair is
  rejected before any effect. The port receives no request kind at preflight; the gateway asks
  `GetAtomicMoveCapabilityAsync` only for a move and before its first step, so the pair answers it
  with a `ProviderUnavailable` failure and the batch stops with zero effects. Answering
  `Unsupported` would let the gateway compose copy, verify, and source deletion, which is the move
  this decision does not enable; a later move decision changes that one answer to `Unsupported` so
  the composite path takes over. Windows segment rules are a subset of Linux rules and NTFS cannot
  hold two names differing only by case in one directory, so no name needs translation and no case
  collision can arise in this direction; the reverse direction waits for the Issue #190 diagnosis
  and a tree-wide pre-scan.
- **Collisions are conflicts, not choices.** An existing destination entry rejects the batch with
  `Conflict` exactly as the WSL adapter does, and so do two sources of one batch whose names derive
  the same WSL target under the provider-aware identity comparer; `KeepBoth` and `Skip` are not
  offered across providers. Partial effects, cancellation points, and `PartiallyCompleted` follow
  ADR-0018 and ADR-0029 unchanged.
- **Metadata is not promised.** Timestamps, attributes, and Linux modes after a 9P copy are whatever
  the redirector produces; verification checks kind, direct entry set, and byte length as it does
  today (FS-009).
- **The drvfs alias fails closed.** A destination under `/mnt/<drive>` of the same machine is the
  same physical tree as a Windows path; the WSL adapter's `Find` on it returns `AccessDenied` over
  the share, so the transfer is refused before any effect. This observed behavior becomes a tested
  invariant; if a future Windows build changes it, the test fails and a new decision is required.
- **Documents and proof.** FS-005 and FS-012 are reworded to say that Windows local to WSL copy is
  supported and every other cross-provider operation remains unavailable; ADR-0036's condition is
  recorded as met for this pair by the policies above. ADV-022 states that a cross-provider copy
  derives targets with the destination provider's rules, that the drvfs alias and every other pair
  fail closed, and that a failed step leaves the source intact. The security deep review runs at
  integration readiness because a fail-closed boundary is opened.

## Rejected alternatives

- Adding `OpenRead`/`CreateWrite` stream operations to `IFileOperationPort` (route i): it puts byte
  streams, interruption, and partial writes into Application and duplicates a copy engine that
  already exists; it becomes necessary only for a provider the Windows namespace cannot reach.
- Enabling every pair at once: the WSL to Windows direction needs name translation and a tree
  pre-scan that the trailing-dot behavior of `Path.GetFullPath` makes unsafe today.
- Offering `KeepBoth` across providers: it depends on Windows reservation rules on a Linux
  destination and on a conflict model the WSL adapter does not expose.
- Promising metadata preservation: the 9P redirector's behavior is unverified and FS-009 forbids
  claiming more than the capability intersection.

## Consequences

- Copying files from `C:\` into a distribution works through `F5` with the existing confirmation,
  progress, cancellation, and partial reporting; nothing else changes for users.
- `ProviderFileOperationPort` gains a small routing type and one internal transfer; the two adapters
  expose the functions the cross transfer reuses as `internal`, and `IWslFileSystem` gains the two
  Windows-source members that resolve the WSL target for `WindowsLocalTreeCopy`.
- A deterministic test maps one `TestOwnedTemporaryRoot` child as Windows local and another as the
  WSL `/owned` tree through the existing `WindowsWslFileSystem` seam, so the gateway runs real I/O
  on NTFS; ext4 case behavior and 9P name visibility are covered only by the live tier, which gains
  one declared cell.

## Migration and removal

No stored data changes. Removing the pair returns the router to same-provider dispatch and removes
the cross transfer, ADV-022, the live cell, and the document sentences together.

## Executable proof

Router tests prove every pair's dispatch, including the failed atomic capability and the rejected
move for the enabled pair. Cross transfer tests on the owned root prove a nested copy with
byte-length verification, collision as `Conflict` for an existing target and for two sources of one
batch, reparse rejection with zero effects, a partial effect reported as `CopyTargetCreated`,
cancellation between steps, a gateway move refused with zero effects, the drvfs alias refused with
`AccessDenied` from the WSL handle query, and the source left intact after every failure. The live WSL tier declares one
cross copy cell. Focused suites, coverage, Infrastructure.Windows mutation, the commit gate, the
canonical Ready gate, and the security deep review at integration readiness prove their tiers.
