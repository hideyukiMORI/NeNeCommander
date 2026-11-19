# 2026-11-19 フッター再bind削減・引き継ぎ

Status: selected implementation proof complete; continue until hide stops the session.

1. Issue #467の製品変更は`DualPanePresenter`の等価list参照再利用と`OperationBarView`の同一参照guard。
   正典`KeyHintPresenter`、順序/内容/automation ID、provider/安全経路は維持する。ADR/waiver追加なし。
2. [品質記録](../quality/2026-11-19-footer-hint-rebinding.md)がコマンド、試行、制約、全4回の数値を所有する。
   赤3件→緑Presentation46件、ownership18件、App/試験build成功。formatはworkspace警告付きexit0。
   Complexity Inspect exit1は既存26指摘不変。全ゲート成功とは呼ばない。影響範囲はADR-0061の限定検証。
3. 同条件の最初2秒でApplyTemplate689/689→357/357、最長Frame約36/36→14/14ms。
   loss0/対象PID/完全pairを確認。合計F6/表示/安全/I/O/左右refresh時間は未測定で#453を閉じない。
4. Nativeでは案内設定、名前入力と取消、F1保持、配色追従を確認。物理held入力、別scale、英語、
   high contrast、Narrator、永久削除確認は今回の実画面証拠に含めない。後者は選択試験で扱う。
5. changed-02のSQL1個はtrace終了後のF8で既存recycle経路に入った。移動後SQLのhashは未取得。
   他3回の計測と両warmupは二項目hash一致。欠けた証拠を再生成したfileや新しい試行で置き換えない。
6. 所有app/traceは終了。通常PID38360/Chrome/元fixture保持。設定は元218B/hashへ復元。
   恒久証拠は`D:/NeNeCommander/evidence/footer-rebind-20261119/`、全73 outputファイルを保全済み。
   `D:/NeNeCommander/worktrees/issue-467`と`D:/NeNeCommander/outputs/footer-rebind-20261119`は
   統合後の整理対象。branch/commit/恒久証拠を残す。
7. PR402は`ddc16f4`のDraftでhideの画面受理待ち。公開beta.2と通常実行binaryはそのまま。
   今回のmain向け修正が統合されたらfeatureへ取り込む範囲を確認する。未受理featureをmainへ入れない。
8. GitHub connector write403と、自動承認レビューのcleanup拒否は別。前者は認証済みghで作業できた。
   credentialを表示しない。拒否済みcleanupや公式winappCli取得は再試行しない。

## ! hide実行待ち：後片付け

以下は既に自動承認レビューで起動前に`blocked by policy`となった4対象。削除を別手段で再試行しない。
保全内容と理由は[進捗報告](../reports/2026-11-19-footer-rebind-progress-report.md)参照。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
