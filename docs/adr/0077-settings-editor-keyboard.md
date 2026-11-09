# ADR-0077: Move through the settings editor with the pane movement keys

Status: accepted

Date: 2026-11-09

Accepted for Issue #364. hide asked on 2026-11-09 to operate the settings editor with `j`/`k` and
with the arrow keys, and chose that movement only moves focus while `Space` or `Enter` selects.

## Context

ADR-0040 opens the settings editor as session-owned modal state that freezes both panes, accepts
its typed selector intents and closes on `Escape`. Its keyboard context was the shared `Modal`
context, which declares only `Enter` and `Escape`; every other key reached the native controls.
Users could therefore move with `Tab` and change a scheme with the native radio arrows, which
selected and saved each scheme they passed, but `j`/`k` did nothing although the file list, the
Locations picker and the keyboard help all move with them.

## Decision

- **A dedicated `Settings` keyboard context.** While the settings overlay is visible the host
  reports `KeyboardContext.Settings` instead of `Modal`. Its canonical declarations are `j`/`Down`
  → `MoveNext`, `k`/`Up` → `MovePrevious`, `Space`/`Enter` → `Confirm`, and `Escape` → `Escape`.
  A repeated `Space` or `Enter` is consumed so a held key changes a setting at most once; movement
  repeats. Undeclared keys, including `Tab` and modified letters, pass through so the native
  controls keep their own behavior. The confirmation and conflict modals keep `Modal` unchanged.
- **Movement moves focus only.** The editor's items in display order are the hidden-at-launch
  check box, each scheme option in declaration order, and the close button.
  `SettingsFocusOrder` decides the next index; movement stops at both ends and never wraps, and an
  unfocused editor starts at the first item. Because the window maps keys in `PreviewKeyDown`,
  the native radio arrows no longer select while moving.
- **Activation reuses the existing routes.** `Confirm` toggles the focused check box, selects the
  focused scheme or invokes the close button. Each change raises the existing control handlers,
  which forward `SelectLaunchHiddenItemVisibility`, `SelectColorScheme` or `Escape` to the settings
  session; no second save path is added and saving still happens on selection.
- **Discoverability.** The `F1` keyboard help (ADR-0076) shows the settings context as its own group.

## Rejected alternatives

- **Keep the native radio arrows.** Passing over a scheme would apply and save it, which hide
  rejected.
- **Map `j`/`k` in the shared `Modal` context.** Confirmation modals would gain movement keys they
  do not own and `Enter` there must stay deferred to the native control.
- **Synthesize native key events.** The canonical key map must own every displayed binding
  (KBD-005); translating `j` into a fake arrow would hide the binding from it.

## Consequences

- One new keyboard context with seven declarations, one Presentation focus-order function and one
  App view that applies focus and activation. The settings session, its intents and its save rules
  are unchanged.
- The native radio arrow behavior inside the settings editor is replaced by focus movement.

## Migration and removal

No path is removed. The window no longer defers `Enter` to native controls while settings is open,
because the settings context now owns `Enter`.

## Executable proof

Mapper tests for the settings declarations, repeats and pass-through; focus-order tests for both
ends, an unfocused start and an empty editor; the declaration-to-intent test and binding counts
include the new context; the keyboard help conformance tests include the settings group; hide's
on-screen confirmation of movement and activation.
