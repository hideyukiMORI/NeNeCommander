# 2026-11-20 ペイン現在行探索 引き継ぎ — Issue #501

Status: #501/PR502統合・今回整理完了。

- main5add0bb基点、branch `refactor/501-pane-focus-scan`。変更はPaneReducerの探索、
  PaneState契約コメント、新規PaneFocusReferenceTestsと記録文書。
- completed stateのfocusはvisible entry.Path実参照（空ならnull）。外部preferred identity解決は不変。
  探索不一致は内部defect例外。回復前のvisibility一時状態はmovementへ渡さない。
- Application103/Presentation28成功、Release警告/error0、最終書式0。
  EOLとXMLコメントのみの訂正後は成功済み動作証明を再利用。既存複雑度26指摘を分離。
- A/B frozen inputs1015一致、Domain/probe同一、Applicationのみ差異。
  固定4process/144batch/120measured成功。全raw/算術/失敗は恒久evidenceへ保存。
  初回集計のconsole文字コード失敗は訂正済み、計測再試行なし。
- 全144バッチ/24集計行/1015入力は独立検算一致、必須修正なし。通常hook/214linkを確認しPR502をsquash/main同期。
  全725file/192,725,213Bのsize/hash保全と絶対パス/未保存/ignored/link/live参照検査を経て今回2対象だけ整理済み。
- PR402はhide画面受理待ち。#491は2回のnative campaignを停止済みで比較受理0。
  第3campaignを自動開始しない。#453/公開release/設定は本件で変更していない。

詳しいコマンド・境界・範囲表は[品質記録](../quality/2026-11-20-pane-focus-reference-scan.md)。
恒久evidence `D:/NeNeCommander/evidence/pane-focus-scan-20261120`。
今回整理対象は `D:/NeNeCommander/worktrees/issue-501` と `D:/NeNeCommander/outputs/issue-501`。
ブランチ/commit/evidenceは保持。未完了の作業や下記の過去拒否対象には触れない。

PR502は10:57:06 JSTに1aed7baへ通常squashし、受理6216db1とtree一致。
今回の2対象は2026-11-20T10:57:57.5322690+09:00に整理、不在確認済み。ブランチ/commit/恒久証拠保持。

## ! hide実行待ち：後片付け

過去の自動承認レビューが起動前に `blocked by policy` で拒否した4対象。
別の手段で再試行しない。恒久evidenceは保全済み。今回の#501とは別件。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
