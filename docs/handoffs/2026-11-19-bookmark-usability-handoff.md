# 2026-11-19 Bookmark操作性改善の引き継ぎ

Status: ongoing; Issue #457, draft implementation and interrupted native proof

hideは外出中も停止指示まで継続するよう依頼している。現在の優先順はBookmark、#454、#453。
単体実装が既定で、今回の複数専門家は明示依頼に沿う読み取り専用レビュー。
NeNe Commanderリナへの実相談、合意と制限は[デザイン記録](../design/2026-11-19-bookmark-keyboard-review.md)。

## 実装候補と証拠

- mainは`189bb61`、PR402は機能`8c1c551`で未受理。作業木は
  `D:/NeNeCommander/worktrees/issue-457`、branchは`fix/457-bookmark-keyboard-usability`。
  基点`eadeda9`でmainの修正を取り込んだ。Issue PRはこの機能ブランチ向けのDraftとし、
  main／Previewへの統合は行わない。
- 正典はKeyboardIntentMapper/BookmarkManagerKeyboard、BookmarkEditorActionから
  SettingsSession、既存のCommanderSessionナビゲーション。別の実行経路は追加していない。
- Ctrl+Enter、検索hint、保存後の選択・検索解除通知、active/passive表示、入力ラベル、
  割当済み枠、IME／popup所有を実装。通常Enter長押しは独立レビュー後にred/green修正。
- 実起動エラーと空一覧・切替focusの再現を受け、ListViewをtab stopにし、
  BookmarkFocusRestorationで明示要求と行再生成復元を区別した。
  draft／確認画面も同じ遅延機構を使う。後続pointer（handledを含む）とinitial Tabは両queueを取消す。
  details初期focus予約中のEnterは消費し、IME／開いたselectorのnative分岐ではEnterを維持する。
- [品質記録](../quality/2026-11-19-bookmark-keyboard-usability.md)が対象試験、試行、失敗、
  修正、正確な最終コマンドと再利用条件を所有する。UIAのfocus欄は初期アドレスを示したままだったため、
  実表示と次のキー結果を照合した。直後のAX値が表示より遅れたケースもあり、成功を推測しない。

## 次の実機確認

Computer Useが`foreground window did not report a process id`を返し、再接続後も失敗した。
この状態では最終のnative受理を行わない。環境が復帰したら、現在の窓を再発見する。
古いPIDやhandleを流用しない。通常mainのアプリとNibのChromeには入力しない。

1. 現在の設定を読み直し、新しい試験前コピーを保存する。今回の基準hashは
   `ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`で終了時も同一。
   今後異なる場合はその新しい設定を保護し、古いbackupで上書きしない。
2. 対象worktreeのReleaseアプリを起動。空一覧からCtrl+B→Aで名前欄へ入力、Escape後にAで
   再登録できること、検索0件でも一覧へ戻れることを確認する。入力ツールの実用キー名は
   `CTRL+B`、`A`等。modifierや小文字表記の差を製品不具合と混同しない。
3. `__NeNe457_`を接頭辞とする自分の登録だけで、名前／パス／カテゴリ／枠、Ctrl+Enter、
   検証拒否、検索後の保存／取消、先頭選択、J/K、Tab、通常Enterを確認する。
   専用targetはoutputs内fixture/target-aとtarget-b。存在しない専用pathで失敗／再試行を確認する。
4. popupのEnter／Escape、実IMEの確定／取消、全フォームと確認のTab循環、後続Tab／clickによる
   初期focus予約取消を確認する。文字列のUnicode入力だけをIME証明にしない。
   キー保持時間指定はこのComputer Useでは未対応で、物理長押しの証明は残る。
5. 削除は対象とCancel初期focusを確認して取消す。削除本体は既存Application試験で検証済み。
   Narrator、狭幅、倍率は未実施。DPIやセキュリティ設定を勝手に変更しない。
6. アプリ終了後、設定差分が自分の試験項目だけであることを確かめてから、新しい試験前内容へ戻しhash照合。
   予期しない変更があれば保持して調査する。最新の実画像を専門家／リナの相談へ追加する。

今回のowned test processは設定不変確認後に終了済みで、保存した試験登録は0。
raw情報は`D:/NeNeCommander/evidence/bookmark-ux-20261119/`。
worktreeとoutputsは未完了のため保持し、既存15対象の拒否済み削除とは混同しない。
公開beta.1、#386/#397、ADR0051の判断、通常アプリが参照するF6 fixtureは別件。
以下の既存cleanup保留を再試行せず持ち越す。


## ! hide実行待ち：後片付け

以下15対象の削除は自動承認審査が起動前に拒否したため保持する。理由は `blocked by policy` のみ。
Folio6対象、11:28のCommander #439の2対象、11:44の#443出力、12:26の#433出力、
公開後の#434出力と失敗候補01/02が対象。別手段で再試行しない。
main取込、未保存物、ignored出力、唯一の成果物、稼働参照、リンクを確認済み。
Commanderは#439の399file、#443の5file、#433のTRXと95 tool artifacts、#434の112fileと
失敗候補01/02を恒久証拠へ保全しhash照合済み。#433/#443の作業木は削除済み。

- `D:/NeNeFolio/worktrees/935-window-width`
- `D:/NeNeFolio/outputs/935-window-width`
- `D:/NeNeFolio/agents/935-window-width`
- `D:/NeNeFolio/agents/window-width-20261118`
- `D:/NeNeFolio/design/20261118-search-explanation`
- `D:/NeNeFolio/agents/session-20261118-sana`
- `D:/NeNeCommander/worktrees/issue-439`
- `D:/NeNeCommander/outputs/issue-439`
- `D:/NeNeCommander/outputs/issue-443`
- `D:/NeNeCommander/outputs/issue-448`
- `D:/NeNeCommander/outputs/latency-20261118`
- `D:/NeNeCommander/outputs/issue-433`
- `D:/NeNeCommander/outputs/issue-434`
- `D:/NeNeCommander/releases/0.1.0-beta.1/candidate-20261118-01`
- `D:/NeNeCommander/releases/0.1.0-beta.1/candidate-20261118-02`

hide実行用の準備済みコマンド（既に無い対象は許容。サナは実行していない）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/session-20261118/cleanup-requested.ps1
```

対象一覧は`cleanup-targets.json`、保全/拒否の原本は`D:/NeNeCommander/evidence/`内の
各Issueとsessionのフォルダへ保存。Commanderのscriptは固定対象のinventory、Git状態、
参照/リンクを照合し、保存済みhash証拠を再利用してdirectory単位のnative削除だけを行う。
既存scriptの成功記録を再利用し、終了時に更新したmasterと診断用helperの2scriptも構文解析成功。
いずれも実行はしていない。Folioの検査付きscriptを呼ぶ経路は維持した。
ブランチ/commit/恒久証拠/公開shipping/候補03/実機用fixture/未完了PR402の作業木は保持する。
hideの実行報告と不在確認まで後片付けは未完了。
