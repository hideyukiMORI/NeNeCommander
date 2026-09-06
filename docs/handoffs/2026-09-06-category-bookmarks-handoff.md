# カテゴリ別ブックマーク 引き継ぎ書 — 2026-09-06

Status: informational

## Current status

- Issue: [#99](https://github.com/hideyukiMORI/NeNeCommander/issues/99)
- Worktree: `C:\Users\info\WORKS\NeNeCommander-99`
- Branch: `feat/99-bookmark-manager`
- Base: `a4532b38231a4ec543116d412143436f2b92f882`
- ADR: [`docs/adr/0041-category-bookmarks-through-settings-and-navigation.md`](../adr/0041-category-bookmarks-through-settings-and-navigation.md)
- Report: [`docs/reports/2026-09-06-category-bookmarks-daily-report.md`](../reports/2026-09-06-category-bookmarks-daily-report.md)
- Draft PR: [#102](https://github.com/hideyukiMORI/NeNeCommander/pull/102); first pushed checkpoint `680d1f6`

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

Application全268件、Infrastructure全222件、Domain全66件、Presentation全92件が成功した。
Application Release buildは0 warning/error、conformanceは112、Commit security checksは18件成功した。
Infrastructure failure-first measured 5 failures among 7 initial behavioral proofs on old production;
strict schema proofはversion 1 readから次writeする移行とroot/nestedの個別wrong-kind/missing casesまで追加した。
path identityのstale比較は`FileSystemPathIdentityComparer`へ統一し、Windows/UNC case-only positiveとWSL
component-case negativeを確認した。These are development checks, not merge readiness.

The complete bookmark editor closed states and actions, Presentation projection, localized XAML overlay,
fixed persistence status/warning, canonical slot labels, and sole `CommanderSession.HandleAsync` route are
present in the current uncommitted checkpoint. The adopted functional handoff is
[NeNe Commander Bookmark Manager](https://claude.ai/design/p/ff811404-8e96-4dd0-92c1-320b3002b4b9?file=NeNe+Commander+Bookmark+Manager.dc.html).
It does not establish real WinUI focus, keyboard, high-contrast, DPI, or narrow-width proof; those remain
release-environment work under Issue #94.

Issue #103 / PR #104 must first close the global `FileSystemPath.Parse` post-normalization length boundary.
After it merges, rebase #99 without adding a bookmark-local duplicate guard, rerun affected checks, and
obtain the required coverage, meaningful mutation, dependency, security-deep, and canonical evidence.

The final unchanged head requires security deep because strict JSON, persisted paths, command routing,
and settings mutation preflight changed. Only after the precursor integration and remaining formal proof
may the Draft PR become Ready for the one canonical CI gate.

Do not implement Issue #100 or #101 in this branch. Do not close the currently running user application
process or modify its settings while hide evaluates it.
