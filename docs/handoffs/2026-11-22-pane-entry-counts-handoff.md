# 2026-11-22 引き継ぎ — ペイン件数表示

#542、branch feat/542-pane-entry-counts、起点main7ef9b96。
専用worktree D:/NeNeCommander/worktrees/issue-542。
[品質記録](../quality/2026-11-22-pane-entry-counts.md)と
[日報](../reports/2026-11-22-pane-entry-counts-report.md)を参照。

既存PaneListingPresenter→PanePresentation→PaneCountsLineの投影のみ。
visible/loadedのfile/folderと明示selectionを分け、欠落・0・部分・前回内容を
区別する。既存snapshotの所有と左右独立を保ち、provider/操作/設定/inputは未変更。
日英resourceと下部TextBlock/Wrap/全文tooltip。中国語出荷やnative確認を主張しない。
新規18+既存61=79件成功、ビルド0警告0エラー、変更format成功、既存CS-013の5件一致。
初回2種の各4診断と修正を保存。final-inputs.jsonが成功証拠の非Markdown入力を固定。

通常Commit hook→Draft PR→差分/未解決thread/証拠の確認→Ready→固定headのsquash、
main ffと受理tree一致、生成物保全・全件照合・未保存/唯一成果物/links/live参照を
確認して専用worktreeだけ整理する。closing PRとcompletion.jsonが実際の完了を示す。
文書やcommit/mergeだけで成功済み製品試験を繰り返さない。

beta.3公開済み。#524/PR555と#550/PR556は統合・専用整理済み。
#523 read-only調査はD:/NeNeCommander/evidence/reactivation-input-20261122/investigation.md。
activation復帰は既に存在。caption siblingとInputSurfaceのevent routeの差は可能性で、
hideの症状の再現ではない。隔離したnative観測なしに推測修正しない。
#453/#491 native応答とAppSDK PR517は別件。ユーザーAppのissue495、未完491/514/
pr-498-reviewと関連outputを消さず、現在の設定を古いbackupで上書きしない。
共有デスクトップ入力禁止。hideの継続指示は有効。
