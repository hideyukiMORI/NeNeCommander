# ADR-0083: Request deletion capability only for ordinary deletion

Status: accepted

Date: 2026-11-19

Issue #476. Accepted by the design owner under hide's instruction to continue the current
milestone and investigate responsiveness, after a read-only independent safety-contract review.
This accepts the design; implementation/integration still requires the proof below.

## Context

`IFileOperationPort.InspectAsync` always freezes identity and deletion capability. Windows obtains
that capability on an owned STA, including Shell entry attributes and a volume recycle query,
even for copy, move, rename and directory creation. The bounded #470/#473 observations motivate
removing this unrelated work; their values do not establish end-to-end latency or future savings.

The current contract cannot express that distinction. Reporting `PermanentOnly` without observing
deletion eligibility would misrepresent evidence. Adding an unqueried case alone is unsafe:
the gateway currently maps every non-Recycle value to permanent deletion and asks for confirmation
only for PermanentOnly. A later unqueried item must not allow an earlier item to be deleted.

## Decision

- Extend the single `InspectAsync` signature with closed `FileInspectionPurpose.IdentityOnly` or
  `.Deletion`, followed by the existing cancellation token. No old overload remains. The gateway
  requests IdentityOnly for copy/move/rename/create-directory, and Deletion for ordinary Delete.
  Routers pass the purpose unchanged; inspection still selects the source provider.
- Keep one immutable `FileEntrySnapshot`, one identity capture and one provider inspection body.
  Add the closed `DeletionCapability.NotRequested` case. It grants no deletion permission and
  means the caller did not request an ordinary-deletion capability observation. A derived
  `IsInspected` predicate recognizes only the two existing inspected cases, Recycle/PermanentOnly.
  Existing positive capability rules and failure normalization remain unchanged.
- Windows IdentityOnly uses the existing ordinary I/O scheduler, captures the same admitted
  `windows-v2` identity and returns NotRequested without entering Shell. Deletion uses the existing
  per-call STA and exactly the existing recycle-capability path. The native scheduler/lifetime,
  recycle flags/admission/query, identity/reparse/drive checks and their effect revalidations are
  unchanged. This refines when ADR-0063's capability inspection is requested, not its safety policy.
- WSL preserves its existing identity capture and returns NotRequested for IdentityOnly or its
  existing PermanentOnly for Deletion. UNC and transfer-only adapters still refuse inspection.
  Null purpose is rejected synchronously before any work is scheduled.
- Before the first ordinary deletion effect, the gateway must have a complete successful batch
  of inspected deletion capabilities. Any NotRequested entry, including a later entry and a
  request carrying confirmation, fails the whole batch with Inspection and no effect/progress.
  Confirmation remains required for every known PermanentOnly batch exactly as before. Only
  known PermanentOnly selects the permanent mode; unknown/absent evidence never selects it.
- The concrete Windows/WSL ordinary-delete boundaries also refuse an uninspected capability
  with Inspection before scheduling an ordinary Permanent/Recycle action. This is a port
  contract guard, not an alternative confirmation policy. The existing VerifiedCopy mode is
  exempt because its independent immutable receipt is the sole authority for source deletion.
  Its full identity/content/ancestor/membership revalidation and partial effects are unchanged.
- Transfer preflight, conflict choices and continuation retain the exact original capability
  value, including NotRequested. Existing full snapshot equality continues to reject capability
  substitution. Resume neither reinspects nor upgrades that value. Atomic move and composite
  move use their existing capability/receipt paths; neither invokes an ordinary recycle delete.

IdentityOnly is the minimum requested observation. The gateway need not reject an already-inspected
capability supplied by a conforming test/provider, since that adds no permission to a transfer.
The concrete production adapters are nevertheless required and tested to return NotRequested and
skip the unrelated capability work. Ordinary deletion always requires an inspected value.

## Rejected alternatives

No fake PermanentOnly default, per-operation cache, deferred delegate inside a snapshot, ambient
operation flag, shared Shell worker, removed recycle availability condition, new inspection port
or compatibility overload is introduced. Separate identity/deletion snapshot hierarchies would
increase migration without strengthening the gateway's required batch admission in this change.
Skipping inspection entirely would lose identity and is prohibited.

## Consequences and migration

Migrate all gateway calls, routers, providers, fixtures and direct contract tests atomically.
Keep the public snapshot factory shape and existing positive capability semantics. No dependency,
schema, UI, settings, native ABI, suppression, waiver or threshold change. Existing cancellation,
normalization and ownership behavior is retained. Old source call sites must fail compilation,
rather than silently taking a default purpose.

## Executable proof

New red/green tests cover every request's purpose, router propagation, null purpose, known/unknown
capability and copy/equality, later uninspected deletion item with and without confirmation,
no prior effect/progress, direct ordinary-delete refusal, and verified-source deletion with
NotRequested. Provider tests prove zero Shell calls for IdentityOnly, the existing STA path for
Deletion, and unchanged identity/drive refusal. Transfer tests cover capability substitution,
NotRequested through target/choice/continuation, and receipt-only composite deletion.

Extend ADV-008/ADV-018 documentation and executable cases. Run affected Application/Infrastructure
and consumer compilation during development, formatting and CS-013 inspection. The shared port
and destructive admission justify one explicitly dispatched deep review on the complete candidate:
its full gate, record-closure proof, coverage, adversarial and mutation results are required before
merge. Do not duplicate that full gate with quality CI. Unchanged native recycle behavior and
unaffected live environments are recorded as reused/not rerun, not claimed as newly observed.
Any before/after latency observation requires its own bounded plan and retained inputs; no predicted
savings or resolution of #453 is claimed by this ADR.
