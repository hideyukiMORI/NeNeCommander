# 2026-11-20 ペインの現在行探索 改善記録 — Issue #501

Status: #501/PR502統合・今回整理完了。hide停止まで継続。

現在行は表示中entry.Pathの同じ参照を保持するという既存の不変条件を確認し、
PaneReducer.GetFocusIndexのパス比較付き列挙を直接参照探索へ変更した。
外部パスのWindows/UNC/WSL同一性判定は既存経路に残る。探索は引き続き線形。
新しい状態・キャッシュ・操作経路・依存は追加していない。

Application103件（新規6件を含む）、Presentation28件が成功し、Release buildは警告/error0。
書式は新規testのLFをCRLFへ同内容訂正後に成功。複雑度は既存26件と同一で増加なし。
独立設計・実装レビューに必須修正なし。詳細なコマンド、失敗、制限、証跡は
[品質記録](../quality/2026-11-20-pane-focus-reference-scan.md)を参照。

固定A1/B1/B2/A2、144バッチ（測定120）を全件保存。1万行末尾付近の1000回平均では、
旧版0.3559993–0.4153311ms/回、変更版0.0782022–0.0885625ms/回。
変更版は全測定192B/回。Reducerと固定検査/ループの合算で、UIやファイル処理の速さは未測定。
初期化・warmup・遅い値・全補正記録を保持し、サンプル差し替えはない。

恒久証跡は `D:/NeNeCommander/evidence/pane-focus-scan-20261120`。
作業木 `D:/NeNeCommander/worktrees/issue-501` と出力 `D:/NeNeCommander/outputs/issue-501` は
全725file/192,725,213Bを保全し、技術受理・main統合・具体パス/参照/リンク検査後に整理済み。
PR402のhide画面受理、#491の未完了native比較、#453、公開beta.2は別件。

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
