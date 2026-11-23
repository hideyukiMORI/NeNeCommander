# 2026-11-24 引き継ぎ — マイルストーン継続

最終更新: 2026-11-24 08:18 JST。hideの停止まで継続する指示が有効。

1. 現在は既存Issue571/PR573のbeta.4準備。作業コピーは
   D:/NeNeCommander/worktrees/issue-571、branch chore/571-preview-beta4。
2. 製品d688d58は不変。新規26ケースと既存helper/呼出5箇所のみを修正。
   ビルド03は警告/エラー0、対象MTP49成功。独立再レビューは必須修正なし。
3. D:/NeNeCommander/evidence/release-beta4-20261124/run-application-proof.ps1が
   affected coverage/format/tool restore/full Application mutationを順に実行する。
   log/JSONとproof-inputs.jsonを読み、同じ実行を重ねない。Application959/959成功、
   branch100%、format成功、complexityは既存5件と完全一致。1246入力のハッシュ一致。
   run-remaining-mutation.ps1の待機処理がApplication成功後に残2層を順次実行する。
   実行中log/完了JSONを確認し、重複起動しない。旧workflowのfailureは書き換えない。
4. Application mutation終了後、baseline115件との対応と全層scoreを確認。
   Infrastructure/Presentationは前回未実施。前回Domain/canonical等の成功は入力対応を
   確認して再利用する。元deep review45195559902はfailureのまま記録する。
5. final CodeQL、PR証拠、mainとの差分/通常squash、正式mainからの配布候補照合へ進む。
   旧package-preflightと原本証拠は上書きしない。現時点でpush/merge/新CI/タグ/公開なし。

[品質記録](../quality/2026-11-24-preview-beta4-continuation.md)と
[日報](../reports/2026-11-24-milestone-daily-report.md)を更新する。

青枠CUA/共有desktop入力は禁止。通常App/設定、Nibその他セッションへ干渉しない。
デザインが必要な次項目はhide指示のバックグラウンドClaude /designを利用する。
旧停止・固定モデル分担を現行指示に優先しない。未完issue491/495/514/544/547/548/551、
pr-498-reviewの作業物を保持。完成済み機能worktreeの整理は再実施しない。
今回の出力D:/NeNeCommander/outputs/release-beta4-20261124とrelease作業コピーは未完で保持。
新しい後片付け拒否はない。

最新差分: HEAD3b0a5c1（tests/docs3fae9db＋main接続、全tree一致）。Application mutation
96.5095986%成功。3242同一mutant、35件が追加テストで新規Killed、未検出80件は残る。
実行session32171がInfrastructure→Presentationを担当し、session9633が参照DLL補足の
CodeQLを担当する。重複起動しない。active-execution.jsonと各log/完了JSONを読む。
初回final-codeql.sarifの0resultsだけでは不十分。resolved-codeql.sarifで164 rule IDs、
1037 source files、コンパイル/抽出診断、1246入力一致を確認し、候補refへのuploadと
open alert readbackが必要。旧CIのrulesはdriverでなくextensionsにもある。
旧成功16stageはpending ledgerに対応付け済み。旧workflowはfailureのまま。
旧証拠の保守的期限は2026-11-27 23:34:56 JST、最終公開時点で再確認する。
