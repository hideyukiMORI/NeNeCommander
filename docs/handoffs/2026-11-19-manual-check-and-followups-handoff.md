# 引き継ぎ — 2026-11-19 実機確認後

## 次回の最初の作業

hideの指示: 「明日でいいので /design をバックエンドのclaudeリナに使ってもらって相談してね」。
次回再開時、バックエンドのNeNe Commanderリナに`/design`を使ってもらい、
[#454](https://github.com/hideyukiMORI/NeNeCommander/issues/454)の狭幅フッターを相談する。
利用できる正式なClaudeの経路を確認する。別モデルをClaudeとして扱わない。
この役割指定は今回の設計相談の範囲に限る。自動予約はしていない。
今日は日報と引き継ぎを書いて終了、という指示に従い停止中。継続goalも停止のまま。

相談する案は、狭い窓でキーと説明の組を複数行へ折り返すことと、設定でキーヘルプだけを表示/非表示にすること。
2段・4段はhideの提案であり固定仕様ではない。操作情報・進捗・結果・警告を残す配置、
一覧に残る高さ、日英・DPI・高コントラスト、設定の既定値・即時反映・保存・移行を比較する。
既存のOperationBarView/OperationKeyHintsとSettingsSessionへ沿う変更範囲を示す。
調査・相談は未着手。実装や画面受理を先取りしない。

## 状態と証拠

- 本体: `C:/Users/info/WORKS/NeNeCommander`。記録前mainは`25856b5`。最終の文書PR、commit、main clean、作業木整理は`D:/NeNeCommander/evidence/session-20261119/completion.json`。
- 通常Releaseはsource `6b0d2316101614ab8bc1cb4a979816e5a98416eb`。実機確認時のPIDは32224、窓は44305820だった。次回は現物を再発見し、古いPIDだけで操作しない。
- #94: hideの表示・キー/小窓・高コントラスト確認項目2・3・4は「大丈夫」の本人報告。選んだ倍率と独立captureは無い。狭幅フッターの欠けは#454に残す。一度の確認実施をもって閉じ、全サイズ良好やPR402受理とはしない。
- F6: hideが専用SQL106B/TXT105Bを移動。source空/target2ファイル/両hash一致/アプリ応答/完了表示を独立確認。1回の管理されたケースであり、元の全条件の再現ではない。詳細は[品質記録](../quality/2026-11-19-manual-screen-and-f6-check.md)。
- [#453](https://github.com/hideyukiMORI/NeNeCommander/issues/453): F6から完了までの1秒未満のもたつきを調査。未着手。キー受付・安全確認・実移動・左右再読込・表示を区分して測る。前日のUIなし約57msやmemory-only値を今回のUI時間と混同しない。
- 公開beta.1はsource `46f1a82`でF6修正を含まない。今回の実機確認だけで次版公開を始めない。公開依頼時には版・publish・成果物と、適用可能な96時間以内のdeep review・CodeQL・依存証拠を確認する。入力不変の成功証拠を再実行しない。
- PR402はDraftで別途の画面受理待ち。#386/PR420、#397、ADR0051の5引数判断も継続保留。

## 保持物と確認範囲

通常アプリは開いたまま。左右が専用fixtureを参照しているため、
`D:/NeNeCommander/outputs/f6-check-20261119-0038/`（source空、targetに2ファイル）を保持する。
前後hashと本人報告は`D:/NeNeCommander/evidence/f6-check-20261119-0038/`。
このfixtureは削除拒否15対象とは別で、稼働参照があるため削除しない。

今回は5文書とIssue記録のみ。build/test/CI、追加性能測定、`/design`、製品変更、公開はしていない。
Windowsの表示設定を自動変更せず、実ファイルを試験へ使っていない。
規則はQLT-009/011/015、GIT-001〜004、ADR-0054/0061。追加waiver、schema、依存pin、gate変更なし。
全経過は[日報](../reports/2026-11-19-manual-check-and-followups-daily-report.md)、
前日分は[11月18日引き継ぎ](2026-11-18-test-refactor-and-mutant-diagnosis-handoff.md)。
過去の継続・並列・初回Preview例外を新しい許可として持ち越さない。


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
