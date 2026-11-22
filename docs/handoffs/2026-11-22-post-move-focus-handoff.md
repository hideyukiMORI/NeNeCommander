# 2026-11-22 引き継ぎ — F6移動後のフォーカス復帰

対象#550、branch fix/550-post-move-focus、起点main0ffcac6。
worktree D:/NeNeCommander/worktrees/issue-550。
[品質記録](../quality/2026-11-22-post-move-focus.md)と
[日報](../reports/2026-11-22-post-move-focus-report.md)を参照する。

PaneReducerの同じ隣接走査を、旧表示順と新一覧の可視identity集合にも適用する。
PaneSessionの成功したPreserve読込だけがこの回復を呼ぶ。元focusが消えたときは
旧順の次の残存項目→前の残存項目→新一覧の既定focus。選ぶpathは新一覧の所有物。
既存focus、explicit新規/rename対象、selection clearing、history、stale read、
左右、sort/hiddenを保持する。操作gate/provider/設定/key mapは未変更。

修正前10失敗/8成功、修正後追加21成功、既存244件のうち旧期待1件を修正して
その1件のみ再検証済み。重複なしApplication265、Presentation52、build警告エラー0、
format成功、CS-013既存5件の完全一致。初回test-only build6診断もログ保持。
final-inputs.jsonが非Markdown入力を固定。文書・commit/push/mergeだけで再試験しない。

通常Commit hook→Draft PR→最終差分/未解決thread/証拠確認→Ready→固定headの
通常squash→main ff・受理tree一致を行う。完了後、生成物保全・全件照合、未保存差分・
リンク・稼働参照・唯一成果物を確認し、専用worktreeのみ整理。branch/commit/恒久証拠を保持。
closing PRとcompletion.jsonが実際の統合/cleanupを確定する。

beta.3は公開済み、#524は統合・専用cleanup済み。再公開やSARIF再送は不要。
release側の整理実結果はD:/NeNeCommander/evidence/release-beta3-20261122/completion.json。
ユーザーAppのissue495、未完了491/514/pr498と関連outputを削除しない。
共有デスクトップ入力・ユーザーApp終了・過去設定backupの復元は禁止を維持。

#550の提案にあったowned native確認は未実施。今回の限定Application変更は
ADR-0061の対象状態/表示試験で受理し、実機入力成功と混同しない。#453/#491の
応答時間証明や#523の再アクティブ化を解決済みとはしない。次は#523の既存activation/
focus/input lifecycleをread-onlyで調べ、隔離されていない入力操作を再開しない。
hideの継続指示は有効。
