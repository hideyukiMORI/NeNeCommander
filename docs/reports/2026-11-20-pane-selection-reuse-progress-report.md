# 2026-11-20 選択一覧の所有権・移動時割当 進捗 — Issue #504

Status: 最終受理・PR505通常統合・今回2対象の整理完了。

選択一覧の内部ListがSyncRoot経由で変更できる反例を確認し、単なる参照共有案を停止。
PaneState内の小さなprivate所有型で外部変更経路を閉じたうえで、同じ選択一覧だけを
Transitionで再利用する。外部入力はコピー、新しいPaneStateの発行は維持する。
clamp/往復のrecord等価判定は変わり得るため明記し、session通知と表示を直接検証した。

- 旧実装は所有権・再利用12件中7件失敗。最終Applicationは重複を除き131件、Presentation29件成功。
  初回buildのanalyzer違反は訂正しログ保持。最終Releaseは警告/error0、変更ファイル書式成功。
- 独立実装レビューの指摘に従い、選択済みCopyの既存1件を実行し、選択2件を確認後にDeleteする
  境界1件を追加。既存の複雑度26指摘は変わらず、追加Deleteは35論理行。
- 固定A1/B1/B2/A2の全288バッチ（測定240・事前除外warmup48）と初期化48件を保全。
  1003入力、10配布ファイル、5helper/planの実行前後ハッシュ一致。
- 主指標128選択・先頭付近・移動＋固定検査の割当は1200→96B/call（92%減）。
  2つの選択HashSetを含む同条件は6752→5648B/call。速度には条件ごとのばらつきがある。
  選択2件の初期化は1501216→1524992Bへ増え、原因は未分離。全値を保持し、全般高速化とはしない。

実コマンド、範囲表、失敗・比較限界は[品質記録](../quality/2026-11-20-pane-selection-reuse.md)。
恒久証拠は`D:/NeNeCommander/evidence/pane-selection-reuse-20261120`。
#491の第3native campaignは開始しない。PR402のhide画面受理、#453、releaseは別件。

PR505は23:34:19 JSTに4224a12へ通常squashし、受理3376ed0とtree一致。
773ファイル/194,767,135Bを全サイズ/hash照合して保全し、23:35:31に今回2対象を整理。
通常hook112規約/24脅威登録、216リンク確認成功。最終状態は[日報](2026-11-20-final-daily-report.md)。

## ! hide実行待ち：後片付け

過去の自動承認レビューが起動前に`blocked by policy`で拒否した以下4対象は再試行しない。
恒久証拠・配布candidateは保全済み。#504の今回作業とは別件。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
