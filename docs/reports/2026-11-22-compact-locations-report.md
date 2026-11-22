# 2026-11-22 日報 — Ctrl+GのLocations幅

#548をmain7ef9b96から独立して実装した。最大幅520、外余白24で上部中央へ配置し、
長いWSL名とドライブ情報は折返し、キー案内は既存KeyHintWrapPanelを使う。
製品変更はXAML2ファイル。session/catalog/navigation/keyboard/設定は未変更。
[設計引継ぎ](../design/2026-11-22-compact-locations-handoff.md)と
[全試行・品質記録](../quality/2026-11-22-compact-locations.md)に判断と制約を残した。

変更前は狭い幅272/128に長いWSL行が496で広がり、詳細やキー案内が欠けた。
変更後24行/案内条件と4外枠条件を照合済み。App Release buildは0警告0エラー。
狭幅の長い行は最大196まで高くなるため、可変行高と一覧スクロールが受理項目。

一覧を含む12条件の追加fixtureは、Windowなしでは終了時にWinUIで異常終了した。
終了の遅延も同じ結果。非表示・非アクティブの専用Windowを作ると正常終了したが、
XamlRootは未接続のまま。領域の収まりは確認できてもScrollIntoViewで末尾は実体化せず、
実際のスクロール到達は未証明として保持した。失敗ログ・元fixture・補正を全保存。
ユーザー画面の表示・キー/マウス入力・通常App・設定操作はしていない。

作業コピーD:/NeNeCommander/worktrees/issue-548、恒久証拠
D:/NeNeCommander/evidence/compact-locations-20261122。最終1079入力を固定した。
ネイティブ境界と画面受理は残っており、Issue完了/main統合とはしない。
#542の公開送信は自動承認拒否によりhideの具体的承認待ち。#525と#548もローカル保持し、
別ブランチを送る回避は行わない。公開済beta.3、#524/#550統合済みの結果は維持。
