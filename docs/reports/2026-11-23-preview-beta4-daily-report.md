# 2026-11-23 日報 — Preview beta.4準備の停止記録

最終更新: 2026-11-24 00:15 JST。
hideの「とりあえず区切ろうか」「日報と引き継ぎ書を書いて」に従い、開発とリリース作業を停止した。
記録の保存・反映だけを行い、追加実装、試験、CI再実行、タグ作成、公開は始めていない。

[品質記録](../quality/2026-11-23-preview-beta4.md) /
[再開用引き継ぎ](../handoffs/2026-11-23-preview-beta4-handoff.md)

## 結果

beta.4は未公開。受理済み機能はmainへ統合済みだが、リリース準備PR573はDraftへ戻した。
残る失敗はApplicationのmutation score 94.98%で、必要な95%を満たさない。
通常の動作テスト失敗やCodeQL未解消とは区別する。先に伝えた00:35前後の見込みは取り下げた。

| 確認 | 確定結果 |
|---|---|
| Canonical gate | 成功。通常テスト2476件中2460成功、0失敗、16環境skip |
| Branch coverage | Domain100%、Application100%、Infrastructure94.65%、Presentation96.08% |
| 依存監査 | 直接・推移依存ともvulnerability entryなし |
| Adversarial | 4層×3回のコマンド成功。各回Infrastructureの環境skip3件を保持 |
| Domain mutation | 97.47%、基準95%を満たす |
| Application mutation | 94.98%、基準95%未満で失敗 |
| Infrastructure / Presentation mutation | 上記停止により未実施 |
| Candidate CodeQL | analysis2230861543、164規則、結果0件、open owned alerts0 |
| Dependency review | run45198574239成功 |
| 仮配布物 | 全588ファイルと別展開、3言語各547文字列、132 XBF、原本notice・テーマ表記が一致 |

deep review45195559902はd688d58で2026-11-23 23:34:56開始、
2026-11-24 00:07:56にApplication mutationで停止した。後処理CodeQLと証拠保存は完了。
これは失敗したdeep reviewであり、release可能とは扱わない。
詳細ログ、失敗report、Domain/Application mutation report、SARIFを恒久保存した。

## 今日進めた変更

- PR559/565/568/569/570/572を通常squashで統合。受理headと各main treeの一致を確認した。
- 129テーマ、即時3言語切替、小型palette/共通対象、位置/サイズ/最大化保存、左右の復帰入力修正を収録。
- beta.3後の既存mainには循環focus、移動後の隣接focus、直接件数表示も含まれる。
- Issue571/PR573で唯一のVersionを0.1.0-beta.4へ更新し、READMEにschema6と旧版との非互換を記載。
- 初回deep review45188453598で見つかった古いApplication fixture2件を44b61baで修正。
- Issue574の件数書式3件/集計loop1件をd688d58で修正。通常3・表示/取得5・修飾1引数を明示。
- 対象Application36/Presentation35が成功。直接影響するassemblyは933/403成功。旧DLLで新回帰が失敗することも確認。
- App/test buildは警告0・エラー0。3言語の12書式、変更format、独立read-only reviewを完了。
- CS-013は既存と同じ5件で位置・値・閾値一致。新しい免除や閾値緩和はない。

ローカルCodeQLでは型解決不足の複雑度ツールに別指摘1件が出た。
そのSARIFと14診断も保持し、0件と偽らず記録した。新CIでは同じ箇所の型解決診断がなく、
同じ164規則で0件となった。製品やツールをその局所指摘に合わせて書き換えてはいない。

## 配布工程と利用環境

clean a111464からcanonical publisherで仮packageを作成し、1681入力/10lock、
21 packageの51原本notice、125輸入テーマの表記と2notice、全588ファイルを照合。
ZIPは113,960,769 bytes、SHA256:
98efde1973c710e1f05798ec8d87528f6d82accab83f433cbe96f07020c52d97。

保存先はD:/NeNeCommander/outputs/release-beta4-20261123/package-preflight。
これは正式mainの配布候補ではない。正式候補の生成・タグ・GitHub公開は未実施。
アプリ起動、共有desktop入力、青枠CUA、古いsettings backupの書戻しは行っていない。

hideの機能別実機所見を再利用する。#94は11月19日に完了報告済みだが、exact scale/captureは不明。
新しい配布物の起動、全live provider環境、全言語の目視/speechを実施済みとしない。
PCを停止してもGitHub検証は継続するが、検証から自動公開するworkflowは存在しない。
今回のGitHub検証も既に終了しており、停止後に自動リリースされない。

## 保存と整理

恒久証拠はD:/NeNeCommander/evidence/release-beta4-20261123。
stop-receipt.json、corrected-deep-review.zip、corrected-deep-review-job.log、
corrected-codeql-analyses.json、corrected-codeql-alerts.json、corrected-codeql.sarifを保存した。
取得archiveはGitHubのdigestと一致:
836182305d4823de054666a589c65c6867bedb9787bf45c07b60138b477253fd。

完了したissue523/525/526/527/528と関連8outputは23:19に整理済み。
全29,115生成物/5,105,771,514 bytesをarchiveへ保存し、全hashを照合した。
branch/commit/恒久証拠は保持。対象と不在確認はfeature-cleanup-completion.jsonにある。

D:/NeNeCommander/worktrees/issue-571とD:/NeNeCommander/outputs/release-beta4-20261123は
release未完のため保持する。仮ZIP、別展開、CodeQL database/toolもこの未完taskの検証資産である。
issue491/495/514/544/547/548/551、pr-498-reviewは別件の未完了なので変更していない。
後片付け拒否・hide実行待ちはない。次の実装・検証はhideの再開指示後に行う。
