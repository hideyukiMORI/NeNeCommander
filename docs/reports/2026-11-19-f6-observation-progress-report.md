# 2026-11-19 F6限定観測・進捗報告

Status: continuing authorized work; this is not a session stop or Issue #453 completion.

公開beta.2の専用窓で、SQL106B/TXT105BをF6で移動した。2回ともsource0、target2、hash一致。
通常アプリPID38360は保持し、専用窓と記録sessionは終了、元設定218Bも不変だった。

最初のtraceは11,508イベント欠落のため時間評価から除外。記録量とbufferを調整した
次のpilotは5,315 XAMLイベント、対象PID以外0、欠落0だった。生QPCによるKeyDown13.5978ms、
最長Frame54.0335msを確認したが、F6全体・ファイル処理・左右再読込・実表示の時間ではない。
解析途中のXML時刻変換の不整合も記録し、1,534区間を元QPCで照合した。

[品質記録](../quality/2026-11-19-f6-xaml-observation.md)に全試行、数値、正確な適用範囲、
コマンド、失敗・未観測区間を収載。97ファイル/22,800,337Bを恒久証拠へ保全・全hash照合済み。
成果は#465の文書checkpointとして扱い、調査#453はopenのまま。製品の最適化は未実装。
製品build/test/CIは実行していない。文書・リンク・通常hookだけを確認する。

Bookmarkの最新PR464はfeatureだけへ統合され、PR402はDraftのまま。
入力・IME開始時の遅延focus取消修正と選択検証はfeatureの品質記録が所有する。
hideの画面受理なしにmainへ取り込まない。公開beta.2は既存のまま。

このcheckpointの追加worktreeは`D:/NeNeCommander/worktrees/issue-465`、
一時出力は`D:/NeNeCommander/outputs/f6-latency-20261119`。文書PR統合後に稼働参照・リンク・
未保存物を確認して整理する。恒久証拠`D:/NeNeCommander/evidence/f6-latency-20261119`は保持する。

## ! hide実行待ち：後片付け

既存の4対象は自動承認レビューにより起動前に`blocked by policy`で削除が拒否された。
別手段で再試行していない。#454の15出力、#459の4作業ファイルと公式candidateを保全し、
展開586ファイルの一致も確認済み。branch/commit/恒久証拠/通常アプリ/未完了Bookmarkを保持する。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
