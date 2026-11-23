# 2026-11-22 日報 — コマンドパレットの幅

#525のパレットを最大640・外余白24の上部中央配置へ変更した。候補の本文と理由を
2対1の伸縮列にし、キー表示をフッターと同じ既存の折り返し部品につないだ。
製品変更はXAML2ファイルだけ。検索・実行・IME・scope・設定・依存は変更していない。
[設計handoff](../design/2026-11-22-compact-command-palette-handoff.md)と
[品質記録](../quality/2026-11-22-compact-command-palette.md)に根拠と全試行を保存した。

Windowを作らず入力も送らない専用WinUI fixtureで、修正前の幅320に対し行376/357、
キー表示417/378のはみ出しを再現。修正後は日英・長文・中国語例の重複なし17件と
枠4件の横方向境界が通った。API登録だけでは使えなかったWrapPanelの失敗と、
中国語fixtureが長い英語タイトルを選んだミスも保持し、該当3件だけ修正確認した。
Appのlocked Release buildは警告/エラー0。共有desktop入力・Window起動・設定変更はない。
全体画面の短い高さ、実DPI、IME、読み上げ、実機フォーカス復帰は未証明と分けている。

恒久証拠 D:/NeNeCommander/evidence/compact-modal-design-20261122。
専用worktree D:/NeNeCommander/worktrees/issue-525、fixture出力は
D:/NeNeCommander/outputs/compact-modal-design-20261122/layout-probe。
#548は同じ設計相談で方針を得たが別実装。#526、#528も別。
#542の送信承認は保留中で、#525を別経路で送ることはしない。PR/統合/整理は未完了。
未統合の両worktreeと証拠は残し、ユーザーAppと未完491/514には触れない。

## 19:43 JST 全体構成の追加確認

非表示の専用WindowでXamlRootへ接続し、125%・実client640/320×800/300の
12条件を測定した。すべて末尾到達・先頭offset0復帰・外枠/案内の収まりを確認。
日英8条件では末尾行全体も収まる。中国語の合成文字列320×300では一覧32に対し
行33.6となり約1.6欠けるため、この1条件は未解決。全12成功とは表現しない。

初回はfixtureの行style未読込でparse失敗し20秒で専用process停止。style補完後は
接続を最初のtickで要求した条件が不適切でexit2。明示的なnativeサイズ設定と
実際の接続待ちへ直した最終版はprocess21264/exit0。3版のsource/logを保持。
製品変更なし、1079入力不変、製品検証は再利用。最終版は表示/activateなしで、
観測した前面windowも不変だった。キー/IME/マウス/focus/tooltip binding、ほかの
倍率と画面受理は未確認。公開承認待ちと未統合の保持方針は変わらない。