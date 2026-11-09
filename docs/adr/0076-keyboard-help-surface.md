# ADR-0076: Show the canonical key map in a session-owned help surface

Status: accepted

Date: 2026-11-09

Accepted for Issue #359. hide asked for an in-app help screen on 2026-11-09, chose `F1` as its key and
selected the centered-panel direction A in the design handoff
[`docs/design/2026-11-09-keyboard-help-handoff.md`](../design/2026-11-09-keyboard-help-handoff.md).

## Context

The product is keyboard-first, but the only in-app view of its bindings is the operation bar's
short hint list and the command palette's twenty-two searchable intents. Pane movement, sorting,
bookmark slots, the `gg` chord, window adjustment keys and Locations picker keys are visible only in
`docs/KEYBOARD_MODEL.md`. hide forgot the bindings after a break and found no in-app help.

KBD-005 already requires every displayed shortcut to come from the canonical key map:
`KeyboardIntentMapper` declares the context bindings (`BindingsFor`), the palette bindings and the
window-adjustment bindings, and `CommandLabelCatalog` owns intent labels. A help screen that kept
its own table would become a second, drifting description of the same bindings.

## Decision

- **`F1` opens one session-owned help surface.** The canonical key map adds one binding,
  `F1` with no modifier, mapped to a new parameterless `UserIntent.OpenHelp` in the `FileList` and
  `NavigationSurface` contexts, the same admission contexts as `Ctrl+P`, `Ctrl+B` and `Ctrl+,`.
  Text entry, a running operation, an awaiting decision and another modal keep precedence; `F1`
  in those contexts passes through unchanged. `F1` is not added to any other context.
- **The surface reuses the existing session-owned transient mechanism.** `CommanderSession` owns
  an open/closed help state alongside the palette, bookmark manager, settings editor, Locations
  picker and window adjustment scope, with the same mutual exclusion: at most one is open, both
  panes are frozen while it is open, and closing restores focus to the file list of the pane that
  was active at entry. No second surface manager, window, or navigation stack is introduced.
- **Content is generated from the canonical declarations only.** `KeyboardHelpPresenter` reads
  `KeyboardIntentMapper.BindingsFor` for the file-list, Locations and help contexts, reuses the
  existing window-adjustment and palette key-hint projections, and takes labels from
  `CommandLabelCatalog`. Groups follow the key map's contexts: file list, window adjustment,
  Locations picker, command palette and the help itself. Aliases of one action share one row. The
  projection adds only two display rules: the stateful `gg` chord, which is not a table binding,
  is shown just before `G`, and the nine bookmark slots share one `Ctrl+1–9` row. The projection
  owns ordering and grouping, not bindings. Conformance tests fail when a declared binding is
  absent from the projection or the projection shows a key the mapper does not perform.
- **Keys inside the surface.** A new `KeyboardHelp` context in the canonical table maps `Escape`
  and `F1` to close, and `Up`/`Down`, `j`/`k`, `PageUp`/`PageDown` and `Ctrl+U`/`Ctrl+D` to the
  existing movement intents, which the view applies as scrolling. Every other identified key is
  consumed so the frozen panes receive nothing, and a repeated `Escape` or `F1` is consumed so a
  held key cannot close and reopen the help; a repeated `F1` in the file list passes through like
  the other single-shot function keys. The surface has no search field, no command execution and
  no tab stops other than its scrolling region. Closing goes through the session's synchronous
  `CloseKeyboardHelp` route with the open state last rendered, so a stale close changes nothing.
- **Pointer.** The surface sits on the same transparent `SmokeFillColorDefaultBrush` scrim as the
  command palette, so the panes stay visible and pointer input cannot reach them. Tapping the
  scrim closes the surface through the same qualified close as `Escape`.
- **Discoverability.** The operation bar's file-list hints gain `F1 ヘルプ` / `F1 Help` through the
  existing `KeyHintPresenter`, and the command palette catalog appends `OpenHelp` after
  `OpenWindowAdjustment` (ADR-0068's ordering rule), so the help can also be found by search.
- **Accessibility and localization.** The surface exposes a stable `KeyboardHelp` automation
  identifier, a localized title, and each row as text the screen reader reads as key then
  action. All labels are resource keys; no literal strings are added to views. Final colors,
  spacing and layout come only from semantic tokens and the approved design handoff.

## Rejected alternatives

- **A hand-written help table or a copy of `KEYBOARD_MODEL.md`.** Two descriptions of one key map
  drift; KBD-005 requires generated hints.
- **Opening the markdown document in a browser or a separate window.** It leaves the keyboard
  session, cannot reflect the running build's bindings, and adds a Shell launch path.
- **`?` as the key.** It is a produced character that depends on the keyboard layout (KBD-003)
  and collides with a likely future backward search. hide chose `F1`.
- **Making the help a filter of the command palette.** The palette lists executable intents with
  availability; most help rows (chords, in-mode keys, aliases) are not executable commands.
- **Allowing command execution from help rows.** That would create a second execution path for
  existing intents; the palette already owns search-and-execute.

## Consequences

- One new intent, one binding per admitted context, one session state and one Presentation
  projection. `KEYBOARD_MODEL.md` gains `F1`, and the operation bar shows one more hint.
- Adding or changing a binding automatically changes the help; the conformance test prevents a
  binding from being silently omitted.
- The surface is read-only and cannot change files, settings, window placement or pane state.
- Narrow windows rely on vertical scrolling; the design handoff decides column layout per width.

## Migration and removal

No existing path is replaced. Implementation lands in one change: intent, bindings, session state,
projection, view, resources, `KEYBOARD_MODEL.md`, the palette catalog append and tests.

## Executable proof

- Mapper tests: `F1` maps to `OpenHelp` only in the admitted contexts and passes through elsewhere.
- Session tests: open/close, mutual exclusion with every existing transient surface, frozen panes,
  focus restoration, stale close rejection.
- Projection conformance: every declared binding of every context appears exactly once per
  intent group, and every shown key exists in the mapper.
- Palette catalog test updated for the appended `OpenHelp`.
- Affected Release build, Presentation and Application tests, and a UI evidence Observe run plus
  hide's on-screen confirmation for the new surface (ADR-0061).
