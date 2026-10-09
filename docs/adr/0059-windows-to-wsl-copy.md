# ADR-0059: Copy and move from Windows local to a WSL distribution through the routed transfer path

Status: accepted

Date: 2026-10-10

Revised: 2026-10-10 (Issue #195, second stage: the move across the pair becomes the gateway's composite move)

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

The first stage (Issue #189) enabled copy only and stopped a move across the pair by answering the
gateway's atomic-move capability question with a failure. The gateway already composes a move as
copy, verify, then permanent source deletion whenever that answer is `Unsupported` (ADR-0032), and
the router already sends deletion to the frozen source provider, so the second stage (Issue #195)
needs no new step, port member, or Application change.

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
- **Copy and move, and only in this direction.** The pair answers `GetAtomicMoveCapabilityAsync`
  with `Unsupported`, and the gateway's composite move performs copy, verify, and the source's
  permanent delete. The deletion is the existing `DeleteAsync(snapshot, Permanent)` that the router
  sends to the source provider, the Windows local adapter, which revalidates the source identity
  immediately before deleting it; the cross transfer itself never deletes and its own `MoveAsync`
  and `DeleteAsync` stay `ProviderUnavailable`. A move needs the same preflight as a copy because
  the port receives no request kind at preflight: destination usability, collision, source
  revalidation, and reparse rejection run for both. No confirmation is requested, exactly as for a
  same-provider composite move (FS-005 and the `F6` contract); the source is deleted only after its
  own verification succeeds, a copy or verification failure leaves it in place (ADV-007), and a
  partial result is reported item by item (ADR-0029). Windows segment rules are a subset of Linux
  rules and NTFS cannot hold two names differing only by case in one directory, so no name needs
  translation and no case collision can arise in this direction; the reverse direction waits for
  the Issue #190 diagnosis and a tree-wide pre-scan.
- **Collisions are conflicts, not choices.** An existing destination entry rejects the batch with
  `Conflict` exactly as the WSL adapter does, and so do two sources of one batch whose names derive
  the same WSL target under the provider-aware identity comparer; `KeepBoth` and `Skip` are not
  offered across providers. Partial effects, cancellation points, and `PartiallyCompleted` follow
  ADR-0018 and ADR-0029 unchanged; a move observes cancellation between copy, verification, and
  deletion like every composite move.
- **Metadata is not promised.** Timestamps, attributes, and Linux modes after a 9P copy are whatever
  the redirector produces; verification checks kind, direct entry set, and byte length as it does
  today (FS-009).
- **The drvfs alias fails closed.** A destination under `/mnt/<drive>` of the same machine is the
  same physical tree as a Windows path; the WSL adapter's `Find` on it returns `AccessDenied` over
  the share, so the transfer is refused before any effect. This observed behavior becomes a tested
  invariant; if a future Windows build changes it, the test fails and a new decision is required.
- **Documents and proof.** FS-005 and FS-012 are reworded to say that Windows local to WSL copy and
  move are supported and every other cross-provider operation remains unavailable; ADR-0036's
  condition is recorded as met for this pair by the policies above. ADV-022 states that a
  cross-provider copy derives targets with the destination provider's rules, that the drvfs alias
  and every other pair fail closed, that a failed step leaves the source intact, and that a moved
  source is deleted only after its verification succeeds. The security deep review runs at
  integration readiness for each stage because a fail-closed boundary is opened.

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
- Deleting the source inside the cross transfer, or giving it its own move step: it would be a
  second move algorithm beside the gateway's composite and would bypass the source provider's
  deletion path.
- Requiring an extra confirmation for a cross-provider move: a move deletes its source only after
  verification, as a same-provider composite move does without confirmation; a modal for one pair
  alone would make the same command behave differently by provider.
- Keeping the first stage's failed capability answer: it was the means of withholding the move
  until this decision, not a safety property of the composite path.

## Consequences

- Copying files from `C:\` into a distribution works through `F5`, and moving them works through
  `F6`, with the existing confirmation rules, progress, cancellation, and partial reporting; nothing
  else changes for users.
- `ProviderFileOperationPort` gains a small routing type and one internal transfer; the two adapters
  expose the functions the cross transfer reuses as `internal`, and `IWslFileSystem` gains the two
  Windows-source members that resolve the WSL target for `WindowsLocalTreeCopy`. The second stage
  changes only the transfer's capability answer.
- A deterministic test maps one `TestOwnedTemporaryRoot` child as Windows local and another as the
  WSL `/owned` tree through the existing `WindowsWslFileSystem` seam, so the gateway runs real I/O
  on NTFS; ext4 case behavior and 9P name visibility are covered only by the live tier, which
  declares one cross copy cell and one cross move cell.

## Migration and removal

No stored data changes. Removing the move returns the pair's capability answer to a
`ProviderUnavailable` failure and removes the move tests and the live move cell. Removing the pair
returns the router to same-provider dispatch and removes the cross transfer, ADV-022, both live
cells, and the document sentences together.

## Executable proof

Router tests prove every pair's dispatch, deletion sent to the source provider, `Unsupported` as
the enabled pair's atomic capability, and a failed capability for every other pair. Cross transfer
tests on the owned root prove a nested copy with byte-length verification, collision as `Conflict`
for an existing target and for two sources of one batch, reparse rejection with zero effects, a
partial effect reported as `CopyTargetCreated`, cancellation between steps, the drvfs alias refused
with `AccessDenied` from the WSL handle query, and the source left intact after every failure.
Gateway tests on the same root prove a composite move that deletes each source permanently only
after its exact-byte target is verified, a target changed before verification that keeps its
source, a failed tree copy that keeps its source with `CopyTargetCreated`, cancellation between
verification and deletion that keeps the source, and a move across every other pair refused with
zero effects. The live WSL tier declares one cross copy cell and one cross move cell. Focused
suites, coverage, Infrastructure.Windows mutation, the commit gate, the canonical Ready gate, and
the security deep review at integration readiness prove their tiers.
