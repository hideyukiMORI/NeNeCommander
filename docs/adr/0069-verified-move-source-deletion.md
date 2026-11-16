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
- External source and target ancestors — every directory from the parent of
  the operation root up to the provider root — are matched by their object
  component throughout verification and deletion, and each must still be a
  non-link directory. Their mutable metadata (timestamps, link count, size) is
  not compared, so an unrelated entry created, removed or renamed in an
  ancestor does not stop a transfer. They are never reanchored by this
  operation. Every recorded entry inside the operation tree keeps the full
  identity and content rules above. Revised on 2026-11-15; see "Revision
  2026-11-15" below for the reason and the residual scope.
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

## Revision 2026-11-15 (Issue #388): external ancestors match by object component

Decided by hide on 2026-11-15. The original rule kept each external ancestor's
full identity, so "unrelated sibling activity that changes an ancestor's
metadata can conservatively stop a transfer", and forbade weakening ancestor
admission to an object component alone.

Evidence that the rule was wrong for ancestors:

- A directory's last-write time (and on WSL its change time and link count)
  changes whenever any entry is created, removed or renamed directly inside it.
  The ancestors of a real transfer include directories that other programs
  write to all the time: a downloads folder, a user profile, a temporary folder.
- The comparison window is per requested item, from the start of its
  verification to the deletion of its last entry. For a large tree that is
  minutes. During that window one unrelated file created in any source or
  target ancestor stopped the item: `Verification` during verification, or
  `IdentityChanged` with `SourcePartiallyDeleted` during deletion. The source
  was kept, so nothing was lost, but the user's move or copy stopped for a
  change that had nothing to do with it.
- The same cause made the verified-move tests nondeterministic. Their fixtures
  live under the shared temporary directory, which is therefore an ancestor.
  On main `e2750be` the Infrastructure test suite failed 8 of 30 runs, and one
  scenario run alone 1,000 times diverged 6 times. Making another process
  change the temporary directory after verification reproduced the failure
  200 of 200 times. The clause "owned-fixture tests ... run without competing
  fixture mutations" could not be honored, because the operating system and
  other processes write there too.

What an ancestor comparison has to detect is that the path to the operation
root now leads somewhere else: an ancestor was replaced by another directory,
or became a link. The object component detects both. It names the object
(Windows volume and file identifier; WSL distribution, inode, kind and reparse
tag), and the non-link check is unchanged. Sibling activity in an ancestor
does not change which objects the recorded entries are.

The earlier concern, that an object component alone is not a lifetime
identity, is about reuse of an inode after deletion. It still governs every
directory inside the operation tree, where this operation itself deletes and
a replacement could reuse the component. For an external ancestor the residual
case is narrower: an ancestor removed and recreated with a reused object
component is not detected at the ancestor. The recorded entries below it are
still revalidated with their full identities and content evidence immediately
before each effect, so a different tree placed under the recreated ancestor is
refused there. This residual case is accepted and is additional to the
residual races already listed under Consequences.

The parent of the operation root is an external ancestor like the others. A
file written next to the source root, as in the `external-parent` case of
Issue #348, no longer stops the transfer; a change to any recorded entry of
the tree still does.

Proof for this revision: the adversarial cases keep rejecting a replaced
ancestor and an ancestor that became a link, on both the source and the target
side, during verification and during deletion; new cases prove that creating,
removing and renaming an unrelated entry in a source or target ancestor during
verification and during deletion does not stop the transfer; the
`external-parent` case is rewritten to state the new rule
(`VerifiedMoveAncestorTests`, `VerifiedTreeBoundaryTests`,
`VerifiedMovePartialDeletionTests`); and the
Infrastructure test suite is repeated to show that the intermittent failures
are gone, with the count recorded in the PR. Because this changes a shared
destructive trust boundary, one deep review is requested under ADR-0061.

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
