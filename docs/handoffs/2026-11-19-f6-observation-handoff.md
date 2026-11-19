# 2026-11-19 F6限定観測・引き継ぎ

Status: continue milestone work until hide stops it. #453 is not complete.

1. #465は[限定XAML観測](../quality/2026-11-19-f6-xaml-observation.md)の文書checkpoint。
   QPC照合済みKeyDown13.5978ms/最長Frame54.0335msはF6全体の待ち時間ではない。
   ファイル処理、安全確認、左右再読込、実表示の各区間と原因は未確定。未観測のまま回数を増やさない。
2. 記録器selftest04は対象79/negative0、連番・欠落0で成功。pilot01は欠落11,508で時間不採用。
   pilot02は5,315 XAMLイベント・欠落0、全1,534区間の生QPCを照合。XML由来の位相時間は使わない。
3. 改善候補は同じキー案内の毎回の再bind。現時点はsource仮説だけで、traceの原因ではない。
   #453は調査専用。製品変更は別Issue、限定した比較と所有層・境界の試験を伴わせる。
4. 2回のF6移動とも2ファイルのhash一致。owned app/emitter/traceは終了。通常PID38360と
   元fixtureを保持。元設定218B/SHA256 ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5不変。
5. 恒久証拠は`D:/NeNeCommander/evidence/f6-latency-20261119/`。
   `runs/`の97ファイルを全size/hash照合済み。追加worktree`D:/NeNeCommander/worktrees/issue-465`
   とoutput`D:/NeNeCommander/outputs/f6-latency-20261119`は統合後の整理対象。
6. PR402はfeature `ddc16f4`でDraft。PR464のnative-input修正を含むが、hideの画面受理は別。
   featureの品質記録・既存の選択検証を使い、mainへ未受理のBookmarkを入れない。
   通常/公開beta.2にはBookmark featureはない。beta.2公開照合は既存のrelease記録が所有する。
7. 公式winappCli取得/展開/helpは11:53に起動前拒否。取得も実行もなく、再試行しない。
   今回の録画は既存Windows APIによる専用プロセス限定方式。製品依存・権限は変更していない。

不変条件は既存F6経路と安全確認の維持。変更ファイルは品質/進捗/引き継ぎ/PROJECT_STATEの文書のみ。
選択範囲はADR-0061の文書のみ、相対リンク・数値・diff・通常commit hook。製品build/test/CIなし。
環境に依存する未観測条件は品質記録に残す。追加ADR/waiverなし。

## ! hide実行待ち：後片付け

下記4対象は自動承認レビューで起動前に`blocked by policy`となった既存の削除。
保全済みの証拠・branch/commit・公式candidate・通常アプリ・未完了Bookmarkは保持し、
拒否済み削除を別手段で再試行しない。詳細は[進捗報告](../reports/2026-11-19-f6-observation-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
