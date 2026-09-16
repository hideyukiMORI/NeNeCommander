# ADR-0050: Adjust the window through one session-owned adjustment mode

Status: proposed

Date: 2026-09-16

Proposed for Issue #100 on 2026-09-16 by the NeNe Commander design owner under hide's delegated
authority, after hide accepted visual direction A (a small helper at the upper centre of the
window) on 2026-09-08. The visual acceptance approved no keyboard mode, shortcut, increment,
restore rule, or implementation; this ADR decides those. It was revised three times after
implementation review; it stays proposed until hide confirms the shortcut, the anchored resize,
the idempotent maximize and restore commands, and the ADR-0051 prerequisite.

## Context

Issue #100 asks for keyboard commands that move, shrink, enlarge, and restore the NeNe Commander
window. The application today never touches its own window geometry: `CommanderApplication`
creates one `CommanderWindow`, activates it, and leaves size, position, maximization, and snapping
to the operating system. Window geometry is process-global operating-system state. The
constitution forbids touching such state outside a declared boundary, forbids application
decisions in XAML code-behind, forbids native imports outside `Infrastructure.Windows`
(SEC-014, CS-018), and requires every non-text shortcut to be mapped by `KeyboardIntentMapper`
and to reach an Application owner through one declared route.

Five forces shape the mechanism:

- **Many adjustments in a row.** A user moving a window across a monitor, or shrinking it to fit
  beside another application, presses the same command repeatedly and holds keys down. Single
  chords for each direction and size change would need eight or more free modifier combinations
  on a key map that already assigns `Alt+Left`/`Alt+Right` to history, `Ctrl+D`/`Ctrl+U` to
  half-page movement, `Ctrl+1`–`Ctrl+9` to bookmarks, and leaves `Win+Arrow` and `Alt+Space` to
  Windows. The host's existing intent dispatch runs through `AsyncWorkOwner`, which rejects
  overlapping work by design (ADR-0030); a 30 Hz key repeat through that path would drop most
  steps.
- **The operating system already owns some of this.** Windows snap, `Win+Arrow`, the system menu,
  the title-bar controls, and per-monitor DPI remain authoritative. The application must not
  intercept them, must not cache geometry that the OS can change underneath it, and must not treat
  a display's scale factor as a constant. The Windows App SDK exposes the window through
  `AppWindow`, `OverlappedPresenter` (whose state is exactly `Maximized`, `Minimized`, or
  `Restored`), and `DisplayArea`; it exposes no snapped-window state, and the only way to learn it
  is an undocumented-header Win32 import that SEC-014 keeps out of App.
- **Existing precedence must not weaken.** `Escape` cancels a running operation before it closes
  transient UI. Modal confirmation, name entry, conflict resolution, settings, bookmarks, address
  editing, and the command palette each freeze the panes. A `g` chord pending in the file list
  must not be disturbed by keys the file list does not map. A window mode must fit into that order
  without giving `Escape` two meanings at once and without ever trapping the user.
- **Composition is explicit and acyclic.** `CommanderApplication.CreateWindow` constructs
  `CommanderSession` before `CommanderWindow`, and the window's `AppWindow` exists only after the
  window. A session that held a window port at construction would need a partially initialized
  object (CS-008) or a service locator (ARC-006). Application must therefore decide without
  holding the window, and the host must read and write the window without deciding.
- **`CommanderSession` already exceeds CS-013.** It holds 324 logical lines inside the type against the
  300-line limit, and QLT-010 requires an existing violation to be fixed before merge rather than
  grown. Removing only the palette scope would leave about 279 lines, and the window mode's
  admission, freeze, delegation, and two synchronous members add an estimated 25 to 35, so one
  extraction is not enough and the split is a work unit of its own.

The `Ctrl+P` palette (ADR-0047) already established the shape of a session-owned transient scope
that captures state, freezes pane intents, owns one keyboard context through a dedicated binding
table, and leaves through a qualified cancellation. This decision reuses that shape for geometry
instead of inventing a second kind of mode. It depends on ADR-0051, which splits
`CommanderSession` into scope owners (palette and address state and validation move out; dispatch
stays) and explicitly supersedes ADR-0047's sentence that `CommanderSession` is the sole palette
interaction owner together with the `COMMAND_MODEL.md` registry row that names it. ADR-0051 lands
first as its own Issue; this ADR is accepted on the condition that after both changes
`CommanderSession` holds at most 300 logical lines.

## Decision

- **One explicit window-adjustment mode, entered by `Ctrl+W`.** `Ctrl+W` is declared for
  `FileList` and `NavigationSurface` and produces one `OpenWindowAdjustment` intent. The key is
  unbound in every existing context and mnemonic for "window". It is also the common "close tab"
  key in browsers and terminals; NeNe Commander has no tabs, nothing destructive happens on entry,
  and the helper names `Esc` as the way out on the first frame, which is the accepted trade-off.
  The mode is persistent, not a timed chord: it stays open until the user leaves it, so repeated
  steps need no re-entry and the accepted helper stays visible while it is open. `Ctrl+W` pressed
  while a `g` chord is pending cancels the chord and opens the mode, as any other key mapped in
  that context does.
- **`WindowAdjustmentSession` owns the mode; `CommanderSession` admits, freezes, and
  delegates.** `WindowAdjustmentSession` is a separate Application state owner in the form of the
  scope owners ADR-0051 establishes, constructed by the composition root without any window
  dependency and passed to `CommanderSession` inside the ADR-0051 `TransientScopeOwners` record.
  It owns the closed `WindowAdjustmentState` hierarchy (`WindowAdjustmentClosed`,
  `WindowAdjustmentOpen`) exposed as a third member of `TransientScopeSnapshot` on
  `CommanderSnapshot`, so neither constructor's arity changes, and every outcome. `CommanderSession` keeps admission, freeze,
  delegation, and the `NavigateAsync` guard, and stays within the CS-013 limit after this change.
  `CommanderSession` opens the window mode only when settings, bookmarks, address editing, and the
  command palette are closed, no file operation is running or awaiting confirmation, name, or
  conflict, and no pane read or launch is in flight. Listed pane content is not required: a pane
  whose last read failed does not prevent moving the window. While the mode is open every pane,
  settings, bookmark, address, and palette intent is frozen, and `CommanderSession.NavigateAsync`
  refuses external navigation exactly as it does under an open palette; only a qualified window
  action or a qualified leave can act. The open state captures only the active `PaneSide` at
  entry and the most recent outcome. It captures no pane snapshot, so a background read that
  completes while the mode is open does not end the scope. The open state is a fresh instance per
  entry; every window action carries the expected open instance, and a stale action is a no-op
  that does not close the current mode.
- **Application decides through one synchronous entry; the host reads, applies, and renders.**
  Application publishes the closed data types `WindowPlacement` (physical-pixel `Bounds`,
  `WindowPresenterState` as the closed set `Restored`, `Maximized`, `Minimized`, `NotOverlapped`,
  or `Unavailable`, the window's current rasterization scale, the presenter's declared preferred
  minimum size or zero when none is declared, the physical work area of the window's display, and
  the physical work areas of every attached display), `WindowBounds`, `WindowAdjustmentAction`,
  `WindowAdjustmentPlan`, and `WindowAdjustmentRefusal`, plus the pure static
  `WindowAdjustmentPlanner`. It declares no port: Application never reads or writes the window,
  so an interface there would have no inversion boundary to serve (CS-009). `CommanderSession`
  exposes two synchronous members beside `HandleAsync`: `AdjustWindow(WindowAdjustmentRequest)`
  returns a `WindowAdjustmentDecision` holding the plan (`MoveTo(WindowBounds)`,
  `ResizeTo(WindowBounds)`, `Maximize`, `Restore`, or `Refused(reason)`) and updates the open
  state's last outcome, and `LeaveWindowAdjustment(WindowAdjustmentOpen expected)` closes the
  mode and requests the one-time focus return. Both validate the expected open instance and
  return a no-op decision for a stale one. Neither awaits nor allocates work in `AsyncWorkOwner`,
  and both read and write only `WindowAdjustmentSession` state and no mutable field of
  `CommanderSession` or of any pane; that invariant, proven by a test that runs them while a pane
  read is pending, is what makes them callable on every key-repeat event from the UI thread
  without the serialization the intent path gets from `AsyncWorkOwner`. This is the mode's
  declared synchronous input route, recorded as its own `COMMAND_MODEL.md` registry row beside
  the intent route; the repeat requirement is why it exists and it carries no other command. The App
  host owns one `AppWindowPlacementAdapter` in `src/NeNeCommander.App/Windowing/`, constructed by
  `CommanderWindow` over its own `AppWindow`, `OverlappedPresenter`, `DisplayArea`, and
  `XamlRoot.RasterizationScale`; it translates and never decides, and it maps any read failure
  into a placement whose state is `Unavailable`. On each window action the host reads a fresh
  placement, calls `AdjustWindow`, applies the returned plan through the adapter exactly once,
  and renders `_session.Current`. Because the plan is a return value and not state, a later
  render, a background read, or a repeated snapshot cannot apply it again. Leaving never reads
  the placement and cannot be refused by any placement state.
- **The mode owns one keyboard context, `WindowAdjustment`, through a dedicated binding table.**
  As with the palette, `KeyboardIntentMapper` declares `WindowAdjustmentKeyBinding` entries that
  map a key to a closed `WindowAdjustmentKeyAction`; `BindingsFor(WindowAdjustment)` is empty, as
  it is for `CommandPalette`. Each entry also declares its hint group and whether it is the
  group's displayed cap, so `WindowAdjustmentKeyHintPresenter` generates the helper's hints from
  the same table (KBD-005) as one hint per group with the group's displayed caps in declaration
  order; arrow aliases and `Ctrl+W` are declared with no displayed cap. The table is:

  | Key | Action | Hint group |
  |---|---|---|
  | `h` or `Left` | move the window left by one step | move (`h`) |
  | `j` or `Down` | move the window down by one step | move (`j`) |
  | `k` or `Up` | move the window up by one step | move (`k`) |
  | `l` or `Right` | move the window right by one step | move (`l`) |
  | `+` | enlarge the window by one step in width and height, anchored at its top-left corner | enlarge |
  | `-` | shrink the window by one step in width and height, anchored at its top-left corner | shrink |
  | `m` | maximize the window | maximize |
  | `r` | restore the window to its normal placement | restore |
  | `Escape` or `Ctrl+W` | leave the mode | leave (`Esc`) |

  Letter and symbol commands use the produced character with explicit modifier state (KBD-003).
  `=` is not an alias: on a JIS layout `+` and `=` are two shifted characters of two different
  keys, so an alias would make one physical key enlarge with Shift and shrink without it. Adding
  `m`, `+`, and `-` to `KeyboardKey` must not disturb a pending `g` chord in the file list, where
  they stay unmapped; `Map` therefore decides chord cancellation by whether the key is declared in
  the current context, which is what `KEYBOARD_MODEL.md` already states, instead of by
  `KeyboardKey.Other`, and the existing chord tests gain cases for the new keys. `Ctrl+W` is
  declared on the virtual-key route in `KeyboardInputTranslator`, which is the route that fires at
  runtime because a handled `PreviewKeyDown` suppresses `CharacterReceived`; the character route
  declares it too so that the translator's own tests stay symmetric with the `Ctrl+B` precedent.
  Auto-repeat is accepted for move, enlarge, and shrink through a context-aware repeat rule inside
  the mode's own mapping branch; a repeated `m`, `r`, `Escape`, or `Ctrl+W` is consumed without an
  action. The existing key-limited destructive-repeat rule is not changed. `Map` gains one
  explicit `WindowAdjustment` branch placed before the `KeyboardKey.Other` pass-through return: a
  declared key yields its action, and every other key, including `Enter`, `Tab`, printable
  characters, editing chords, and `Other`, yields `KeyboardConsumed` so nothing reaches a pane,
  editor, or native control. Leaving the mode has one meaning in both keys: it closes the mode and
  requests a one-time return of focus to the active pane captured at entry. There is no revert:
  every applied step already happened on the desktop, and remembering an origin placement would
  make the mode hold geometry the OS may have changed in the meantime.
- **`m` and `r` are idempotent commands, not a toggle.** `m` maximizes from `Restored` or
  `Minimized`; on a maximized window it is refused with `WindowIsMaximized`. `r` restores from
  `Maximized` or `Minimized`; on a restored window it is refused with `WindowIsRestored`. Move,
  enlarge, and shrink are refused with `WindowIsMaximized` or `WindowIsMinimized` in those
  presenter states. A snapped window reports `Restored` because the SDK exposes nothing else;
  moving or resizing it un-snaps it, which is the user's explicit request and the same effect a
  drag has. Every move, size, maximize, and restore action is refused with
  `PresenterIsNotOverlapped` or `PlacementUnavailable` in those placement states; leaving is not
  an action of the planner and always succeeds. The `Minimized` branches exist for closure of the
  state set even though a minimized window rarely holds keyboard focus. The mode never restores
  implicitly on the user's behalf.
- **Planning is a pure Application calculation with one boundary rule and fixed steps.**
  `WindowAdjustmentPlanner` turns a placement and an action into a plan. All arithmetic is in
  whole physical pixels. The step is 32 device-independent pixels multiplied by the placement's
  rasterization scale and rounded half away from zero; the constant lives in Application as
  `WindowAdjustmentStep` because it is behavior, not a visual token. Crossing onto a display with
  another scale changes the step on the next keystroke, which is the documented consequence of
  reading fresh. The one boundary rule is the **caption rule**: a placement is acceptable when
  some attached work area `w` contains a horizontal segment of the window's top edge row at least
  `min(step, width)` pixels long, and either `w` extends at least `min(step, height)` pixels
  below that row or another attached work area whose top edge is `w`'s bottom edge covers the
  same segment, so that at least one step of caption height stays visible across a seam. A move
  whose target fails the caption rule is refused with `CaptionWouldLeaveDesktop`; a move whose
  target passes is applied unchanged, so a window can cross onto another monitor and its caption
  always keeps a pointer-reachable run of pixels. Both tests are rectangle comparisons. Enlarge adds one step to width and to height with
  the top-left corner fixed, so it never changes the caption's position and needs no boundary
  check; it is refused with `AtMaximumSize` when the window is already at least as wide and as
  tall as the work area of its display. Shrink removes one step from width and from height with
  the top-left corner fixed and is refused with `AtMinimumSize` when either resulting dimension
  would fall below the effective minimum: the presenter's declared preferred minimum when the
  placement reports one greater than zero, otherwise one step, so that no dimension can reach
  zero. The application declares no preferred minimum of its own in this change; the
  `OverlappedPresenter` preferred-minimum properties remain the single mechanism for a window
  minimum if one is ever decided. The planner is deterministic, has no OS dependency, and is
  proven at 100, 125, 150, 175, 200, and 300 percent plus one non-standard scale that exercises
  rounding.
- **Outcomes are closed and rendered, never thrown.** The open state records the most recent
  `WindowAdjustmentOutcome`: `Planned` with the action, or `Refused` with one of the closed
  reasons above. The helper labels a planned outcome by the action's name, so it never claims
  more than the decision; if the adapter's apply call throws, the host reports it through the
  existing defect observer, the mode stays open, and the next read shows the real placement.
  Nothing is persisted; window geometry is not written to the settings document and no schema
  changes.
- **The helper is the accepted direction A rendered from state, inside the existing shell
  markup.** One Presentation `WindowAdjustmentPresenter` projects the open state into the helper:
  a localized mode title, the localized label of the last outcome, and the hints from
  `WindowAdjustmentKeyHintPresenter`. The helper overlay is declared inline in
  `CommanderWindow.xaml`, like the palette overlay, so that it resolves the shared `KeyHintTemplate`
  and stays inside the ARC-012 scheme-resource scan; its wiring lives in a
  `Views/WindowAdjustmentView` class in the form of `BookmarkManagerView`, so that
  `CommanderWindow.xaml.cs` does not grow. The host places the helper in the upper centre over the
  still-visible panes and reuses the palette's transparent scrim so that pointer input cannot reach
  the frozen panes; tapping the scrim calls the same qualified leave as `Escape`. The helper hosts
  one focus sink: a focusable control with no visible chrome that receives programmatic focus when
  the mode opens and is collapsed whenever the mode is closed, so it is never a tab stop then;
  while open it is the only reachable focus because the mode consumes `Tab`. No text control owns
  focus, so no IME composition can start. The helper exposes one UIA element named by the mode
  title whose help text is the current outcome, raised as a live-region change on every outcome.
  It contains no buttons and uses only the existing semantic Surface, Text, Border, Focus,
  Selection, Status, Operation, Density, Spacing, Typography, Radius, Elevation, and Motion
  resources, plus the same system `SmokeFillColorDefaultBrush` theme resource the palette scrim
  already uses; the idle, planned, and refused outcome tones are `TextSecondaryBrush`,
  `TextPrimaryBrush`, and `StatusWarningBrush`, so no scheme colour key is added. No fixed colour, new token family, animation, or timer is
  added. Exact layout values follow the engineering handoff
  `docs/design/2026-09-16-window-adjustment-handoff.md`, written in the same change.
- **Coverage and mutation thresholds are untouched, and no exclusion is added.**
  `eng/verify-coverage.ps1` measures Domain, Application, Infrastructure.Windows, and
  Presentation.WinUI only; `NeNeCommander.App` is not a measured project, and `eng/conformance.ps1`
  pins the exclusion list to exactly what ADR-0008 names. Every decision in this change lives in
  Application and Presentation at their existing thresholds; the App adapter and view are
  framework translation proven at runtime, not unit-tested. No App test project is created:
  `AppWindow` needs a running WinUI process, which is runtime proof, not a unit tier.
- **The operating system keeps everything it owns.** `Win+Arrow`, snap layouts, the system menu,
  title-bar controls, drag, and per-monitor DPI changes are neither intercepted nor mirrored. The
  mode adds no window subclassing, no message hook, no native import, and no global shortcut.

## Rejected alternatives

- One chord per direction and size change (for example `Ctrl+Alt+Arrow`): consumes eight or more
  free modifier combinations, makes repeated adjustment tiring, and leaves no natural home for the
  accepted helper, which shows the current operation of a mode.
- A timed chord prefix like `gg`: repeated steps would re-enter the prefix on every keystroke, and
  a 750 ms expiry is wrong for a user watching the window move.
- `Alt+Space` or `Win`-based entry: reserved by Windows for the system menu and snapping.
- Detecting a snapped window through `IsWindowArranged`: it is a Win32 import without a header,
  the SDK presenter reports `Restored` for a snapped window, and SEC-014 confines native imports
  to `Infrastructure.Windows`; the value would buy one refusal reason at the price of a new native
  surface and a security-sensitive review.
- Holding a window port inside the session: the session is constructed before the window, so the
  port could only arrive through a partially initialized object or a locator; it would also let a
  future refactor call the port after a `ConfigureAwait(false)` continuation off the UI thread.
- Declaring the port interface in Application while only the host calls it: an interface with no
  dependency-inversion boundary and one implementation (CS-009), and two routes outside the
  command pipeline.
- Carrying the plan in the snapshot state: `RenderAfterAsync` renders twice per intent and again
  after every background read, so a plan held in state would be applied more than once.
- Dispatching window actions through the existing `AsyncWorkOwner` path: it rejects overlapping
  work by design, so a held key would drop most repeats.
- Reverting geometry on `Escape`: requires the mode to hold an origin placement that the OS can
  invalidate, gives `Escape` and `Ctrl+W` different meanings, and contradicts the settings
  editor's no-rollback rule for immediately effective changes.
- A single maximize/restore toggle: its effect depends on OS-side state the user may not see,
  whereas two idempotent commands are always predictable.
- Restoring implicitly when a move or size command runs on a maximized window: hides a state
  change inside another command and makes the first keystroke do two things.
- Enlarging and shrinking around the window's centre: moves the caption on every size change,
  which either needs a second boundary rule or lets a shrink push the caption off-screen, and a
  centre-preserving enlarge clipped to the display's work area would teleport a window the user
  had deliberately moved partly off-screen.
- Computing geometry in the App host: `AppWindow` arithmetic in code-behind would put behavior
  outside the testable Application boundary and could not be proven at several scale factors.
- An adapter in `Infrastructure.Windows` or `Presentation.WinUI`: neither may reference
  `Microsoft.WindowsAppSDK`; adding that reference would widen the framework boundary that
  ADR-0008 isolates.
- A separate `UserControl` for the helper under `App/Windowing/`: it could not resolve the
  window-scoped `KeyHintTemplate` and would sit outside the ARC-012 XAML scan.
- Adding a coverage exclusion for App files: App is not measured, and `eng/conformance.ps1`
  fails QLT-008 on any exclusion list other than ADR-0008's.
- Generating hints from `KeyboardIntentMapper.BindingsFor`: that table holds static intents, and
  a window action must carry the expected open instance; the palette already solved this with a
  dedicated table and presenter.
- Folding hints by hand in the presenter: would decouple the displayed hints from the key-map data
  (KBD-005); the grouping is declared in the table instead.
- Adding window actions to the general repeat rules: the destructive-repeat rule is key-limited
  and context-free, so adding `r` there would silently stop `Ctrl+R` repeat in the file list.
- Cancelling a pending chord on any non-`Other` key: would let the new keys break the file list's
  `gg` chord although they are unmapped there.
- An `=` alias for enlarge: on a JIS layout it inverts the meaning of the `-` key with Shift.
- An Application-owned minimum size such as 900 × 600: the design brief's 900 px is a responsive
  breakpoint below which the shell may stack, not a floor, and the presenter already owns the
  drag minimum; two minimums would be two mechanisms for one concern.
- A new success colour key for the planned tone: eight scheme dictionaries would change for one
  label, and `TextPrimaryBrush` already distinguishes a result from the idle secondary tone.
- Leaving `CommanderSession` above the CS-013 limit or waiving it: QLT-010 requires the existing
  violation to be fixed before merge.
- Extracting the palette scope inside this change: removing it alone leaves no room for the mode,
  the extraction must supersede an ADR-0047 sentence and a `COMMAND_MODEL.md` row, and bundling
  it here would make this Issue two work units (GIT-004); ADR-0051 owns the split.
- Persisting the window placement: not requested by Issue #100, changes the settings schema, and
  would need its own restore-on-launch and off-screen policy under ADR-0040.
- Clamping moves to the current display: prevents the most common reason to move a window by
  keyboard, which is to carry it to another monitor.
- Putting window commands into the command palette catalog: ADR-0047 keeps window commands out of
  the first catalog, and a mode command executed from the palette would have to open the mode
  first; a later ADR may add `OpenWindowAdjustment` to the catalog.

## Consequences

The application gains one more transient scope beside the palette, one state owner, one closed
placement model, and one pure planner. `CommanderSession` gains one admission rule, delegation,
and two synchronous members on top of the ADR-0051 split, and its logical line count is measured
in the PR to stay at or below 300. The key map gains one entry in two contexts
and one dedicated table with fourteen entries for nine actions; `KeyboardKey` gains `W`, `M`,
`Plus`, and `Minus` with their labels, `KeyboardInputTranslator` translates `Ctrl+W`,
`KeyBinding` gains the `Ctrl+W` label row, and the chord rule becomes context-aware. The App host
gains the `Windowing` adapter, the inline helper overlay, and `Views/WindowAdjustmentView`.
`docs/GLOSSARY.md` gains window placement, window bounds, adjustment step, work area, and caption
rule. No Domain, Infrastructure.Windows, filesystem, provider, settings, or dependency change
exists, and no native import is added, so no Issue-specific security deep review is required
beyond the scheduled tier; that judgement depends on the rejected `IsWindowArranged` alternative
staying rejected.

Issue #100's own acceptance line for native evidence is met inside this Issue for the current
environment: before merge, the mode is exercised on the real window at the current scale with
`AppWindow` bounds read before and after each action, a screenshot, and the UIA name of the
helper, recorded in the PR. The 100–300 percent, high-contrast, Narrator, and eight-scheme cells
of the helper join the Issue #94 release matrix, which already owns those environments; #100 does
not claim them.

## Migration and removal

Extend `UserIntent` with `OpenWindowAdjustment`; add `WindowAdjustmentSession`,
`WindowAdjustmentState` with `WindowAdjustmentClosed` and `WindowAdjustmentOpen`,
`WindowAdjustmentRequest`, `WindowAdjustmentDecision`, `WindowAdjustmentAction`,
`WindowAdjustmentPlan`, `WindowAdjustmentOutcome`, `WindowAdjustmentRefusal`,
`WindowPlacement`, `WindowPresenterState`, `WindowBounds`, and `WindowAdjustmentPlanner` under
Application on top of the ADR-0051 scope owners; add the `WindowAdjustment` keyboard context, `WindowAdjustmentKeyBinding`,
`WindowAdjustmentKeyAction`, the mode branch and context-aware chord rule in `Map`, and the new
keys to Presentation input; add `WindowAdjustmentPresenter` and
`WindowAdjustmentKeyHintPresenter` under Presentation; add `AppWindowPlacementAdapter` under
`App/Windowing`, the helper overlay in `CommanderWindow.xaml`, and `Views/WindowAdjustmentView`;
add the localized labels and key-label resources, the `KEYBOARD_MODEL.md` table, context rule,
chord sentence, and `Escape`-order sentence, the `COMMAND_MODEL.md` registry rows for window
placement translation (`AppWindowPlacementAdapter`), window adjustment decisions
(`WindowAdjustmentSession` with `WindowAdjustmentPlanner`), and the mode's synchronous input
route (`CommanderSession.AdjustWindow` and `LeaveWindowAdjustment`), the glossary terms, the design
handoff, and focused tests, all in one change. No old path exists to remove; the OS keeps every
behavior it had.

## Executable proof

The first implementation step is a runtime spike, before any other code: a focusable control
without text, focused programmatically inside the real `CommanderWindow`, must deliver
`CharacterReceived` and `PreviewKeyDown` to the root input surface for `h`, `+`, and `Ctrl+W`,
and one `AppWindow.MoveAndResize` by 32 pixels at 100 percent must move the window by exactly 32
device pixels while `XamlRoot.RasterizationScale` reads 1.0; and `PreferredMinimumWidth` and
`PreferredMinimumHeight` must read zero while nothing has declared them. If any of those fails,
implementation stops and this ADR is revised; no fallback is pre-approved, because a virtual-key
fallback for letters would contradict KBD-003 and a non-zero undeclared minimum would change
what `AtMinimumSize` means.

Application tests prove open admission and rejection for each blocking state, full freeze
including `NavigateAsync` while open, stale expected-state no-ops for both synchronous members,
that a completed background read does not end the scope, idempotent `m` and `r` with their
refusals, refusal of every planner action in `Maximized`, `Minimized`, `NotOverlapped`, and
`Unavailable`, that leaving succeeds in every placement state, and the exact plan returned for
each action. Planner tests prove the 32-DIP step at 100, 125, 150, 175, 200, and 300 percent and
the rounding rule at one non-standard scale, the caption rule at every desktop edge, across a
horizontal seam, across a vertical seam, and for a window narrower than one step, top-left
anchored enlarge and shrink, `AtMaximumSize` for a window larger than its work area, and
`AtMinimumSize` against both a declared preferred minimum and the one-step floor. Adversarial
tests prove that a window request cannot start, cancel, or confirm a file operation, that a stale
request from an earlier mode instance returns a no-op decision, and that the outcome and
refusal models are exhaustive, and that both synchronous members touch no `CommanderSession` or
pane state while a pane read is pending. The PR records the logical line count of
`CommanderSession` after the change.

Presentation tests prove `Ctrl+W` only in `FileList` and `NavigationSurface` on both translation
routes, chord preservation when `m`, `+`, or `-` follows a pending `g` in the file list, chord
cancellation when `Ctrl+W` does, the dedicated table's exact key set, repeat acceptance for move
and size and consumption for the others, `KeyboardConsumed` for every undeclared key including
`Other`, grouped hint generation from the same table, and the presenter's outcome labels. The
architecture conformance scan proves that `Microsoft.WindowsAppSDK` is still referenced only by
App, that no native import was added, and that the coverage exclusion list is unchanged. The
final integration candidate records the runtime evidence named in Consequences and passes the
canonical gate at Draft-to-Ready.
