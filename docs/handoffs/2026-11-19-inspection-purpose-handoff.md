# 2026-11-19 用途別ファイル検査・引き継ぎ

Status: initial deep review failed; local dispatch correction verified; CodeQL test correction verified; final deep review pending; continue until hide stops.

1. #476、ADR-0083。唯一のInspectAsyncに閉じた目的を必須化。IdentityOnlyでもidentityは検査し、
   削除能力はNotRequested。通常Deleteは既知能力の全batchが揃うまでeffect/progressを始めない。
   Windows/WSL直接境界もschedule前に拒否。verified receipt/preflight/resumeの既存検証を維持。
2. [品質記録](../quality/2026-11-19-inspection-purpose.md)にred/green、対象既存境界、build、
   format、複雑度26既存、失敗と修正、独立reviewを記録。正確なcommands/logsは
   `D:/NeNeCommander/evidence/inspection-purpose-20261119/`。成功済みの無関係な試験は再実行しない。
3. `D:/NeNeCommander/worktrees/issue-476`、`refactor/476-inspection-purpose`。開始main9268717。
   PR477/head733fb31に対しsecurity-deep-review.ymlのrun44473140316を15:34:22にdispatch済み。
   初回は試験2094成功/16skip後、Application分岐99.93%で停止。成功型の重複dispatchを
   一か所へ整理し、同じguardを維持した。局所757成功/分岐100%、build/format成功、複雑度26既存。
   gatewayだけが1013入力中で変わり、inputs-corrected.jsonへ保存。修正版deep reviewが必要。
   full gateを含むのでqualityは重複させない。受理前merge禁止。artifact/CodeQLと最終hashを確認する。
   固定ABBA非UI比較は全16move/144span/32progress/32hash成功。全Moveが44.4178〜46.6596msから
   7.5380〜8.4309ms、Inspectが36.9129〜39.2614msから0.1128〜0.1787ms。限定条件のみの結果。
   これはswitch整理前733fb31の数値。最終binaryや#453の原因・解決・体感へ一般化しない。
   全682outputは恒久runsへ保全済み。
4. 今回output `D:/NeNeCommander/outputs/inspection-purpose-20261119` は作業中。
   技術受理/main統合/必要反映/恒久証拠保全後、今回分の参照・link・未保存物を検査して整理する。
   通常app/公開beta.2は不変。PR402 Draft9006020はhideの画面受理待ちで、受理を代行しない。

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
