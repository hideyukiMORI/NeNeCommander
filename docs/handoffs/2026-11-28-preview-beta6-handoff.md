# Preview beta.6 handoff

今回の作業は完了。[Preview 0.1.0-beta.6](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.6)を公開し、公開3assetの再取得hash照合、恒久保存、完了worktreeと一時出力の整理まで済んだ。新しい作業はhideの指示から開始する。

配布ソースはPR #595のmain e25186725441c8c81e16a5e09f38c9f3422bcb62。受理候補175ebd7とtreeが一致する。タグv0.1.0-beta.6はannotated tagで、このsource commitを指す。完了文書の後続main更新は配布ソースや公開assetを変更しない。

正式3asset: D:/NeNeCommander/releases/0.1.0-beta.6/candidate-20261128-01。
証拠root: D:/NeNeCommander/evidence/release-beta6-20261128。
公開readbackはpublished-assets-63.json、恒久配布物はpermanent-release-64.json、
元生成物保存はraw-preservation-complete-65.json、整理完了はcleanup-completion-66.json。

固定入力final-inputs-41.jsonの1,628件、CodeQL43/44の164規則0件と全C#1,419件の抽出hash、
final-release-evidence-50.jsonの元report/入力/期限対応を読む。候補/mainのCodeQL分析IDは
2267898755 /2267904411で、API未解決alert0。通常consumerはmutation42後に復元した。
後の正式publishはcommit情報を含む配布binaryを生成しており、昔のtest DLLへそのまま
hash一致を要求したり、元のmutation結果を新しいbinaryの実行と呼んだりしない。

旧出力D:/NeNeCommander/outputs/release-beta6-20261128以下は、
証拠root/completed-release-files-65/outputs/release-beta6-20261128へ元directoryごと移した。
旧worktreeのbin/obj/artifacts/TestResultsは同じ保存rootのworktree-ignored以下に元相対pathで残る。
47,571ファイル4,044,696,998bytes・5リンク定義・23directory IDとmetadataの一致を確認済み。
コピー後の全ファイルhash照合や、絶対リンクの旧targetの到達可能性は主張しない。

D:/NeNeCommander/worktrees/issue-594と旧出力rootは存在しない。build/594-preview-beta6の
175ebd7と公開tag/commitは保持する。他のissue491/495/514/564とpr498-reviewのworktree、
通常アプリ・利用者設定は触れていない。後片付けのhide実行待ちはない。

最終packageの物理キー・IME・画面受入れ、正常終了、全live provider、電源断は未実施。
#94の一度の確認は11月19日完了済み。フォルダーマージはIssue #560の別範囲であり、
このPreview公開で完了・着手したとはみなさない。

詳細は[品質記録](../quality/2026-11-28-preview-beta6.md)と[日報](../reports/2026-11-28-preview-beta6-daily-report.md)。
