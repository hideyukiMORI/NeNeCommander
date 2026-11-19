# ADR-0080: Show the bookmark manager on the scrim and drive it with the pane movement keys

Status: accepted

Date: 2026-11-16

Identifier correction (2026-11-18, Issue #445): this bookmark decision was
formerly numbered ADR-0079 on the feature branch. It is ADR-0080 because the
accepted and published main assigns ADR-0079 to the first Preview's timing.
The decision, acceptance date and required manual screen confirmation are unchanged by that correction.

Amended 2026-11-19 for Issue #457 under hide's request for keyboard-only usability, multiple
expert reviews and design consultation with NeNe Commanderリナ. The approved Direction A remains;
the draft-save, native-input ownership, browse-return and focus details below are updated.
This amendment does not claim hide's separate on-screen acceptance of PR402.

Accepted for Issue #398. This ADR amends the overlay layout and the keyboard of the bookmark
manager accepted in ADR-0041, and puts bookmark deletion behind a confirmation. The catalog, its
schema and saving, `Ctrl+B` and `Ctrl+1` to `Ctrl+9` are unchanged. The design handoff is
[`docs/design/2026-11-16-bookmark-manager-handoff.md`](../design/2026-11-16-bookmark-manager-handoff.md).

## Context

The bookmark manager opened as an opaque full-window overlay that hid both panes. Browsing used
eight buttons (move, register, edit, delete, add category, rename category, delete category,
close) and the shared `Modal` keyboard context, which declares only `Enter` and `Escape`; every
other key reached the native controls. The list and the category column moved only with the native
arrows, `j`/`k` did nothing, and register, edit and delete needed the pointer or `Tab` through the
buttons. The settings editor (ADR-0077), the Locations picker (ADR-0055) and the keyboard help
(ADR-0076) already move with `j`/`k` and sit on the transparent scrim.

## Decision

### hide's decisions (2026-11-16)

- **Direction A.** A centered panel on the transparent scrim, the category column on the left,
  the bookmark list on the right and a band of key hints at the bottom.
- **One-letter commands.** `a`, `e` and `d` may be used in the manager.
- **Category editing stays in the manager.** Adding, renaming and deleting categories remain in
  this panel rather than moving to the command palette.
- **Scrim tap closes.** Tapping the scrim outside the panel closes the manager.

### Design decisions (設計リナ, to be adjusted after hide's on-screen check)

- **Overlay.** The overlay uses the same `SmokeFillColorDefaultBrush` scrim as the other overlays
  and keeps the window's outer spacing. The panel is centered, at most
  `DensityBookmarkManagerMaxWidth` (820) by `DensityBookmarkManagerMaxHeight` (540) DIP, and
  shrinks with the window. The panes stay drawn behind it and stay frozen: the scrim takes pointer
  input and the existing modal freeze keeps keyboard input from them.
- **Scrim tap.** A tap on the scrim forwards `UserIntent.Escape` once, the route the `Escape` key
  already takes in the current state: it closes while browsing and cancels a draft, a category
  draft or a deletion confirmation. A tap inside the panel is handled by the panel and does
  nothing. No second way to close is added.
- **Browse layout.** The header holds the title, the shown bookmark count and the search field.
  The category column is the existing `BookmarkCategories` list, and each category shows, in
  monospace secondary text, the number of its bookmarks: every bookmark for All, those without a
  category for Uncategorized, the category's entries otherwise, independent of the search text; the bookmark list shows the slot,
  name and location (monospace) at the file list's row height with the file list's 2 px left mark
  for the selected item, and both lists show the system focus visual on the focused item. The
  bottom band shows the key hints and the save status. The eight browse buttons are removed; each
  hint except movement is a flat button that runs exactly the action of its key in the focused
  region, so pointer-only use keeps a route. The hint buttons are not tab stops and do not take
  focus, like the caption buttons.
- **Bookmark deletion asks first.** hide approved the canvas proposal "`d` 削除（確認あり）", so
  deleting a bookmark opens a session-owned `BookmarkDeleteConfirmation` in the same panel, shaped
  like the category-delete confirmation: a title, the bookmark's name and location, and Delete
  and Cancel buttons. `BookmarkEditorAction.BeginDeleteBookmark` replaces the former immediate
  `DeleteBookmark` and validates the displayed entry; `ConfirmDeleteBookmark` takes the existing
  catalog deletion and save path once and returns to browsing with focus on the list. Until then
  the catalog is unchanged and nothing is saved; an entry changed meanwhile is rejected as stale.
  The question uses the shared `Modal` context and starts on Cancel, so `Enter` right after
  `d` deletes nothing; `Escape` and a scrim tap cancel it back to browsing without closing.
- **Forms keep native input.** Bookmark/category drafts use `BookmarkDraft`: Ctrl+Enter saves
  through the existing Save action and Escape cancels. Ordinary Enter, arrows, text and Tab stay
  native. Both delete confirmations and failed navigation keep `Modal`; no save binding applies
  there. The panel cycles native Tab/Shift+Tab. All draft and confirmation bodies can scroll
  vertically, while their action buttons stay in the footer.
- **Three bookmark keyboard contexts.** While browsing, the host reports `Bookmarks`, or `BookmarkSearch`
  while the search field holds focus. Their keys are one dedicated table of Presentation actions
  (`BookmarkManagerKeyAction`), like the command palette and window adjustment tables, held in the
  internal `BookmarkManagerKeyMap` that only `KeyboardIntentMapper.Map` consults and that is
  published only through `KeyboardIntentMapper.BookmarkManagerBindings`. The table is outside the
  mapper's own source only to keep the mapper within the CS-013 type-size limit; it is still the
  one declaration of these keys.
  - Two focus regions, the category column and the bookmark list; the list holds focus when the
    manager opens and whenever a substate returns to browsing.
  - `j`/`Down` and `k`/`Up` move the selection inside the focused region, stop at both ends and
    repeat when held. Moving in the category column selects that filter through the existing
    `Filter` action, as a click does. It changes only what the list shows and saves nothing, so it
    does not conflict with ADR-0077's rule that movement does not save.
  - `Tab` switches between the two regions. The translator does not report Shift, so `Shift+Tab`
    arrives as `Tab`; with two regions both directions are the same move.
  - `Enter` in the list navigates to the selected bookmark through the existing manager
    navigation; in the category column it moves focus to the list.
  - `a`, `e` and `d` act on the focused region: in the list they register the current folder,
    edit and delete the selected bookmark; in the category column they add a category, rename and
    delete the selected user category. Each submits the action of the button it replaces, and is
    consumed and does nothing exactly where that button was disabled (no selected bookmark, or All
    or Uncategorized selected).
  - `/` moves focus to the search field and selects its text. In `BookmarkSearch` every key except
    `Escape`, `Down` and `Enter` reaches the field, and those three return focus to the list;
    there `Escape` does not close the manager. The query stays, the visible selection or first
    result receives focus, and only a new Enter opens it. Search shows only these three hints.
    While an input method composes in any bookmark text field no key is mapped.
  - `Escape` in `Bookmarks` closes through the existing `Escape` route.
  - A held `Enter`, `Tab`, `a`, `e`, `d`, `/` or `Escape` acts once; its repeats are consumed.
    Undeclared key/modifier combinations pass through, as in the `Settings` context. Ctrl+Enter
    is declared only for bookmark/category drafts and shown on their Save buttons and in F1 help.
    Issue #430 restores this contract across ownership changes: the mapper's existing palette
    execution repeat guard also retains accepted non-repeating bookmark presses until the next
    initial event for the same identified key. Search and drafts cannot receive the opening
    character's repeats, and completed navigation cannot leak a held `Enter` into `OpenFocused`.
    Other keys and raw `Other` events do not release the guard. Issue #457 also retains native
    Enter/Escape ownership while IME composition or an open ComboBox bypasses action mapping.
    When composition ends or the popup closes, repeats cannot save/cancel/activate the next
    owner; ordinary text input and movement retain their rules.
- **Decisions live in Presentation.** `BookmarkManagerKeyboard` decides the owned context, what
  each action does in each region and where movement lands; `BookmarkManagerKeyHintPresenter`
  projects the band and the help rows. The App's `BookmarkKeyboardView` only remembers the focused
  region and applies the resolved `BookmarkManagerCommand`.
  The browse view owns its deferred focus request through the internal, framework-free
  `BookmarkFocusRestoration`. A later search focus, native region focus, requested region or exit
  from browsing invalidates the older request before its queued callback may apply focus. The
  view still supplies scheduling and native controls; no session or asynchronous-work owner moves.
  First-row selection is deferred with focus so it submits through Select after the command
  that opened the manager releases the work owner. The former ListView Enter handler is removed;
  modifier-aware mapping is the only keyboard navigation path. Double-tap retains its pointer path.
- **Selection and focus.** The focused region uses existing active-pane markers and an outline;
  the other region retains its selected item with passive-pane markers. Search leaves both
  passive. Empty lists retain the region outline. On successful bookmark save, the query is
  cleared, All is preserved or the saved category is selected, and the saved entry is selected.
  A status notice explains the query reset independently of persistence results. Cancel retains
  its return context. A drive root defaults its name to the canonical path; names select all on
  entry. Slot choices show existing assignments, and duplicate-slot validation stays unchanged.
- **Key band.** The band is generated from the bookmark table for the focused region: movement
  (`j`/`k`), `Enter`, `/`, `Tab`, `a`, `e`, `d` and `Esc`, with region-specific wording from
  resources.
- **Keyboard help.** `F1` gains a "ブックマーク（Ctrl+B）" / "Bookmarks (Ctrl+B)" group generated from
  the same table, one row per action.
- **Automation.** The panel keeps `BookmarkManagerModal` and gains a localized name so its peer is
  exposed. Row and category containers receive `BookmarkRow_<name>` and `BookmarkCategory_<name>`
  identifiers and their names through `RowAutomationIdentity`. The band is `BookmarkKeyHints` and
  each hint button has a stable `BookmarkHint*` identifier.
  Input labels are associated with their controls. Row names include category to distinguish
  identical names, with full path and slot in HelpText. Important changed status text raises one
  live-region event; unchanged text is not announced again. Native screen-reader behavior remains
  a separate environmental check.

### Browse update refinement (Issue #495, 2026-11-20)

The existing browse view binds each list once to its own read-only observable source, maintained
by `BookmarkBrowseRows` in Presentation. A render replaces only changed display/capture rows,
adds new rows and removes obsolete tail rows; it never resets either source. Full category
membership captures remain part of comparison even when labels and counts are unchanged.
Non-selected rows ignore invisible focus-region marks. The view selects objects from the retained
sources, suppresses synchronous native-event reentry while updating them, and keeps deferred focus
under the existing owner. When actual search text/IME input cancels deferred restoration, its
visible focus marks follow Search. Closing the manager restores the active file list before
collapsing the focused overlay. This changes no filter, selection, save or navigation policy.

## Rejected alternatives

- **Direction B (a single column like the command palette, categories as headings inside the
  list) and direction C (the former two-column layout and its buttons inside a panel, adding only
  the scrim and `j`/`k`).** hide chose A.
- **Keeping the eight browse buttons next to the band.** Two visible controls per action would
  duplicate the band and crowd the panel; the hint buttons keep the pointer route without it.
- **Moving category editing to the command palette.** hide chose to keep it in the manager; the
  palette lists executable intents, and category actions need a selected category.
- **Mapping `j`/`k` and the letters in the shared `Modal` context.** Drafts and confirmations
  would gain keys they do not own and their text editors need the letters.
- **Declaring the keys as `UserIntent`s in the shared table.** `a`, `e`, `d`, `/` and `Tab` have no
  session meaning of their own; their effect depends on the focused region, and new
  Application intents the session never handles would widen the closed intent set for a view
  concern.
- **A second close path for the scrim.** The tap reuses the `Escape` route so a draft cannot be
  dropped by a route that differs from the key.

## Consequences

- Three bookmark contexts, one dedicated bookmark table of sixteen entries, three new key identities
  (`a`, `e`, `/`), Presentation decision and projection types and two App views. The catalog, its
  schema and the save path are unchanged; the bookmark session owns deletion confirmation and
  the saved entry's return context.
- `d` and the Delete hint never delete at once: bookmarks and categories are both deleted only
  from their confirmation. One new editor state and two actions replace the immediate action.
- The panes stay visible behind the manager. Pointer input cannot reach them.
- `CommanderWindow` grows by four logical lines (320 to 324) over its pre-existing CS-013 type-size finding.

## Migration and removal

The eight browse buttons and their resources are removed; their actions are reached through the
keys and the band. The empty-catalog guidance names `a` instead of the removed button.

## Executable proof

Mapper tests for both contexts, repeats, modifiers and pass-through; table conformance (each
declaration maps to its action once); translator tests for `a`, `e` and `/`; decision tests for
every action in both regions, including the unavailable cases; session tests that the deletion question changes neither catalog nor saved settings, that Cancel restores browsing, that confirmation deletes and saves once and that a changed target is rejected; presenter tests for the question and the category counts; movement end tests; band projection
tests for both regions; keyboard help conformance including the bookmark group; the presenter's
row and category marks and automation identifiers; the architecture ownership test for the new
view; the App Release build; and hide's on-screen confirmation of layout, focus movement, the
search field with an input method, the scrim tap and the narrow window.
