# 2026-11-21 表示一覧の所有権修正・進捗

hideの継続指示により単体で再開。Issue #508は、PaneStateの公開可視一覧から内部Listを
変更できる経路を、既存SelectVisibleが作るprivate所有snapshotで閉じる。
表示順・hidden判定・focus実参照・選択・状態発行の所有者を維持した。

修正前は新規9ケース中4件が一覧の変更により失敗。修正後はApplication139件と
Presentation36件が全成功、両Release buildは警告/error0。書式確認成功、CS-013は
既存26指摘と完全一致。初回テスト構文の4analyzer指摘と訂正も保存している。
性能計測・native UI・全件CI・releaseはこの変更の検証に含めていない。

詳細・正確なコマンド・制約は[品質記録](../quality/2026-11-21-visible-entry-ownership.md)。
通常PRの統合と今回作業物の最終整理は、#508を閉じるPRと恒久証拠のcompletion.jsonで読む。
証拠: `D:/NeNeCommander/evidence/visible-entry-ownership-20261121`。

PR402はhideの画面受理待ち。#491の2系列は停止したまま、#453は未解決。
AppSDK PR498も別件。旧#454/#459の後片付け4対象は現在すべて不在で、
11月20日23:53〜23:56の完了記録と今回の不在確認がある。再削除していない。
