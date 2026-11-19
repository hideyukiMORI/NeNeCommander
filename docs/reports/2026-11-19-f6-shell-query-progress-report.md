# 2026-11-19 Shell問い合わせ計測・進捗

Status: measurements complete; documentation integration and task cleanup pending; continue until hide stops.

#473で、移動前Inspectが使うread-only native APIだけを専用D領域の2fileで観測した。
製品の実行経路・安全性・portは不変。queryのごみ箱件数/容量は保存せず、HRESULTと時刻だけを記録。

最初のwarmupは診断のthread join失敗で中断。native呼出は成功したが有効標本から除外し、元source・
log・結果を保持した。完了継続が同じworkerでself-joinする条件を専用proofで再現し、診断だけの
非同期継続指定で解消した。製品schedulerにjoinはなく、製品変更はしていない。

修正後はwarmup1組＋計測5組を1回だけ実行。query2回の合計は30.0840〜31.6343ms、中央値30.6415ms。
全12call/72span/12STA終了・元file hashを確認。非UI Inspect全体や体感停止時間への寄与率は断定せず、
以前の値と足し引きしない。#453は継続。正確な全コマンド・失敗・数値・限界は
[品質記録](../quality/2026-11-19-f6-shell-query-observation.md)。

probe40872終了、通常38360と元218byte設定/hash不変。全60file/30,267,365byteを
`D:/NeNeCommander/evidence/f6-shell-query-20261119/runs/`へ保全しサイズ/hash一致。
文書以外のrepo変更なし。通常hook/links/diff/数値照合のみで製品build/test/CIは追加しない。
main統合と今回D作業木/output整理は未完了。PR402は9006020のDraftでhideの画面受理待ち。
公開beta.2と通常アプリは不変。今回の知見を安全性を保つ設計検討へ渡す。

## ! hide実行待ち：後片付け

以前の自動承認レビューが起動前に`blocked by policy`で拒否した4対象。再試行していない。
詳細は[既存報告](2026-11-19-footer-rebind-progress-report.md)。今回の未完了対象とは別。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
