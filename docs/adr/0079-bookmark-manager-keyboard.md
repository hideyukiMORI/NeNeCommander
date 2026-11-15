# ADR-0079: Show the bookmark manager on the scrim and drive it with the pane movement keys

Status: accepted

Date: 2026-11-16

Accepted for Issue #398. This ADR amends the overlay layout and the keyboard of the bookmark
manager accepted in ADR-0041; the bookmark session, its actions, the catalog, its schema and
saving, `Ctrl+B` and `Ctrl+1` to `Ctrl+9` are unchanged. The design handoff is
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
  The category column is the existing `BookmarkCategories` list; the bookmark list shows the slot,
  name and location (monospace) at the file list's row height with the file list's 2 px left mark
  for the selected item, and both lists show the system focus visual on the focused item. The
  bottom band shows the key hints and the save status. The eight browse buttons are removed; each
  hint except movement is a flat button that runs exactly the action of its key in the focused
  region, so pointer-only use keeps a route. The hint buttons are not tab stops and do not take
  focus, like the caption buttons.
- **Other states keep their forms.** The bookmark draft, the category draft, the category-delete
  confirmation and failed navigation show their existing forms and buttons inside the same panel,
  keep the shared `Modal` context and behave as before, because their text editors need raw keys.
- **Two new keyboard contexts.** While browsing, the host reports `Bookmarks`, or `BookmarkSearch`
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
    there `Escape` does not close the manager. While an input method composes in the field no key
    is mapped, the same guard the command palette search uses.
  - `Escape` in `Bookmarks` closes through the existing `Escape` route.
  - A held `Enter`, `Tab`, `a`, `e`, `d`, `/` or `Escape` acts once; its repeats are consumed.
    Undeclared keys and declared keys under a modifier pass through, as in the `Settings` context.
- **Decisions live in Presentation.** `BookmarkManagerKeyboard` decides the owned context, what
  each action does in each region and where movement lands; `BookmarkManagerKeyHintPresenter`
  projects the band and the help rows. The App's `BookmarkKeyboardView` only remembers the focused
  region and applies the resolved `BookmarkManagerCommand`.
- **Key band.** The band is generated from the bookmark table for the focused region: movement
  (`j`/`k`), `Enter`, `/`, `Tab`, `a`, `e`, `d` and `Esc`, with region-specific wording from
  resources.
- **Keyboard help.** `F1` gains a "ブックマーク（Ctrl+B）" / "Bookmarks (Ctrl+B)" group generated from
  the same table, one row per action.
- **Automation.** The panel keeps `BookmarkManagerModal` and gains a localized name so its peer is
  exposed. Row and category containers receive `BookmarkRow_<name>` and `BookmarkCategory_<name>`
  identifiers and their names through `RowAutomationIdentity`. The band is `BookmarkKeyHints` and
  each hint button has a stable `BookmarkHint*` identifier.

## Rejected alternatives

- **Direction B (a right-side drawer) and direction C (a compact top sheet).** hide chose A.
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

- Two new contexts, one dedicated bookmark table of fourteen entries, three new key identities
  (`a`, `e`, `/`), Presentation decision and projection types and one App view. The bookmark
  session, actions, catalog and save path are unchanged.
- `d` in the list deletes the selected bookmark at once, as the removed Delete button did;
  category deletion still goes through its confirmation.
- The panes stay visible behind the manager. Pointer input cannot reach them.
- `CommanderWindow` grows by four logical lines over its pre-existing CS-013 type-size finding.

## Migration and removal

The eight browse buttons and their resources are removed; their actions are reached through the
keys and the band. The empty-catalog guidance names `a` instead of the removed button.

## Executable proof

Mapper tests for both contexts, repeats, modifiers and pass-through; table conformance (each
declaration maps to its action once); translator tests for `a`, `e` and `/`; decision tests for
every action in both regions, including the unavailable cases; movement end tests; band projection
tests for both regions; keyboard help conformance including the bookmark group; the presenter's
row and category marks and automation identifiers; the architecture ownership test for the new
view; the App Release build; and hide's on-screen confirmation of layout, focus movement, the
search field with an input method, the scrim tap and the narrow window.
