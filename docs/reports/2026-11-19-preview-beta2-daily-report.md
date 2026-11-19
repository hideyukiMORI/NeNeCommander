# 2026-11-19 Preview beta.2 公開日報

Status: published and public assets verified; milestone work continues until hide stops it.

hideの「区切りのいいところでプレビューリリース」という依頼に従い、
[v0.1.0-beta.2](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.2)
を12:19:28 JSTに公開した。配布元はPR461を統合したmain `555704e`。
F6進捗表示のリソース修正と、狭幅フッターの折返し・通常キー案内の表示設定を含む。
hideの画面受理待ちのBookmark PR402は含めていない。

## 公開証拠

- deep review 44441599491は12:11:50 JSTに成功。正典gateは2,065成功、失敗0、
  live opt-in skip 16。依存監査・adversarial反復・全4層mutationも成功した。
  同reviewに含まれるqualityを別workflowで繰り返していない。
- CodeQL analysis 2201989996は検出0・errorなし。別途ページングしたopen alert APIも0件。
  review元`5197ca8`と配布元`555704e`のツリー完全一致を確認した。
- 公式publishをcleanなaccepted sourceで実行し、警告/エラー0で生成した。
  586 payload/ZIP/独立展開ファイル、1,323 source inputs、10 locks、元license/notice、
  11 XBF、日英350項目ずつを照合した。
- 展開版の専用窓で左右一覧、F1ヘルプ、新設定の表示と通常終了を確認した。
  設定を変更せず、終了後も元の218byteのhashが一致。通常アプリと他案件を保持した。
- 公開3assetを認証情報なしで再取得し、12:20:18にsize/SHA-256/service digestすべて一致。
  annotated tagの参照先も`555704e`。正確なコマンドとhashは[品質記録](../quality/2026-11-19-preview-beta2.md)。

## 並行したBookmarkの確定

PR458を機能ブランチへ統合し、mainのschema/フッター/focus修正と版・文書を同期した。
結合後のApplication 206、Presentation 74、view所有権24が成功、App buildは警告/エラー0。
独立した結合source reviewに必須指摘なし。空フォルダー移動直後にキーだけのCtrl+Bで再表示できた。
807×658ピクセルでは空一覧と登録フォームが収まり、Tabで保存/取消へ到達しEnterで取消できた。
寸法調整時の試行、取得の遅れ、pointerによるfocus確立、未観測範囲もfeature側の記録へ残した。
PR402はDraftのまま、説明を最終実装・証拠へ更新済み。hideの画面受理は代替しない。

## 遅延調査と限界

#453のF6遅延はまだ測定していない。sourceでは移動後に左右の一覧を順に再読込する経路を確認し、
入力・gateway/provider・左右refresh・描画を分離する計画を準備した。作業中のDドライブ競合を
元の遅延原因とは扱わない。既存WPR等の存在確認と公式資料の調査は行ったが、11:53の自動承認
レビューがMicrosoft公式の計測CLI取得・展開・help起動を実行前に`blocked by policy`で拒否した。
取得も実行も行われず、別手段で再試行していない。F6の性能値や改善は主張しない。

公開は署名なしPreview。新規Windows環境、明示DPI、英語実表示、新フッターのHC/Narrator、
全provider live環境は今回の証拠外。#94はhideの報告で完了済みだが、正確な倍率と独立画像は未記録。
通常Cアプリとそのfixture、PR402の未完了作業物は保持している。

恒久証拠: `D:/NeNeCommander/evidence/release-beta2-20261119/`。
公式配布候補: `D:/NeNeCommander/releases/0.1.0-beta.2/candidate-20261119-01/`。
公開記録の変更は文書のみで、product test/build/CIを追加実行しない。

## ! hide実行待ち：後片付け

旧拒否15対象は10:47の読み取りで不在を確認済み。次の#454完了分2対象は出力15ファイルを
恒久証拠へ保全・hash照合し、安全確認後の削除が11:05の自動承認レビューにより起動前に
`blocked by policy`で拒否された。削除は実行されず、別手段で再試行していない。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`

hide実行用（branch/commit/恒久証拠を保持）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/footer-20261119/cleanup-for-hide.ps1
```
