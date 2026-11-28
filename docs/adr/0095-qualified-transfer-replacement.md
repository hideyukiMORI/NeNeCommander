# ADR-0095: Overwrite confirmed file contents through the existing transfer gateway

Status: accepted

Date: 2026-11-25

Issue: #560. Design accepted under hide's autonomous milestone/design instruction,
after independent read-only review of revision12. Internal provider integration is
implemented in stages; hide approved unpublished local activation on November 27.
The local initial inspector is connected and full acceptance is in progress. Folder-content merge remains a subsequent, unaccepted scope.
The separately verified exact-modal-owner correction is commit11e25f1.

## One mechanism and activation boundary

FileOperationGateway remains the sole transfer orchestrator: frozen sources, complete-
batch preflight, qualified continuation, immutable target plan, CopyAsync, verification
and conditional source-provider deletion for Move. No replacement command, alternative
provider router, App executor, atomic-move shortcut or fallback mutation API is added.

ADR-0039 and FS-005's general existing-target prohibition is narrowed only by this
explicitly confirmed OverwriteContents scope. Unsupported cases still refuse overwrite.
Implementation must migrate the one provider receipt and its consumers together and
complete the destructive/shared-contract proof below before exposing this capability.
This accepted design is not a successful product implementation, release or gate claim.

## Exact consent and supported observations

ConflictDecisionSubmission retains the exact displayed OperationAwaitingConflict;
DualPaneSession admits reference identity only, including Cancel buttons. Native
controls belong to that owner; stale gestures cannot bind to a later conflict.
Keyboard Escape retains the existing current-operation interrupt route.

OverwriteContents means update the contents of the exact confirmed file object. It
is not atomic pathname replacement. Display source and target paths, kind, size/time
when known, and this effect before consent. Initial focus stays Cancel; no default
destructive Enter or new shortcut is added. Known multiple hardlinks are unavailable.
An alias created after final observation may see that same object's changed contents;
this new residual must be explicit in the confirmation explanation and must not be
inferred from the former generic Replace wording. Source/target object aliases always
refuse the operation, even if their displayed paths differ.

The first scope requires Windows-local regular files on observed local NTFS, with one
observed link each and only their unnamed data stream. It refuses directories, UNC,
WSL, cross-provider pairs, reparse points, sparse/compressed/encrypted/offline/device
entries, delete-pending entries, named streams including empty streams, and attributes
outside Normal/Archive. Provider/path labels never substitute for actual capability.
A batch reservation without an existing observed object cannot authorize overwrite.
Unknown, failed, incomplete or unrepresentable observations remain unavailable.

The conflict retains the original target's full identity and displayed facts separately
from the frozen source. The choice and plan retain that exact target evidence. Complete-
batch preflight and effect admission compare it; they never silently refresh consent.
A changed target before effects needs a fresh conflict. Apply-to-all is initially off
and limited to the already disclosed eligible conflicts; unsupported rows remain
unresolved and later discoveries cannot inherit permission. Owned immutable collections
must not expose their backing storage through ICollection/SyncRoot.

## Preparation and mutation boundary

The Windows adapter admits exact source/target/parent/ancestor identities, non-link
shape, local drive and actual filesystem before effects. Hold exact source and target
handles with the supported data/delete-sharing exclusions. No universal mapped-writer,
metadata, security or alias freeze is claimed. Final path/ancestor ambiguity fails
closed; a native experiment inside a trusted root is not ancestry proof.

Create a protected, visible, create-new recovery record, original-content backup and
prepared-content file through the exact parent boundary. All share one bounded opaque
operation identifier from one injected provider-owned source. Initial DACL permits only
the current user. Do not create broadly accessible content then restrict it. No privilege
enabling, time-derived name, retry overwrite, foreign cleanup or hidden rollback.

Copy the original target's default stream into backup and source bytes into preparation
using bounded buffers and cancellation checks. Verify bytes, length and identities.
Immediately before target mutation compare the original target's full identity AND
complete bytes with the verified backup, and the original source with preparation;
recheck both stream memberships and attribute shapes. Do not reset either baseline.
An observed mismatch retains preparations and leaves target untouched by this operation.
The final-check-to-mutation interval remains vulnerable to an arbitrary mapped/kernel
writer; neither the backup nor target is advertised as a universally frozen snapshot.

Save/flush the complete recovery mapping described below before any target mutation.
Update contents through the exact target handle, including truncation for empty input.
Enter MayHaveChanged BEFORE the first potentially mutating call. A failed first write
may have written bytes; a truncate is independently destructive. No exception implies
no effect. Verification compares complete desired bytes/length, original source and
exact target object, plus admitted streams/attributes. No in-place rollback is attempted.

## Metadata decision

| Property | Operation behavior |
| --- | --- |
| Target object identity/name | Write/truncate through held handle; do not replace or rename |
| Owner/group/DACL/SACL/labels/resource attributes | Do not set/copy back; do not revert concurrent changes; no whole-descriptor freeze claim |
| Creation time | Do not set or restore; ordinary-write observation preserves it |
| Last-write/change/access time | Allow OS read/write/close updates; no SetFileTime or copied source timestamp |
| Archive/ordinary attributes | Allow OS bookkeeping; do not restore snapshots; require admitted Normal/Archive shape |
| Named streams | None admitted, including empty streams; never remove them to qualify a file |

The result verifies content and observed identity, not complete source metadata fidelity.

## One receipt for preparations, uncertainty and completion

Migrate ProviderStepOutcome's optional effect into one immutable owned receipt snapshot
used by existing provider operations as well. Target observation and artifact inventory
are fields of that same mechanism. Success can also carry retained/removed-artifact
facts. No parallel replacement-specific result or unqualified compatibility path remains.

| Event | Target observation | Artifacts | Source |
| --- | --- | --- | --- |
| Admission fails before create | UntouchedByOperation | none | retained |
| Preparation create/write fails | UntouchedByOperation | role-specific partial/verified/unknown state | retained |
| Preparation and old-content comparison complete | UntouchedByOperation | verified backup/stage and complete mapping | retained |
| Enter first write/truncate | MayHaveChanged | verified preparations retained | retained |
| Write/truncate throws or cancellation arrives | MayHaveChanged, or OriginalContentVerified after exact size/byte proof | all known artifacts retained | retained |
| Complete desired verification succeeds | DesiredContentVerified with exact hash/length/ID | backup/stage retained; receipt append pending/complete | retained |
| Cleanup partly succeeds/fails | previous target observation | owned link removed, retained or uncertain per role | retained |
| Copy cleanup completes | DesiredContentVerified | owned artifact links removed | retained until gateway verification/deletion |

Each artifact carries role, exact handle-derived object identity, content-verification
state, deletion result and last-observed typed path with CurrentPathVerified/LastKnownPath
qualification. Stable object identity remains identified even if complete metadata has
never been obtained. Full-identity knowledge is closed: NotObserved or the last successful
complete identity, retained on later failure. Neither a historical full identity nor a
receipt grants cleanup authority; the live owned handle and fresh admission remain required. A failed lookup or changed ancestor cannot be called the current actual
path. Cleanup acts on proven owned handles only, never a reopened name or foreign
occupant. Failure/ambiguity retains artifacts and source, without a rollback promise.

TransferExecution aggregates these facts into the one FileOperationOutcome on success,
failure and cancellation; skipped-source merging and Presentation retain them. The
existing effects-empty AwaitingConflict is allowed only during complete-batch preflight
before effects. After any artifact exists, a new conflict/refusal terminates with its
partial receipt. It cannot discard artifacts by returning that old conflict state.

## Recovery mapping and process-crash ordering

Use one bounded append-only recovery record. Its first complete length/checksum-framed
UTF-8 JSON record contains schema, operation identifier, original target/source paths
and identities, exact backup/stage names and identities, verified original/desired
lengths/hashes, and PreparedWithTargetPossiblyChanging. Flush it before target mutation.
An incomplete initial record never permits mutation. Append/flush DesiredContentVerified
later; never truncate or rewrite the initial mapping merely to advance the phase.
Inspection ignores a partial trailing record; an invalid initial record authorizes nothing.

The envelope is bounded from the canonical path limits, generated child names, fixed
identity/hash fields and schema overhead. Serialization must round-trip exact path text;
failed/ambiguous encoding refuses before target mutation. Do not normalize a path to
make the record fit. The visible common identifier and human-readable mapping let a
person identify the original backup after process crash. Document manual inspection and
current identity/content revalidation: a receipt is evidence, not permission to overwrite
a newer file. No automatic startup scan, restore, deletion or executable recovery command.

The implemented pure codec fixes schema version 1 and names the three target siblings
`NeNeCommander-<32 lowercase hexadecimal token>-recovery.txt`, `-original.bin` and
`-prepared.bin`. Names come from one framework Guid candidate token; Guid.Empty and
repeated values are not ownership proof. The preparation attempt owner requests one token
from the same injected IIdentifierSource after pair admission and fixed sibling-shape
validation. Shape checking does not reject a collision with a dummy token; only the
actual token candidates undergo collision admission. No second identifier source exists.

Each frame is eight uppercase hexadecimal payload-length digits, a colon, the 64-digit
uppercase SHA-256 of the payload, LF, strict UTF-8 JSON payload, and LF. Payloads are
bounded to 1 MiB; framing adds 75 bytes, and input is bounded to two maximum frames
before snapshot allocation. Four canonical paths at 32,767 UTF-16 units and eight
512-unit identities cost at most 810,984 escaped JSON bytes; fixed fields and structure
remain below the payload limit. The parser rejects impossible partial length prefixes
as soon as their minimum completion exceeds that limit. Initial text, all nested fields
and canonical path spelling must round-trip exactly. Duplicate/unknown/reordered
properties, unsupported schema/phase, malformed Unicode and alternative numeric/text
representations refuse through canonical reserialization comparison.

The completion frame additionally contains the SHA-256 of the exact complete initial
frame. A repeated token, identical target identity and identical desired content cannot
substitute a completion belonging to another initial mapping. Only an incomplete append
with an otherwise valid available header prefix may be ignored after a complete initial;
a complete append must be followed by EOF. Invalid complete frames and any third-frame
bytes refuse. Decoded prepared/completed states describe saved observations only and
create no current pathname, current content, native flush or recovery authority.

Flush calls do not guarantee hardware durability or power-loss consistency. A process
crash after valid mapping may leave mixed target content, conservatively described by
that retained mapping. Cleanup removes owned backup/stage links and recovery record last;
partial cleanup remains represented. Process-crash feasibility is distinct from final
product failure/cleanup proof and must not be described as automatic recovery.

## Move and future merge

Extend the existing immutable VerifiedCopyReceipt/descendant evidence with the admitted
source/target stream shape and attribute requirements. The source-provider deletion
admission rechecks them immediately before removal alongside its existing identities,
content and ancestors. Added/changed streams or incomplete/failed queries retain source.
No new deletion method or foreign-stream removal. OverwriteContents never admits atomic
move; the sole gateway retains copy/verify/conditional source-delete ordering.

Directory-content merge is not enabled by this ADR. A later accepted descendant plan
must preserve destination-only children, distinguish additions/overwrites/skips, require
separate child-file consent and retain partially transferred source directories. It
cannot weaken the existing exact-tree verifier or infer child deletion from root consent.
Issue #560 stays open until that separate scope and the file scope are implemented/proved.

## Consultation, alternatives and required proof

Actual Claude /design consultation is recorded in the quality evidence. The installed
skill exposes access management only; no grants/revokes or Canvas claim are made.
Ordinary written advice informs labels using existing semantic tokens. It cannot change
these safety/command decisions. No new visual token values are selected here.

Reject path-based unconditional overwrite, a rename/publication fallback with an
unresolved copied-security race, and treating share exclusion as an alias/security
freeze. Native failures and these alternative investigations remain in the quality record.

This new destructive/shared contract requires deterministic adversarial tests, normal
builds and affected consumers, native owned-filesystem races/failures, zero-length and
write-exception uncertainty, original-backup admission, stream changes through source
deletion, malformed/partial recovery records, bounded owned process-crash termination,
cleanup identity, coverage, changed-layer mutation, architecture/conformance and the
canonical deep-review scope. Reuse unchanged proof only when its inputs remain unchanged.
No lowered threshold or waived failure. The owner-only focused proof establishes none
of this new capability's product acceptance.

Reference: [quality and exact commands](../quality/2026-11-25-transfer-conflicts.md).


### Preparation evidence semantics

Verified artifact content retains the last successful complete byte observation, not
current equality or permission to mutate/clean. Read-only final verification keeps
that historical length/hash even on mismatch, read failure or cancellation; its path
is LastKnown until fresh native admission succeeds. The one-shot preparation refuses
another write or baseline replacement once ordinary artifact content is verified.
Only successful fresh identity, stream, shape and complete byte comparisons to the
FIRST original/desired digests satisfy current preparation admission. The recovery
record likewise keeps its initial frame and flush acknowledgement; final preparation
rechecks its exact complete bytes and stable/full identity against the saved initial
frame, including EOF. No renewed check replaces a baseline or reinterprets a failed
check as present equality. These remain preparation prerequisites; provider activation,
target mutation, cleanup and verified source deletion are not implemented by them.


### Post-content identity admission

The same WindowsTransferPathLease owns both forms of observation. Original admission
continues to require the original full file identity. Post-content observation uses
that SAME root mapping, ancestor chain, native path and stable file object checks,
then the same NTFS/regular-file/link/attribute/default-stream admission. Only the old
length/timestamp equality is omitted after content effects; no original confirmation
or lease baseline is refreshed. Current facts must agree across the final path and
stream observation. Artifact admission after target effects borrows that same parent
chain and keeps its own stable identity and full shape/stream checks. These methods
prove observations only; they neither authorize a target write nor claim byte equality.


### Internal target-write stage

The preparation owner is renamed to WindowsOverwriteAttempt and retains the SAME
canonical target/artifact receipt. OverwritePreparedAsync consumes Prepared before its
first await, repeats final original content/frame/identity admission, and copies the
prepared held stream into the exact target held object through the existing content
copy/I/O owners. Private target-write/truncate delegates record MayHaveChanged BEFORE
invoking that existing I/O boundary. A prior read failure or cancellation remains
UntouchedByOperation; any exception after entering a potentially mutating call retains
uncertainty, even if the injected failure happened before observable bytes changed.

Final target verification requires original source full-identity/shape/stream admission,
current target shape/stream/identity and FIRST desired size, complete source-target
bytes and both EOFs with the FIRST desired digest, then repeated source/target admission.
Only then is DesiredContentVerified retained, before later cancellation is checked.
The stage is one-shot after failure, cancellation or verification; disposal qualifies
saved target/artifact paths as LastKnown and closes artifact handles without rollback.
No completed provider effect, completion append, cleanup or source deletion follows
merely from this internal stage. Production capability and provider wiring remain off.


### Internal completion append

The same attempt consumes a successfully returned target-verification stage before
its first append await. Desired-content evidence is retained before cancellation,
but the success continuation is established only after the cancellation check;
a failed or cancelled target stage cannot later resume into completion or cleanup.

The existing recovery writer rechecks the exact initial frame, Confirmed initial
flush, current owned record/parent and complete initial bytes/EOF. It writes only
after that prefix, never truncating it. The existing codec binds completion to the
exact initial-frame digest. Full initial-plus-completion readback and EOF establish
append-frame evidence before later fallible metadata, flush or cancellation steps.
The initial frame/flush is never replaced by the append's independent frame/flush.
Acknowledgement is recorded before subsequent cancellation, without durability claims.

Every failure retains target evidence, source and all artifacts. The one-shot
continuation rejects reentry and replay; success still has no completed provider
effect. Owned cleanup, source-delete admission, provider activation and complete
ADR0095 destructive proof remain separate required steps.

### Native held-link removal prerequisite

WindowsTransferArtifactRemoval provides only the unconnected native primitive:
FileDispositionInformationEx with DELETE|POSIX followed by acknowledged CloseHandle.
A protective SafeFileHandle reference remains held until the successfully consumed
handle is marked invalid, preventing a later managed release from closing a reused
native value. Failures do not acknowledge removal; an acknowledged disposition with
unconfirmed close may still remove its link during later disposal. This primitive
accepts no pathname, alternate deletion method, retry or readonly bypass.

It grants no ownership, current-content or cleanup authority. Integration must retain
the one canonical artifact receipt, freshly admit source/target/artifact/record facts,
record Uncertain before possible effects and Removed only after acknowledgement,
remove recovery last and preserve source on any incomplete stage. Primitive tests
alone do not activate cleanup, overwrite capability or verified source deletion.

### Accepted internal owned-artifact cleanup

After a successfully returned completion append, the SAME attempt consumes that
continuation once. WindowsOverwriteCleanup borrows its owners and keeps the existing
receipt as the sole evidence mechanism. Each removal requires fresh original-source
admission, desired source/target bytes and first digest/EOF, exact target object/path/
ancestry, every remaining artifact's first full identity and complete content, and
both exact recovery frames with confirmed flush observations. Identity checks repeat
after reads. The order is prepared, backup, then recovery record.

The artifact owner consumes removal access and records Uncertain/LastKnownPath before
the native call; only acknowledged close changes it to Removed. Failure/cancellation/
disposal cannot resume and cannot resurrect path qualification or erase learned content.
The enclosing attempt invalidates its target path on every incomplete cleanup, including
a later artifact-admission failure after desired verification. Successful internal cleanup
still grants no source deletion and supplies no provider Copy success.

hide explicitly approved this internal integration and dedicated-file proof on November
25 at 21:43. Source deletion and provider/UI activation were excluded. Focused internal
acceptance does not satisfy the full destructive proof or activate OverwriteContents.


### Internal verified-file requirements — November 26

VerifiedCopyEntry.File now requires FileVerificationRequirements explicitly; there is
no default overload. DefaultStream retains the ordinary copy verifier's existing
contract. OverwriteContents carries the stricter local NTFS, regular, single-link,
Normal/Archive and only-unnamed-stream observations through the SAME immutable
VerifiedCopyReceipt. A requirement is neither consent nor deletion permission.

The existing VerifiedTreeAccess reuses WindowsTransferPathLease and the native
WindowsOverwriteFileAdmission owner to compare complete source/target snapshots,
reject object aliases, and observe source again after target admission. The shared
verifier checks requirements after content hashes. VerifiedSourceDeletion checks
them again after its final source/target identity queries. Added empty or nonempty
named streams, disallowed attributes, multiple links, a changed full identity, foreign
providers or failed native queries retain source through the existing failure route.

The existing final-query-to-path-deletion residual in ADR-0069 remains; disposing
these read-only admission leases does not freeze future metadata or mapped writers.
No new deletion API, stream removal, baseline refresh or atomic guarantee is added.
The ordinary producer still emits DefaultStream. Provider/gateway integration must
produce strict evidence for overwrite and reject a downgrade before Move deletion;
that wiring, UI activation and full destructive acceptance remain future work.


### Internal provider integration — November 27

The existing WindowsLocalFileOperationAdapter.CopyAsync queues its operation through
WindowsLocalIoExecutionBoundary and unwraps the inner task. WindowsOverwriteCopy
owns the pair and the existing WindowsOverwriteAttempt until preparation, target
write, completion append and cleanup all return. Expected I/O/access failures and
cancellation return the same retained receipt after disposal; success requires the
complete sequence. No new result abstraction, retry, fallback, rollback or deletion
entry point is introduced.

The closed OverwriteContents decision is available only on a conflict with qualified
evidence. The resumed native preflight compares the exact original pair before adding
the original ordinary target to the plan; it never refreshes consent after a mismatch.
The copy boundary independently checks source, original target and destination binding.
Both gateway selection and provider capability/execution refuse atomic move.

WindowsLocalTreeCopy produces FileVerificationRequirements.OverwriteContents through
the same verifier, with fresh strict native admission and original target-object binding.
TransferExecution rejects ordinary requirements, directories and different source or
target objects before the existing verified source-deletion call. Source retention on
failed preparation/write/append/cleanup is preserved by the one attempt receipt.

WindowsIdentifierSource is composed once at App startup and injected into settings and
the provider router. Local adapter construction now requires that source explicitly;
there is no ambient generator or constructor that silently supplies one. A cohesive
local-adapter dependency holder supplies the existing platform owners to tests.

This stage remains unexposed: initial production conflict construction still passes
no overwrite evidence. Dedicated test ports supply the initial qualified conflict to
exercise the real resumed provider/gateway path. Initial qualification and the explicit
three-language consent UI, full ADR-0095 proof and separate directory merge remain.

### Unexposed initial qualification and disclosure — November 27

Closed TransferOverwriteCapability replaces nullable evidence with one qualified or
unavailable value. The initial inspector uses existing native/pair owners; both
production sites still supply Unavailable(Unconfirmed). Automatic approval rejected
connecting it before complete destructive acceptance; that action did not execute.

Gateway and choice construction refuse unavailable overwrite. Overwrite-all includes
only already disclosed eligible original pairs. The same exact-owner modal retains
Cancel focus, separate scopes, original metadata and three-language late-alias/
partial-stop wording. The operation bar discloses the same retained receipt, including
uncertain cleanup and unacknowledged Move deletion, without a recovery executor.

Focused tests and three-locale isolated presentation proof pass, with full scope still
pending. The next requested authorization is limited to local initial-preflight wiring
and dedicated-file integration/full ADR0095 proof before publication/main integration.
No gate waiver, feature acceptance or release follows from this checkpoint.

### Read-only result keyboard access

Visible result evidence joins the existing F10 command-entry native Tab cycle.
It retains the same CommandEntry input classification, native scrolling and
F10/Escape return to the active pane; no new shortcut or execution route exists.
The result viewport has a localized accessible name. The additional overwrite
list keeps its native ItemsControl focus and names its disclosed scope; native
PageDown/End must reach its final row before keyboard acceptance is claimed.

### Authorized unpublished activation — November 27

hide's15:41 approval supersedes the preceding held connection only for the unpublished
branch and dedicated-file full proof. Initial preflight now invokes the existing
eligibility owner. Batch reservation alone cannot authorize overwrite. Native malformed
recovery text refuses before effects, and expected provider InvalidDataException retains
the same receipt. The immutable choice factory owns qualification admission; plan
validation keeps decision/exact-target checks without an unreachable duplicate null
condition. Full final mutation and CodeQL acceptance are still in progress. Publication,
main integration, release and directory merge are not authorized by this local checkpoint.
