# カテゴリ別ブックマーク 引き継ぎ書 — 2026-09-06

Status: informational

## Current status

- Issue: [#99](https://github.com/hideyukiMORI/NeNeCommander/issues/99)
- Worktree: `C:\Users\info\WORKS\NeNeCommander-99`
- Branch: `feat/99-bookmark-manager`
- Base: `a4532b38231a4ec543116d412143436f2b92f882`
- ADR: [`docs/adr/0041-category-bookmarks-through-settings-and-navigation.md`](../adr/0041-category-bookmarks-through-settings-and-navigation.md)
- Report: [`docs/reports/2026-09-06-category-bookmarks-daily-report.md`](../reports/2026-09-06-category-bookmarks-daily-report.md)
- PR: not created yet; work remains an uncommitted implementation checkpoint

## Canonical mechanisms

- `UserSettings` owns one immutable `BookmarkCatalog`; valid schema version 1 maps to its empty value,
  and the sole serializer writes complete version 2 settings.
- `SettingsSession` remains the only current settings owner and ordered writer. Preference revisions
  preserve the catalog and catalog revisions preserve both preferences.
- Category and bookmark names, bookmark paths, and fixed slots use typed parsing. The bookmark path
  wrapper rejects lossy UTF-16 before the unchanged global `FileSystemPath.Parse` boundary.
- Catalog creation owns its lists, resolves category references case-insensitively to the preserved
  category spelling, and validates global slot and per-category name uniqueness.
- `BookmarkSelection` and `BookmarkCategorySelection` retain complete immutable snapshots. Stale
  manager actions and category collisions reject the whole metadata mutation.
- `Ctrl+B` and `Ctrl+1` through `Ctrl+9` are static canonical bindings. A session-resolved path enters
  `DualPaneSession` and the active pane's existing navigation method; the view never opens a path.
- `WindowsLocalSettingsStore` keeps ADR-0040 atomic write and identity rules. Version 2 size rejection
  happens on serialized UTF-8 bytes before any settings-directory or temporary-file mutation.

## Current proof and incomplete work

Application全256件、Infrastructure全219件、Domain全66件、Presentation全86件、Architecture全5件が
成功した。Infrastructure failure-first measured 5 failures among 7 new behavioral proofs on old
production; the current focused persistence set passes 10/10. Ctrl+B追加後に既存modal testのhint件数が
9のままで1件失敗し、Ctrl+B、既存Ctrl+,、Escapeを含む10件の表示契約へ更新後にPresentation全件を
再確認した。These are development checks, not merge readiness.

The complete bookmark editor state and actions, Presentation projection, XAML overlay, localized
product copy, and final Claude Design link remain incomplete. The final visual implementation must use
existing semantic resources, keep persistence warning outside the scrolling list, avoid adding all
slot hints to the permanent footer, and preserve Cancel initial focus for category deletion.

After implementation, run affected whole tests, coverage and meaningful mutation, dependency review,
conformance, and Commit checks. The final unchanged head requires security deep because strict JSON,
persisted paths, command routing, and settings mutation preflight changed. Only then may the Draft PR
become Ready for the one canonical CI gate.

Do not implement Issue #100 or #101 in this branch. Do not close the currently running user application
process or modify its settings while hide evaluates it.
