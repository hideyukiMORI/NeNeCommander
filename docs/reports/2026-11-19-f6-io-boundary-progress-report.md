# 2026-11-19 F6 I/O境界・進捗報告

Status: #485/PR486統合・今回の整理完了。hide停止まで継続。

通常のF6実行経路を維持したD診断コピーで、I/Oの投入・開始・終了を分けて記録した。
専用アプリ41792でwarmup1回＋測定5回を完了し、全12ファイルの移動結果・内容・完了合図が一致。
測定totalは24.8712〜29.3126ms、9 callback合計は5.8750〜6.8247ms。
投入から開始の合計は0.2063〜0.3367msで、残りは処理間と前後の区間にある。
5測定とも最大の処理間区間は最初のatomic capability完了から次の投入までだった。
その区間は同期描画3区間とは重ならないが、実行・待機の原因はこの記録から断定できない。

固定標本以外の追加試行はしない。前回#482の長い値は保持し、異なる計測条件との改善比較や
percentile、純I/O時間、実表示遅延、#453解決とは扱わない。初期失敗とwarmupも保存した。

不変条件は既存scheduler/task/await・安全検査・呼出回数/順序。元1,352ファイル中3箇所のみ診断変更、
追加4箇所込み1,356入力と12helperのhash一致。製品コード/依存/gate/通常app/公開beta.2は不変。
最終診断selfcheck、明示Release restore/buildは成功、警告/error0。54 callback/162tick、
168分割、18描画span、6totalと全hashの検算が成功した。詳細と正確なcommandは
[品質記録](../quality/2026-11-19-f6-io-boundary.md)に記載。文書のみの統合で製品test/CIを追加しない。

各F6後は対応DONEを確認してから明示log保存・AX/画像・fixture読取。tool/OS内部影響は未測定。
専用41792は通常終了、observer exit0、通常38360と元218B設定は保持。全記録の恒久保存先は
`D:/NeNeCommander/evidence/f6-io-boundary-20261119/`。
PR402はDraft f6859fa、機能63f4769の画面はhide受理待ち。#453はOPEN。

独立事後レビューは必須修正なし。全2,007出力/243,657,084Bはzipに保全し、全件size/SHA256一致。

PR486は19:23:58に12a6c13として通常squash、受理a0f10abとtree一致。23数値行/209link/通常hook合格。
今回のissue-485作業木とf6-io-boundary出力は19:24:12に確認付き整理を完了し、不在確認済み。
branch/commit/全恒久証拠は保持し、過去の拒否4対象は再試行していない。

## ! hide実行待ち：後片付け

以前の自動承認レビューが起動前に`blocked by policy`で拒否した4対象。別手段でも再試行しない。
ソース・試遊・配布物・ログ等は各恒久evidenceに保存済み。今回の#485整理とは別件。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
