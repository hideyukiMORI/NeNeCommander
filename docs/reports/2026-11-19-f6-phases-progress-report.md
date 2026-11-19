# 2026-11-19 F6の非UI区間計測・進捗

Status: diagnostic complete; documentation integration pending; continue until hide stops.

Issue #470で#453の未観測区間を既存provider/read portごとに測った。確認済みbeta.2のDLLを
変更せず、DualPaneSession→FileOperationGateway→Windows providerと左右readを専用hostで実行した。
本物の安全確認・同一性確認・atomic moveを保ち、専用D領域の2fileだけを扱う。

事前計画どおりwarmup1回と計測5回で終了。全6回とも元0/先2、左右一覧0/2、各2hash一致。
計測器の保留・即時・例外・同一結果・呼出回数の自己検証、54区間/12進捗/12hashの独立照合も成功。
5回の非UI全体は57.9852〜67.3116ms、中央値64.0019ms。2回のInspectが51.1665〜57.1311msで
全体の約85〜88%。左右readは各約0.12〜0.20msだった。UI応答や元の体感遅延の原因とは同一視しない。

source上のInspectは移動時にもSTAでごみ箱利用可否を調べている。ただし内部query/STA初期化/identity
等の個別寄与は未分離。安全処理の削除・cache・port変更・最適化はしていない。#453は開いたまま。
正確なコマンド、全5回とwarmup、試行、境界、hashは[品質記録](../quality/2026-11-19-f6-provider-phases.md)。

probe42996は終了、通常38360と元218byte設定/hashは不変。UI起動/操作・設定復元なし。
出力全66file/30,257,615byteを`D:/NeNeCommander/evidence/f6-phases-20261119/runs/`へ保全し、
サイズ/hash一致を確認。文書のみのPRで、通常hook/links/diffを確認する。製品build/test/CIは追加しない。
今回の作業木issue-470とoutputs/f6-phases-20261119は統合・参照確認後に整理する。

Bookmark PR402は`9006020`のDraft。直前の実アクセシビリティ情報確認も記録・push済みだがhide受理は別。
通常アプリと公開beta.2は変更なし。停止指示はなく、作業継続中。

## ! hide実行待ち：後片付け

下記4対象は既に自動承認レビューが起動前に`blocked by policy`で拒否。再試行していない。
保全/取込/参照確認の詳細は[既存報告](2026-11-19-footer-rebind-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
