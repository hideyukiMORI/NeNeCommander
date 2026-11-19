# 2026-11-19 F6非UI区間計測・引き継ぎ

Status: #470 diagnostic complete; documentation integration pending; continue until hide stops.

1. #453は調査中。今回#470の記録は非UIの既存port区間のみ。nativeキー→完了表示、内部STA/query、
   AVやcold/percentileは未測定。UIを含まない値を以前のnative値へ足し引きしない。
2. 不変なbeta.2 source555704eのDomain/Application/Infrastructure DLLを直接使用。main3865060との
   当該source差分0。DualPaneSession Move→FileOperationGateway→既存provider/readが唯一の実行経路。
   wrapperは結果/例外を変えず、専用fixture以外・atomic move以外を拒否。製品変更なし。
3. 事前の1warmup+5測定を完了。5回全体58〜67ms、Inspect2回が51〜57msで約85〜88%。
   source上のInspectは移動時にもSTAとrecycle capability queryを使うが、その内訳の原因確定はしない。
4. 自己proof成功、probe build警告/エラー0、計測6回成功、独立54区間/12進捗/12hash照合成功。
   正確な値・コマンド・制約は[品質記録](../quality/2026-11-19-f6-provider-phases.md)。
5. 恒久証拠は`D:/NeNeCommander/evidence/f6-phases-20261119/`。全66outputファイル保全/hash一致。
   probe42996終了、通常38360/設定元218byte/hash不変。UI操作/設定復元不要。追加build/test/CI不要。
6. 文書branchは`docs/470-f6-provider-phases`、D作業木issue-470。main統合と保全/clean/参照/link確認後に
   今回のworktree/outputだけを整理する。未受理Bookmark PR402 (`9006020`)のD作業木は残す。
7. 次はInspect内部の寄与を分ける観測方法を検討する。削除安全性を弱めたり、未受理の共通port契約を
   変えたり、計画済み5回を理由なく増やしたりしない。hide停止まで継続の依頼は有効。

## ! hide実行待ち：後片付け

以前の自動承認レビューで起動前に`blocked by policy`となった4対象。拒否した削除を別手段で再試行しない。
保全内容と理由は[進捗報告](../reports/2026-11-19-f6-phases-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
