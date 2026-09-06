# 日報 — 2026-09-06（カテゴリ別ブックマーク）

Status: informational

## 現在の範囲

[Issue #99](https://github.com/hideyukiMORI/NeNeCommander/issues/99) は、表示名、保存 path、
カテゴリ、任意の固定ショートカットを持つ bookmark catalog を実装中である。
[ADR-0041](../adr/0041-category-bookmarks-through-settings-and-navigation.md) は、既存の
`settings.json`、`WindowsLocalSettingsStore`、`SettingsSession` ordered queue、active pane の
`IDirectoryReadPort` 経路だけを使う契約を採択した。別 bookmark file、writer、navigation engine、
dynamic key map は追加しない。

初版は user category 32件、bookmark 128件、category name 64 UTF-16 code units、display name
128 UTF-16 code unitsに制限する。Uncategorized は persisted null で、UIだけがlocalized labelへ
変換する。bookmark key は category/name のcase-insensitive pairで、manager selectionはkeyに加えて
完全なimmutable entryを保持する。同じkeyのpath等が差し替わったstale actionはI/Oとmetadata変更前に
拒否する。category削除は全entryを一括でUncategorizedへ移し、同名衝突時は全体を無変更で拒否する。

schema version 2はbookmark arraysを含むexact shapeで、valid version 1だけをempty catalogとして
明示移行する。readerはinvalid UTF-8とescaped lone surrogateをtyped rejectionへ閉じる。writerは
完全なversion 2 documentをUTF-8へserializeし、65,536 bytesを超える場合はdirectory/temp作成前に
`TooLarge`で拒否する。既存ADR-0040のancestor、document、temporary identityとatomic publish契約は
変更しない。

keyboardはcanonical tableへ`Ctrl+B`と固定`Ctrl+1`から`Ctrl+9`だけを追加する。`Ctrl+P`は
[Issue #101](https://github.com/hideyukiMORI/NeNeCommander/issues/101)用に未割当のまま保持する。
bookmarkからのnavigationはcurrent catalogをsession ownerが再解決した後、既存DualPane/Pane routeへ
typed pathを渡す。未割当、stale、modal、operation busyではprovider readを開始しない。

## failure-firstと現在の検証

Infrastructure担当のisolated filterでは、旧productionにversion 2、strict UTF-8、UTF-8 size、
version 2 serializationを要求する7件のうち5件が失敗し、valid version 1 migrationとclosed nested
shapeの2件だけが成功した。修正後はversion 1 readから次writeでversion 2になること、root/nestedの
missing/wrong-kind、invalid Unicodeを個別に追加し、Infrastructure全222件に成功した。
session-owned browse/draft/confirm/pending/failed state、stale action、category rename/delete、path identity、
one queue/one navigation routeを追加したApplication全305件、Domain全71件にも成功した。
Ctrl+B追加後に既存modal proofのhint件数期待が9のまま残って1件失敗したが、canonical Ctrl+Bと
既存Ctrl+,/Escapeの順序を含む10件の表示契約へ更新した。bookmark projectionを含むPresentation全98件、
Application Release build 0 warning/error、conformance 112、Commit security 18に成功した。
typed navigation failure/cancellationは正規化reasonと選択をmanagerに保持し、Retryはそのcomplete snapshotだけを
current catalogへ再照合する。別selectionのcrafted navigationはfailure state、catalog、writeを維持してread0で
拒否する。empty catalogとfilter/search no-resultsは別のlocalized copyを出し、selection-dependent actionを無効にする。
protected branch coverageはDomain/Application 100.00%、Infrastructure.Windows 91.14%、
Presentation.WinUI 90.84%で全thresholdを満たした。
現時点は実装途中であり、coverage、mutation、security deep、Draft-to-Ready canonical CIを
merge-readiness evidenceとしてまだ主張しない。

## GUI handoff

採択済みの[Claude Design handoff](https://claude.ai/design/p/ff811404-8e96-4dd0-92c1-320b3002b4b9?file=NeNe+Commander+Bookmark+Manager.dc.html)に沿い、
上段search、左categoryと右bookmarkの2列、独立Bookmark modal、明示Save/Cancel draft、category削除専用
confirm、成功時だけmodalを閉じるnavigation、固定status/warning領域を既存semantic resourcesだけで実装した。
Canvasで視認できたfocus ringはRetryとcategory deletion Cancelだけで、native WinUIの実focus、keyboard、
high contrast、DPI、狭幅を実証したとは扱わない。生成mockの数値styleや新しいtoken名は未承認であり
productionへ持ち込んでいない。これらの実環境証拠はIssue #94に残す。

## 次の作業

- path canonicalization closureの先行[Issue #103](https://github.com/hideyukiMORI/NeNeCommander/issues/103) /
  [PR #104](https://github.com/hideyukiMORI/NeNeCommander/pull/104)はmerge `0dab3f67`で統合済み。
  deep `34041924385`、canonical `34043839856`成功の最新mainへ#99を更新済み。
- current UI/state/schema候補の独立reviewはblocker 0。post-rebase behavioral suites、coverage、
  conformance、Commit checksは成功した。
- affected mutationとdependencyを確認し、Draft PR #102を最終候補へ更新する。
- final exact headでsecurity deepを実行し、Ready後にcanonical CIを一度取得する。

[Issue #100](https://github.com/hideyukiMORI/NeNeCommander/issues/100)のwindow shortcutと
[Issue #101](https://github.com/hideyukiMORI/NeNeCommander/issues/101)のcommand searchは追跡のみで、
このbranchでは実装しない。live WSLとWindows UI matrixは引き続きIssue #93/#94のrelease tierである。

## 2026-09-07 checkpoint

実装は完了候補だがDraft PR #102は未mergeである。current baseは`0dab3f67c07fceafc77827e78f068a36c36e7003`、作業headは`895057ff3ed61bf7efde55b9c50185991cfe97c6`。dirty差分はBookmarkCatalogのCodeQL #101修正（`Select`/`OfType`/`Any`の1-pass判定）、既存Presentationテスト4件相当の追加・更新、および新規`BookmarkPresentationBehaviorTests`の5件で、focused Application 21/21、Presentation 107/107が成功している。commit/pushとDraft PR更新はこのcheckpoint保存後にownerが行う。

CodeQL alert #101はまだ再解析していない。正式deep `34049550861`はPresentation mutation 76.33%で失敗した。手動のOperationStatus文字列mutantは既存testがFAILし、isolated coverage-off focused runは35/35 Killedだった。一方、full scope 910 mutantsのcoverage-off、coverage-off+disable-mix-mutants、concurrency=1診断は全て80.95%で、OperationStatus 35、PaneStatus 10、SettingsSaveStatus 4がSurvivedのまま。証跡は`artifacts/security/deep-review-34049550861/mutation/NeNeCommander.Presentation.WinUI/reports/mutation-report.json`、`artifacts/security/mutation/presentation-full-off/reports/mutation-report.json`、`artifacts/security/mutation/presentation-full-off-no-mix/reports/mutation-report.json`、`artifacts/security/mutation/presentation-full-off-no-mix-concurrency1/reports/mutation-report.json`に保持し、off/no-mix/concurrency1のreport SHA256は`327BC00DBD4AD1D67833E5D13E0260CBB808325FE2D256F2B05588A6058FF89D`である。各runの終了codeは順に2、2、1で、canonical config・ADR・閾値・依存は変更していない。

Stryker upstreamの未修正candidateとして[PR #3695](https://github.com/stryker-mutator/stryker-net/pull/3695)（提案commit `b2fd312`）と[Issue #3742](https://github.com/stryker-mutator/stryker-net/issues/3742)がMTP testhost reuse/static initializerの判定汚染を扱う。4.16.0への包含・修正版releaseは未確認で、ローカル適用や依存更新は実施していない。再開時はこのupstream状態と正規の検証機構を確認し、同設定のblind rerunや閾値緩和を行わない。

Issue #99/#102はDraft維持。#93/#94は未確認、#100/#101は未実装。hideのPID 3188とreal settingsは無操作。診断worktreeとartifactは保持する。次回は`C:\Users\info\WORKS\NeNeCommander-99`を開く（original workspace mainは`a453`で古い）。
