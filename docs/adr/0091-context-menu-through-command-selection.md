# ADR-0091: Share command selection between the palette and context menu

Status: accepted

Date: 2026-11-24

Issue: #531. Depends on the local technical pointer candidate #530 and on #544 for
New > Text file. This is separate from the frozen beta.4 release candidate.

## Context and invariant

Right-click and Shift+F10/Menu must expose the same commands and exact operation
targets. A native selected item is not an accepted application target. Focus can
also differ from an explicitly selected batch. The menu must show that distinction
and never reinterpret the existing copy/move/delete source policy in a view.

The application already has one catalog-backed command selection owner:
CommandPaletteSession captures both pane snapshots, holds a frozen candidate list,
validates an exact expected open state, consumes it once, and returns the intent for
CommanderSession to dispatch. The menu extends this owner, not the execution paths.
Existing name entry, provider validation, FileOperationGateway, confirmation and
refresh remain mandatory. The requested Explorer reveal extends the existing Shell
handoff boundary with an explicitly separate action, not a filesystem mutation.

## One command-selection owner

Add a closed command-surface distinction: searchable palette, item context, and
folder context. CommandPaletteOpen retains its exact left/right/active endpoints
and adds this surface. Construct it from the already captured DualPaneSnapshot,
candidates and surface instead of adding a fifth independent constructor argument.
No additional TransientScopeOwners member, context-menu session or parallel dispatch
pipeline is introduced. Existing public palette entry keeps its searchable surface.

The sole CommandCatalog declares each intent once, its availability and applicable
menu surface/group. Palette and menu derive candidates from that same declaration.
A menu submission must name a candidate actually present in that captured surface;
being in the broader catalog is insufficient. Missing, disabled, stale or already
consumed submissions do nothing. Existing palette cancellation/submission types
continue to carry the exact open state. A delayed Closed callback from an old native
flyout cannot close a replacement surface or request focus over its new owner.

The initial context commands are existing OpenFocused, Rename, Copy, Move, Delete,
CreateDirectory, Refresh and OpenBookmarks, the accepted #544 CreateTextFile, and
explicit RevealFocused/RevealLocation actions. Presentation groups these as focused
item, selected batch, and current folder; New contains Folder and Text file. Catalog
membership is product policy. Search/sort/settings and unadopted clipboard/properties/
Shell-extension proposals are not silently added to this menu. No keyboard chord is
invented for Reveal: the menu and searchable palette provide keyboard access.

## Target acquisition before capture

The pointer request carries exact rendered PaneSnapshot, addressed side, and either
the exact owned visible DirectoryEntry or a declared pane background/header surface.
PaneSession/PaneReducer validate that request and accept selection/focus through the
existing pointer mechanism. CommanderSession alone coordinates that synchronous
transition and the subsequent command capture, without an intervening await.

- Right-click a selected row: activate its pane, focus that row, preserve the
  selected batch and range anchor.
- Right-click an unselected row: activate its pane and perform the existing single
  selection transition, including its anchor, before the menu is captured/rendered.
  This is an explicit visible selection transition, not a different command policy.
- Right-click background/header: activate that pane, preserve its focus/selection/
  anchor, and open folder context. Item and batch operations are absent there.
- Shift+F10/Menu: preserve active side, focus, selection and anchor. Use item context
  when a focus item exists; otherwise use folder context for a listed empty folder.
  When focus is outside a selected batch, the menu explicitly names both targets.
- A loading/launching pane, operation/name/confirmation/conflict owner, other modal,
  address/theme editor, help/window-adjustment/Locations scope or stopping host
  refuses opening without changing active side, focus, selection or range anchor.
- An unlisted/failed-unlisted pane has no admitted directory context. It is refused;
  no inferred path or new refresh/retry behavior is introduced. A retained listed
  pane keeps the same command rules as its existing keyboard route.

Every admitted context acquisition creates an actual new target PaneSnapshot wrapper,
even when its underlying pane model is unchanged. The current-context replacement
route may close an existing context surface only after its captured endpoints still
match and the new qualified acquisition is admitted. It then captures the new state
synchronously. Searchable palette or unrelated input ownership never permits this
replacement. Exact endpoint transitions reject delayed pointer events; exact open-
state submissions/close events reject delayed flyout callbacks. No fake receipt is
inferred from native focus, a row index, path equality or a current selected control.

Opening invalidates the left-button double-tap candidate. Rendering reconciles native
selection from the accepted model. No arbitrary command/event completion returns
focus: dismissal must still own its expected scope, and current text/modal/stopping
ownership is rechecked before the existing pane-focus helper runs.

## Keyboard and framework boundary

Map Shift+F10 and the Menu/Apps key in KeyboardIntentMapper from the FileList context.
The existing F10 command-entry route and file-list Tab pane switching stay intact.
Map the opening key once and consume repeat across the new ContextMenu context.
The pointer ContextRequested route and canonical key route converge on the same
application acquisition/capture; a positionless framework keyboard context event
must not create a second invocation after the mapped key. Actual event ordering is
an explicit host-proof obligation, not inferred from a mapper unit test.

Use the native menu's Up/Down, submenu Left/Right and Enter behavior. Its root/popup
key adapter forwards to the same mapper and repeat owner, not a private key map.
Root dismissal consumes the same physical Escape press across the returned pane.
A submenu Escape first closes that submenu through native behavior, without
cancelling the root scope. Enter/Space invocation is qualified through the existing
initial-activation guard before it can open another surface. Ordinary file commands
cannot leak through while menu input owns the session.

Pointer placement uses the admitted pane-relative point. Keyboard placement uses
the realized focused row, with the pane as geometry fallback; geometry never chooses
an execution target. ContextCanceled/light dismiss close only the matching surface.
The view is an adapter: it cannot decide selection policy, candidates, provider
support, cancellation semantics or operation destinations.

## Explorer reveal through the existing Shell boundary

Extend IFileLauncher/WindowsShellFileLauncher with a RevealAsync action returning
FileLaunchOutcome. Activation keeps its existing LaunchAsync semantics. PaneSession
uses one external-handoff lifecycle with a closed activation/reveal purpose attached
to its launching/cancelled/failed state; it must not add a competing busy flag or
forget any navigation/refresh/operation freeze. Purpose-qualified presentation uses
reveal wording without changing existing activation messages. Both RevealFocused
and RevealLocation derive their exact target from the admitted pane state, never
from a view-supplied arbitrary path. Neither changes pane contents or history.

Reveal executes through WindowsLocalIoExecutionBoundary.ExecuteShellAsync and its
existing WindowsShellIoScheduler. That scheduler owns STA COM initialization and
teardown. No new scheduler, global COM state, unawaited worker, command line or
explorer.exe path search is introduced. Reuse the existing Shell path-admission
policy for Windows local, UNC and Windows-compatible WSL names; do not widen the
UNC/WSL limits or change file activation's parent requirement. RevealLocation may
name a provider root because the Shell owns the selected item's parent namespace.

A narrowly owned Infrastructure helper calls SHParseDisplayName for the exact
canonical path with null bind context and no requested attributes, then calls
SHOpenFolderAndSelectItems with that absolute PIDL, cidl0, null child array and
flags0. cidl0 means select that item in its Shell parent, including a folder; it
does not mean open the folder's contents. The menu therefore says reveal/show this
folder, never promises navigation inside it. No edit-mode or desktop-selection flag,
Shell-extension menu, custom verb, path suffix or string-interpolated argument exists.

The PIDL is owned and freed exactly once on every native return path, including
cancellation or a failed invocation. Observe cancellation before scheduling, before
parse, and before the final Shell handoff. An accepted final handoff stays accepted
if cancellation arrives afterwards. Normalize expected HRESULT failures to the
existing closed failure model; preserve defects rather than swallowing all exceptions.
No retry, fallback activation or permission elevation is permitted. Native parsing
and its memory lifetime are testable through a bounded adapter seam without opening
Explorer in ordinary automated tests.

This is an explicit user-requested path handoff, as in ADR-0046. A pane snapshot is
not a filesystem identity lock. No existence/reparse/identity preflight can bind
Explorer's later resolution, so none is invented. The Shell may select a replacement
or reject a missing item; success records handoff acceptance, not a selected-window
receipt, process lifetime or stable target identity. No source-deletion capability
or FileOperationGateway mutation is added by reveal.

## Design handoff and engineering decisions

The background /design invocation only reports access management in the installed
CLI. A separate tools-disabled ordinary consultation of public #531/#544 requirements
returns target-group hierarchy, localized wording and accessibility advice. No source
files are transmitted, no access consent is changed and no Canvas URL is claimed.
The exact brief/invocation/raw advice is retained in the task evidence directory.

Adopt explicit focus, selected-batch and folder targeting and existing semantic
resources. Do not automatically adopt advice to preserve a batch when right-clicking
an unselected row, expose batch mutations on blank space, activate a loading pane,
change Refresh behavior or add an unsupported read-only/provider reason. The selected
row transitions above are visible model changes before capture. All disabled reasons
must come from actual canonical availability; a view cannot infer filesystem ACLs.

A compact native MenuFlyout is the proposed adapter. Its actual support for non-
interactive group headings, full accessible target names, disabled reasons, native
submenu Escape and long-name layout must be demonstrated. If stock headings cannot
express that hierarchy, use explicit per-command target wording or native grouped
submenus; do not claim an inaccessible disabled command is a semantic heading, invent
final token values or introduce a custom keyboard navigation table to fit a mockup.
This visual choice remains a bounded design-prototype question before implementation
acceptance, not permission to hide target distinctions. Three locale resource sets,
canonical shortcut hints, semantic/high-contrast resources and long-name access are
required. Physical/native/visible acceptance remains separate from technical review.

## Migration, dependencies and alternatives

Before production editing, independently review this shared-owner/selection/Shell
contract. Upon technical acceptance update CMD-009, the mechanism registry, keyboard,
filesystem/Shell, security/threat and glossary records in the same feature. Update
all launcher fakes, pane activity consumers and command-selection presenters together;
there is no compatibility owner, nullable purpose default or fallback execution path.
Keep existing palette behavior unless this ADR explicitly extends it. Where class
budgets are reached, put scope snapshot construction in its existing owner and isolate
a named existing responsibility; do not raise limits or add a catch-all utility.

The #544 core is locally implemented and has selected native Windows/WSL evidence,
but remains incomplete without mouse entry and full/deep acceptance. Integrate that
exact dependency into the feature before claiming New > Text file works. Preserve
its exclusive zero-byte/no-overwrite/name-entry/provider/cancellation contracts and
its unfinished acceptance. The local #530 dependency remains separate from visible
acceptance and publication. No part of this feature joins the frozen beta.4 payload.

Rejected: view-owned target/selection, native SelectedItem as authority, a second
context-menu session/command catalog/dispatcher, asynchronous select-then-open input,
path/index-based stale-row recovery, borrowed keyboard maps, direct code-behind Shell
calls, explorer.exe command-line construction, unconditionally embedding shell
extensions, blind clipboard/properties scope and calling a real Explorer handoff as
an unattended shared-desktop test.

## Required proof and acceptance

- Causal owner cases: both panes; selected/unselected/context row; keyboard focus
  outside a marked batch; blank/header; empty/unlisted/retained-failure; range-anchor
  preservation/change; immutable old snapshots; every busy/modal/stopping boundary;
  stale/foreign/recycled row; late menu cancellation/submission; replacement menu;
  candidate absent from this surface; disabled candidate; one accepted dispatch.
- Existing palette consumers: filtering, candidate targets, captured opposite pane,
  availability, duplicate/reentrant admission, focus return, Enter/Escape repeat,
  and global/transfer command explanations. Context menus use the same validation.
- Existing command effects through a menu: Open, Rename, Copy/Move/Delete source
  choice, normal confirmation, CreateDirectory and CreateTextFile, refresh/focus,
  cancellation and provider refusal. Do not bypass a name/confirmation surface or
  count a fake only as provider proof. New-file WSL/native evidence retains its own
  input mapping and must be re-established if an affected implementation changes.
- Shell adapter: exact target, no verb/arguments/fallback, existing provider/name/
  length policy, unavailable parent namespace, normalized failures, one STA operation,
  cancellation before/after parse/final call, non-null PIDL ownership on every path,
  no double free and accepted-late-cancellation semantics. Existing LaunchAsync tests
  remain direct consumers. Native PIDL parse/free on an owned path can run separately
  from a real display call; the latter is a separate consented/environmental tier.
- Keyboard and actual controls: initial/repeated Shift+F10 and Menu; no duplicate
  framework ContextRequested; native arrow/submenu/Enter/Escape ordering; old popup
  close after new owner; pointer admission/reconciliation; root versus popup routing;
  IME/text ownership; shutdown; three locales/long names/narrow sizes/DPI; UIA names,
  disabled reasons, high-contrast and physical input explicitly separated.

Run affected builds/tests, branch coverage and source/record conformance first. The
combined completed feature changes shared command-selection and Shell contracts and
integrates #544's new mutation boundary, so one justified canonical deep review is
required before technical completion when the full candidate is ready. Reuse its
canonical result rather than repeat quality. Documentation alone starts no product
build/test/CI. Keep every failed attempt and unrun environmental tier explicit.

## Primary framework references

- [SHOpenFolderAndSelectItems](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shopenfolderandselectitems): exact PIDL, cidl0 selection, flags and COM initialization.
- [SHParseDisplayName](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shparsedisplayname): parse ownership, attributes and background execution.
- [WinUI ContextRequested](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.contextrequested): explicit flyout presentation/cancellation and routed events. Documentation is not evidence of this host's physical event ordering.

## Independent-review clarifications — 12:16 JST

The first independent read-only review accepts the owner/PIDL direction but keeps
this ADR proposed pending three clarifications. These clauses refine the proposal;
no implementation or physical event-order result is implied.

1. **Exactly one visible surface.** CommandPaletteView renders search UI only for
   the searchable-palette surface; the context adapter renders only item/folder
   context. Each remembers its own exact rendered state and only hides its own
   surface. A transition to another surface cannot run the departing view's pane-
   focus return. Qualified cancellation/closed-focusing from the current owner is
   the sole dismissal focus receipt. Caption lookup represents no declared shortcut
   as an empty hint rather than `bindings.First(...)` failure or an invented chord.
   Preserve every currently declared hint and prove unbound Reveal in both surfaces.
2. **Framework/key event admission and finalization.** A list-region
   ContextRequested with no position is handled and ignored: Shift+F10/Menu have
   already gone through the canonical mapper. A positioned request is qualified
   against its rendered pane/entry. Do not install ContextFlyout as a competing
   automatic opener. Native Closed signals a pending cancellation for that exact
   open state, and queues one finalizer on the owning dispatcher after the current
   native invocation turn. Click/Invoke submission first consumes the same state;
   the later finalizer cannot cancel a consumed/replaced state. This handles Click
   then Closed and Closed then Click within that invocation turn. A genuinely late
   invocation after finalized dismissal is refused. Prove both orders at the adapter
   and observe the actual control's native invocation order before accepting the host.
   Stopping closes admission to queued finalizers and never restores pane focus.
3. **Existing cancellation semantics.** Reveal inherits the current PaneLaunching
   freeze. Escape is not a new reveal cancellation route. Only the existing supplied
   cancellation source, including normal host stop, is observed at the named phase
   boundaries. A running synchronous parse or Shell call cannot be interrupted;
   closing may await its return. No watchdog, abandon state, UI-displayed cancel
   promise, forced process termination or independent cancellation owner is added.

For native menu input, the canonical mapper arms the existing cross-context repeat
owner on an initial Escape/Enter/Space and lets the native control handle that first
press. Repeated events from that press are consumed before they can close a parent
submenu or act on a newly opened surface. Native root Closed carries qualified
cancellation; submenu closure alone does not cancel the root scope. The popup adapter
uses the same mapper as the root, without a private key table. Existing shortcut
hints remain canonical; any shortcut supported while the menu is open must submit a
qualified captured candidate, never dispatch a bare file intent around the scope.
The exact popup/root wiring and physical event order remain required host evidence.

The initial neutral view prototype will use explicit target wording per command and
native separators/New submenu, rather than treating disabled MenuFlyoutItem rows as
non-interactive accessible group headings. Focused operations name the focused item;
batch operations name the captured selected count or focused fallback; folder actions
name the captured folder. Full target text remains in accessible names/help/tooltips.
Stock theme resources supply the menu; no final visual constants are chosen. This is
a bounded adaptation of the advice's target-group concept, not approval of final
visual styling. Long-label fit, disabled reasons, native submenu behavior and the
actual-control ordering above remain acceptance conditions for that prototype.

## Native prototype clarification — single routed input entry

The isolated pinned WinUI prototype observes that submenu PreviewKeyDown already
routes through the owning window root. Registering the same repeat owner at both
root and presenter processes one initial Escape twice; the second call suppresses
that initial event. This is disproved by attempt2 and corrected by root-only attempt3.

Use the existing WindowFrame preview route as the sole input registration for the
root menu and its submenus. The context-menu adapter must not register another
preview handler on MenuFlyoutPresenter/items. Native navigation keeps its first
event after canonical interpretation/initial-activation guard; repeat is consumed
at that one root entry. If the final product host differs from the observed rooted
popup path, resolve that host boundary before acceptance, without accepting duplicate
mapper calls or treating a second callback as a physical repeat.

Attempt3 observes initial Escape collapsing only the submenu, repeated Escape
preserving the root, and a fresh Escape after key-up dismissing root. This uses
explicit messages to the owned focus HWND on a non-input desktop and a diagnostic
guard; it is framework evidence, not proof of physical input, the final product
mapper, modifier state, IME, every focus path, or visible acceptance. Root/submenu
UIA invocation records Click before Closed/finalization. Both callback orders still
need deterministic application/adapter tests in the final implementation.

## Technical acceptance — November24 12:38 JST

Independent read-only review finds no mandatory mechanism correction after the
root-only clarification. The design owner accepts this technical contract under
hide's active continuation instruction. This is permission to implement the shared
owner and adapter design, not implementation, physical/visual or release acceptance.

The reviewer reconciles the six prototype owner submission counts1,1,0,0,1,1 and
seven source hashes plus runtime/launch receipts. Attempt4 sends Enter KeyUp before
its synthetic repeat-bit event on the next menu: it proves suppression of that
repeat-marked event, not an uninterrupted physical press across an owner transition.
The final product must prove actual held Escape/Enter/Space across pane/modal/menu
returns using the existing mapper/repeat owner and a qualified isolated native tier.
Retain both callback-order tests, late/stale/replaced/finalized/stopping ownership,
context-key/framework duplicate prevention, modifier/IME/focus cases and complete
three-locale/target/accessibility/visual acceptance as implementation obligations.

## Host callback qualification — November24 independent implementation review

A positioned request returns a cancellation callback only for the native invocation
created synchronously by that accepted request. The callback captures that exact
invocation and menu. It does not look up and hide a later current menu. Pane adapters
retain the receipt with its request surface, reject a foreign sender and discard it
on pointer press/release. A rejected request receives no receipt. ContextCanceled
cannot be used as an unqualified window-wide dismissal. Keyboard and other-pane
replacement, consumed/stopped receipts and repeated old callbacks remain no-ops.

Dismissal focus uses only CommandPaletteClosed.FileListFocusSide supplied by the
Application cancellation validation, with the existing idle/text/modal/stopping guard.
Any closed state without that receipt does not itself authorize pane focus. Successful
command execution keeps its existing destination-owner rendering and native dismissal;
it is not reinterpreted as a cancellation receipt.

[ContextCanceled](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.contextcanceled?view=windows-app-sdk-1.8)
describes manipulation after a context request and before PointerReleased. It carries
no request ID. This gesture lifecycle qualifies retention; the exact returned callback
qualifies the menu. Actual physical touch/drag ordering remains separate from the
controlled adapter/native tests recorded in the quality report.
