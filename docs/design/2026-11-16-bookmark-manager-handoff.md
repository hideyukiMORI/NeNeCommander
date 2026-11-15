# Bookmark manager engineering handoff

Status: approved direction A for Issue #398

Date: 2026-11-16

This handoff records the bookmark manager layout and keyboard decided in
[ADR-0079](../adr/0079-bookmark-manager-keyboard.md). Engineering owns the content, behavior,
accessibility and integration constraints below. The visual direction is chosen by hide; final
token values follow [DESIGN_HANDOFF.md](../DESIGN_HANDOFF.md). The reference mock is the design
canvas `bookmarks/project/Main.dc.html` (direction A); its colors are not copied, only its
structure and proportions.

## Content inventory

1. Header: title `ブックマーク` / `Bookmarks`, the shown bookmark count (`{0} 件` / `{0} items`)
   and the search field (`ブックマークを検索`).
2. Status line under the header: the existing manager status or problem text, and the existing
   persistence warning when saving failed.
3. Category column (`DensityBookmarkCategoryColumnWidth`): header `カテゴリ`, then All bookmarks,
   Uncategorized and the user categories in catalog order.
4. Bookmark list: column headers `枠` / `名前` / `場所`, then one row per shown bookmark with the
   canonical slot label (`Ctrl+1`–`Ctrl+9` or empty), the name and the location in monospace.
5. Bottom band: the key hints of the focused region and, at the right, the save status.

| Hint | Bookmark list | Category column |
|---|---|---|
| `j` `k` | 移動 / Move | 移動 / Move |
| `Enter` | 開く / Open | 一覧へ / To list |
| `/` | 検索 / Search | 検索 / Search |
| `Tab` | 切り替え / Switch | 切り替え / Switch |
| `a` | 登録 / Add | カテゴリを追加 / Add category |
| `e` | 編集 / Edit | 名前を変更 / Rename |
| `d` | 削除 / Delete | カテゴリを削除 / Delete category |
| `Esc` | 閉じる / Close | 閉じる / Close |

Every key cap comes from the canonical bookmark table; nothing in the band is hand-written.
Representative long strings: long bookmark names and UNC or `\\wsl.localhost` locations are trimmed
with an ellipsis; long category names are trimmed in the column.

## States and behavior

- **Browsing.** Two focus regions; the list holds focus on open. `j`/`k` and the arrows move the
  selection in the focused region and stop at both ends; moving in the category column filters the
  list. `Tab` switches region, `Enter` opens the bookmark or moves from the column to the list,
  `a`/`e`/`d` act on the focused region, `/` focuses the search field, `Escape` closes. The selected
  row and category carry the file list's focus mark; the focused item also shows the system focus
  visual.
- **Searching.** Text goes to the field; `Escape`, `Down` and `Enter` return to the list without
  closing. Input-method composition is never interpreted.
- **Bookmark draft, category draft, category-delete confirmation, failed navigation.** The existing
  forms and their buttons replace the browse body and the band; keyboard behavior is unchanged.
- **Navigation pending.** The browse body stays visible but disabled.
- **Empty catalog / no matches.** The existing guidance text; the empty-catalog text names `a`.
- **Pointer.** Each hint except movement is a flat button running the same action as its key.
  Tapping the scrim takes the `Escape` route once; tapping inside the panel does nothing.

## Accessibility and environment constraints

- The panel keeps `BookmarkManagerModal` and has a localized name. Lists: `BookmarkCategories`,
  `BookmarkRows` (named regions); row containers `BookmarkRow_<name>`, category containers
  `BookmarkCategory_<name>`, each named by its visible text. The band is `BookmarkKeyHints`; hint
  buttons are `BookmarkHintActivate`, `BookmarkHintSearch`, `BookmarkHintSwitch`,
  `BookmarkHintAdd`, `BookmarkHintEdit`, `BookmarkHintDelete`, `BookmarkHintClose` and the
  disabled `BookmarkHintMove`. Search `BookmarkSearch`, count `BookmarkCount`, status
  `BookmarkStatus` (polite live region), save status `BookmarkSaveStatus`.
- Hint buttons are not tab stops and do not take focus.
- Light, dark, high contrast and 100–300 % scaling use only existing semantic tokens
  (`SurfacePane`, `TextPrimary`, `TextSecondary`, `TextKeyHint`, `BorderSubtle`, `FocusRing`,
  `FocusSurface`, `StatusWarning`); no new color token is added. New dimension tokens:
  `DensityBookmarkManagerMaxWidth` 820, `DensityBookmarkManagerMaxHeight` 540,
  `DensityBookmarkCategoryColumnWidth` 180, `DensityBookmarkSlotColumnWidth` 48,
  `DensityBookmarkNameColumnWidth` 200, `DensityBookmarkSearchWidth` 300,
  `BorderBookmarkCategoryColumnThickness` and `BorderBookmarkFooterThickness`.
- At the narrow window (900×600 DIP) the panel shrinks inside the window's outer spacing and the
  band wraps onto a second line instead of clipping.

## Selected direction

hide selected direction A on 2026-11-16: the centered panel on the transparent scrim with the
category column, the list and the key band, one-letter `a`/`e`/`d` commands, category editing kept
in the manager, and a scrim tap that closes.

## Directions considered

- **A. Centered panel with column, list and key band (selected).**
- **B. Right-side drawer.** Not selected.
- **C. Compact top sheet with category editing moved to the command palette.** Not selected.
