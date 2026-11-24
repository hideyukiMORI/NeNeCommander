# ADR-0090: Qualify pointer pane interaction through the existing owners

Status: accepted (technical mechanism; implementation and visible acceptance remain pending)

Date: 2026-11-24

Issue: #530. Related context-menu work #531 remains separate.

## Context

The requested mouse entry points must identify the same operation target as the
keyboard. The current file ListView can change its native selected item, but the
pane view only projects Application focus into that control; no file-row pointer
or double-click input updates the Application owner. A displayed control selection
therefore cannot be treated as an accepted command target. Inactive-pane input and
stale/recycled rows also need explicit ownership.

CMD-002 currently says only ActivateOtherPane changes the active side, and the
Issue523 keyboard repair deliberately preserves it after blank-space reactivation.
Issue530 asks an intentional click to activate its target pane. This ADR
extends those two statements for admitted pointer interaction only; window
reactivation by itself retains its current behavior. PaneReducer, DualPaneSession,
CommanderSession and the existing gateway/navigation/launch paths remain owners.

## Chosen mechanism

One Presentation pointer mapper translates framework left-button/modifier/gesture
facts into a qualified UserIntent. App forwards facts and applies the resulting
presentation; it chooses no selection or operation semantics. No global input,
clock, synthetic click, native platform dependency or separate file operation is
introduced. Right/middle buttons, drag/drop and context menus are not this route.

A row request carries the pane side, the exact rendered PaneSnapshot, its owned
entry, and a closed action. Blank/header requests carry the exact rendered PaneSnapshot
and a closed action. Raw indexes and path text are not identities. Application
requires the exact current snapshot and actual visible entry membership before
changing either pane. A stale owner, foreign entry, hidden row or replaced listing
performs no transition or read/launch. Equal path text does not substitute for owner
identity. Blank/header activation admits empty/unlisted panes and terminal read/launch
outcomes (Idle, ReadCancelled, ReadAbandoned, ReadFailed, LaunchCancelled,
LaunchFailed). It refuses Loading/Launching, rather than requiring reference
identity with Idle. With absent content, activation changes the active side and
replaces the target snapshot wrapper while preserving its content and activity;
with listed terminal content, only an admitted selection transition updates that
content and the existing failure indication is retained. The asynchronous work
owner retains single admission.

A blank target means the file-list background, not every non-row descendant.
ScrollBar, Thumb, scroll buttons and other interactive descendants are excluded.
A header target means its passive background/path/number text, excluding any
independent interactive control. The existing path PointerPressed handler is
removed as an independent behavior and joins this same qualified header route.

CommanderSession's existing scope precedence refuses background pane input while
any editor/modal/palette/help/window-adjustment/Locations scope owns it. Running or
pending file operations and either pane's read/launch freeze these pointer actions.
No click closes a modal, cancels work, replaces a draft or changes a frozen source.
After admission, DualPaneSession activates the explicit side and passes the action
to its existing PaneSession/PaneReducer. No other component writes active side.

### Focus and explicit selection

Focus remains distinct from explicit selection (KBD-004). The pointer
contract is:

| Gesture | Accepted result |
|---|---|
| Plain left click on a row | Activate that pane; focus that exact entry; replace explicit selection with that one entry; set the range anchor. |
| Ctrl+left click | Activate/focus that entry; toggle it in the existing explicit selection; set the anchor. |
| Shift+left click | Focus the target and replace selection with the inclusive range in current visible order. |
| Ctrl+Shift+left click | Focus the target and add that inclusive range, without duplicates. |
| Plain blank-list click | Activate that pane; retain focus; clear explicit selection and the anchor. |
| Plain header click | Activate that pane while preserving focus/selection; do not open the path editor. |
| Modified blank/header click | No change. |
| Unmodified double click on a row | Open that one entry through existing OpenFocused after qualified focus; no copy/move/delete. |

An absent anchor uses the focus immediately before the first range gesture, or the
target when no focus exists. That first range transition commits this fallback
anchor into PaneState; later range gestures retain it (A to Shift+B to Shift+C
continues A through C). The anchor is a path instance owned by a visible entry,
never an index. Plain/toggle pointer selection and keyboard Space establish it. Ordinary
keyboard movement preserves it and selection. Escape clears it with selection.
Sorting preserves it by identity; hiding/refresh rebind it with the existing
provider-aware FileSystemPathIdentityComparer to the new visible entry's owned
path when present, otherwise remove it. Successful location navigation clears it.
All changes belong to PaneReducer. An internal selection calculation may be split
by responsibility to respect complexity ceilings, but it cannot become a public
second reducer or directly mutate a session.

The OS/framework decides double-click timing. Use Tapped for the first selection
and DoubleTapped for opening: WinUI reports Tapped for the first tap and does not
report an additional Tapped for the second tap of a DoubleTapped sequence. Each
Tapped first invalidates any old candidate. Only an unmodified row request admitted
by AsyncWorkOwner and the Application route can establish a new candidate. The
single route returns its actual Application snapshot to the Presentation gesture
owner; native SelectedItem is never evidence of acceptance. A row selection is
synchronous and every admitted row selection creates a distinct target PaneSnapshot.
After AsyncWorkOwner admits the request, capture the actual Application target
snapshot immediately before calling HandleAsync, without an intervening await.
The owner confirms that the returned target snapshot changed from this actual
before-call snapshot, its active side is the requested side, and its listing and
focused entry are the same owned objects. Comparing only against the request's
rendered/expected snapshot is forbidden: that request may already be stale while
the actual state still focuses the same entry. Rejected work cannot satisfy the
actual before/after transition receipt. No selection policy is recalculated in
App or Presentation to infer success. The single synchronous row transition makes
this observation bounded; external/open actions cannot establish a first-tap receipt.

Retain side, DirectoryListing and DirectoryEntry reference identities after this
accepted first tap. Do not retain PaneRow or require the first PaneSnapshot across
clicks: selection replaces those projections. A DoubleTapped request carries the
current exact rendered/current snapshot and must still contain that visible entry
under the same listing. It consumes the candidate once before attempting work.
Rejected first taps, a different target, a modified gesture, any non-pointer intent,
input-owner change, window deactivation, read/launch/operation start or listing
replacement clear the candidate. A consumed candidate cannot launch again, and a
refused DoubleTapped does not seed a new one. No wall-clock timer is introduced.
The existing OpenFocused file-versus-directory path opens the named clicked row
only after qualified focus; modified double clicks never launch.

Every admitted synchronous selection or surface action replaces its target
PaneSnapshot wrapper. Header and absent-content actions preserve Content, Activity
and PaneState identities; the new wrapper is the accepted-input receipt, not a new
model value. The mapper returns the accepted side only when the actual before
snapshot matches the request, the returned target snapshot changed, the target is
active and input ownership remains available. After rendering, App rechecks current
ownership and stopping state and uses the existing synchronous FocusFileList helper.
This lets an explicit accepted pane click leave the command-entry keyboard context.
Open completion never requests this synchronous focus return. Rejected input cannot
move native focus through this receipt. Only plain row selection seeds double tap.

Framework selection is not accepted Application state. The adapter must prevent or
reconcile native selection changes when an intent is rejected or work is busy,
even when the projected state object is unchanged. Programmatic rendering must not
emit another user intent. Native event ordering requires direct host evidence and
is not inferred from pure mapper tests.

### Existing command entry points

Add a single application-level group for Bookmarks, Settings and Keyboard help.
Each invokes its existing UserIntent and existing session admission; this work adds
no new surface, settings policy or command execution route. Tooltips/automation
names use the existing localized command labels and canonical key-map data.
Disabled appearance is a projection; the owner validates again at invocation.

The proposed group sits before the system caption controls, outside the declared
drag area, using existing semantic density/spacing/text/focus resources. The
caption's minimize/maximize/close and drag/hit-test contract must remain proven.
No fixed new colors, raster assets or independent visual identity are introduced.

For keyboard access, preserve ordinary file-list Tab as pane switching. Unmodified
initial F10 enters this group through KeyboardIntentMapper, choosing the first
enabled button only while ordinary pane input owns the session. The canonical map
returns a closed Presentation focus action, not a new Application modal. Add the
CommandEntry keyboard context for focus within these three controls, below every
existing modal/editor/window scope. Native Tab/Shift+Tab traverses and cycles at
the ends through the container's TabFocusNavigation=Cycle; native Enter/Space
activates the focused button. No ordinary file-list movement/open/delete command
maps in this context. Unmodified Escape/F10 returns to the active pane through the
existing focus helper. Existing modal commands retain their pane-return behavior.

GetFocusedControlContext must recognize the group's descendants; queued and
reactivation focus helpers must recheck that context at execution so they cannot
steal focus. Existing modal precedence wins over a stale native button focus.
The mapper's one repeat guard consumes held F10/Escape across context changes,
preventing reentry or clearing pane selection after leaving. Native Enter/Space
activation also enters that same guard before a modal opens, preventing its held
key from immediately activating the returned/next surface. Disabled/busy admission
is validated again without opening an Application scope for the group.

Shift+F10 is not an entry/exit binding and remains available to #531. Extend raw
F10 translation with explicit Shift state (Shift alone is a closed modifier;
combined Shift+Control/Alt is Other). Other virtual-key/produced-character
translation retains the existing Control/Alt contract, including uppercase G and
layout-produced symbols. This is a bounded F10 distinction, not a rewrite of
letter/IME/dead-key mapping. Update CMD-009, KEYBOARD_MODEL, canonical focus binding/
hint data, KeyboardContext, translator and repeat-guard proof in the same change.
Existing command shortcuts remain available. Opening/closing each existing surface
keeps that surface's declared pane-focus return and dismissal; the proposal does
not change Escape or scrim behavior to restore a toolbar button or prompt on drafts.

## Design consultation and scope

The requested background Claude /design command reports that it only manages
project access in this CLI and produces no design. A separate tools-disabled text
consultation of public Issue530 requirements returns a proposal. Its single strip,
localized names, semantic states and selection model inform this draft. Its roving
Tab group, changed modal dismissal, focus-return changes, busy timers and new
minimum window width are not adopted automatically. See the design record.

Technical design was independently accepted at08:55 JST after the recorded corrections.
No production implementation or visual acceptance is claimed at that acceptance point.
No source is sent to Claude; both exact brief hashes, invocations and raw answers
are retained outside the checkout. The pending beta.4 source-publication approval
is a separate payload and is not retried through this work.

## Alternatives rejected

- Treat ListView.SelectedItem as the source of truth or operate directly from a
  control event: focus/selection and the validated operation target can diverge.
- Send a sequence of repeated MoveNext/ActivateOtherPane/ToggleSelection commands
  from code-behind: intermediate state and asynchronous admission create ambiguity.
- Resolve a row by its current index or by path equality alone after a refresh:
  virtualization/replacement can target a different entry.
- Use native Extended selection as another durable selection owner, a private key
  table, direct Shell activation, or UI-level capability/overwrite decisions.
- Broaden this change to context menus, clipboard, shell properties, New-file
  creation, global hotkeys, drag/drop, or unaccepted other-pane navigation.

## Required proof and migration

Before accepting the mechanism, independently review ownership, stale admission,
anchor lifetime, double-click sequencing and keyboard access. Then update CMD-002,
KEYBOARD_MODEL, GLOSSARY and design records in the same implementation change.

Owning tests must prove both sides, single/empty lists, toggle/removal, inclusive
ranges in both directions, Ctrl+Shift union/deduplication, absent/removed anchors,
provider path identity, hidden/sorted/refreshed rows, immutable owned snapshots,
foreign/stale owners (including a rejected stale first tap whose actual current
state still focuses the same row), double-click listing replacement, every modal/busy owner,
reentrant admission, and no rejected read/launch/effect. Existing navigation,
selection fallback, copy/move/delete source choice and native keyboard routing
remain directly affected consumers. Control proof must distinguish native visual
selection from Application focus and prove rejected-event reconciliation.

Select affected Application/Presentation tests and App build first. Because the
immutable pane state and input admission are shared by every pane command, the
final full canonical gate is needed if focused proof cannot establish those wider
consumer invariants. No automatic full/deep run or all-layer mutation is selected
by this document-only draft. Native mouse/keys/focus/caption hit testing requires
an isolated session or hide's later operation; shared-desktop input is forbidden.
Visible acceptance and technical acceptance remain separate.


Framework references (consulted November24; source documentation, not observed host proof):
- [WinUI Tapped sequence](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.tapped).
- [Tab focus navigation](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.tabfocusnavigation) and [Cycle semantics](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.keyboardnavigationmode).
