# 引き継ぎ 2026年10月18日 名前の自然順

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)、仕様は[ADR-0074](../adr/0074-natural-name-ordering.md)、
正確なコマンドと検証結果は[日報](../reports/2026-10-18-natural-name-order-daily-report.md)。
[Issue #246](https://github.com/hideyukiMORI/NeNeCommander/issues/246)の旧保留はhideの自然順選択で解除された。

`EntryOrdering.Apply`のprivate比較だけが製品変更。数字はASCIIのみ、値が同じならゼロ埋めの
短い方が先、非数字はOrdinalIgnoreCase、全体同値はOrdinalで決める。
名前キーの両方向と他キーの昇順name tie-breakに適用し、provider identityやcanonical listingは変えない。
新しい設定、並行sort経路、依存はない。

Application87件、Presentation29件相当（28件の成功再利用＋修正1件成功）が有効。
対象Release build警告/エラー0、変更C# format成功。通常CommitチェックとPR/統合結果はPRおよび
`D:/NeNeCommander/evidence/issue-246/completion.json`を読む。full/deep、全件回帰、coverage/mutation、
desktop/live providerは範囲外で未実行。#94は別の手動release確認。

作業branchは`feat/246-natural-name-order`。統合後はmain同期・cleanを確認し、
`D:/NeNeCommander/worktrees/issue-246`と`D:/NeNeCommander/outputs/issue-246`をpreflight後に整理する。
恒久証拠、branch/commit、共有SDKを保持し、後片付け完了はcompletion記録を確認する。
Issue #306のrecord閉包案はproposedのままで、今回実装していない。
