# 2026-11-19 F6のUIホスト区間計測・進捗

Status: 診断採取完了。文書統合・今回の作業物整理は未完了。hide停止まで継続。

Issue #479で、受理済みmain1cc5d75の診断コピーに4記録点を入れ、F6受付から既存ビューへの
値設定完了までを測った。移動・安全確認・両ペイン再読込・描画の順序は維持している。
製品repositoryへのcode変更はなく、1,346元ファイル中の差分はhostの観測callだけ。
追加2診断fileを含む1,348入力hashは採取後も一致した。

事前計画のwarmup1回＋計測5回を専用appで完了。全6回とも元0/先2、完了効果2、未転送0、
左右idle、12fileの内容hash一致。24tick/18区間/6totalの独立検算と順序・件数照合も成功。
本計測の全体は23.2953、28.1066、31.1175、47.4008、297.8077ms。最後の長い1回も残した。
その295.0270msは、処理・初回/進捗表示・再読込・待機・継続を含む区間で、原因は未分離。
最終ビュー設定区間は2.6067〜3.3900msだった。値を過去の非UI計測へ足し引きしない。

App内部の記録はbufferのみで、ファイル保存は通常close後。ただし外側のCUA操作側はF6直後に
key logを書きAX/imageを取得した。外部I/O/automationが測定区間外だった証明はなく、厳密な
外部I/O-free条件は未達。純粋な性能値・原因・実表示完了時間とは扱わず、追加試行で置換しない。
直後に一覧が描画途中だったsample02/04/05も保存し、後の確定表示と区別した。#453は未解決。

記録器の順序・無関係入力・上限・欠落・重複などの自己確認は成功。初回buildの書式2errorは
抑制せず修正し、初回source/logを保存。修正後buildは警告/error0。正確なコマンド・全数値・
制約は[品質記録](../quality/2026-11-19-f6-ui-boundary.md)。文書変更に製品test/CIは追加しない。

専用PID25580は通常終了、通常38360/元218byte設定/hashは不変。設定復元不要。
全1,950file/187,569,304byteを恒久evidenceのdiagnostic-output.zipへ保全し、size/hash照合済み。
文書はissue-479作業木でPR化する。今回のコピー/output整理は受理・main統合後。
公開beta.2は不変。Bookmark PR402は63f4769のDraftでhideの画面受理が別途必要。

## ! hide実行待ち：後片付け

下記4対象は以前の自動承認レビューが起動前に`blocked by policy`で拒否。再試行していない。
保全と取込確認の詳細は[既存報告](2026-11-19-footer-rebind-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
