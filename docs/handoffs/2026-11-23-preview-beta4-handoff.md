# 2026-11-23 引き継ぎ — Preview beta.4準備

記録時点: 2026-11-23 23:51 JST。まだ作業中。
[日報](../reports/2026-11-23-preview-beta4-daily-report.md)と
[品質記録](../quality/2026-11-23-preview-beta4.md)を併読する。

## 現在の指示と現在地

hideの最新依頼は「リリース準備」。受理済み機能の統合、リリース証拠、配布候補を用意する。
タグ作成・GitHub prerelease公開を実行したとは扱わない。過去のrelease-first/無期限継続を
新規機能の実装許可に広げない。共有desktop操作・青枠CUA・通常設定の巻戻しは禁止。

- mainはe0a06164ce63399eeec193d29a6ce47af12af79c、同期済みでclean。
- PR559/565/568/569/570/572はmerged。受理headとの完全tree一致を保存済み。
- 作業: D:/NeNeCommander/worktrees/issue-571、chore/571-preview-beta4。
- リリース: Issue571 / Ready PR573、追加CodeQL対応はIssue574。
- 現head a111464はd688d58から文書だけ追加、1243非文書入力一致。
- 証拠: D:/NeNeCommander/evidence/release-beta4-20261123。
- 一時出力: D:/NeNeCommander/outputs/release-beta4-20261123。

## 検証の再開点

初回45188453598は失敗。2件の古いApplication fixtureとCodeQLの4件は修正済み。
修正の独立review・対象build/test・3言語書式・旧DLLでの失敗証明・coverageは完了し、
再実行しない。CS-013は既存と完全一致の5件で、cleanとは報告しない。

修正後のsecurity-deep-review run45195559902 / job137764954226がd688d58で実行中。
canonicalも含むため別qualityを実行しない。終了したらrun/job/log/artifactを保存し、
各gate/coverage/audit/adversarial/mutationを確認する。失敗を閾値変更で回避しない。

local counts-codeql.sarifは164規則、対象4件なし/別指摘1件。
RecursionGraphのPartialDefinitionPart/ReducedFromを同一扱いするが、localはその型を
解決できず14診断。以前のCIではその解決エラーなし。新candidateのanalysis/ref alertsを
読み戻して区別する。local0件とせず、既定mainの0件でも代用しない。
比較はlocal-codeql-extraction-comparison.json。ソース/設定/抑制の変更はしていない。

23:49までにclean a111464からtask output/package-preflightへ仮packageを生成し、
全588ファイル、1681入力/10lock、3言語各547文字列、132 XBF、全notice、別展開が一致。
証拠はevidence/preflight、source/ZIPは仮候補として保持する。最終mainとは別sourceなので、
最終配布物は後でcanonical publisherから生成する。Appは起動していない。
実装/独立review/preflightが完成して23:50にPR573をReadyへ。dependency-review45198574239
はa111464で成功、レビュー/未解決threadなし。deep/CodeQL成功待ちは継続する。

## 残りの順序

1. 新deep成功と候補refの成功CodeQL/owned alerts0を確認。96時間以内を数値で記録する。
2. PR本文を最終証拠へ更新。Ready/依存reviewは上記で完了、関連入力が同じなら再実行しない。
3. 適用証拠、レビュー/未解決thread、exact headを確認して通常squash。mainを同期しtree一致。
4. clean accepted sourceでeng/publish-release.ps1を一回実行する。正式候補は
   D:/NeNeCommander/releases/0.1.0-beta.4/candidate-20261123-01を予定。
   SDK license原本はD:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf。
5. 準備済みverify-candidate.ps1、verify-pri-locales.ps1、verify-extracted-candidate.ps1で
   全hash/notice/locale/XBF/runtime/sourceと別展開を照合。失敗した候補は上書きせず記録する。
6. release-notes-draft.mdを実結果で確定し、日報/引継/PROJECT_STATEへ記録する。
   タグ/公開はこの準備依頼の完了と区別する。
7. 恒久成果物を収載して稼働参照/links/未保存を確認し、完了したtask専用コピーを整理する。

## 保存と整理

issue523/525/526/527/528と関連8outputは保存・全hash照合後、23:19に削除完了。
再削除しない。integrated-feature-artifacts.zip、manifestとcompletion receiptを保持。
同archive内に旧Presentation DLLと旧CS-013結果があり、検証済み比較証拠も保存済み。
issue491/495/514/544/547/548/551、pr-498-reviewは別件の未完了で触らない。
issue571/outputはrelease準備未完のため保持。後片付け拒否・hide実行待ちはない。

受理済みのhide操作結果は新しい配布物の起動確認ではない。#94のexact scale/captureと
新package launch/live-provider/speechの未確認を保持し、古い設定を戻さない。
