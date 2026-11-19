# 2026-11-19 F6のUIホスト区間計測・引き継ぎ

Status: #479/PR480統合・今回の整理完了。hide停止まで継続。

1. #453はOPEN。今回はnative OnKeyDown→mapped Move→operation/refresh後のawait復帰→既存
   RenderSession終了だけを同じ時計で観測。物理キー・XAML完成frame・表示時刻ではない。
2. 基点は受理main1cc5d75。D専用exportのhost観測call＋2診断file以外は完全一致。
   通常session/provider/safety/render順は不変。製品repository変更や新しい実行経路はない。
3. 予定のwarmup1＋本計測5を完了。全6回の状態/hash合格、24tick/18区間/6total独立検算合格。
   本計測全体23.2953〜297.8077ms。5回目の長い295.0270ms中間区間は原因未確定のまま残す。
   外部CUAのkey-log I/OとAX/image採取は測定区間に重なり得る。厳密な外部I/O-free証明なし。
   純粋性能baseline・percentile・過去計測との差・#453解決と主張しない。追加sampleを採らない。
4. 初回診断buildのIDE0032/IDE0037を抑制せず修正。修正後selfcheck/build成功、警告/error0。
   1,348入力は最終hash一致。文書だけの統合に追加製品test/CIは不要。
5. 全試行/source/diff/raw/AX/image/commandsは`D:/NeNeCommander/evidence/f6-ui-boundary-20261119/`。
   全1,950output file/187,569,304byteのzip保全・size/hash検算済み。PID25580終了、通常38360と
   元218byte設定/hash不変。PR480は18:00:00にf11380bへ通常squash統合、受理c8a0a73とtree一致。
   6数値行/204link/通常hook成功。下記2対象はclean/未追跡/ignored/参照0/link0/archive確認後に
   削除し不在確認済み。branch/commit/恒久証拠を保持。
   `D:/NeNeCommander/worktrees/issue-479`、`D:/NeNeCommander/outputs/f6-ui-boundary-20261119`。
6. PR402は63f4769のDraft、hide画面受理待ち。公開beta.2と通常appは不変。過去拒否済み4対象は再試行禁止。
7. 次は中間区間を本当に分離できる観測方法を検討する。外部入力/読取の干渉を記録せず純粋値としない。
   正確な境界・全数値・コマンドは[品質記録](../quality/2026-11-19-f6-ui-boundary.md)。

## ! hide実行待ち：後片付け

以前の自動承認レビューで起動前に`blocked by policy`となった4対象。別手段でも再試行しない。
保全内容は[進捗報告](../reports/2026-11-19-f6-ui-boundary-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
