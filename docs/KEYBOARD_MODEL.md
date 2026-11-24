# Keyboard Model

Status: normative

Keyboard input is translated only by `KeyboardIntentMapper`. Arrow and function-key aliases are entries in that mapper, not separate command implementations.

The App registers its single preview-key and character handlers on the window's root
content. Clicking blank pane space can leave focus on the framework root scroll host,
outside the inner pane surface; that focus still uses the same input route and current
session-owned active pane. Reactivation alone does not change that pane. An admitted
intentional row/header/list-background gesture activates its addressed pane through
ADR-0090; stale, busy and unrelated scroll-control gestures do not.
Before translating a file-list preview key, focus on a scroll host is returned through
the existing active-file-list helper so the following character reaches the same owner.
Text/IME, modal, repeat and shutdown guards continue to apply before dispatch.

## Normal-mode movement

| Input | Intent |
|---|---|
| `j` or `Down` | focus next visible item; from the last, cycle to the first |
| `k` or `Up` | focus previous visible item; from the first, cycle to the last |
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
| `Ctrl+F5` | sort the active pane by size, unknown sizes last; again reverses the direction, from another key starts ascending |
| `Ctrl+F6` | sort the active pane by modification time, unknown times last; again reverses the direction, from another key starts ascending |
| `Escape` | cancel a running file operation, then abandon the active pane's loading read, then cancel pending chord, then close transient UI, then clear selection |

Single-step movement cycles within the active pane's current visible projected order (hide, 2026-11-22;
Issue #524). An empty visible set stays unfocused and one visible item keeps its focus. Cycling preserves
explicit selection and the passive pane. Half-page movement still stops at either end, even when its
step is one row; first/last targeting is unchanged. The Locations, Settings and Bookmark contexts keep
their own declared movement behavior.

`Ctrl+F3` and `Ctrl+F4` are declared for the file list and the navigation surface (ADR-0053). Plain
`F3` and `F4` are unassigned and pass through. `Ctrl+F5` and `Ctrl+F6` are declared for the same two contexts
(ADR-0056); plain `F5` and `F6` keep copy and move in the file list and `F5` keeps refresh on the
navigation surface, and the destructive-command repeat guard on `F5` and `F6` also ignores a held
`Ctrl+F5` or `Ctrl+F6`. Directories always precede files; the order keeps the
focus item and selection, and it is carried through refresh, reads, and history of the same pane.
Each accepted sort change also saves that pane's key and direction through the existing settings
queue for the next launch (ADR-0067); a refused intent or navigation does not write settings.
Save failure keeps the current order and uses the existing settings warning. `Ctrl+H` remains
session-only.

Back and Forward retain at most 100 successful locations per pane, including current. They restore
location only and commit their cursor after the target read succeeds. Refresh, same-location,
failed, cancelled, superseded, and stale reads do not change history.

A successful refresh preserves the current focus identity when it remains. If it was removed,
focus moves to the next identity from the previous visible order that is still visible, then
backward to the previous survivor. If none survives, the first new visible item is used; an empty
visible set has no focus. This applies equally to explicit refresh and the existing post-operation
refresh, including move/deletion and partial or cancelled outcomes. The new listing owns the
chosen path. Explicit creation/rename targets retain their own preferred-focus policy, and a
normal navigation still starts at its declared target or first item.

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
| `Ctrl+F7` | begin empty text-file creation in the captured active location, from FileList or NavigationSurface |
| `F8` | request deletion under the provider's declared delete policy |
| `Ctrl+L` | open the active pane path-input modal and select its current location |
| `Ctrl+R` or `F5` with no file-command context | refresh through an explicit context decision; plain `F5` always means copy in the file list |
| `Ctrl+,` | open the session-owned settings editor from the file list or navigation surface |
| `Ctrl+P` | open the session-owned command palette from the file list or navigation surface |
| `Ctrl+B` | open the session-owned bookmark manager from the file list or navigation surface |
| `Ctrl+W` | open the session-owned window adjustment mode from the file list or navigation surface |
| `Ctrl+G` | open the session-owned Locations picker of drives and WSL distribution roots from the file list or navigation surface |
| `Ctrl+1` through `Ctrl+9` | navigate the active pane to the bookmark assigned to that fixed slot; an unassigned slot performs no read |
| `F1` | open the session-owned keyboard help from the file list or navigation surface; a held `F1` opens it once |

`F5` is never inferred from timing. The focused control context is an explicit mapper input.

The two pane paths are always display-only `TextBlock` controls. `Ctrl+L` opens one path-input
modal for the active pane through the existing address owner (ADR-0044, Issue #495). A click on
a displayed path does not open editing. The open modal owns `AddressEntry`; native Tab/Shift+Tab
cycle within its input controls, while noninteractive key hints are excluded from the tab order.
Repeating `Ctrl+L` selects the current native text without replacing it. Enter submits the exact
text to the existing parser/navigation route; invalid input remains in the modal with its error.
Escape or an outside scrim tap cancels the captured state and returns focus to its file list.
Native focus changes and temporary window deactivation preserve the draft. IME composition keeps
its Enter/Escape until composition ends. Startup and leaving the bookmark manager still restore
the active file list; other text editors and modal owners retain their existing precedence.

The command palette also opens Bookmarks and window adjustment (ADR-0068), using the same owners
as `Ctrl+B` and `Ctrl+W`. Its existing twenty entries keep their order; Bookmarks and Window
adjustment follow them, and Help follows Window adjustment (ADR-0076). A qualified selection closes
the palette before opening the chosen scope.
Direct shortcuts remain frozen while the palette owns input, and bookmark slots are not candidates.

## Window adjustment mode

`Ctrl+W` opens one persistent window adjustment mode (ADR-0050). It opens only when settings, bookmarks, the Locations picker, address editing, and the command palette are closed, no file operation is running or awaiting confirmation, name, or conflict, and no pane read or launch is in flight. While it is open the host's keyboard context is `WindowAdjustment`, which the mode owns through its own dedicated table rather than through `KeyboardIntentMapper.BindingsFor`:

| Input | Action |
|---|---|
| `h` or `Left` | move the window left by one adjustment step |
| `j` or `Down` | move the window down by one adjustment step |
| `k` or `Up` | move the window up by one adjustment step |
| `l` or `Right` | move the window right by one adjustment step |
| `p` or `Ctrl+Left` | move the visible normal window to the previous geometric display (ADR-0092) |
| `n` or `Ctrl+Right` | move the visible normal window to the next geometric display (ADR-0092) |
| `PageUp` / `PageDown` | scroll the mode explanation by half its visible height, clamped at both ends; does not move the window or leave the mode |
| `+` | enlarge the window by one adjustment step in width and height, anchored at its top-left corner |
| `-` | shrink the window by one adjustment step in width and height, anchored at its top-left corner |
| `m` | maximize the window |
| `r` | restore the window to its normal placement |
| `Escape` or `Ctrl+W` | leave the mode and return focus to the file list of the pane active at entry |

Letters and `+` and `-` are produced characters with Control and Alt absent (KBD-003); `=` is not an alias. Auto-repeat is accepted for move, enlarge, and shrink; a repeated display-transfer key, `m`, `r`, `Escape`, or `Ctrl+W` is consumed without an action. Accepted display-transfer presses remain guarded across input-owner changes until the next initial event for that key. In the `WindowAdjustment` context a declared key yields its action; an event the translator does not identify, which includes the raw virtual-key event that precedes a produced character, passes through to the mode's focus sink, which handles nothing; and every identified key the mode does not declare, and every declared key under an undeclared modifier, is consumed, so no pane, editor, or native control receives it. `Tab` is consumed, so focus cannot leave the mode. The helper's hints are generated from the same table, one hint per group. Previous/next-display groups each show the letter and control-arrow cap; ordinary movement arrow aliases and `Ctrl+W` show no additional cap.

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

## Settings editor

`Ctrl+,` opens the session-owned settings editor (ADR-0040). While it is open the host's keyboard
context is `Settings` (ADR-0077), declared in the canonical table of `KeyboardIntentMapper`:

| Input | Intent |
|---|---|
| `j` or `Down` | focus the next item; the focus stops at the last item |
| `k` or `Up` | focus the previous item; the focus stops at the first item |
| `Space` or `Enter` | toggle the focused check box, select the focused language or color scheme, or close |
| `Escape` | close the editor |

The items are, in order, the hidden-at-launch check box, the footer-key-hint check box, the four
language options (System, Japanese, English, Simplified Chinese), the theme search field, visible theme choices, and the close button. The footer preference (ADR-0082) changes immediately and saves through
the same settings queue. Turning it off retains F1 and confirmation/name-entry Enter/Escape hints;
all commands, warnings, results, progress and other surfaces keep their existing behavior.
Movement only moves focus: passing over any radio option neither applies nor saves it. `Space` or
`Enter` selects through the existing Checked forwarder. Language and scheme use separate native
radio groups. The initial focus remains Close. Language choices save through the same queue and
apply only at the next launch; the current process retains its language (ADR-0086). A repeated `Space` or `Enter` is consumed so a
held key changes a setting at most once. Undeclared keys, including `Tab` and `Shift+Tab`, reach
the native controls.

## Bookmark manager

`Ctrl+B` opens the session-owned bookmark manager (ADR-0041). While its browse state is shown the
host's keyboard context is `Bookmarks`, or `BookmarkSearch` while the search field holds focus
(ADR-0080). Drafts use `BookmarkDraft`. All three are declared in the bookmark table of the canonical key map
(`BookmarkManagerKeyMap`, published through `KeyboardIntentMapper.BookmarkManagerBindings`). The
browse state has two focus regions, the category column and the bookmark list; the list holds
focus when the manager opens or returns to browsing.

| Input | Action in the bookmark list | Action in the category column |
|---|---|---|
| `j` or `Down` | select the next bookmark; stops at the last | select the next category, which filters the list; stops at the last |
| `k` or `Up` | select the previous bookmark; stops at the first | select the previous category, which filters the list; stops at the first |
| `Enter` | navigate to the selected bookmark (the existing manager navigation) | move focus to the bookmark list |
| `Tab` or `Shift+Tab` | move focus to the category column | move focus to the bookmark list |
| `a` | register the current folder (the existing add draft) | add a category (the existing category draft) |
| `e` | edit the selected bookmark | rename the selected user category |
| `d` | ask to delete the selected bookmark through its confirmation | ask to delete the selected user category through its confirmation |
| `/` | move focus to the search field | move focus to the search field |
| `Escape` | close the manager through the existing `Escape` route | close the manager through the existing `Escape` route |

In `BookmarkSearch`, `Escape`, `Down` and `Enter` return focus to the bookmark list without closing
the manager; every other key, including printable letters and `j`/`k`, reaches the search field
(KBD-001), and no key is mapped while an input method is composing. Letters and `/` are produced
characters with Control and Alt absent (KBD-003); a declared key under a modifier, and every
undeclared key, pass through as in the settings context. Movement repeats; a held `Enter`, `Tab`,
`a`, `e`, `d`, `/` or `Escape` acts once and its repeats are consumed, so a held `Escape` that left
the search field does not also close the manager. The mapper's existing palette execution repeat
guard also retains each accepted non-repeating bookmark press across input-owner changes: the
opening `/`, `a` or `e` cannot become text in the search or draft, and navigation's `Enter` cannot
open a file after the manager closes. Only a new initial event for the same identified key releases
its guard; other keys and raw `Other` events do not. IME composition stays outside action mapping, and
native composition or an open category/slot selector retains an `Enter` or `Escape` press in the
same guard until its next initial event. Finishing composition or closing a selector therefore
cannot turn a held key into save, cancel or navigation. All four bookmark text fields use this
ownership boundary; an unfocused field cannot block the current owner.
ordinary text presses and repeating movement acquire no such guard. An `e` or `d` with nothing selected, and an `e`
or `d` on All bookmarks or Uncategorized, are consumed and do nothing, exactly where the removed
buttons were disabled. Moving in the category column only changes the displayed filter and saves
nothing. Neither `d` deletes at once: the bookmark-delete confirmation starts on Cancel, so an
`Enter` right after `d` deletes nothing, and `Escape` or a scrim tap returns to browsing.
`BookmarkDraft` owns `Ctrl+Enter` to submit the existing `BookmarkEditorAction.Save`, and
unmodified `Escape` to cancel through the existing route. Ordinary `Enter`, arrows, text and
Tab remain native control input. An open ComboBox keeps its keys, including `Escape` to close
the popup; a fresh `Escape` after it closes cancels the draft. A held save cannot reach the list
that replaces the draft. Both delete confirmations and failed navigation retain `Modal`;
confirmation starts on Cancel and receives no draft-save binding.

The panel cycles native Tab/Shift+Tab inside its visible controls. Browsing still uses the two
region switch above. Entering a list preserves its visible selected row, or selects the first
row through the existing Select action. A zero-row list receives focus itself; if the framework
cannot focus it, search receives focus. Selecting a row saves nothing. Search Enter/Down/Escape
keeps the query and enters the list; a second, fresh Enter opens the selected result. Search
has its own three-key hint, and browse Tab hints name the destination.

Successful bookmark save clears the query, preserves All or selects the saved entry's category,
and selects the saved entry for continued editing/navigation. The status explains this query
reset; the separate persistence status still reports disk saving/failure. Cancel preserves the
return context. Registration at a drive root offers its canonical path as the initial name;
initial name text is selected so typing replaces it. Slot choices show their current assignment
without changing collision validation. Tapping the scrim takes the Escape route once.

The browse view owns one current deferred focus request. Incidental layout focus cancels passive
row restoration but retains an explicit state-transition request. Explicit search, pointer input,
initial native Tab, another requested region, or leaving browsing supersedes an earlier request.
Actual input passed to a focused bookmark text field and IME composition start cancel both browse
and detail requests, so queued focus or SelectAll cannot overwrite that editor's newer ownership.
Raw repeated character keys await the produced character's existing repeat-guard decision;
mapped commands and pending-confirmation Enter do not surrender their focus request. A callback
may focus its control only while that request remains current.

## Keyboard help

`F1` opens one session-owned keyboard help (ADR-0076) under the same admission as the window
adjustment mode. It lists the bindings of the file list, window adjustment mode, Locations picker,
settings editor, bookmark manager, command palette and the help itself, generated from the canonical key map; aliases of one action
share a row, `gg` is shown before `G`, and the nine bookmark slots share one row. The panes stay
visible behind a transparent scrim and are frozen. While it is open the host's keyboard context is
`KeyboardHelp`, declared in the canonical table of `KeyboardIntentMapper`:

| Input | Intent |
|---|---|
| `j` or `Down` | scroll the help down by one row |
| `k` or `Up` | scroll the help up by one row |
| `PageDown` or `Ctrl+D` | scroll the help down by half its visible height |
| `PageUp` or `Ctrl+U` | scroll the help up by half its visible height |
| `Escape` or `F1` | close the help and return focus to the file list of the pane active at entry |

Letters are produced characters with Control and Alt absent (KBD-003). The raw virtual-key event that
precedes a produced character passes through; every other identified key, every declared key under an
undeclared modifier, and a repeated `Escape` or `F1` are consumed, so the frozen panes receive nothing
and a held key cannot close and reopen the help. Tapping the scrim closes it like `Escape`.

## Context precedence

### KBD-001 — Text entry owns printable keys

- Status: **active**
- Enforcement: mapper tests.

When focus is inside an address, rename, search, settings, or dialog text editor, printable keys and editing chords pass to that editor. Vim movement does not run. `Escape` exits or cancels the editor according to its explicit editing state.

### KBD-002 — Modal UI owns its declared keys

- Status: **active**
- Enforcement: mapper tests.

A modal confirmation, conflict resolver, or settings editor receives only its documented keys. Destructive confirmation cannot be bypassed by the underlying file-list key map. The permanent-deletion confirmation owns a new initial `Enter` press (confirm) and `Escape` (cancel); a repeated `Enter` from the physical press that opened the modal is consumed before native control invocation and emits no intent. Every other key passes through and the file list stays frozen. The directory-name entry opened by `F7` owns the same two keys; every other key reaches the name editor, the file list stays frozen, and the host attaches the editor text to the confirmation as one typed name submission that the session validates. The text-file name entry opened by `Ctrl+F7` is that same modal, with a localized editable `.txt` suggestion selected in full (ADR-0085); the submitted name is used unchanged and a held opening key does not reopen it. The rename name entry opened by `F2` is the same modal and owns the same two keys; it differs only in the frozen subject and in starting the editor with the focus item's current name. The settings editor owns the keys of its own `Settings` context (ADR-0077): `Escape` closes it, it saves each selection immediately, and it never rolls a saved or pending selection back when it closes.

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

No context may map one keystroke to multiple intents, and no view may add a private binding. All displayed shortcut hints are generated from the canonical key-map data: `KeyboardIntentMapper` declares every binding in one table, maps from that table, and publishes it per context through `KeyboardIntentMapper.BindingsFor`, from which `KeyHintPresenter` projects the ordered key hints a surface shows. The command palette, the window adjustment mode and the bookmark manager own dedicated tables of Presentation actions inside the same key map; the bookmark table lives in the internal `BookmarkManagerKeyMap`, which only the mapper consults and which is published only through `KeyboardIntentMapper.BookmarkManagerBindings` (ADR-0080). Hint wording, both the key cap and the intent, comes from localization resources.

The theme picker reuses the existing `CommandPalette` context and its sole action
table (ADR-0087, November23 revision). Settings' current-theme button and the
palette's searchable `:colorscheme` entry open the same settings-owned scope.
Up/Down move the candidate and preview only; Enter or a candidate click accepts
through the settings writer and closes. Escape/scrim cancel and restore the latest
accepted palette. Enter/Escape repeats are retained by the shared repeat guard
across owner changes, so a held key cannot accept again or close the parent.
Native Tab/Shift+Tab cycle inside the picker, including its Cancel button. On that
button Enter cancels. Text, editing shortcuts and IME remain native; composing
Enter/Escape do not reach the picker. The sole virtualized candidate list scrolls,
while search, accepted-name text and controls stay fixed. Candidate selection is
preview, with accepted state named separately. Zero results restore accepted
colors without saving. Progress renders retain query, rows, candidate and scroll.
Theme hints derive their actual keys from `CommandPaletteBindings`.

## Accessibility

Every Vim movement has a standard Windows keyboard alternative. Tab order, focus visuals, automation names, high-contrast resource use, and keyboard-only dialog completion are implementation requirements proven by mapper and presentation tests; their on-screen confirmation follows ADR-0054.

## Pointer selection and command-entry focus (ADR-0090)

Qualified plain row clicks replace explicit selection and set focus/anchor; Ctrl
click toggles one entry and sets focus/anchor. Shift replaces the inclusive visible
range and Ctrl+Shift adds it without duplicates. A missing anchor is established
from pre-gesture focus (or target) on the first range gesture and persists for later
ranges. Space sets that same anchor; movement preserves it; Escape/blank clearing
removes it. Sort retains it, visibility/refresh recover its owned visible identity,
and successful navigation clears it. Header activation preserves marks. ScrollBar/
Thumb and interactive descendants are not blank-list targets. Terminal read/launch
indications survive a pointer selection; Loading/Launching refuse it.

A first Tapped must actually change the Application snapshot captured immediately
inside admitted work before HandleAsync. A stale request returning a newer state
with the same focused row is not acceptance. DoubleTapped requires the accepted
first side/listing/entry, the current snapshot and visible entry, and consumes the
candidate once. Other intents/owners/work/listings invalidate it. No click timer,
framework selection owner or direct Shell route is added.

Unmodified initial F10 from ordinary pane input enters the Bookmarks/Settings/Help
button group. CommandEntry context exists only while those native controls own
focus and loses precedence to every existing modal/editor/window scope. Native
Tab/Shift+Tab cycles the three buttons; Enter/Space invokes their existing commands.
File-list commands do not map there. F10/Escape returns to the active pane. The
canonical mapper supplies focus actions and hint declarations. Its existing repeat
guard holds F10/Escape and native Enter/Space across context changes, so a held key
cannot reenter, clear pane marks or immediately invoke a newly opened surface.
Queued/reactivation focus restoration rechecks context and leaves the group alone.
An admitted synchronous row/header/background click returns to its explicit pane
through the existing focus helper after rendering. The receipt requires the exact
before/request owner, a new target snapshot, the matching active side and available
input ownership; rejected or asynchronous Open results cannot request this return.
The host checks stopping before consulting native context and rechecks current
ownership before applying focus. Header and absent-content receipts preserve their
content/activity/model identities while replacing the snapshot wrapper.

Only raw F10 adds explicit Shift distinction: Shift+F10 opens context under ADR-0091,
and Shift with Control/Alt remains an undeclared combination. Other
virtual keys and produced characters keep their current Control/Alt mapping,
including uppercase G, layout-produced symbols, IME and dead-key exclusions.
This contract is under implementation; technical design acceptance does not claim
native event-order, physical-key or visible-layout acceptance.

## Context-menu input (ADR-0091)

Initial Shift+F10 or Apps from FileList preserves the active pane and marks, opening
item context when focus exists or folder context for an empty listed folder. Bare
F10 and file-list Tab retain command-entry/pane-switch semantics. Positionless native
ContextRequested events never reopen the context after this mapped key. Pointer
requests use exact rendered row/background/header ownership and invalidate double tap.

ContextMenu is a native-control input context over the existing command selection
owner. Up/Down and submenu Left/Right remain native. Enter/Space and Escape pass only
the initial event; the shared repeat guard consumes held repeats across submenu,
root, replacement owner and returned pane. The first submenu Escape collapses only
that submenu, and a fresh Escape dismisses root. No ordinary pane intent is mapped
while ContextMenu owns input, and no extra popup/root key registration exists.
Text/IME ownership and stopping keep precedence. Shift+F10/Apps captions are generated
from canonical declarations; Reveal has no invented shortcut and renders an empty cap.

Native Closed defers exact-owner cancellation until the current invocation turn ends.
Click/Invoke may consume that same owner first; genuinely late invocation is rejected.
Replacing, submitting, stopping or showing another surface detaches old callbacks;
a departing surface cannot take focus from the new owner. Actual event ordering,
physical modifiers/IME, layout/accessibility and visible acceptance retain distinct
proof requirements in ADR-0091 and the feature quality record.
