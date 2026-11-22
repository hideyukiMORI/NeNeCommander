# 2026-11-22 引き継ぎ — 一覧フォーカスの循環

現在の対象は#524、branch feat/524-cyclic-pane-focus、
D:/NeNeCommander/worktrees/issue-524。起点main2dbef98。
[品質記録](../quality/2026-11-22-cyclic-pane-focus.md)と
[日報](../reports/2026-11-22-cyclic-pane-focus-report.md)を参照する。

実装はPaneReducer.Applyの単一行移動のindex計算だけ。表示中の項目内で循環し、
空/1件、選択、owned path、表示projection、history、左右独立を保持。
ページ移動とfirst/last、他モーダルは既存のまま。正本COMMAND/KEYBOARDを更新した。
新しい依存・command・provider I/O・設定・UIレイアウトはない。

旧コードの6件の因果失敗を保存。Application291件は最初289成功/旧期待2失敗、
期待列を補正後のownership12件成功と他の不変成功証拠を組み合わせる。
Presentation37件、App/対象test build警告エラー0、変更format成功。
CS-013は既存5件のまま。改行だけの補正は内容一致を記録済み。
コードが変わらなければcommit/push/mergeのために再試験しない。

通常Commit hookとDraft PR、最終差分/未解決thread/証拠確認後のReady、
固定head条件の通常squash、main ff同期、受理tree一致確認を続ける。
その後、生成物の保全/全件照合、未保存差分・唯一成果物・links・live参照確認を経て、
専用worktreeのみを整理する。branch/commitと恒久証拠は保持。

beta.3は公開済み。再公開・再tag・新たなSARIF送信承認は不要。
D:/NeNeCommander/evidence/release-beta3-20261122の保全/cleanup処理が先行作業として残る。
同completion.jsonが存在しなければ整理完了を主張しない。
issue495ユーザーApp、未完了491/514/pr498は対象外。
共有デスクトップ入力・ユーザーApp終了・古い設定backupの復元は行わない。

hideの継続指示は有効。次の候補は#550の移動後focusか#523の入力復帰調査。
native条件が必要な箇所は未実施を明記し、managed/API証拠で実機入力を証明したと扱わない。
