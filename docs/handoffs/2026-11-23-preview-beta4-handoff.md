# 2026-11-23 引き継ぎ — Preview beta.4準備の停止地点

最終更新: 2026-11-24 00:15 JST。
[日報](../reports/2026-11-23-preview-beta4-daily-report.md) /
[品質記録・正確なcommands](../quality/2026-11-23-preview-beta4.md)

## 最優先の指示

hideは「とりあえず区切ろうか」「日報と引き継ぎ書を書いて」と明示した。
開発・試験・リリース作業は停止。記録を保存したら終了し、再開指示まで自動継続しない。
以前の継続指示や「リリース準備」を、停止後の実行許可として使わない。
青枠CUA、共有desktopへの入力、通常設定の巻戻しは禁止。

## 現在地

- 製品mainはe0a06164ce63399eeec193d29a6ce47af12af79cまで統合済み。締め記録は別の文書PRで反映する。
- PR559/565/568/569/570/572はmerged。各受理headとの完全tree一致を保存済み。
- Release Issue571 / PR573はDraft。CodeQL対応Issue574はPR573に含まれるが未merge。
- 製品・テストの確定候補はd688d58d01c966da79a0c64b142362388c0906e1。
- 12ce741までの後続差分は記録だけ。この締め記録も文書だけで、実装・テストは変えない。
- 作業場所: D:/NeNeCommander/worktrees/issue-571、chore/571-preview-beta4。
- 恒久証拠: D:/NeNeCommander/evidence/release-beta4-20261123。
- 一時出力: D:/NeNeCommander/outputs/release-beta4-20261123。
- 正式beta.4候補、タグ、GitHub prereleaseは未作成。

## 確定した失敗と成功

修正後deep review45195559902 / job137764954226は終了してfailure。
canonical、coverage、audit、4層×3回adversarial、Domain mutation97.47%は成功。
Application mutation94.98%が必要95%に届かず停止。Infrastructure/Presentation mutationは未実施。
通常テストは2476件中2460成功/0失敗/16skip。coverageは100/100/94.65/96.08%。

Application reportの状態数はKilled2147、Timeout30、Survived113、NoCoverage2、
CompileError427、Ignored523。ツールの元分類をそのまま保存し、この記録で新しい除外を追加していない。
再開時の入口:
D:/NeNeCommander/evidence/release-beta4-20261123/corrected-deep-review/mutation/NeNeCommander.Application/reports/mutation-report.json。
まだsurvivorの原因調査・追加修正は始めていない。

CodeQL analysis2230861543はd688d58のcandidate refで164規則/結果0件。
open owned alertsも0でAPIのerror/warningは空。同じ箇所のRoslyn型解決診断はない。
これはlocal counts-codeql.sarifに残った型解決不足による別指摘1件とは区別する。
local/CI両方のSARIFを保持し、local自体が0件だったとは書かない。
dependency-review45198574239はa111464で成功。PR573は停止時Draftへ戻した。

全job log、deep-review.json、mutation JSON、GitHub artifact ZIPを取得済み。
artifact14831372423のSHA256は836182305d4823de054666a589c65c6867bedb9787bf45c07b60138b477253fd。
stop-receipt.jsonと最終PR readbackが終了状態を所有する。新たなCIは起動していない。

## 再開後の順序

1. hideの再開指示と最新main/PR状態を確認。文書PRによるmain移動を製品変更と取り違えない。
2. 保存したApplication mutation reportから、未検出の振舞いと同値変更を区別して調べる。
   閾値を下げず、必要な契約を直接証明する試験・修正に絞る。数値を超えるためだけの試験は加えない。
3. 変更に対応するIssue/検証範囲を確定。成功済みの未変更証拠を再利用し、未実施2層も確認する。
   releaseに必要な96時間以内の成功deep reviewを満たす。未変更全件を無条件に再実行しない。
4. 必要なCodeQL再確認、PR本文、レビュー/thread/依存証拠、exact headを確認して通常squash。
   新しいmainとの接続は実際のdiffで判断し、treeが異なるのにoursで捨てない。
5. clean accepted mainからeng/publish-release.ps1で正式候補を生成し、
   verify-candidate.ps1 / verify-pri-locales.ps1 / verify-extracted-candidate.ps1で照合する。
6. release-notes-draft.mdを最終sourceと証拠で確定。タグ/公開は準備完了と区別し、
   その時点のhide指示の範囲を確認する。

## 仮packageと残すもの

a111464のpackage-preflightはcanonical publisherで生成済み。
588ファイル、1681入力/10lock、3言語各547文字列、132 XBF、全noticeと別展開は一致。
正本はevidence/preflight。Appは起動していない。正式mainとはsource identityが違うため、
最終配布物は別の新しい出力先へ生成し、この仮候補を上書きしない。
SDK license原本: D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf。

issue523/525/526/527/528と関連8outputは保存・照合後23:19に削除完了。再削除しない。
integrated-feature-artifacts.zip、manifestとcompletion receipt、branch/commitを保持。
issue571/outputはrelease未完で保持。別件issue491/495/514/544/547/548/551とpr-498-reviewは触らない。
通常のユーザー設定とbeta.3公開物を保持する。#94のexact scale/captureと新package launch、
live-provider/speechの未確認を引き継ぐ。後片付け拒否・hide実行待ちはない。
