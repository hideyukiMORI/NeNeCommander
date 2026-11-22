# Current implementation checkpoint — 23:06 JST

Local feature implementation and selected proof are complete; commit/publication follow.
Use [the implementation proof](../quality/2026-11-22-language-selection-implementation.md)
as current evidence. Infrastructure122, Application59, Presentation21; App0 warnings/errors;
398 exact keys/PRI values per locale; changed-file format and112 conformance rules pass;
complexity is the exact existing5. final-input-hashes.json freezes non-Markdown inputs.

Ten copied-code startup states and the injected cancellation branch have bounded evidence.
Explicit host initialization is deferred; do not infer when framework dictionaries materialize.
The fixture substitutes settings/pane paths, activation, focus and diagnostic hooks. Its original
activation1/resource-count failures remain recorded, not retried into equivalent success.
Unmodified startup/restart, physical input/IME/focus/speech and screen fit remain required.
No user App/settings mutation. No #528 public PR/main integration or cleanup yet.
The #547 exact approval is resolved as Draft PR558/65a6fb2; it does not cover #528 payload.

All earlier entries below are time-qualified history.

# 2026-11-22 引き継ぎ — 言語選択の設計調査

#528、docs/528-language-selection-design、main7ef9b96起点。
D:/NeNeCommander/worktrees/issue-528。証拠
D:/NeNeCommander/evidence/language-selection-20261122、診断
D:/NeNeCommander/outputs/language-selection-20261122/probeを保持。
[設計案](../design/2026-11-22-language-selection-investigation.md)、
[品質](../quality/2026-11-22-language-selection-investigation.md)、
[報告](../reports/2026-11-22-language-selection-report.md)を読む。

製品/schema/翻訳未変更。13独立非表示processで各320通常キーと4属性を照合、exit0。
遅い英語指定ではstatic converter/既存loaderが日本語を保持する2反例あり。
指定だけではLanguageがjaのまま。初期化前の指定とroot.Languageで日英の文字と
子要素言語が揃う。地域設定は不変。全条件を「切替成功」とまとめない。

次は中国語の19:47質問（簡体/繁体/両方）の回答を確認。未回答で範囲を決めない。
Systemを維持するchoice/schema5と次回起動反映案を採否判断し、必要なADR/正本を
実装前に整える。0084は別#547が使用中。配置の設計相談も未完。
保存失敗文言の「このsessionでactive」はnext-launch choiceに不正確なので要修正。
queue/旧schema/拒否時保全、focus-only移動、全localeキー/placeholder、
実製品startup/restart、native input/IME・accessible speechが今後の対象。
#525合成中国語320×300の末尾行1.6px不足は未解決。

initial-source/final-source、各JSON/run、verification-summary/logとverify-probe.ps1を保存。
起動前guard誤りと実行前文書生成構文誤りを記録。診断build2回、製品試験なし。
同じ入力で再検証しない。通常hook、Draft PR、選択した証拠による受理を維持。

#542 push（7724d88/19ファイル）と#547 Claude briefの承認は未解決。
別branch/送信手段で拒否を回避しない。#528は未実装/未公開/未統合のため削除しない。
受理/main統合/成果物保全後に専用対象だけ整理し、closing PR/completionに実績を残す。
ユーザーApp issue495/設定、未完491/514/pr498-reviewと他候補を保持。継続指示は有効。

## hide回答の反映 — 2026-11-22 21:34 JST

hideは中国語について「まず簡体字（推奨）」と回答。初回中国語は簡体字で確定。繁体字は今回の初回範囲に加えない。既存の日英と同一の選択/設定/起動経路へ統合する。前段の未回答記述はこの回答で解消。製品実装・schema/翻訳検証・native証拠は引き続き未完了。

#542はPR557/main b82aa1eへ統合済み。#547の2593バイト設計brief送信もhideの明示承認後に完了。過去の承認待ち記載はこの範囲で解消し、別payloadへの許可と混同しない。


## 実装準備の続行 — 22:20 JST

feat/528-language-selectionへ作業branchを切替（旧docs branchは82e0892で保持）。
main b82aa1eを1692b22で取込。proposed ADR-0086の独立技術レビュー待ちで製品未変更。
新schema5正常入力4件の試験を追加、旧製品で4件拒否のred baselineを保存。
locked restore/buildは成功、警告0/エラー0。詳細は新しいimplementation品質記録。
#547 source49files/head65a6fb2の公開pushは22:13に別途自動承認が拒否、質問待ち。
#542承認を他payloadへ広げず、ローカルの言語実装準備を継続する。


## 現在の実装 — 22:57 JST

accepted ADR-0086、branch feat/528-language-selection、HEAD592d74eから実装差分あり。
選択肢/4引数DisplayPreferences/schema5/SettingsSession/設定radio/起動順序/zh-Hansを実装。
Infrastructure122件、Application57件、Presentation21件の対象検証成功（重複なし）。
App build警告0/エラー0。398項目×3言語の原本キー/書式と実PRI値を全照合。
独立レビュー22:41:38で製品の必須修正なし。完全な未改変startup/restartは別OS環境が必要。

専用startup-fixtureは実ソースcopyを使用するが、設定先・初期pane・Activate・focusなどを
差替えており完全な実製品証拠ではない。初回英語の非表示focusでactivation1を記録し停止、
該当focus要求をfixtureだけで除いた後10状態でactivation0/非表示/前面不変。
取消はwindowなし/override不変だがResources2という観測を精査中。通常App/ユーザー設定と
共有desktop入力には触れない。すべての失敗/診断差分/ログはimplementation品質記録と
D:/NeNeCommander/evidence/language-selection-20261122へ保存。

残りは取消の説明、fixture全値照合、最終書式/規約/複雑度比較、入力固定、文書/commit。
実画面・再起動・IME・speech・公開PR/統合/後片付けは未完。#547はhide承認後PR558を
Draftで作成済み（head65a6fb2、49ファイル一致）。古い#547送信保留はこのpayloadのみ解消。
#528への送信承認とは混同しない。未完のworktree/outputsは保持する。
