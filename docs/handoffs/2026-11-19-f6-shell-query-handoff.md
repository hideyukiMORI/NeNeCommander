# 2026-11-19 Shell問い合わせ計測・引き継ぎ

Status: PR474 integrated and task cleanup complete; continue until hide stops.

1. #473はread-only native呼出の独立計測。製品Inspect/UI全体は測らず、#453はopen。
   既存gateway/provider、安全確認/identity/revalidation/deletion条件を変えていない。
2. 最初のwarmup1fileは診断の終了待ちで失敗。元source/log/resultを保全し除外。
   controlled self-join proofと診断専用RunContinuationsAsynchronouslyで修正した。
   製品schedulerにjoinなし。元失敗時のcontinuation thread IDは未記録で、原因説明は再現に基づく。
3. 修正後warmup1組＋計測5組、全12call/72span/hash/STA終了/独立算術成功。
   query2回の合計30.0840〜31.6343ms。以前の非UI/XAML値との足し引き、原因/AV/percentile断定は禁止。
4. 恒久証拠`D:/NeNeCommander/evidence/f6-shell-query-20261119/`に全60output保全/hash一致。
   通常38360と元218byte設定/hash不変。probe40872終了。UI操作/設定復元なし。
   正確なcommands/exitsは[品質記録](../quality/2026-11-19-f6-shell-query-observation.md)。
5. PR474は14:58:33にmain `4ee6890`へsquash統合、受理head `f9f6d5c`と全tree一致。
   188links/10数値行/diff/通常hook成功。14:59:05に取込/clean/参照0/link0/全60file保全を確認して
   issue-473作業木/outputを削除し不在確認。branch/commit/恒久証拠を保持。文書のみ、追加test/CI不要。
   未受理PR402の9006020と作業木は保持。hideの画面受理を代行しない。公開beta.2は不変。
6. 次はoperation-specific capabilityの設計妥当性を調べる。削除安全性の弱体化、無根拠なcacheや
   第2の実行経路を追加しない。shared port変更ならADR/影響証明を先に具体化する。

## ! hide実行待ち：後片付け

以前に自動承認レビューで起動前に拒否された4対象。`blocked by policy`の拒否を別手段で再試行しない。
詳細は[進捗報告](../reports/2026-11-19-f6-shell-query-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
