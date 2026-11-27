# Command Model

Status: normative

All behavior follows one pipeline:

```text
WinUI input
  -> input-specific mapper
  -> typed intent
  -> application command/query
  -> validation and capability decision
  -> gateway or reducer
  -> typed outcome and immutable state
  -> presentation rendering
```

There is no direct UI-to-filesystem route.

### CMD-001 — Mutations use `FileOperationGateway`

- Status: **active**
- Enforcement: dependency boundary and forbidden-API scan.

Every user-file mutation is represented by one typed operation request and executed by `FileOperationGateway`. The gateway owns preflight validation, conflict policy, progress, cancellation, provider dispatch, error normalization, and outcome creation. The sole settings document is a separate application-owned persistence concern and is written only as one complete typed value through `ISettingsStore` under ADR-0040.

Empty text-file creation (ADR-0085) uses the existing name-entry state and
`CreateTextFileRequest` to derive exactly one direct child of the captured active
location. The gateway requests `IdentityOnly`, admits one `CreateEmptyFileAsync`
step and, only on success, publishes `FileCreated(target)` with progress1/1. An
admitted successful step remains successful if cancellation arrives during it.
No overwrite, extension change, suffix retry, implicit opening or rollback deletion
is added. The existing refresh focuses the created target while retaining the
completed effect if a subsequent read fails.

### CMD-002 — Pane transitions use `PaneReducer`

- Status: **active**
- Enforcement: state-surface scan and reducer tests.

Navigation, focus, selection, sorting, hidden-item visibility, name filtering, history, and refresh state are transitions applied only by `PaneReducer` to an immutable `PaneState`. Single-step next/previous focus cycles through the current visible projected order; paging and explicit first/last targeting retain their existing boundaries. Ordinary movement preserves selection and uses the exact path owned by a visible entry; explicit keyboard range movement replaces the inclusive anchored interval and always clamps (ADR-0093). The active side is held only by `DualPaneSession` and changes on `ActivateOtherPane` or an admitted exact-owner pointer interaction (ADR-0090). `PaneSession` resolves `OpenFocused` once: a directory uses the existing navigation path, while every file uses the sole `IFileLauncher` handoff (ADR-0065). Its existing Windows Shell adapter owns path admission: local, UNC and compatible WSL paths are supported; unsupported variants, roots, excessive UNC/WSL lengths and incompatible WSL names are refused before scheduling, and pane content remains unchanged by every launch outcome.

`OpenCurrentLocationInOtherPane` (ADR-0084) is the explicit cross-pane navigation intent.
`DualPaneSession` captures the active pane's currently listed location and opposite side,
then calls its existing `NavigateAsync` for that side. The source snapshot and active side
are preserved; destination ordering, focus, selection and history remain owned by its
`PaneSession`/`PaneReducer`. The derived `CanOpenCurrentLocationInOtherPane` predicate admits
only listed-source, idle-read/launch and non-modal-operation snapshots. Equal locations,
using `FileSystemPathIdentityComparer`, are no-ops. No selected-file copy or handoff occurs.
A successful refresh preserves the current focus identity when it remains. If it was removed,
focus moves to the next identity from the previous visible order that is still visible, then
backward to the previous survivor. If none survives, the first new visible item is used; an empty
visible set has no focus. This applies equally to explicit refresh and the existing post-operation
refresh, including move/deletion and partial or cancelled outcomes. The new listing owns the
chosen path. Explicit creation/rename targets retain their own preferred-focus policy, and a
normal navigation still starts at its declared target or first item.

Pointer row/header/background requests carry the explicit side and exact current
rendered PaneSnapshot; row actions additionally require reference membership of
an owned visible DirectoryEntry. Stale/foreign/recycled input cannot change active
side, selection or effects. CommanderSession keeps scope precedence; DualPaneSession
refuses pointer interaction during pending/running operations or either pane's
read/launch before activating the explicit side. PaneReducer alone commits focus,
selection and the visible owned selection anchor. Opening reuses OpenFocused.
Native ListView selection is only a projection and is reconciled after rejection.

ADR-0093 extends these same owners with visible select-all, inversion, explicit
clear and six keyboard range intents. PaneSelection calculates values without
committing state. PaneReducer alone applies them to owned visible paths and the
same pointer/keyboard anchor. Bulk operations clear the anchor and preserve focus;
ranges clamp and replace the current interval. All admission remains in the existing
CommanderSession / DualPaneSession / PaneSession route, and batch commands still
capture their targets through the existing FileOperationGateway path.
The per-pane parent button adds a NavigateParent surface action with the same exact
owner. Absent content and provider roots reject before activation; admitted input
uses the existing PaneSession NavigateParent command for read/history/origin focus.
No UI-level activate-then-navigate sequence or provider-parent implementation is added.
The accepted gesture, anchor and first-tap receipt rules are in ADR-0090.
ADR-0094 extends the same projection with PaneState.NameFilter. The immutable query
compiles literal OrdinalIgnoreCase whitespace-separated AND terms; filtering touches
only the loaded eligible listing. PaneReducer repairs focus, removes invisible marks
and anchors, and never restores them when clearing. Refresh/same-location navigation
retain the query; a true provider-identity location change clears it. Query-only updates
retain cancelled/failed/abandoned read activity and do not call a provider. File commands
continue to consume the same visible owned selection and existing gateway.

### CMD-003 — UI emits intents, not decisions

- Status: **active**
- Enforcement: project boundary and presentation tests.

Code-behind may translate a framework event into an intent and forward it. It may not choose operation semantics, destination policy, overwrite behavior, provider behavior, or domain state transitions.

### CMD-004 — Preflight is atomic and complete

- Status: **active**
- Enforcement: command tests.

A mutating command validates sources, destination, collisions, recursion, provider capabilities, and destructive policy before beginning work. A batch is not partially started because later inputs failed validation.

### CMD-005 — Outcomes are exhaustive

- Status: **active**
- Enforcement: closed result type and exhaustive tests.

Every command returns the canonical outcome model. Boolean success flags, magic strings, null-as-failure, provider exceptions leaking to presentation, and unrelated result types are prohibited.

### CMD-006 — Cancellation has one meaning

- Status: **active**
- Enforcement: cancellation tests.

Cancellation means no new unit of work starts after observation. Already completed filesystem effects are reported explicitly. Cancellation is an outcome, not an error dialog and not an unobserved exception. Abandoning a pane read with `Escape` (ADR-0058) is a distinct pane state, not a cancellation outcome: the pane keeps its content, the read's token is cancelled, and whatever the provider step returns later is discarded.

For the explicit opposite-pane command (ADR-0084), an active read retains first priority.
When it is not loading, Escape may reach the passive read only while that exact captured
`PaneLoading` instance is still current. It uses the same `PaneRead.Abandon`, releases the
host waiter immediately and retains both pane contents. A newer or unrelated passive read
is never cancelled by that capture. The capture owns no token/read and is cleared in
`finally` only if it is still the command's own capture. File-operation/modal precedence and
`AsyncWorkOwner`'s Escape-only interrupt admission remain unchanged.

### CMD-007 — Queries do not mutate

- Status: **active**
- Enforcement: interface separation and tests.

Directory enumeration, capability discovery, metadata reads, and settings reads are queries. A query may populate an owned cache only behind its declared boundary; it may not alter pane or operation state as a hidden side effect.

### CMD-008 — Adapters translate; they do not reinterpret

- Status: **active**
- Enforcement: adapter contract tests.

Provider adapters translate canonical requests and platform responses. Product policy belongs in application commands; path invariants belong in domain types; UI wording belongs in presentation resources.

### CMD-009 — Keyboard input uses `KeyboardIntentMapper`

- Status: **active**
- Enforcement: presentation scan and keyboard contract tests.

All non-text keyboard shortcuts are mapped by one `KeyboardIntentMapper`. Pages, controls, and view models do not maintain private key maps. Context such as text-entry focus is an explicit mapper input. ADR-0090 adds the Presentation-only CommandEntry context and closed F10 focus action to this mapper. Native button Tab/Enter/Space remain native; no second keyboard table or Application modal is introduced. Existing modal precedence, repeat ownership and focus-return guards apply.

## Canonical mechanism registry

| Concern | Only approved mechanism |
|---|---|
| filesystem path parsing | `FileSystemPath.Parse` |
| filesystem mutations | `FileOperationGateway` |
| Windows-side file-operation provider routing | `ProviderFileOperationPort` |
| current drive type and admission to local storage operations | `WindowsDriveBoundary`, queried inside existing Windows I/O workers and reused at effect revalidations (ADR-0072) |
| Windows local file-operation provider | `WindowsLocalFileOperationAdapter` behind the provider router |
| WSL mutation and transfer provider | `WslFileOperationAdapter` behind the provider router |
| composite-move verification and conditional source deletion | the existing `FileOperationGateway` pipeline carrying immutable descendant evidence from the shared tree verifier to the source provider (ADR-0069) |
| Windows local atomic-move capability | provider query using mounted-volume identity, consumed by `FileOperationGateway` |
| directory reads | `IDirectoryReadPort` boundary with `DirectoryListing` ordering |
| visible entry order of a pane | `EntryOrdering` over the `PaneSortOrder` held by `PaneState`; accepted changes are persisted by the existing settings queue for that pane's next launch (ADR-0067) |
| pane navigation, history reads, and intent routing | `PaneSession` |
| active side, intent routing between panes, and starting file operations | `DualPaneSession` |
| pane projection | `PaneListingPresenter` over `PaneSnapshot` |
| observed network-drive indication | optional read-success `DriveKind`, retained by `PaneContentListed` and projected through `PanePresentation` / `PaneStatusLine` (ADR-0072) |
| pane state | `PaneReducer` |
| keyboard mapping | `KeyboardIntentMapper` |
| qualified pane pointer mapping and accepted-gesture receipts | `PanePointerMapper`, dispatched through the existing CommanderSession/DualPaneSession/PaneSession route (ADR-0090) |
| view-model notifications and commands | CommunityToolkit.Mvvm source generators |
| expected operation results | canonical closed `OperationOutcome` model |
| settings persistence and atomic complete-document write | `ISettingsStore` boundary coordinated by `SettingsSession` |
| immediate interface language | `InterfaceLanguageSelection` through the existing settings modal/queue; the window-owned `ViewTextResources` applies the current choice to its sole explicit MRT context and existing localized controls, retaining the original System qualifier (ADR-0086, ADR-0088) |
| settings modal ownership and intent routing | `CommanderSession` over `DualPaneSession` and `SettingsSession` |
| palette/context selection state, admission, and qualified validation | `CommandPaletteSession` with the surface/group metadata in `CommandCatalog` (ADR-0091) |
| synchronous context acquisition and dispatch of a validated palette/context intent | `CommanderSession`; `CommandContextRequest` only derives the qualified target/surface (ADR-0091) |
| address-editor state, admission, and submission validation | `AddressEditorSession` |
| applied pane name filter and visible membership | `PaneState.NameFilter` and `PaneReducer.ApplyNameFilter`, over the existing loaded listing (ADR-0094) |
| native name-filter editing and qualified completion | `PaneFilterSession`; `CommanderSession` applies accepted queries through the existing pane route (ADR-0094) |
| bookmark navigation coordination | `CommanderBookmarkCommands`, composed with the same panes/settings and invoked only by `CommanderSession` (ADR-0094) |
| localized command selection rendering | `CommandPalettePresenter` with `CommandPaletteViewState`; `CommandMenuPresenter` groups those same rows and `CommandMenuView` adapts native invocation callbacks (ADR-0091) |
| startup and final window placement | `WindowRestorePlanner` decides one corrected normal rectangle; the existing `AppWindowPlacementAdapter` moves to the target display before applying it and tracks its own window. `CommanderSession.StopAsync` queues one changed final value and drains `SettingsSession`; stopping session/operation callbacks cannot rebuild content or refocus lists (ADR-0089) |
| window placement translation | `AppWindowPlacementAdapter` in the App host; it reads and applies placement and decides nothing |
| framework asynchronous work | `AsyncWorkOwner`; Stop closes admission before cancelling and awaiting admitted work (ADR-0030, ADR-0071) |
| normal final-window shutdown | `AppWindowCloseAdapter` translates system close to one `WindowShutdownOwner` drain, then the composition root closes the window (ADR-0071) |
| window adjustment decisions | `WindowAdjustmentSession` with `WindowAdjustmentPlanner` |
| Windows drive enumeration | `IDriveCatalog` implemented only by `WindowsDriveCatalog` through the Windows local I/O execution boundary |
| Locations picker state, admission, focus, and qualified validation | `LocationsSession` over `IDriveCatalog` and `IWslDistributionCatalog` |
| window adjustment mode synchronous input route | `CommanderSession.AdjustWindow` and `CommanderSession.LeaveWindowAdjustment` |
| color scheme | sole fixed ColorScheme catalog and constant resource projection, copied into the existing merged brushes by the composition root (ADR-0087); transient theme search never applies or saves |
| file activation and Explorer reveal | `PaneSession` through `IFileLauncher` and one purpose-qualified `PaneLaunching` lifecycle; `WindowsShellFileLauncher` owns admission; activation retains `ExecuteAsync`, while reveal uses the existing STA `ExecuteShellAsync` boundary (ADR-0091) |
| time | `IClock` boundary |
| identifiers | `IIdentifierSource` boundary, composed with `WindowsIdentifierSource`; settings writes and admitted transfer preparations request one candidate token per attempt (ADR-0070, ADR-0095) |
| object construction | `NeNeCommander.App` composition root |

A new row requires an accepted ADR. Replacing a row is one migration: the old mechanism and compatibility path are removed in the same change.

## Context surfaces and native invocation (ADR-0091)

The palette, item context and folder context are closed CommandSurface cases over
one CommandPaletteSession, CommandCatalog and CommanderSession dispatch path.
CommandContextRequest derives a qualified pane interaction and surface without
effects. CommanderSession validates current ownership, synchronously applies that
interaction through the existing pane reducer, and captures the actual new endpoints
before any await. Native row focus, path equality and native SelectedItem cannot
substitute for the exact rendered snapshot and owned entry.

CommandPaletteView renders only Palette; CommandMenuView renders the two context
surfaces. CommandMenuPresenter groups the same canonical candidates by explicit
focused item, selected batch/focus fallback, New destination and current folder.
The adapter's CommandMenuInvocation only qualifies native callbacks: Closed queues
one dispatcher finalizer, a same-turn Click consumes first, and detached/stopping
callbacks cannot dispatch. Application revalidates exact open state and membership
before every submission/cancellation. No menu-local dispatcher or state owner exists.

Shift+F10 and Apps map from FileList to OpenContextMenu through the sole mapper.
Positionless ContextRequested is handled/ignored; positioned events carry exact
rendered ownership. Native navigation stays native in ContextMenu, while the existing
KeyboardRepeatGuard retains Enter/Space/Escape and opener presses across owner changes.
Only the existing WindowFrame preview/character route calls that mapper; no item or
popup adds a private key table. Cancellation uses the existing guarded focus helper.

## Native name-filter scope (ADR-0094)

PaneFilterSession owns only Closed/Editing and exact-state admission. PaneScopeOwners
and PaneScopeSnapshot group address, Locations and filter composition without another
registry or lifetime. CommanderSession keeps all dispatch and precedence; bookmark
navigation coordination is one cohesive dependency with the original latch and route.

Text callbacks carry the exact editor. Commit/Blur also carry native committed text
so a deferred TextChanged cannot lose the last edit. Exact owner, active side, exact
source pane and ordinary admission precede reducer application. Stale-pane Blur retires
without applying; stale owners do nothing; Cancel ignores text and clears. The native
adapter preserves pending text during ordinary render and excludes IME intermediates.
Enter/Cancel return file-list focus through an exact current closed receipt; Blur has
no focus effect. An external qualified entry finishes that exact editor before its
original request and never rebinds a now-stale request to a newer pane snapshot.

## Exact conflict-modal ownership

ConflictDecisionSubmission carries the exact OperationAwaitingConflict displayed by
ActiveConflictModal. DualPaneSession checks reference identity before resuming that
continuation, including Cancel button requests. Value-equal clones, previous conflict
owners and completed submissions cannot decide or cancel a later operation. Keyboard
Escape continues through the existing current-operation interrupt route.

The presenter reuses a modal only for that exact operation. ConflictModalView loads
the existing XAML template into fresh controls for a new owner, so a delayed gesture
or UIAutomation callback retains its old owner and checkbox. Native forwarding requires
the current owner, child and completed render. Native reentry cannot restore old
readiness. Same-owner rendering retains focus and the selected scope; a new owner
starts on Cancel with current-conflict scope. This preparation adds no Replace/Merge
provider capability and does not change ADR-0039's supported transfer decisions.

## Accepted content-overwrite design, activation pending

ADR-0095 extends this same conflict/preflight/CopyAsync/verification route with an
exact-target OverwriteContents choice. A source/target identity alias is refused;
known multiple links and named streams are unavailable. Only the already displayed
eligible targets can share explicit overwrite-all consent. Preparation artifacts and
uncertain target effects must remain in the one typed provider/outcome receipt on
success, failure and cancellation; after an artifact exists, an effects-empty
AwaitingConflict cannot discard it. Move retains the existing verification and
source-provider deletion path with stream/attribute admission added to its receipt.
This is an implementation contract, not an enabled command or product-proof claim.


The internal verified-file prerequisite now requires a closed FileVerificationRequirements
value on every VerifiedCopyEntry.File. Ordinary producers explicitly retain DefaultStream;
OverwriteContents requests the stricter native observations at the shared verifier and
final source-deletion admission. This uses the same VerifiedCopyReceipt and deletion
route. The November 27 internal provider integration produces strict overwrite evidence
and the gateway rejects downgraded requirements, directories or substituted objects
before deletion. Resumed preflight preserves the exact original target and both the
gateway and provider refuse atomic move. The initial production preflight still supplies
explicit Unavailable(Unconfirmed). The unexposed consent/result view and internal
qualification have focused proof; production connection and full destructive
acceptance remain pending after the recorded automatic-approval rejection.
