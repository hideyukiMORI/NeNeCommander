# 2026-11-19 F6描画区間と完了合図・進捗

Status: PR483統合・今回の作業物整理完了。hide停止まで継続。

Issue #482で、F6処理中の既存の初回描画1回・進捗描画2回を、4つの外側の記録点に加えて
診断コピー上で区切った。元のmapper/session/gateway/provider/再読込/描画とawait順序は維持。
製品repositoryの変更はない。元1,349ファイルのうち差分はhostの観測・診断資源所有と、
composition rootの診断資源解放1行。残る1,347元ファイルは一致し、追加3診断ファイルを含む
1,352ソース入力と9補助入力は測定後も一致している。

完了記録の後にのみ専用eventを通知し、別CLIが出す対応するDONEを読んでから、操作ログ保存・
AX・画像・ファイル確認へ進めた。事前レビューで「不正な記録でも通知できる」点を修正し、
正常記録だけを通知できる条件と異常例の自己検証を追加。全6回の通知と操作記録が一致した。
これにより明示的な観測の順序は確認できたが、入力注入・tool内部・OS等の影響は未計測。
あらゆるI/Oや観測負荷の排除、物理入力から表示完了までの測定とは主張しない。

事前に固定したwarmup1回と本測定5回を専用PID32792で完了。全6回で元0/先2、効果2、未転送0、
左右idle、内容hash一致。本測定totalは44.0052、243.7985、43.4898、27.9512、46.2321ms。
長い2回目も保持し、その234.0125msは3つの同期描画区間の外だった。残りには処理・再読込・
待機・継続・遅延したUI処理・診断負荷などが含まれ、原因や純provider時間は確定していない。
warmup965.8245msも記録し、値を見る前の計画どおり本測定から除外。#479との性能比較や
percentile、#453解決とは扱わない。24外側tick・18描画span・6total・12hashの独立検算が成功。

準備では短いソース断片が2か所に一致して書込前に停止し、対象メソッド全体の照合へ修正。
初回2回のApp build失敗は、diagnosticのpartial/書式/null検証/所有と解放を抑制せず修正。
最終selfcheck/App buildは成功し、警告/error0。終了前の別REPLセルによる件数readbackは
初期値を返してguardが停止したが、closure内の保存操作列とDONEは0〜5の6件で一致していた。
独立照合後に通常終了し、追加F6・標本置換はない。この経緯も保存した。

終了後の手順監査で、流用restoreにRelease指定がなかった点を検出。実測時の全出力2,029file・
188,598,393byteを先にzip保全しsize/hash照合した上で、QLT-014に従う明示Release restore/buildと
実測時355配布ファイルを照合した。354件は完全一致し、実行DLL/EXE/PDB/resource等の差はない。
差は生成API説明XML1件の7説明項目だけで、残るXML内容も一致。最初の全件一致assert失敗と
両XMLを保存し、全件一致とは報告しない。実行入力の同一性によりnative証拠を再利用する。
復元/build後の生成出力8件は別zipへsize/hash保全し、現在2,029file/188,600,262byteを全て収載。
数値・観測順序の独立レビューは必須修正なし。このビルド手順の補正も完了した。
追加の製品testやCI、実測は開始しない。

正確な境界・全数値・コマンド・制約は[品質記録](../quality/2026-11-19-f6-ui-render-spans.md)。
全証拠は`D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/`へ保存。PID32792とobserverは
終了済み、通常38360/元218byte設定/hashは不変。公開beta.2は不変、PR402/63f4769のhide画面受理は別途必要。
PR483は18:42:16に57da22fとして通常squash統合し、受理f348661とtree一致。12数値行/208相対linkと
通常hookが合格。18:42:53に取込/clean/未追跡/ignored/参照0/link0/両archiveと全収載を確認し、
今回のissue-482作業木と診断outputだけを削除、不在確認済み。branch/commit/恒久証拠は保持。

## ! hide実行待ち：後片付け

下記4対象は以前の自動承認レビューが起動前に`blocked by policy`で拒否。別手段でも再試行していない。
保全と取込確認の詳細は[既存報告](2026-11-19-footer-rebind-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
