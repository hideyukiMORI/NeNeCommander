# 2026-11-22 作業報告 — 言語選択の起動順序

#528、main7ef9b96起点の設計調査。製品への言語選択は未実装。
[設計案](../design/2026-11-22-language-selection-investigation.md)と
[品質記録](../quality/2026-11-22-language-selection-investigation.md)に全試行を記録。

非表示診断13プロセスがexit0、各320通常キーと4つのXAML属性を照合した。
英語へ遅く切り替える2条件で古いloader/static converterが日本語を保持する反例を確認。
リソース初期化前に指定し、次回起動で反映する設計案とした。
root.Languageを指定した日英2条件では子要素言語も一致、地域設定は不変。
Narrator音声・実settings読込・製品再起動は未確認。

Locked restoreと診断build2回は0警告0エラー。起動前guardの成功ログ表記誤り、
実行前の文書生成構文誤りも保持。成功試験を繰り返していない。
既存SettingsSession/validator/storeを拡張するschema5案、旧schemaのSystem初期値、
保存失敗文言を整理。中国語の字体は19:47の回答待ち。

表示/activate/共有desktop入力、ユーザーApp/設定変更は行わず、観測前面window不変。
製品テスト/build/CIなし。通常Commit hookを維持。
branch docs/528-language-selection-design、D:/NeNeCommander/worktrees/issue-528。
証拠D:/NeNeCommander/evidence/language-selection-20261122、診断
D:/NeNeCommander/outputs/language-selection-20261122/probeを保持。
未実装/未統合なので削除しない。#542公開pushと#547 Claude送信は既存承認待ちを維持し、
本branchを回避策として送らない。継続指示は有効、通常App/設定/未完491/514等を保持する。
