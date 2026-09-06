# ADR-0039: Resolve transfer conflicts through the existing operation gateway

Status: accepted

Date: 2026-09-06

## Context

Issue #73 requires a typed conflict decision while preserving the transfer safety
contract established by ADR-0037. The current gateway rejects a destination
collision before mutation, but has no resumable operation state or way to show
the affected source and target to the user. A resolver must not create a second
copy or move path, replace an existing target, or trust an inspection performed
before the user answered.

## Decision

Under hide's authorization to execute the remaining work and make the required
design judgments, NeNe Commanderサナ selected the following contract.

- `FileOperationGateway` remains the sole transfer orchestrator. The preflight
  result becomes a closed transfer-specific outcome with success, rejection, or
  a `ConflictSet`; ordinary provider-step outcomes remain mutation-step results.
- `DualPaneSession` owns one `OperationAwaitingConflict` continuation. It freezes
  both panes and the active side while the decision is pending. Escape cancels
  the whole operation without a new effect. The conflict modal initially focuses
  Cancel so an unattended Enter cannot skip a source.
- The continuation retains the original ordered `FileEntrySnapshot` values and
  destination. Resume does not replace them with a later `InspectAsync` result.
  Resume performs complete-batch preflight again, including source identity,
  containment, provider comparison, destination capability, and every
  collision, before any mutation.
- Windows-local transfer is the only provider that emits the new resolver
  choices: `Skip`, `KeepBoth`, and `Cancel`. Replace and directory merge are not
  available. WSL and cross-provider paths retain their existing safe conflict or
  `ProviderUnavailable` outcomes.
- KeepBoth allocates the smallest free number. For a file it inserts ` (n)`
  before the final extension; for a directory it appends ` (n)` to the complete
  name and creates a distinct directory without merging. It neither truncates a
  name nor uses an arbitrary search limit. Candidate names are reserved within
  the batch and used consistently by copy and verification. Segment/path
  validation uses the existing path boundary. Linked directories and
  reparse-point trees are rejected. A race that takes a candidate returns a new
  conflict and never overwrites it.
- Skip is represented in the `NotTransferred` outcome while ordinary progress
  records the processed-item count. It is not a filesystem effect. Move deletes
  an original source only after its copy and verification have succeeded.
- Apply-to-all is explicit, disabled by default, and scoped to this operation.
  A later conflict still revalidates the batch and may return a new conflict.

The approved semantic handoff preserves the existing two-pane shell,
one-operation bar, and generated hints. It keeps the conflict modal's candidate
name visible, separates untransferred/copied/verified/source-deleted states,
and keeps #72's active-pane session behavior independent from startup defaults.
The review artifact is [NeNe Commander UI Preview](https://claude.ai/design/p/ff811404-8e96-4dd0-92c1-320b3002b4b9?file=NeNe+Commander+UI+Preview.dc.html).
Final fonts, colors, and new semantic token names remain outside this ADR.

## Consequences

The application gains one typed continuation state and one typed preflight
contract. The presentation layer can render the existing one-operation bar and
modal ownership without selecting provider policy. The #85 transfer behavior
remains the dependency base; this ADR does not enable cross-provider or atomic
WSL transfer.

## Verification

Gateway tests cover frozen identity, complete-batch resume preflight, Skip
without effects, KeepBoth reservation, candidate races, cancellation, and move
copy/verify/delete ordering. Windows-local adapter tests cover numbering,
case-insensitive collisions, path and segment limits, containment, links, and
reparse points. Router tests prove WSL and mixed-provider fail-closed behavior.
Session tests prove modal freeze, Escape, and operation-scoped Apply-to-all.
Focused suites, coverage/mutation proof, architecture/conformance checks, and
the security-sensitive deep review are required before the dependent PR becomes
Ready. Changing it to Ready requests the canonical gate; the final merge
candidate must then retain fresh successful canonical-gate evidence.
