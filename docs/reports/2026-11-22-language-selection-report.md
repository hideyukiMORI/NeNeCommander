# Current ownership correction — 23:58 JST

PR559 is OPEN/Draft at published4a41dc4 after hide's explicit23:10 approval.
The local catalog correction closes a newly reproduced ICollection.SyncRoot array
leak without changing language values/queue/schema/resources/startup. Regression
baseline1 fails as expected; corrected Application12/Infrastructure24/Presentation16
pass, App/test builds have0 warnings/errors, two-file formatting passes and the
same5 CS-013 findings remain. Independent read-only review accepts the correction.
Overall distinct Application61, Infrastructure122, Presentation21; do not add
rerun subsets. See ownership-* evidence and the updated implementation quality record.

Only2 of1092 non-Markdown inputs change. Commit and Draft update remain next;
native/unmodified startup/IME/focus/speech/screen acceptance and main integration/
cleanup remain incomplete. Reuse unaffected proofs and retain the unfinished D tree.
No user App/settings or shared-desktop input was used. Basic-operation TODOs are
registered as Issues560–564 with explicit public-text approval; tracker564 reuses
existing filter/search/mouse/input Issues. No release scope or date is fixed.

All older entries are time-qualified history.

# 2026-11-22 作業報告 — 言語選択の実装

23:06 JST現在、#528をaccepted ADR-0086に沿ってローカル実装。設定画面でSystem・日本語・
English・简体中文を選び、保存した値を次回起動から反映する。既存保存queue、schema5、
旧1-4の非書換えSystem移行を実装。3言語各398項目を実PRIまで全照合した。

対象検証はInfrastructure122、Application59、Presentation21が成功。製品App buildは
警告0/エラー0、変更C#書式成功、規約112項目成功、複雑度は既存5件と完全一致。
新言語型の外部継承・既知コピーは5条件で期待どおり。全コマンド・失敗を含む試行は
[実装品質記録](../quality/2026-11-22-language-selection-implementation.md)に保存。

実ソースcopyの非表示fixtureでは、10正常状態で329通常キー・画面属性・4radioが一致し、
activation/表示0、設定バイト列/地域設定/前面window不変。最初のfocus付きfixtureは
activation1を記録し中止、fixtureのみfocusを除いて区別している。取消はwindowなし・
明示初期化なし。Resourcesの観測2件は評価時点を断定できず、保証を広げない。

未改変製品startup/再起動、native input/focus/IME、speech、狭幅/DPI/HC/画面受理は未完。
公開PR・main統合・後片付けも未完。通常App/ユーザー設定は変更していない。
未完のD作業ツリーと証拠・診断出力を保持する。#547は別途hideの明示承認でPR558をDraft作成済み。

## Earlier investigation — historical

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
