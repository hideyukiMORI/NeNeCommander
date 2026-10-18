# CS-013 優先順の整理・引き継ぎ — 2026-10-19

hide が指定した①残りのPresentation、②共通ファイル操作契約、③プロバイダー
境界までを完了する区切り。19日00:08の依頼に従い、Issue #340 の統合・整理後に
今回の作業を終了する。④設定保存と⑤残りの整理／全体適用は今回着手しない。

## 引き継ぐ状態

- #332 / PR #333：表示モデル・キー入力。114件成功、残件153→141。
- #334 / PR #335：Gatewayの責務・転送契約。275件成功、141→118。
- #336 / PR #337：管理側ごみ箱コールバック。82件成功、118→110。
- #338 / PR #339：Windowsローカル操作と事前確認。190件成功、110→104。
- #340 / 対応PR：WSL観測値・事前確認・直接影響するfixture。273件成功、
  Live WSL 3件未実行、104→80。最後に変えたテストの3ケースは再実行済み。

現在の残件は製品20・テスト60・eng 0。今回の変更・新規C#はすべて上限以内。
全体ゲートは未接続。メトリクス定義、閾値、native ABIの扱いはADR-0075のまま。
テスト数には作業間の重複があるので合算しない。

[日報](../reports/2026-10-19-cs013-priority-daily-report.md)に全体成果、#340の不変条件、
コマンド、初期失敗と修正、成功結果の再利用、実環境検証の限界を記録した。
最新計測は `D:/NeNeCommander/evidence/wsl-provider-refactor-20261019/measurement.json`、
構造照合は同領域の `Verify-WslMigration.py` / `migration-review.json`。
821ソースのhash照合済み。GitHubの対応PRと `completion.json` で実際の統合と
後片付けを確認し、日付の古い「継続中」の記録から未完了と推測しない。

## 次回の作業順

1. **④ 設定保存の責務を整理する。** 現行mainと証拠を確認し、新しいIssueと
   D側worktreeで開始する。`WindowsLocalSettingsStore` は662 logical lines、
   `WriteDocument` は200行・cognitive 34。1回の保存試行、出版、cleanupの責務を
   検討する。`SettingsDocumentValidator`、`SettingsSortOrderCodec`、
   `SettingsSession` の残件と直接影響するテストを同時に扱う。#243/#250/#251で
   確定した、残留一時物・保存失敗・次回保存・終了時drainの契約を変えない。
2. **⑤ 残りの製品フローとテストを整理する。** 設定以外の製品残件は
   `BookmarkCatalog` cognitive 14、`BookmarkCategoryName` nesting 4、
   `CommandCatalog` cognitive 12、`PaneReducer` nesting 4、`PaneSession`
   cognitive 13、`CommanderSession` cognitive 16。テスト60件は最新JSONで
   責務と直接影響を確認し、ケース・assertion・競合順序を落とさず分ける。
3. **全体適用は残件を解消してから。** `src` / `tests` / `eng` 全体でInspectが
   exit 0になることを確認し、ADR-0051/0075に従って別の明示された採用変更として
   `eng/check.ps1` とCIへの接続を扱う。閾値緩和、baseline、除外、変更ファイル
   だけのゲートで既存違反を残さない。#318の構成テストとの重複もこの段階で整理。

計測値はmethod/typeを重複して数える場合がある。「80件」はファイル数や未完了
Issue数ではない。古い暫定計測と現在のcs013-v1の数値を混同しない。

## 維持する契約と検証範囲

`FileOperationGateway` は引き続き唯一のlease／continuation owner。
`TransferExecution` は1回の試行、`FrozenTransfer` は中断をまたぐ不変の元一覧を
所有する。`ResumeAsync` の移行済み `TransferConflictResponse` を使用する。
WSLの `wsl-v2` トークン、独立に観測した名前・kind・属性、同一ハンドルのfacts、
リンク／ドライブ／包含／予約、コピー検証後だけの元削除を維持する。

製品20件のうちSettings系は14件、その他Application系は6件。
metric別の全80件はmethod-lines 32、cognitive 11、nesting 5、parameters 13、
type-lines 19。次回は実際に変更した機能と境界だけを検証し、入力が同じ成功証拠を
push・レビュー・mergeだけの理由で繰り返さない。公開契約・信頼境界に新たな広域
影響が生じる場合だけ、その影響からfull/deep検証の必要性を判断する。

Live WSLの3件はRootParameterAbsentで未実行。実WSL、Shell recycle、全製品UIの
新しい実環境証拠は今回取得していない。#94のhide同席の手動リリース確認は独立した
残件で、開発作業を止める理由にも今回通過した検証にも扱わない。

## 作業領域と終了時確認

本体は `C:/Users/info/WORKS/NeNeCommander`。追加作業はD側のみ。
今回のtaskは `D:/NeNeCommander/worktrees/issue-340`、一時出力は
`D:/NeNeCommander/outputs/issue-340`。恒久証拠は前述のevidence領域に保持し、
ブランチ・commitも保持する。main反映と証拠収載後、未保存物・無視ファイル・唯一の
成果物・稼働参照・リンクを確認してtaskの2領域を削除する。
実行結果は対応PRの完了記録と `completion.json` に記載する。整理が拒否された
場合は日報と本引き継ぎへ正確な残存パス・理由・hide用コマンドを追記し、別方式で
削除を再試行しない。
