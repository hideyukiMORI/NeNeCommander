# 日報 — 2026-11-19 実機確認と次回の調査

hideの「今日はこの辺で区切ろう日報と引き継ぎ書を書いて」に従い、記録までで停止する。
作業対象はNeNe Commander。前日の作業は[11月18日の日報](2026-11-18-test-refactor-and-mutant-diagnosis-daily-report.md)に残す。

## 今日の結果

- #94: 通常Release source `6b0d231`を起動。hideが表示・キー/小窓・高コントラストの確認項目2・3・4を「大丈夫」と報告した。正確な選択倍率や各画面の独立captureは無い。
- 専用のSQL106B/TXT105Bを用意し、hideがF6で移動。source空、target2ファイル、両SHA256一致、アプリ継続・完了表示を独立確認できた。今回の移動は成功。
- [#453](https://github.com/hideyukiMORI/NeNeCommander/issues/453)を登録。F6から完了までの1秒未満のもたつきを調査する。体感の報告であり、所要時間や原因は未実測。
- [#454](https://github.com/hideyukiMORI/NeNeCommander/issues/454)を登録。狭幅フッターの欠け、複数行への折り返し、キーヘルプ表示設定を検討する。
- 次回はバックエンドのNeNe Commanderリナに`/design`を使ってもらい、#454の案を相談する。今回は相談・実装を始めていない。

詳細は[品質記録](../quality/2026-11-19-manual-screen-and-f6-check.md)。
#94の完了は一度の確認と結果の記録を意味する。狭幅の問題は残り、全サイズ合格やPR402の受理には広げない。
公開beta.1はF6修正を含まない旧source `46f1a82`のまま。新しい公開作業は始めていない。

## 記録・確認と失敗

README・現在欄・品質記録・この日報・[引き継ぎ](../handoffs/2026-11-19-manual-check-and-followups-handoff.md)を更新。
文書のみなのでADR-0061に従い本文・local link・差分・通常Commit hookを確認し、build/test/CIは追加しない。
実行コマンド・結果・統合commit・記録用作業木の整理は#94のPRと
`D:/NeNeCommander/evidence/session-20261119/completion.json`を正本とする。
規則はQLT-009/011/015、GIT-001〜004、ADR-0054/0061。waiver、schema、依存、gate変更なし。

UI準備では最初の文字入力が表示に反映されず、対応する値の設定とReturnでアドレスを確定した。
自動化のfocus情報は古いままだったため、Tab後の画面で左一覧の選択を確認した。製品不具合とは断定していない。
文書作成の最初の呼出しは構文エラーで実行前に失敗し、作業木に変更が無いことを確認してから書き直した。
終了記録中にFolioの「見出し」欄を案内したのは対象の取り違えであり訂正した。Folioへの編集はしていない。

## 保持物と残り

原本は`D:/NeNeCommander/evidence/f6-check-20261119-0038/`と`session-20261119/`。
通常アプリは開いたまま。左右が参照している`D:/NeNeCommander/outputs/f6-check-20261119-0038/`
（source空、targetに2ファイル）を保持する。下の削除拒否15対象とは別。
PR402、#386/PR420、#397、ADR0051の5引数判断は引き続き保留。


## ! hide実行待ち：後片付け

以下15対象の削除は自動承認審査が起動前に拒否したため保持する。理由は `blocked by policy` のみ。
Folio6対象、11:28のCommander #439の2対象、11:44の#443出力、12:26の#433出力、
公開後の#434出力と失敗候補01/02が対象。別手段で再試行しない。
main取込、未保存物、ignored出力、唯一の成果物、稼働参照、リンクを確認済み。
Commanderは#439の399file、#443の5file、#433のTRXと95 tool artifacts、#434の112fileと
失敗候補01/02を恒久証拠へ保全しhash照合済み。#433/#443の作業木は削除済み。

- `D:/NeNeFolio/worktrees/935-window-width`
- `D:/NeNeFolio/outputs/935-window-width`
- `D:/NeNeFolio/agents/935-window-width`
- `D:/NeNeFolio/agents/window-width-20261118`
- `D:/NeNeFolio/design/20261118-search-explanation`
- `D:/NeNeFolio/agents/session-20261118-sana`
- `D:/NeNeCommander/worktrees/issue-439`
- `D:/NeNeCommander/outputs/issue-439`
- `D:/NeNeCommander/outputs/issue-443`
- `D:/NeNeCommander/outputs/issue-448`
- `D:/NeNeCommander/outputs/latency-20261118`
- `D:/NeNeCommander/outputs/issue-433`
- `D:/NeNeCommander/outputs/issue-434`
- `D:/NeNeCommander/releases/0.1.0-beta.1/candidate-20261118-01`
- `D:/NeNeCommander/releases/0.1.0-beta.1/candidate-20261118-02`

hide実行用の準備済みコマンド（既に無い対象は許容。サナは実行していない）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/session-20261118/cleanup-requested.ps1
```

対象一覧は`cleanup-targets.json`、保全/拒否の原本は`D:/NeNeCommander/evidence/`内の
各Issueとsessionのフォルダへ保存。Commanderのscriptは固定対象のinventory、Git状態、
参照/リンクを照合し、保存済みhash証拠を再利用してdirectory単位のnative削除だけを行う。
既存scriptの成功記録を再利用し、終了時に更新したmasterと診断用helperの2scriptも構文解析成功。
いずれも実行はしていない。Folioの検査付きscriptを呼ぶ経路は維持した。
ブランチ/commit/恒久証拠/公開shipping/候補03/実機用fixture/未完了PR402の作業木は保持する。
hideの実行報告と不在確認まで後片付けは未完了。
