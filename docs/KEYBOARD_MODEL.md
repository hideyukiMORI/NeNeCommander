# Keyboard Model

Status: normative

Keyboard input is translated only by `KeyboardIntentMapper`. Arrow and function-key aliases are entries in that mapper, not separate command implementations.

## Normal-mode movement

| Input | Intent |
|---|---|
| `j` or `Down` | focus next visible item |
| `k` or `Up` | focus previous visible item |
| `h`, `Backspace`, or `Alt+Up` | navigate to parent |
| `l` or `Enter` | open the focus item: navigate into a directory or hand a Windows local file to its Shell association |
| `Alt+Left` | navigate to the previous successful location in the active pane |
| `Alt+Right` | navigate to the next successful location in the active pane |
| `g` then `g` | focus first visible item |
| `G` | focus last visible item |
| `Ctrl+D` or `PageDown` | move focus down by half the visible page |
| `Ctrl+U` or `PageUp` | move focus up by half the visible page |
| `Tab` | activate the other pane without changing either pane's focus item |
| `Space` | toggle selection of the focus item without moving focus |
| `Ctrl+H` | toggle hidden and system entries in the active pane |
| `Ctrl+F3` | sort the active pane by name; again reverses the direction, from another key starts ascending |
| `Ctrl+F4` | sort the active pane by extension; again reverses the direction, from another key starts ascending |
| `Escape` | cancel a running file operation, then cancel pending chord, then close transient UI, then clear selection |

`Ctrl+F3` and `Ctrl+F4` are declared for the file list and the navigation surface (ADR-0053). Plain
`F3` and `F4` are unassigned and pass through. Directories always precede files; the order keeps the
focus item and selection, and it is carried through refresh, reads, and history of the same pane.

Back and Forward retain at most 100 successful locations per pane, including current. They restore
location only and commit their cursor after the target read succeeds. Refresh, same-location,
failed, cancelled, superseded, and stale reads do not change history.

A pending focused-file handoff freezes every pane intent and direct navigation or refresh entry
point until its typed outcome arrives. The handoff never changes content, focus, selection, or
history. Windows UNC and WSL files report an unavailable provider without reaching the Shell.

The `gg` chord expires after 750 ms, measured through the injected monotonic clock. Whether a second key cancels the pending chord is decided by whether the current context declares that key with that modifier state: a declared second key cancels the pending chord and is then processed normally. A key the current context does not declare passes through without touching the chord; that includes the raw virtual-key event that precedes a produced character and keys that only another context declares, such as the window adjustment mode's `m`, `+`, and `-` in the file list. Auto-repeat is accepted for single-key movement and ignored for chord prefixes and destructive commands. When an initial Enter executes a palette candidate, the mapper consumes every repeated Enter from that physical press across later file-list, address-entry, and modal contexts. Other keys and context changes do not release that guard. The next initial Enter releases it and is mapped normally; an ordinary file-list Enter repeat remains unchanged when no palette guard is active.

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
| `Ctrl+P` | open the session-owned command palette from the file list or navigation surface |
| `Ctrl+B` | open the session-owned bookmark manager from the file list or navigation surface |
| `Ctrl+W` | open the session-owned window adjustment mode from the file list or navigation surface |
| `Ctrl+G` | open the session-owned Locations picker of drives and WSL distribution roots from the file list or navigation surface |
| `Ctrl+1` through `Ctrl+9` | navigate the active pane to the bookmark assigned to that fixed slot; an unassigned slot performs no read |

`F5` is never inferred from timing. The focused control context is an explicit mapper input.

## Window adjustment mode

`Ctrl+W` opens one persistent window adjustment mode (ADR-0050). It opens only when settings, bookmarks, the Locations picker, address editing, and the command palette are closed, no file operation is running or awaiting confirmation, name, or conflict, and no pane read or launch is in flight. While it is open the host's keyboard context is `WindowAdjustment`, which the mode owns through its own dedicated table rather than through `KeyboardIntentMapper.BindingsFor`:

| Input | Action |
|---|---|
| `h` or `Left` | move the window left by one adjustment step |
| `j` or `Down` | move the window down by one adjustment step |
| `k` or `Up` | move the window up by one adjustment step |
| `l` or `Right` | move the window right by one adjustment step |
| `+` | enlarge the window by one adjustment step in width and height, anchored at its top-left corner |
| `-` | shrink the window by one adjustment step in width and height, anchored at its top-left corner |
| `m` | maximize the window |
| `r` | restore the window to its normal placement |
| `Escape` or `Ctrl+W` | leave the mode and return focus to the file list of the pane active at entry |

Letters and `+` and `-` are produced characters with Control and Alt absent (KBD-003); `=` is not an alias. Auto-repeat is accepted for move, enlarge, and shrink; a repeated `m`, `r`, `Escape`, or `Ctrl+W` is consumed without an action. In the `WindowAdjustment` context a declared key yields its action; an event the translator does not identify, which includes the raw virtual-key event that precedes a produced character, passes through to the mode's focus sink, which handles nothing; and every identified key the mode does not declare, and every declared key under an undeclared modifier, is consumed, so no pane, editor, or native control receives it. `Tab` is consumed, so focus cannot leave the mode. The helper's hints are generated from the same table, one hint per group with the arrow aliases and `Ctrl+W` shown under no cap of their own.

`Escape` keeps its order. The mode cannot open while a file operation is running or awaiting a decision, so cancelling a running operation still comes first; inside the mode `Escape` and `Ctrl+W` have one meaning, leaving the mode, and leaving has no revert because every applied step already happened on the desktop.

## Locations picker

`Ctrl+G` opens one session-owned Locations picker (ADR-0055) under the same admission as the bookmark manager: settings, bookmarks, address editing, the command palette, and the window adjustment mode are closed, no file operation is running or awaiting confirmation, name, or conflict, and no pane read or launch is in flight. While it is loading or open the host's keyboard context is `Locations`, declared in the canonical table of `KeyboardIntentMapper`:

| Input | Intent |
|---|---|
| `j` or `Down` | focus the next entry; the focus stops at the last entry |
| `k` or `Up` | focus the previous entry; the focus stops at the first entry |
| `Enter` | navigate the pane active at open to the focused root through the single pane navigation route, closing the picker first |
| `Escape` | close the picker without navigating |

Letters are produced characters with Control and Alt absent (KBD-003). The raw virtual-key event that precedes a produced character passes through; every other identified key, every declared key under an undeclared modifier, and a repeated `Enter` are consumed, so no pane, editor, or native control receives them and a held `Enter` selects at most once. While both sections are loading every intent is frozen and nothing is routed. Plain `g` and `G` keep their file-list meaning, and `Ctrl+G` cancels a pending `gg` chord like any other declared key.

## Context precedence

### KBD-001 — Text entry owns printable keys

- Status: **active**
- Enforcement: mapper tests.

When focus is inside an address, rename, search, settings, or dialog text editor, printable keys and editing chords pass to that editor. Vim movement does not run. `Escape` exits or cancels the editor according to its explicit editing state.

### KBD-002 — Modal UI owns its declared keys

- Status: **active**
- Enforcement: mapper tests.

A modal confirmation, conflict resolver, or settings editor receives only its documented keys. Destructive confirmation cannot be bypassed by the underlying file-list key map. The permanent-deletion confirmation owns a new initial `Enter` press (confirm) and `Escape` (cancel); a repeated `Enter` from the physical press that opened the modal is consumed before native control invocation and emits no intent. Every other key passes through and the file list stays frozen. The directory-name entry opened by `F7` owns the same two keys; every other key reaches the name editor, the file list stays frozen, and the host attaches the editor text to the confirmation as one typed name submission that the session validates. The rename name entry opened by `F2` is the same modal and owns the same two keys; it differs only in the frozen subject and in starting the editor with the focus item's current name. The settings editor owns `Escape` as close, saves each selection immediately, and never rolls a saved or pending selection back when it closes.

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

Every Vim movement has a standard Windows keyboard alternative. Tab order, focus visuals, automation names, high-contrast resource use, and keyboard-only dialog completion are implementation requirements proven by mapper and presentation tests; their on-screen confirmation follows ADR-0054.
