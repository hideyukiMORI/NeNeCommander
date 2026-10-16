# ADR-0069: Carry verified descendant evidence to composite-move source deletion

Status: accepted

Date: 2026-10-16

Issue #243. Accepted by the design owner under hide's 2026-10-16 delegation to
resume implementation. This supersedes the byte-count-only verification and
root-only recursive source deletion of ADR-0014, ADR-0037, ADR-0059, ADR-0062 and
ADR-0066. Their provider admission and the explicit residual race scope of
ADR-0033/0049/0062 remain in force.

## Context

Two deterministic owned-fixture tests show that a descendant rewritten after
copy or verification is deleted even though the copied bytes are older. Its
containing operation root can retain the same identity. Repeating one tree scan
before recursive deletion still permits an earlier inspected child to change
while another child is inspected.

The invariant is to reject observed source changes that have not been preserved
in the target, retain the affected source, and report any completed effects.
This is a correction within the existing path-based provider contract, not an
atomic tree snapshot or a claim that concurrent writers are excluded.

## Decision

Keep one `FileOperationGateway` copy/verify/delete algorithm, the existing
provider router, shared tree engine and source-provider deletion owners.

- Successful copy verification returns immutable, typed evidence bound to the
  frozen source root and exact copied target. It records typed descendant
  source/target paths, kinds, provider identities, directory membership and
  file-content evidence. Failure returns the canonical normalized failure.
  Ordinary copy uses the same verifier; it never requests source deletion.
- The shared tree engine compares file bytes with bounded streams, retaining
  SHA-256 content evidence for deletion admission. Capture and recheck both
  sides' identities around content reads and traversal. Require exact child
  membership, typed name admission and non-link entries. No file-size-only or
  timestamp-only success path remains. SHA-256 is cryptographic evidence, not
  a claim of mathematically collision-free equality.
- The gateway passes the successful verification evidence in a distinct closed
  deletion instruction through the existing `DeleteAsync` boundary. It never
  converts a verified move into ordinary confirmed permanent deletion. The
  source provider remains the deletion owner, including cross-provider moves;
  transfer adapters only produce verification evidence. A mismatched source,
  target, provider, entry set or stale receipt is refused without widening the
  request. A receipt is immutable workflow evidence, not a persisted token or
  global authorization registry.
- Before the first deletion, validate the full source and target evidence.
  Consume only recorded entries bottom-up, with siblings ordered by ordinal
  canonical source path text so behavior does not depend on OS enumeration
  order. Immediately before each effect,
  revalidate that entry, its ancestors and matching target, including file
  content evidence. Delete directories nonrecursively, so an unrecorded new
  child is never swept up. A detected rewrite, replacement, link, membership
  change, denied query or unavailable provider stops the remaining deletions.
- Full directory identity changes when this operation removes direct children.
  Keep an operation-private expected full identity for every surviving directory;
  every later admission must match it, including WSL change time. Immediately
  after one successful owned direct-child deletion, record its effect first,
  then capture that direct parent's full identity A, require its original object
  component and exact expected remaining membership, capture full identity B,
  and require A equals B. Only then update that parent's expected full identity.
  The original receipt remains immutable. Never refresh before deletion, after a
  failed deletion, or for an unrelated sibling/ancestor. A failed post-effect
  query or comparison stops with the already completed partial effect. Files
  retain their original full identity and content checks; target evidence never
  changes. An inode/object component alone is not a lifetime identity.
- External source and target ancestors retain their original full identities
  throughout verification and deletion. They are never reanchored by this
  operation. Unrelated sibling activity that changes an ancestor's metadata
  can therefore conservatively stop a transfer; it must not weaken admission
  to an object component alone. Owned-fixture tests that require a stable shared
  temporary ancestor run without competing fixture mutations.
- Windows object identity comes from the existing no-follow volume/file-id
  query. WSL object identity is composed by `WindowsFileIdentifier` from the same
  guarded 9P facts as its full identity: distribution, inode, kind and reparse
  tag, without mutable size, link count or timestamps. Consumers never parse
  opaque tokens. Existing 9P/inode scope and closed query failures remain.
- A failure after some source descendants were deleted reports the explicit
  `SourcePartiallyDeleted` effect for that requested source. `SourceDeleted`
  means the entire requested source was removed. Already deleted unchanged
  descendants remain represented by the verified copy; changed or unprocessed
  descendants remain at source. No automatic rollback or recycle substitution
  is introduced. Cancellation retains the existing between-provider-step
  contract and completed effects.

## Rejected alternatives

- Check only length, timestamp, or the operation root: misses demonstrated
  descendant changes, including equal-length rewrites.
- Reverify once, then recursively delete: later traversal can invalidate an
  earlier observation and recursive deletion can sweep up new children.
- Parse identity tokens or refresh a changed directory baseline: weakens the
  existing provider boundary and can admit replacement.
- Add a second move command, background cleanup, shell mutation or rollback:
  creates a competing policy/ownership path and can destroy unrelated changes.
- Claim complete concurrency protection from a receipt: a receipt is evidence
  to revalidate, not a lock or a namespace transaction.

## Consequences

Verification and deletion admission read file contents and retain O(entry count)
metadata. No full file is materialized in memory and no package is added.
Shared verification now detects equal-length corruption for ordinary copies.
Partial source deletion is an explicit possible result, never a rollback claim.

The final path reopen interval between the last query and a mutation, ancestor
replacement during that interval, and concurrent writes after the final content
read remain residual races under ADR-0033/0062. This decision also explicitly
accepts the narrow interval from an owned direct-child deletion to its immediate
validated parent recapture: a replacement completed before that recapture with a
reused object component and the expected remaining membership may be accepted.
This interval is additional to the old final-query interval, not hidden inside
that claim. After recapture, the full identity including WSL change time is again
mandatory, so ordinary inode reuse during later traversal is rejected. Keeping
only the weak object component after the first deletion would leave a much wider
unprotected interval and is prohibited. Neither Windows handles used only for
identity queries nor 9P observations freeze the tree. The existing WSL
privileged mount/FUSE and clock scope remains unchanged. Tests using mapped
NTFS namespaces prove deterministic algorithm behavior, not real 9P exclusion.
Stronger handle-relative or transactional mutation would require a separate
capability design and real provider proof; this ADR does not claim it.

## Migration and removal

Migrate verification outcomes, the gateway, both source adapters, cross-transfer
adapters, router, filesystem seams and test doubles together. Remove the old
byte-count-only verifier and ordinary permanent-delete instruction from the
composite-move path. Explicit F8 deletion, recycle, atomic move, source/destination
admission, naming, containment and existing copy partial effects keep their
current mechanisms. Update FS-005/012, SEC-003, ADV-007 and effect consumers.

## Executable proof

Keep the two original `VerifiedMoveSourceRaceTests` unchanged in intent. Add
deterministic before/after verification and deletion cases for equal-length
rewrites, restored timestamps, replacement, added/removed/renamed children,
links, earlier-child changes during later inspection, changed target evidence,
invalid receipt binding and partial source deletion. Prove successful nested
deletion despite its own directory-metadata changes. Prove that a touched empty
WSL directory replaced with the same object component but changed change time
after parent recapture is rejected; that only the direct parent is reanchored;
and that recapture failure retains the completed partial effect. Prove gateway cancellation,
provider routing, failure normalization and truthful effect presentation.

During implementation run affected builds and selected transfer/gateway/identity
tests. The final candidate changes a shared destructive trust boundary, so
request one existing deep-review workflow under ADR-0061: it includes the full
canonical gate, adversarial cases, coverage, mutation and CodeQL. Do not request
a duplicate quality run for the same inputs. Record any unexecuted live-provider
or desktop proof honestly; Issue #94 remains separate.
