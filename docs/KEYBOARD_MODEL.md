# Keyboard Model

Status: normative

Keyboard input is translated only by `KeyboardIntentMapper`. Arrow and function-key aliases are entries in that mapper, not separate command implementations.

## Normal-mode movement

| Input | Intent |
|---|---|
| `j` or `Down` | focus next visible item |
| `k` or `Up` | focus previous visible item |
| `h`, `Backspace`, or `Alt+Up` | navigate to parent |
| `l` or `Enter` | open focused item; enter it when it is a container |
| `g` then `g` | focus first visible item |
| `G` | focus last visible item |
| `Ctrl+D` or `PageDown` | move focus down by half the visible page |
| `Ctrl+U` or `PageUp` | move focus up by half the visible page |
| `Tab` | activate the other pane without changing either pane's focus item |
| `Space` | toggle selection of the focus item without moving focus |
| `Ctrl+H` | toggle hidden and system entries in the active pane |
| `Escape` | cancel a running file operation, then cancel pending chord, then close transient UI, then clear selection |

The `gg` chord expires after 750 ms, measured through the injected monotonic clock. An unrelated mapped second key cancels the pending chord and is then processed normally. An unmapped event, including the raw virtual-key event that precedes a produced character, passes through without touching the chord. Auto-repeat is accepted for single-key movement and ignored for chord prefixes and destructive commands.

## File commands

| Input | Intent |
|---|---|
| `F2` | begin rename of the focus item |
| `F5` | copy selection, or the focus item when selection is empty, to the passive pane |
| `F6` | move selection, or the focus item when selection is empty, to the passive pane |
| `F7` | create a directory in the active pane |
| `F8` | request deletion under the provider's declared delete policy |
| `Ctrl+L` | focus and select the active pane address input |
| `Ctrl+R` or `F5` with no file-command context | refresh through an explicit context decision; plain `F5` always means copy in the file list |
| `Ctrl+,` | open the session-owned settings editor from the file list or navigation surface |

`F5` is never inferred from timing. The focused control context is an explicit mapper input.

## Context precedence

### KBD-001 — Text entry owns printable keys

- Status: **active**
- Enforcement: mapper tests.

When focus is inside an address, rename, search, settings, or dialog text editor, printable keys and editing chords pass to that editor. Vim movement does not run. `Escape` exits or cancels the editor according to its explicit editing state.

### KBD-002 — Modal UI owns its declared keys

- Status: **active**
- Enforcement: mapper tests.

A modal confirmation, conflict resolver, or settings editor receives only its documented keys. Destructive confirmation cannot be bypassed by the underlying file-list key map. The permanent-deletion confirmation owns `Enter` (confirm) and `Escape` (cancel); every other key passes through and the file list stays frozen. The directory-name entry opened by `F7` owns the same two keys; every other key reaches the name editor, the file list stays frozen, and the host attaches the editor text to the confirmation as one typed name submission that the session validates. The rename name entry opened by `F2` is the same modal and owns the same two keys; it differs only in the frozen subject and in starting the editor with the focus item's current name. The settings editor owns `Escape` as close, saves each selection immediately, and never rolls a saved or pending selection back when it closes.

### KBD-003 — Key mapping is layout-safe

- Status: **active**
- Enforcement: mapper tests.

Letter commands use the produced character with explicit modifier state; function and navigation keys use virtual keys. IME composition and dead-key input are never interpreted as commands.

### KBD-004 — Focus and selection are distinct

- Status: **active**
- Enforcement: reducer tests.

Movement changes the focus item and preserves explicit selection. File commands operate on selection when non-empty, otherwise on the focus item. Navigation clears selection only after a successful location change.

### KBD-005 — Every binding has one intent

- Status: **active**
- Enforcement: key-map uniqueness test.

No context may map one keystroke to multiple intents, and no view may add a private binding. All displayed shortcut hints are generated from the canonical key-map data: `KeyboardIntentMapper` declares every binding in one table, maps from that table, and publishes it per context through `KeyboardIntentMapper.BindingsFor`, from which `KeyHintPresenter` projects the ordered key hints a surface shows. Hint wording, both the key cap and the intent, comes from localization resources.

## Accessibility

Every Vim movement has a standard Windows keyboard alternative. Tab order, focus visuals, narrator names, high-contrast behavior, and keyboard-only dialog completion are release-gate requirements.
