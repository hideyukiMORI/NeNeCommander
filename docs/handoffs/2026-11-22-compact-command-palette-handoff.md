# 2026-11-22 引き継ぎ — コマンドパレット幅

#525、feat/525-compact-command-palette、起点main7ef9b96。
D:/NeNeCommander/worktrees/issue-525、証拠D:/NeNeCommander/evidence/compact-modal-design-20261122。
[品質記録](../quality/2026-11-22-compact-command-palette.md)と
[日報](../reports/2026-11-22-compact-command-palette-report.md)を参照。

製品はXAML2ファイルだけ。640最大幅・24外余白、上部中央、本文/理由2対1、
既存KeyHintWrapPanelを利用。新しい入力・実行・layout owner・依存を作っていない。
App build0警告0エラー、detached native geometryの17+4件成功、baseline320の
はみ出しとWrapPanel unavailable、中国語fixture修正を全て保持。
下の追加確認が125%のAPIスクロールと全体構成の一部を補う。Keyboard/IME/visible画面/他倍率/screen reader成功と混同しない。

#542のpushは自動承認拒否でhideの具体的送信承認待ち（head7724d88）。
#525を代わりに送らない。#542/main統合が進んだら、正本の時点情報を確認し、
必要なmain統合は両方のPROJECT_STATE追加を保持する。製品入力の無変更を照合して
既存成功試験を再利用する。文書だけで製品テストを再実行しない。
通常Commit hookとDraft PRから進め、受理tree一致と生成物保全/links/live参照を
確認してから専用worktreeのみ整理する。#548の候補もローカル保存済み（5a26d32）。未完了の証拠と出力を保持する。
閉じるPRとcompletionが実際のintegration/cleanupを確定する。

#523はactivation処理が既存で、報告症状未再現。#453/#491とAppSDK#514/PR517は別。
ユーザーApp issue495、設定、未完出力に触れず、共有desktop入力禁止を維持。
hideの継続指示は有効。

19:43追加: D:/NeNeCommander/outputs/compact-modal-design-20261122/attached-probe。
元のlayout-probeは保持。実XamlRoot・125%・640/320×800/300の12条件で末尾到達と
先頭offset0復帰、modal/hint収まりが成功。行全体は日英8/8、合成中国語3/4。
中国語320×300はviewport32/row33.6で約1.6欠け、未解決。修正や合否緩和をしていない。
full-attached-lifecycle-layout.json、attached-scroll-comparison.json、source/hashを読む。
style欠落で20秒停止、単一tick接続想定でexit2という2失敗も保存。最終21264はexit0、
表示/activateなし、観測foreground不変。1079入力不変で製品build/testを再実行しない。
最終fixtureは明示的に自己所有windowをサイズ設定し、実接続を20秒上限内で待つ。
中国語/短高さの可読性、native入力/IME/tooltip/他倍率/画面確認と公開承認が残る。