# 2026-11-19 用途別ファイル検査・進捗報告

Status: initial deep review failed; corrected local coverage passes; CodeQL test correction verified; final deep review/integration pending. hideの停止指示まで継続。

#476/ADR-0083で、既存InspectAsyncに検査目的を必須化した。通常削除だけが削除能力を
取得し、コピー・移動・名前変更・ディレクトリ作成は同じidentity検査を保ちながら
不要なShell問い合わせを省く。未取得を明示し、通常削除は全batchの能力が揃わなければ
最初のeffect前に拒否する。verified moveは既存receipt検証を維持した。

新規red/green、既存gateway97件、provider353件＋修正した1件、最終削除経路48件を確認。
件数は重複を含む。App/consumer buildは警告・エラー0。changed whitespaceは成功、
複雑度は既存26件と一致。試験作成時のcompile失敗、positive fixture不足、反射引数の
移行漏れ、最初の過剰なformat呼出、新規複雑度超過と修正も[品質記録](../quality/2026-11-19-inspection-purpose.md)に残した。

独立read-only reviewに必須修正なし。共有portと破壊的操作の受付変更なので、最終候補の
deep reviewが統合条件。PR477/733fb31のrun44473140316は試験2094成功/16環境skip後、
Application分岐99.93%で停止した。成功型を二度照合するswitchを一か所へまとめ、
同じ削除能力条件を維持。局所buildは警告・error0、757試験/分岐100%、whitespace成功、
複雑度26既存である。変更候補のdeep review受理は未完了。quality CIは重ねない。UI全体の待ち時間と#453の解決は主張しない。PR402のhide受理と公開beta.2は別範囲。

固定ABBA順の非UI比較は、各process warmup1＋計測3、全16回の移動/32hashが成功。
同じRelease IL条件で、全Move/refreshは変更前44.4178〜46.6596ms、変更後7.5380〜8.4309ms。
Inspect2回は36.9129〜39.2614msから0.1128〜0.1787msになった。独立算術で144span/32progressを確認。
小ファイル・同一volume・非UIの限定観測で、体感時間/percentile/元の停止原因へ一般化しない。
数値はswitch整理前の733fb31の観測であり、整理後binaryの計測とは主張しない。
全682output/65,022,426byteを恒久runsへhash一致で保全。probe4process終了、通常38360と元設定は不変。

恒久証拠: `D:/NeNeCommander/evidence/inspection-purpose-20261119/`。
今回のworktree/outputは作業中のため保持し、受理・統合・必要反映・証拠保全後に整理する。

## 16:08時点の修正版確認

修正版6499b0aのdeep review44476340651を16:00:44に開始し、受理は待機中。
最終App buildは警告・error0。専用窓11032で通常のF6から46B/49Bの2ファイルを移動し、
完了表示・両pane再読込・source0/target2・両hash一致を確認。初回のaddress入力setupが
既存C:\BUFFALOへ移動した試行も品質記録へ残した（そこへのfile effectなし）。
専用窓終了を確認し、16:08:37に元設定218byte/hash不変、通常38360保持を照合した。
これは1回の正常系で速度測定ではない。新規fixture2件の保全により全684file/65,022,521byteを保持。
製品入力は修正版と同一。CI受理前のmerge、PR402のhide受理代行、旧拒否削除の再試行をしない。

## 16:18時点のCodeQL指摘修正

初回CodeQL analysis2203018686のAPI読戻しでalert153（cs/linq/missed-select）を確認。
5providerのnull目的を検証するforeachで例外を一時変数へ代入していた。返る例外のParamNameを
直接assertする形へ整理し、同じ5回の呼出・例外型・引数名を維持した。製品変更・抑制・dismissなし。
対象test buildは警告/error0、該当1caseとformat成功。製品inputは不変でnative F6証拠を再利用する。
run44476340651には16:18:15にcancel要求を出した。最終候補deep reviewは未受理。
inputs-codeql-corrected.jsonが1013入力の最終正本。既存deep workflowがCodeQLも含む経路なので、
今回まだ受理されていない共有/破壊境界の最終候補で再dispatchする。qualityは重ねない。

## ! hide実行待ち：後片付け

以前に自動承認レビューが起動前に`blocked by policy`で拒否した4対象。再試行しない。
今回の#476とは別で、元の恒久証拠・branch/commitは保持済み。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
