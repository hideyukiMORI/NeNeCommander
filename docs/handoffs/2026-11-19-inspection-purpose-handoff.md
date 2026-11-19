# 2026-11-19 用途別ファイル検査・引き継ぎ

Status: implementation/local proof complete; deep review and integration pending; continue until hide stops.

1. #476、ADR-0083。唯一のInspectAsyncに閉じた目的を必須化。IdentityOnlyでもidentityは検査し、
   削除能力はNotRequested。通常Deleteは既知能力の全batchが揃うまでeffect/progressを始めない。
   Windows/WSL直接境界もschedule前に拒否。verified receipt/preflight/resumeの既存検証を維持。
2. [品質記録](../quality/2026-11-19-inspection-purpose.md)にred/green、対象既存境界、build、
   format、複雑度26既存、失敗と修正、独立reviewを記録。正確なcommands/logsは
   `D:/NeNeCommander/evidence/inspection-purpose-20261119/`。成功済みの無関係な試験は再実行しない。
3. `D:/NeNeCommander/worktrees/issue-476`、`refactor/476-inspection-purpose`。開始main9268717。
   Draft PRと最終候補のsecurity-deep-review.yml明示dispatchが次。full gate/coverage/adversarial/
   mutation/CodeQLを含むのでqualityは重複させない。受理前のmerge禁止。必要なら比較計画を先に
   固定して非UI計測を行い、#453の原因・解決・体感時間を推測で断定しない。
4. 今回output `D:/NeNeCommander/outputs/inspection-purpose-20261119` は作業中。
   技術受理/main統合/必要反映/恒久証拠保全後、今回分の参照・link・未保存物を検査して整理する。
   通常app/公開beta.2は不変。PR402 Draft9006020はhideの画面受理待ちで、受理を代行しない。

## ! hide実行待ち：後片付け

以前の自動承認レビューで起動前に`blocked by policy`となった4対象を保持。別手段で再試行しない。
恒久証拠、branch/commitは保持済み。詳細は[進捗報告](../reports/2026-11-19-inspection-purpose-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
