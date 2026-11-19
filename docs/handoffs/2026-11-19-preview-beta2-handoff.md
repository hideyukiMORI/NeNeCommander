# 2026-11-19 Preview beta.2 引き継ぎ

Status: public Preview verified; continue authorized milestone work until hide stops it.

## 現在の事実

- beta.2は12:19:28 JST公開済み。release id 450836498、tagはaccepted main `555704e`。
  公開3assetを12:20:18に匿名再取得しsize/hash/service digest一致。既存beta.1は変更していない。
- deep review 44441599491/CodeQL成功、analysis 2201989996の検出0、別APIでopen alert 0。
  reviewed `5197ca8`とshipping `555704e`のツリー同一。freshnessとartifact digestも照合済み。
- 公式publish、独立展開と全586ファイル、日英350文字列ずつ・11 XBF・元noticeが成功。
  展開版の左右一覧/ヘルプ/新設定を実起動で確認し、設定を変えず終了した。
- [品質記録](../quality/2026-11-19-preview-beta2.md)が正確なコマンドと全hashを所有する。
  `[日時の古い] preparation/pending`記録を現在の公開状態として扱わない。

## 次の作業と保持対象

1. 公開記録PR462はmainへ31af3deとして統合済み。文書のみの差分でproduct build/test/CIを追加していない。
2. issue-459の作業コピーと一時outputの削除は実行前に拒否された。末尾の4対象をhide実行待ちとして保持する。
3. #453はsource調査と計画まで。11:53の自動承認レビューが公式計測CLI取得/展開/helpを
   起動前に拒否したため、同じ操作を別手段で再試行しない。計測値・原因・改善の断定は未成立。
4. PR402はDraftを維持し、hideの画面受理後だけmainへ統合する。#457/PR458とmainの同期は済み、
   combined 206+74+24件と空ペイン復帰、807×658の空一覧/登録フォームの観測を追加した。
   物理長押し、速いfocus競合、分離retry/back、狭幅の実データ行や確認フォーム、DPI/HC/Narratorは別。

通常CアプリPID38360と元のF6 fixtureは保持。各専用UI窓は終了し、元設定の218byte/hashを維持。
Bookmark関連の未完了worktree/outputも保持する。追加コピーはDドライブの専用領域のみ。

恒久証拠は`D:/NeNeCommander/evidence/release-beta2-20261119/`。
公式candidateは`D:/NeNeCommander/releases/0.1.0-beta.2/candidate-20261119-01/`。
task outputは`D:/NeNeCommander/outputs/release-beta2-20261119/`、worktreeは`D:/NeNeCommander/worktrees/issue-459`。
公開記録の統合・整理が未確定なら具体的な状態を追記し、完了扱いで消さない。

## ! hide実行待ち：後片付け

旧拒否15対象は10:47の読み取りで不在を確認済み。#454の2対象は11:05、今回の#459の2対象は
12:29の自動承認レビューにより起動前に`blocked by policy`で拒否された。いずれも削除は未実行で、
別手段で再試行していない。公開記録はPR462/`31af3de`でmainへ統合済み。

#454は15出力ファイル、#459は4作業ファイルを恒久証拠へ保全・hash照合済み。
#459の独立展開586ファイルは保持する公式candidateと全size/hash一致。worktreeはclean、
ignored 803ファイルはbin/obj/artifacts、linksと稼働参照は0だった。
`D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-preflight.json`、
`cleanup-preservation-manifest.json`、`cleanup-blocked.json`が詳細を所有する。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用（branch/commit/恒久証拠/公式candidate/通常アプリ/未完了Bookmarkを保持）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
