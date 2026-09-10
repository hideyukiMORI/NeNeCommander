# ADR-0047: Search and dispatch existing commands through one session-owned palette

Status: accepted

Date: 2026-09-09

Accepted for Issue #101 on 2026-09-09 by the NeNe Commander design owner under hide's delegated
authority, after hide accepted visual direction B on 2026-09-08.

## Context

The application has a closed `UserIntent` vocabulary, one `KeyboardIntentMapper`, and one
`CommanderSession` route into the settings and dual-pane state owners. It has no searchable view
of those commands. Issue #101 adds `Ctrl+P` command search without creating another execution
route, changing provider policy, or weakening modal, busy, text-entry, or destructive-operation
precedence.

A command palette introduces three distinct concerns. Application must own whether the palette is
open, which dual-pane state it captured, which existing intents it may dispatch, and whether a
candidate is available from that state. Presentation must own localized labels, localized search,
the filtered rows, and the selected row. The native search editor must continue to own ordinary
text editing and IME composition. Combining those concerns in Application would introduce
localized text and UI-culture behavior there; deciding candidates or availability in App
code-behind would instead move application policy to the framework boundary.

The palette can reach copy, move, rename, create-directory, and delete. Opening and cancelling it
must preserve both pane contents, focus items, selections, histories, and the active side. A delayed
Enter event must not run against a later active source or passive destination, and palette display
must not repeat filesystem, provider, or capability preflight that already belongs to the existing
command route.

## Decision

- **`UserIntent` remains the command identity.** No parallel `CommandId` enum or string identifier
  is added. A single Application `CommandCatalog` declares an ordered subset of existing
  parameterless singleton intents. The palette-open intent and typed palette submission extend the
  same closed vocabulary, but submission accepts only a catalog member. A caller cannot inject
  `Confirm`, `Escape`, a parameter-bearing submission, or a recursive palette-open command.
- **The first catalog has this exact stable order:** `OpenFocused`, `NavigateParent`,
  `NavigateBack`, `NavigateForward`, `Refresh`, `FocusAddress`, `Rename`, `Copy`, `Move`,
  `CreateDirectory`, `Delete`, `ToggleSelection`, `ToggleHiddenItems`, `ActivateOtherPane`, and
  `OpenSettings`. These are implemented user actions with a direct file-list binding. Row movement,
  first/last focus, and half-page movement are excluded because they are navigation inside the
  current file list rather than useful palette actions. `Confirm` and `Escape` belong to transient
  contexts; all typed submissions require data the palette does not own. Fuzzy aliases, recent-use
  entries, persisted history, window commands, bookmark commands, and unimplemented commands are
  outside this Issue.
- **`CommanderSession` is the sole palette interaction owner.** `Ctrl+P` produces one
  `OpenCommandPalette` intent. It is declared only for `FileList` and `NavigationSurface`.
  `CommanderSession` opens the palette only when settings and address editing are closed, no file
  operation is running or awaiting confirmation, name, or conflict, neither pane is `PaneLoading`
  or `PaneLaunching`, and the active pane has listed content. A completed or rejected operation
  status is not work in flight. Direct navigation and every underlying pane, settings, and address
  intent are frozen while the palette is open; only palette cancellation or one qualified palette
  submission can leave it.
- **One open scope freezes both command endpoints.** The open state captures the exact left and
  right `PaneSnapshot` instances plus `ActiveSide`; it also exposes the catalog candidates and
  their state-level availability. It does not compare `DualPaneSnapshot` by reference because
  `DualPaneSession.Current` creates a new wrapper on every read. Capturing both owned pane snapshots
  prevents a delayed copy or move from retaining its source while silently changing its passive
  destination.
- **Availability is a closed Application projection of captured state.** `OpenFocused`, `Rename`,
  and `ToggleSelection` require a focused entry. `Copy`, `Move`, and `Delete` require a non-empty
  selection or focused entry; `Copy` and `Move` also require listed content in the passive pane.
  Parent, Back, and Forward require their existing state-level navigation candidate. The remaining
  catalog commands are available after the open admission rules pass. Closed unavailable reasons
  distinguish missing focus/source, missing passive listing, and unavailable Parent/Back/Forward.
  Provider support, filesystem capability, collisions, identity, containment, and destructive
  policy are not queried for display. The existing pane session and `FileOperationGateway` make
  those decisions after dispatch and return their existing typed outcomes.
- **Localized filtering belongs to Presentation.** One Presentation command-label catalog maps
  each supported intent to its existing `IntentLabel...` resource key. `KeyHintPresenter` and the
  palette presenter consume that same correspondence; missing labels are added to the same
  resource family rather than creating an Application resource mechanism. The canonical shortcut
  comes from the first direct file-list binding for the intent in
  `KeyboardIntentMapper.BindingsFor`, so neither the view nor the palette owns another key map.
  App resolves resource keys and passes the localized title and shortcut text to the pure
  Presentation search model. App code-behind does not choose catalog membership, order, or
  availability.
- **Search is deterministic and session-only.** The native search editor owns the query text.
  Presentation matches an ordinal case-insensitive substring against the localized title or
  canonical shortcut. An empty query returns catalog order. A query change selects the first
  result and scrolls it into view; zero results have no selection. There is no fuzzy matching,
  tokenization, alias expansion, scoring, history, persistence, debounce, timer, or match-run
  decoration in the first implementation. Unavailable rows remain selectable and readable, are
  not represented with `IsEnabled=false`, and show their localized reason.
- **Palette navigation has one explicit keyboard context.** `CommandPalette` owns Up, Down, Enter,
  and Escape through `KeyboardIntentMapper`. Up and Down move the Presentation-owned selection
  from either palette tab stop and clamp at the first and last row. Enter submits the selected
  canonical intent with the exact expected open state; with zero results or an unavailable row it
  is a no-op and the palette remains open. Escape cancels the palette. Printable keys, editing
  chords, dead keys, and IME events pass to native controls. While IME composition is active, Up,
  Down, Enter, and Escape belong to the IME; they do not move or execute a candidate or close the
  palette. An initial palette Enter arms one mapper-owned repeat guard. Every repeated Enter from
  that physical press is consumed without an intent even after execution changes the context to a
  file list, address editor, or existing confirmation modal; other keys and context changes do not
  release the guard. The next initial Enter releases it and follows normal mapping, arming a fresh
  guard when it executes another palette candidate. Ordinary file-list Enter repeat behavior is
  unchanged when no palette guard is active. Only a later initial Enter press or an explicit native
  button click can confirm a modal opened by the palette.
- **Tab uses one Presentation focus action inside a two-stop native focus loop.** Initial focus is
  the search field. `KeyboardIntentMapper` maps Tab to a Presentation-only focus action; App only
  applies native focus to the other stop and emits no `UserIntent`. Tab and Shift+Tab cycle only
  between `SearchField` and the composite `CandidateList`. Context, detail, hints, rows, badges,
  and the scrim are not extra tab stops.
  The underlying panes remain inert. Up and Down change selection without moving UIA focus away
  from the search field when it owns focus.
- **Submission is qualified and fail-closed.** A typed submission carries the expected open-state
  instance and one catalog intent. Before any effect, `CommanderSession` verifies that the expected
  state is still current, the intent is a catalog member, it was available in that scope, the
  active side is unchanged, and both current owned `PaneSnapshot` instances are the captured
  instances. A submission from an older palette, an arbitrary intent, or an unavailable candidate
  performs no dispatch and does not close the current valid palette. If the current open scope
  itself no longer matches its captured pane state, it closes without dispatch and follows the
  current active pane or existing modal focus; it does not force focus to the captured old side.
- **Execution closes and routes once inside the owner.** After successful validation,
  `CommanderSession` closes the palette and calls the same private dispatch path used by normal
  input exactly once. It does not publish an intermediate closed notification that can re-enter
  before dispatch. Rename, create-directory, delete, and conflict handling therefore enter their
  existing modal states; copy and move enter the existing gateway; file open retains the existing
  provider decision. No `FileOperationRequest`, gateway, provider, or direct pane-reducer path is
  added to the palette.
- **Focus follows the transition that actually occurred.** Escape requests a one-time return to
  the active pane captured at open and does not clear selection. Successful execution adds no
  fixed old-side focus request: `OpenSettings` and `FocusAddress` focus their existing owner,
  rename/create/delete use their existing modal focus, `ActivateOtherPane` focuses the new active
  pane, and other commands retain their existing result focus. This prevents palette closure from
  overwriting the focus selected by the executed command.
- **Pointer input is a thin adapter to the same qualified routes.** Clicking or tapping a candidate
  selects that exact current filtered row and submits it through the same expected open-state and
  catalog-intent pair as Enter. An unavailable row is selected to expose its reason and remains
  open. Clicking or tapping the scrim emits an expected-state-qualified palette cancellation;
  events inside the palette surface do not bubble into that cancellation. A stale row or scrim
  callback cannot close or execute a later palette or modal.
- **Visual integration follows the accepted B engineering handoff.** The palette is a compact
  top-aligned overlay with the detail bar below the candidate list. It reuses existing semantic
  Surface, Text, Border, Focus, Selection, Status, Operation, Spacing, Typography, Radius,
  Elevation, Density, and Motion resources. The first implementation adds no animation, timing
  boundary, refusal flash, match decoration, or new top-level token family. Exact layout and
  accessibility constraints are recorded in
  `docs/design/2026-09-09-command-palette-handoff.md`.

## Rejected alternatives

- Adding a `CommandId` enum beside `UserIntent`: it would duplicate the closed command vocabulary
  and require identity synchronization without adding a boundary.
- Building candidates, availability, or dispatch switches in App code-behind: the framework
  adapter would own application policy and could bypass state freshness.
- Localizing or filtering commands in Application: UI culture and resource lookup are
  presentation concerns, and localized strings are not command identity.
- Opening while a pane read, file launch, or file operation is in flight: the captured endpoint
  could become stale and Escape would conflict with operation cancellation precedence.
- Computing display availability through gateway or provider preflight: preflight cannot reserve
  a target, would duplicate the existing command path, and could become stale before Enter.
- Keeping only the active pane snapshot: copy and move could use a changed passive destination.
- Dispatching the selected intent after a public close/render callback: focus or event reentry
  could change the active target between validation and command start.
- Disabling unavailable rows: it would prevent keyboard and assistive-technology users from
  reaching the reason the accepted visual direction is designed to explain.
- Mapping Tab to `ActivateOtherPane` while the palette is open: it would escape the palette's
  focus scope and mutate the frozen active side.
- Mirroring every query keystroke in Application or intercepting composition keys: the native
  editor already owns text and IME semantics.

## Consequences

The application gains one small modal-like interaction state and one explicit palette subset, but
no new operation implementation. Presentation gains the localized filter and selected-row state;
the App remains a resource and framework-event adapter. Catalog order and availability reasons
become public behavior covered by tests. New direct commands must be considered explicitly for
both the keyboard map and palette membership, without automatically appearing in the palette.

Unavailable display is intentionally conservative. A state-available command may still return an
existing provider or capability rejection after execution. That result is more accurate than a
speculative palette preflight and retains the sole safety boundary.

## Migration and removal

Extend `UserIntent`, `CommanderSession`, `CommanderSnapshot`, the canonical keyboard context and
binding table, the shared Presentation intent-label correspondence, localized resources, the host
overlay, and focused tests atomically. Add the palette catalog and state under Application
sessions/commands and the deterministic filter/view state under Presentation. No Domain,
Infrastructure.Windows, filesystem adapter, settings schema, dependency, or persisted data changes
exist. No old execution path is retained or removed because every candidate continues through the
existing session route.

## Executable proof

Application tests prove open admission and rejection for settings, address, operation-awaiting,
operation-running, `PaneLoading`, and `PaneLaunching` states; catalog order and membership;
state-only availability; full freeze; Escape preservation; and exact routing of each available
catalog intent. Adversarial tests prove stale expected-state rejection, both-pane and active-side
freshness, passive-destination retention for copy/move, unavailable and arbitrary-intent no-op,
single dispatch, and preservation of the existing confirmation/preflight targets.

Presentation tests prove `Ctrl+P` only in `FileList` and `NavigationSurface`, the dedicated context,
IME composition deferral including Escape, two-stop Tab behavior at the host boundary, shared localized label and
shortcut correspondence, ordinal case-insensitive substring matching, stable empty order,
first-row reset, clamped selection, zero-result behavior, and selectable unavailable rows. Host
tests prove one-time cancellation focus, executed-command focus precedence, localized UIA Name
including shortcut, target, and unavailable reason, full-name tooltip, and no private key map.

Because the palette can dispatch existing destructive operations, the final exact head requires a
security deep review before the Draft-to-Ready canonical CI gate. Focused Application and
Presentation behavior tests, affected coverage, conformance, and dependency-impact tests run during
implementation. Native WinUI IME, Narrator/UIA, high contrast, 100/150/200/300% DPI, narrow-window,
and all eight scheme cells remain explicit Issue #94 environmental proof until actually executed.
