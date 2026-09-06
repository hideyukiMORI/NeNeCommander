# カテゴリ別ブックマーク 引き継ぎ書 — 2026-09-06

Status: informational

## Current status

- Issue: [#99](https://github.com/hideyukiMORI/NeNeCommander/issues/99)
- Worktree: `C:\Users\info\WORKS\NeNeCommander-99`
- Branch: `feat/99-bookmark-manager`
- Base: `0dab3f67c07fceafc77827e78f068a36c36e7003`
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

Application全305件、Infrastructure全222件、Domain全71件、Presentation全98件が成功した。
Application Release buildは0 warning/error、conformanceは112、Commit security checksは18件成功した。
Infrastructure failure-first measured 5 failures among 7 initial behavioral proofs on old production;
strict schema proofはversion 1 readから次writeする移行とroot/nestedの個別wrong-kind/missing casesまで追加した。
path identityのstale比較は`FileSystemPathIdentityComparer`へ統一し、Windows/UNC case-only positiveとWSL
component-case negativeを確認した。manager navigation failureは正規化されたread reasonとretained selectionを
保持し、別のcurrent selectionを差し込むRetryは追加read/writeなしで拒否する。empty catalogとfiltered
no-resultsは別のlocalized guidanceへ投影する。protected branch coverageはDomain/Application 100.00%、
Infrastructure.Windows 91.14%、Presentation.WinUI 90.84%である。These are development checks, not merge readiness.

The complete bookmark editor closed states and actions, Presentation projection, localized XAML overlay,
fixed persistence status/warning, canonical slot labels, and sole `CommanderSession.HandleAsync` route are
present in the committed Draft candidate. The adopted functional handoff is
[NeNe Commander Bookmark Manager](https://claude.ai/design/p/ff811404-8e96-4dd0-92c1-320b3002b4b9?file=NeNe+Commander+Bookmark+Manager.dc.html).
It does not establish real WinUI focus, keyboard, high-contrast, DPI, or narrow-width proof; those remain
release-environment work under Issue #94.

Issue #103 / PR #104 closed the global `FileSystemPath.Parse` post-normalization length boundary in merge
`0dab3f67c07fceafc77827e78f068a36c36e7003`. Its exact head passed deep run `34041924385` and canonical
run `34043839856`. Issue #99 is rebased onto that merge without a bookmark-local duplicate guard; its
post-rebase affected suites, coverage, conformance, and Commit checks pass. Meaningful affected mutation,
dependency, security-deep, and canonical evidence remain incomplete.

The final unchanged head requires security deep because strict JSON, persisted paths, command routing,
and settings mutation preflight changed. Only after the remaining formal proof may the Draft PR become
Ready for the one canonical CI gate.

Do not implement Issue #100 or #101 in this branch. Do not close the currently running user application
process or modify its settings while hide evaluates it.

## Stop checkpoint — 2026-09-07

再開時はoriginal workspace main（`a453`、古い）ではなく、`C:\Users\info\WORKS\NeNeCommander-99`を開く。現状はbase `0dab3f67c07fceafc77827e78f068a36c36e7003`、head `895057ff3ed61bf7efde55b9c50185991cfe97c6`、Draft PR #102未merge。dirty差分はBookmarkCatalogのCodeQL #101修正、既存Presentationテスト差分、新規`BookmarkPresentationBehaviorTests` 5件（Ptests合計107 PASS）である。

保存済み証拠: Application focused 21/21、Presentation 107/107、manual mutantは既存test FAIL、isolated coverage-offは35/35 Killed。正式deep `34049550861`はP76.33で失敗し、full coverage-off / `disable-mix-mutants` / concurrency=1 はいずれも80.95%で静的3型が全Survived。report JSON SHA256は`327BC00DBD4AD1D67833E5D13E0260CBB808325FE2D256F2B05588A6058FF89D`。証跡は`artifacts/security/deep-review-34049550861/mutation/NeNeCommander.Presentation.WinUI/reports/mutation-report.json`、`artifacts/security/mutation/presentation-full-off/`、`presentation-full-off-no-mix/`、`presentation-full-off-no-mix-concurrency1/`。Stryker run終了codeは2/2/1。canonical config、ADR、閾値、依存は未変更。SOL usage上限とLuna担当状態は親rootで管理する。

有力なupstream候補は[Stryker PR #3695](https://github.com/stryker-mutator/stryker-net/pull/3695)（提案commit `b2fd312`）と[Issue #3742](https://github.com/stryker-mutator/stryker-net/issues/3742)で、MTP testhost reuse/static initializerのverdict汚染を扱う。4.16.0への包含・修正版releaseは未確認。次回はPR/修正版の確認と正規の検証機構設計から再開し、blind rerun、基準緩和、依存更新をしない。

未確認は#93/#94、未実装は#100/#101。hide PID 3188、real settings、診断worktree、artifactには無操作・保持。再開手順はstatus/diff確認、focused evidence確認、ownerと同期して必要な軽量gateを実行し、canonical Ready/deep新実行は禁止とする。
