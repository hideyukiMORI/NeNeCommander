# ADR-0070: Allocate one settings temporary candidate per write attempt

Status: accepted

Date: 2026-10-17

Accepted by the NeNe Commander design owner under hide's delegated authority for
the resumed implementation of Issue #250. This supersedes only ADR-0040's fixed
temporary name and its incidental serialization of overlapping writers.

## Context

An unexpected interruption after the settings temporary stream is flushed and
closed can leave `settings.json.tmp`. A new store reads the settings document,
but every later write rejects that occupied fixed name. Startup cannot prove
ownership of the previous process's temporary entry. Deleting it, or promoting
its contents, would violate the existing ownership and malformed-input rules.

The invariant is that an unrelated or interrupted temporary artifact must not
permanently occupy the only ordinary save candidate, while the existing store
still preserves every observed foreign entry and every rejected destination.

## Decision

- Keep `ISettingsStore`, `WindowsLocalSettingsStore`, the complete settings queue,
  atomic publish, and persistent warning as the sole execution path. Reads remain
  queries and never enumerate, repair, promote, or remove temporary artifacts.
- Materialize the command registry's `IIdentifierSource` boundary in Application.
  It returns a framework `Guid` as an opaque candidate token, not a filesystem
  identity, authorization token, or uniqueness guarantee. The sole production
  implementation is `WindowsIdentifierSource` in Infrastructure.Windows; App
  explicitly composes and injects it. Tests inject deterministic implementations.
- One admitted write requests one token, after baseline preflight and before any
  directory or temporary mutation. Its sibling is
  `<document>.<32 lowercase hexadecimal characters>.tmp`. Validate that fixed
  maximum path shape at construction using `Guid.Empty`, without generating a
  token or doing I/O. Each write keeps its candidate local to that attempt.
- Keep one attempt: any observed file, directory, or reparse entry at the selected
  candidate is `TemporaryArtifactCollision`. `CreateNew` is the final exclusive
  creation primitive. A collision is not retried under another candidate.
  Probabilistic name separation never establishes ownership.
- Preserve flush, handle-captured temporary identity, exact bytes, the approved
  ancestor anchor, destination identity/content comparisons immediately before
  publish, and the existing atomic move/replace. Cleanup may remove only the
  current attempt's verified owned artifact. Replaced, linked, inaccessible, or
  otherwise uncertain artifacts remain with the existing typed residue outcome.
- Retain the old fixed `.tmp` and all other attempt artifacts without opening,
  enumerating, adopting, or deleting them. This change restores ordinary saving;
  it is not a stale-file collection feature. A later explicit repair policy would
  require its own ownership contract.
- Cancellation keeps ADR-0040's boundary: observe it before the first mutation;
  after directory/temporary mutation begins, complete publish or report the exact
  rejection and effects. No retry loop, background cleanup, scheduler, or alternate
  writer is introduced.
- CS-010 permits the direct `Guid.NewGuid()` call only at the exact repository
  path of `WindowsIdentifierSource`, for this concern alone. References elsewhere,
  including method groups,
  identifier type aliases and static imports are rejected. Other ambient APIs
  remain prohibited even inside that adapter. The existing raw-text language
  restrictions still apply; this is not semantic C# analysis.

## Concurrent writers and residual scope

The existing session queue serializes selections in one application session.
Different stores can now own distinct temporary entries concurrently. If another
writer publishes before this writer's final destination comparison, the stale
writer rejects `DestinationChanged` and cleans only its own verified temporary.
Equal candidate tokens still collide through the same ownership checks.

The fixed name previously serialized writers while that temporary entry existed;
that incidental exclusion is deliberately removed. Both writers may pass their
last comparisons before either replaces an existing document. The last atomic
replace may then win. ADR-0040 already excludes cross-process compare-and-swap and
merge guarantees; this change increases the opportunities for that documented
race and does not claim to retain the former temporary-name exclusion. An absent
destination is still installed with non-overwriting `Move`.

Ancestor/document/temporary revalidation followed by path-based BCL operations
retains ADR-0040's final reopen races. No frozen directory, writer lock, crash
durability beyond the existing flush/publish, or recovery of interrupted choices
is claimed. Candidate collisions remain safe failures, including a faulty or
repeating identifier source.

## Rejected alternatives

- Delete or reuse an old fixed temporary: there is no current ownership proof.
- A fixed alternate name or bounded numbered slots: accumulated interruptions
  can occupy every ordinary slot and recreate the permanent saving obstruction.
- Generate a token in the store or use time/process IDs: ambient identity inputs
  bypass ARC-007/CS-010 and make deterministic testing unreliable.
- Add a global lock, lock-file protocol, or startup collector: each adds a new
  lifetime and trust boundary beyond this bounded recovery requirement. None is
  needed to claim the deliberately limited existing publish contract.
- Retry on collisions: retries widen mutation and cancellation semantics without
  evidence that the competing artifact belongs to this write.

## Migration and removal

Remove the store's fixed temporary-path field and inject the identifier port at
every construction site. Move existing temporary-safety tests to deterministic
candidate names without weakening their assertions. Keep legacy residue only as
data; there is no legacy writer, schema migration, new dependency, or waiver.
Update ADR-0040, SEC-011, CS-010, the mechanism registry and ADV-012 atomically.

## Executable proof

First reproduce the old failure with the existing `TemporaryClosed` seam: an
unexpected test-only exception leaves a flushed temporary, then a new store
reads and attempts a save. Preserve the red result. Prove recovery with both
legacy and current-attempt residue, unchanged foreign bytes, fresh candidates
on subsequent writes, same-candidate collision, and two distinct stores whose
stale writer observes another completed publish. Prove original-document
preservation, linked/replaced temporary refusal, owned cleanup/residue, and both
cancellation boundaries through the affected ADV-012 cases.

Under ADR-0061, use affected Release builds, settings-store and direct settings
consumer tests, composition/architecture proof, changed-file formatting, and
Commit checks. The CS-010 change has bounded positive/negative proof in
`eng/prove-gates.ps1`, also retained in its default full proof. Review the final
settings concurrency and ownership diff independently. These tests cover the
changed path selection and its direct boundaries; other provider operations,
schemas, command routing and UI behavior are unchanged. No full/deep workflow,
all-layer coverage/mutation, live provider tier, or release-screen proof is
selected merely because this isolated adapter input changes. Expand proof only
for a concrete wider impact discovered in implementation or review.
