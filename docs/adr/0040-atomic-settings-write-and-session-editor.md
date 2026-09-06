# ADR-0040: Persist complete settings through one session-owned editor

Status: accepted

Date: 2026-09-06

Accepted under hide's delegated implementation authority and adopted by the NeNe Commander design
owner for Issue #74.

## Context

ADR-0022 established `%LOCALAPPDATA%\NeNeCommander\settings.json`, `ISettingsStore`,
the closed schema-version-1 `UserSettings`, and the composition-root color-scheme mapping, but
deliberately made the store read-only. The only way to select a scheme is to edit JSON outside
the application. ADR-0024 and Issue #72 make hidden-item visibility pane-owned session state:
`Ctrl+H` changes the active pane for this process and must not silently redefine a persisted
launch preference.

Issue #74 requires one atomic write path. A write API with no user-reachable command would leave
the first-release settings feature incomplete, while a view-owned writer or a second settings
store would violate ARC-001, CMD-003, CMD-007, and the command registry. The settings editor also
has to coexist with the one operation bar and with file-operation modal ownership.

## Decision

- **One boundary reads and writes one complete document.** `ISettingsStore` gains
  `WriteAsync(UserSettings, CancellationToken)` and returns the closed
  `SettingsWriteOutcome`. `WindowsLocalSettingsStore` remains its sole production adapter and
  serializes the entire schema-v1 value in the fixed property order `schemaVersion`,
  `showHiddenItems`, `colorScheme`, as UTF-8 without a byte-order mark. No partial settings value
  crosses the boundary. Startup ancestor inspection, Win32 identity queries, and document reads,
  as well as writes, run behind the existing `WindowsLocalIoExecutionBoundary`.
- **Detected baseline changes are rejected before this writer publishes.** The adapter validates
  an existing document before creating a sibling temporary file and captures either absence or the
  valid document's provider identity plus exact bytes. Immediately before publish it reopens and
  revalidates that identity and content; a detected appearance, disappearance, replacement, or
  rewrite is the closed `DestinationChanged` rejection, and this writer does not replace the
  destination. A successful new document is installed by a same-directory move; a successful
  update replaces a valid existing document through the provider-owned atomic replace path. The
  settings document itself must be a direct non-reparse file at read, preflight, and publish
  revalidation; a file symbolic link is rejected without intentionally following its target. The
  temporary stream is flushed before installation. Expected access, I/O, rejected-document,
  destination-change, and temporary-collision failures are closed outcomes. A rejected outcome
  reports directory creation separately from temporary-artifact residue rather than confusing
  either with the failure reason. The residual final path-reopen race is stated below and prevents
  an unconditional preservation claim in the presence of an external writer.
- **The fixed temporary path has one-attempt ownership.** The adapter creates the sibling
  `settings.json.tmp` with `CreateNew`; a file, directory, or reparse entry found there is a
  collision and is not opened, overwritten, or deleted. Cleanup runs only after this attempt
  successfully created that file.
  Cleanup revalidates the parent chain and both the temporary file's own identity and exact
  serialized bytes. A detected
  parent or temporary replacement is not deleted. A cleanup denial or identity ambiguity leaves
  the bytes for diagnosis and reports `TemporaryArtifactLeft`. The temporary entry's captured
  identity and exact bytes are also revalidated before publish, so an in-place rewrite with
  restored length and timestamps cannot become the settings document. The final `File.Delete` path reopen
  remains subject to the residual race stated below.
- **The Windows local ancestor chain stays local.** The store accepts only a `WindowsLocalPath`;
  UNC and WSL paths cannot enter its constructor. From the document parent to the drive root, each
  existing directory must be a directory rather than a reparse point and is captured by the
  existing Win32 volume/file identifier mechanism. Expected-absent ancestors stay absent until
  the directory creation call, after which the chain is captured again. Existing ancestor
  identities must still match and every resulting ancestor must be a direct non-reparse directory.
  The verified chain becomes the baseline immediately, so a later temp or publish failure does not
  make the next owned retry reject its own observed directory state. After publish, the new
  document baseline is accepted only when both exact bytes and the stable provider identifier link
  the document back to this attempt's temporary entry. Startup capture and verified owned directory
  creation are the only points that adopt an ancestor anchor. Temporary and post-publish capture
  must match that approved chain; a different chain is blocked rather than becoming a new baseline.
  The chain is revalidated
  before directory or temporary creation, before publish, and before cleanup. A junction,
  symbolic link, ancestor replacement, non-directory ancestor, or identity-query ambiguity found
  by those checks fails closed as `UnsafeLocation`, `Unauthorized`, or `IoFailure`; the checks do
  not make the following path-based operation race-free.
- **Directory and temporary effects stay distinct.** A rejected outcome carries a closed directory
  state (`NotAttempted`, `CreationObserved`, or `CreationUnconfirmed`) and a separate temporary
  residue state (`None` or `TemporaryArtifactLeft`). `CreationObserved` means the provider creation
  call returned and the resulting safe chain was verified; it does not claim exclusive ownership
  against an external creator. Creation is never rolled back recursively. If creation starts but
  throws or its resulting chain cannot be verified, the state is `CreationUnconfirmed`.
- **Cancellation stops only before mutation starts.** The token is observed before directory or
  temporary creation. After that point the adapter completes installation or reports the exact
  outcome and both effect states. The synchronous install primitive runs through the existing
  `WindowsLocalIoExecutionBoundary`; no second scheduler or filesystem gateway is introduced.
- **One application session owns choices and ordered writes.** The settings session holds the
  current complete `UserSettings`, the closed editor state, and the closed persistence state
  (`Succeeded`, `Pending`, or `Failed`). A selection changes session state immediately and queues
  its complete value for write in intent order. An older completion may update persistence
  evidence for its own revision but can never roll back a newer selected value. The sole
  `CommanderSession.HandleAsync` route enqueues a selector write and returns the pending snapshot
  without holding the UI intent until storage completes. Shutdown awaits the queue. A raw task
  completion callback observes a write fault exactly once, invokes the existing host defect
  observer outside a child task, and completes the queue tail in `finally`; a throwing host observer
  is surfaced through its framework context without stranding the queue. Shutdown linearizes with
  the final accepted tail under the session lock; later selections are rejected before state or I/O
  changes with `InvalidOperationException`, while an intent accepted during shutdown is included in
  the awaited tail. Repeated shutdown is idempotent.
- **The editor changes two persisted launch preferences.** It offers the eight existing
  `ColorScheme` members and the next-launch hidden-item default. Changing the hidden-item default
  never calls `PaneReducer.ApplyHiddenItemVisibility` and therefore never changes either current
  pane. `Ctrl+H` remains the active-pane session toggle and never writes settings.
- **The scheme remains a composition concern.** A selected scheme applies immediately for the
  current session, including after a failed write. The session publishes the typed scheme to the
  existing composition-root mapping, which replaces the one merged scheme dictionary and updates
  the window content's `RequestedTheme`. Persisted text never becomes a resource address and the
  view does not add a theme mechanism.
- **`Ctrl+,` is the sole settings entry.** The canonical `KeyboardIntentMapper` maps the produced
  comma with Control to `OpenSettings` in `FileList` and `NavigationSurface` contexts. Text entry,
  a running operation, and another modal keep precedence. The Windows low-level Control+OEM-comma
  event maps directly because it does not produce a character event on the supported runtime; an
  unmodified raw or produced comma passes through. The settings editor is session-owned modal state: it freezes both
  panes, accepts its typed selector intents, and closes on `Escape`. Because changes save on
  selection, Escape means only “close”; it never restores old settings. The editor explains that
  behavior with localized text.
- **Persistence warning is independent of the operation bar.** A failed write keeps the selected
  session value and exposes a localized persistent warning until a later write succeeds. The
  warning neither changes `OperationActivity` nor creates a second operation bar. A rejected
  startup document is also visible through this settings-warning channel and is never repaired by
  an ordinary change.

The fixed sibling temporary name serializes writers that overlap while one owns the temporary file,
without ambient time or randomness. Same-process selections are additionally serialized by the
settings session. Pre-publish identity and byte comparison detects changes since preflight. The
final path reopen between the last ancestor/document comparison and `Directory.CreateDirectory`,
`CreateNew`, `Move`, `File.Replace`, or cleanup `File.Delete` remains a provider race because these
BCL primitives reopen by path. The adapter does not claim handle-relative traversal,
cross-process compare-and-swap, or merge semantics; another actor may still change a path
immediately after its last revalidation, and another valid writer may win immediately before or
after the atomic installation. Revalidation detects prior changes, while an uncertain cleanup is
reported as residue instead of being claimed as safely removed.

## Rejected alternatives

- Writing on `Ctrl+H`: it would conflate one pane's current visibility with the next launch's
  default and could overwrite the other pane's independent state.
- A read-only store plus a second writer: it would create two settings boundaries and duplicate
  the location, schema, and failure vocabulary.
- Save and Cancel buttons: explicit Save creates an unsaved draft state, while Cancel contradicts
  save-on-change once a write has completed.
- Saving only at shutdown: a crash would discard accepted choices and shutdown would become the
  first mutation observation point.
- Automatically repairing malformed JSON: it would destroy hostile-input evidence and weaken
  SEC-011.
- Putting settings failures in `OperationActivity`: settings persistence and file operations have
  different lifetimes; sharing that status would erase one of them and turn the single operation
  bar into a second settings mechanism.
- Resolving a scheme dictionary in the window or from the identifier text: both bypass the
  exhaustive composition-root mapping required by ADR-0022.

## Consequences

The settings modal is a complete first user-facing entry, so the write boundary is not shipped as
an unreachable API. A failed save is honest: the current colors remain selected, the launch-hidden
choice remains visible in the editor, and the persistent warning says that a restart may use the
older/default value. A malformed document remains in place until a future explicit Reset settings
command owns repair policy.

The approved Canvas handoff is [NeNe Commander UI Preview](https://claude.ai/design/p/ff811404-8e96-4dd0-92c1-320b3002b4b9?file=NeNe+Commander+UI+Preview.dc.html),
with review notes in the same project at `NeNe Commander UI Review.dc.html`. It approves the modal
content, order, auto-save explanation, startup-rejection and save-failure states, independent
persistent warning, and `Ctrl+,` entry. The implementation uses existing semantic tokens. Numeric
style, color, font, and spacing values from the generated mock are not adopted, and its internal
generation label `保存 #1` is not product text.

## Migration and removal

ADR-0022's read-only-store statements and the glossary's “never written” statement are replaced in
this change. There is no compatibility writer, legacy schema, registry fallback, cloud store,
package dependency, or waiver. Issue #72 continues to own `Ctrl+H` as session-only behavior.

## Executable proof

Application tests prove modal freezing, both complete setting changes, current-pane isolation,
ordered pending writes through the sole command route, old-completion non-reversal, success and
failure persistence states, raw host-defect surfacing, and shutdown observation. Infrastructure
tests prove scheduled read preflight, exact serialization, absent move, valid atomic
replace, malformed preservation, denied/locked input, foreign temporary preservation, owned
temporary cleanup and typed residue, observed and unconfirmed directory creation, retry after a
post-creation failure, cancellation before directory mutation, publish completion after late
cancellation, destination identity/content changes, same-length temporary rewrites with restored
metadata, all temporary entry kinds, document symbolic links, reparse ancestors, and parent
replacement before temporary capture, before publish, and after publish under ADV-012. Dangling
ancestor junctions and dangling document or temporary symbolic links are rejected without creating
their targets. Presentation and App-boundary tests
prove the eight choices, localized warning, operation-bar independence, composition-root scheme
replacement, and the `Ctrl+,` / raw-key / plain-comma/context matrix. The security deep review and
final Draft-to-Ready canonical CI remain integration evidence.
