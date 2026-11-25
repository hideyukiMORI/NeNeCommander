# 2026-11-22 引き継ぎ — Ctrl+GのLocations幅

#548、branch feat/548-compact-locations、起点main7ef9b96。
D:/NeNeCommander/worktrees/issue-548。恒久証拠
D:/NeNeCommander/evidence/compact-locations-20261122、専用fixture/output
D:/NeNeCommander/outputs/compact-locations-20261122/layout-probe。
[品質記録](../quality/2026-11-22-compact-locations.md)と
[日報](../reports/2026-11-22-compact-locations-report.md)を参照。

XAML2ファイル：最大520・外余白24・Top中央、name/detail2対1で折返し、
既存KeyHintWrapPanel。C#/resw/key map/session/catalog/provider/設定は未変更。
24行/案内+4外枠の直接geometry成功、App build0警告0エラー。
初期12全体構成は領域だけの限定証拠。Windowなし2回の終了faultを保持。
初期の非表示Windowは正常終了するがXamlRootなし、末尾ScrollIntoViewは未実体化。
19:31の別fixtureはdispatcher進行でXamlRoot接続。125%、実client640/320×800/300の同じ12条件で末尾全体到達と先頭offset0復帰、modal/hint収まりが成功。
native入力/pointer/focus、ほかのDPI/text scale、Narrator、画面受理は成功としない。
API到達は確認済み。可変行高の画面レビューと入力による到達が残り、Issue完了/merge-readyではない。

final-inputs.jsonで1079入力を固定。文書やcommit/pushだけで成功試験を再実行しない。
通常Commit hookは維持。共有デスクトップ入力・ユーザーApp終了/設定変更は行わない。
隔離された実機確認が可能になったときだけ上記の直接境界を検証する。

#542のpushは自動承認拒否でhideの具体的承認待ち（7724d88、19ファイル）。
#525はed2a6d6でローカル保存済み。#548や他branchを送信の回避策にしない。
この3worktreeは未統合のため削除しない。将来統合時はPROJECT_STATEの各追加を保持し、
対象入力の一致を確認して証拠を再利用、通常PR/squash/受理tree一致、成果物保全後に整理。
実際の統合と後片付けはclosing PR/completion記録が確定する。
#523症状未再現、#453/#491とAppSDK#514/PR517は別。ユーザーApp issue495と設定、
未完491/514/pr498-reviewと関連outputを保持。hideの継続指示は有効。

追加証拠: full-attached-scroll-layout.json、attached-scroll-comparison.json、
full-attached-scroll-exit.json、attached-fixture-source/。process24152/exit0、
表示/activateなし、観測した前面window不変。1079入力不変。診断だけrestore/buildし、
製品検証は再利用した。D:/NeNeCommander/outputs/compact-locations-20261122/attached-probe
も未完了のため保持。旧layout-probeと失敗・限定証拠は削除/上書きしていない。