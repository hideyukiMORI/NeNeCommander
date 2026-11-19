# 2026-11-19 Bookmark操作性改善の引き継ぎ

Status: ongoing; PR458 integrated into feature PR402; hide acceptance pending

hideの停止指示まで継続する依頼が有効。追加依頼のPreview beta.2は#459で公開と照合を完了した。
単体実装が既定。今回の複数専門家は明示依頼に沿う読み取り専用レビュー。
実際のNeNe Commanderリナへの相談と合意は[デザイン記録](../design/2026-11-19-bookmark-keyboard-review.md)。

## 現在の候補と証拠

- mainは#459/PR461を統合した`555704e`。PR458は11:37にPR402の機能ブランチへ
  squash統合済み（`9ba489a`）。issue-398作業木でmain同期と結合検証を進めた。
  PR402はhideの画面受理前でDraft。main/Previewへは統合しない。
- 正典はKeyboardIntentMapper/BookmarkManagerKeyboard、BookmarkEditorActionからSettingsSession、
  既存CommanderSessionナビゲーション。別の実行経路は追加していない。
- Ctrl+Enter、検索専用案内、保存後の選択と検索解除通知、active/passive表示、入力ラベル、
  割当済み枠、全入力欄のIMEとpopup所有を実装。通常Enter反復はred/green修正済み。
- 実起動リソース例外、空一覧と画面切替のfocus不良を修正。既存BookmarkFocusRestorationで
  明示要求と受動的な復元を区別し、draft/確認の初期focusも所有する。後続pointer/Tabは取消し、
  初期focus予約中のEnterは消費。IMEと開いたselectorはnative入力を維持する。
- 保存案内の消失は同値Select/Search/Filterがreceiptを捨てることをsource reviewで確認。
  完全stale検査とカテゴリ解決を先行したまま、同値入力はcontext/receiptを保ち問題表示を解除する。
  新規12件は旧製品で6失敗/6成功、修正後の関連28件は全成功。独立reviewに必須指摘なし。
  元のnativeイベントの特定はしていない。
- [品質記録](../quality/2026-11-19-bookmark-keyboard-usability.md)が正確なコマンド・試行・再利用条件を所有する。
  Appの修正後buildは警告/エラー0。Inspectは既存7件で追加なし。

## 実機観測と残る範囲

11:07〜11:20のcandidate95549f5で空一覧からの登録/再登録、Ctrl+Enterと保存ボタンEnter、
検索から一覧/編集、検証拒否、カテゴリと枠popup、名前/パス/カテゴリ名の実IME、フォームと
確認のTab循環、両削除確認のCancel初期focusを観測。削除本体は送信していない。
移動失敗は元フォルダーを保持し、専用target作成後に移動成功を読み取ったが、Return/pointerの
試行と観測遅延が重なったためキーだけのretry成功は分離できない。誤った失敗推測は撤回した。
AXのfocus欄は初期addressのままなので、実際の入力先・caret・次のキー結果を照合する。

試験窓だけAlt+F4で閉じ、差分が専用2登録/1カテゴリだけであることを確認して元の218byteへ復元。
SHA-256は`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`。
通常Cアプリと他案件Chromeを保持。保存案内修正後も専用1登録で約69秒の無操作後に
通知と選択の維持をAX/画像で確認し、終了後の元設定復元とhash一致まで完了した。
毎回直前の設定を保全し、終了後に自分の変更だけであることを検証して復元する。

残る範囲は物理長押し、速いTab/pointer競合、今回の検索欄IME、分離したretry/back、狭幅/DPI/HC/Narrator。
main #454の既存focus修正を同期した結合版では、11:59〜12:02に空フォルダーへの移動後、
pointerなしのCtrl+Bで再表示と選択行を確認した。新しいfocus経路は追加していない。
結合後Application 206 / Presentation 74 / view所有権24が成功、App含むbuildは警告/エラー0、
Inspectは既存7件。競合解消C#8ファイルの書式確認はexit 0（workspace警告を記録）。
独立した結合source reviewも必須修正なし。終了後の設定は専用1登録と想定schema移行のみで、
12:03に元の218byteへ復元してhash一致。これらはhideのPR402画面受理を代替しない。

main同期は`15bbf77`、版・文書の追加同期は`380739f`でpush済み。PR402説明も最終実装・証拠へ更新。
12:11〜12:16には807×658ピクセルで空一覧/登録フォームの収まりとTabによる保存/取消への到達、
Enterによる取消を確認した。保存せず設定は完全一致。寸法調整の試行とpointerでのfocus確立は品質記録参照。
狭幅の実データ行・削除/カテゴリフォーム、明示DPI/HC/Narrator等は引き続き未観測。
#459はaccepted mainから公式publishし、586ファイルの独立展開・両言語350項目ずつの照合、
展開版の起動/ヘルプ/設定表示/終了を確認。deep review/CodeQL成功・open alert 0を確認後、
12:19:28 JSTにbeta.2をPreview公開した。公開3assetの匿名再取得照合とmain記録の確定を続ける。
既存公開beta.1と通常稼働アプリはそのまま。#453の遅延調査は未解決。

恒久証拠は`D:/NeNeCommander/evidence/bookmark-ux-20261119/`。
未完了の作業木`D:/NeNeCommander/worktrees/issue-457`と出力`D:/NeNeCommander/outputs/bookmark-ux-20261119`
は保持する。PR402と他の未完了作業、通常アプリが参照するF6 fixtureも保持する。

## 12:54時点の追加結果

mainは公開/後片付け記録まで統合した`5b54b0a`。Preview beta.2の公開3assetは匿名再取得で一致。
Bookmarkの追加修正は`fix/457-bookmark-native-input`、既存issue-457作業木でfeature527ac43から実装。
PR402はDraftのまま、hide受理前にmainへ統合しない。公開物も更新しない。

12:33〜12:40の追加nativeで、キーだけのretry/backと819×658の実データ行・両確認・カテゴリ入力、
検索IMEを確認した。誤ったcategory名の証拠ファイルや速い入力の不確かな順序も品質記録に明記した。
設定は専用1登録/1カテゴリと想定schema移行だけを確認して元の218byteへ復元、hash一致。

source reviewで実入力/IME開始後に古いfocusが残る経路を確認し、rootの既存mapperがnativeへ
渡した入力だけを4 TextBox実focusで観測して既存CancelPendingFocusへつないだ。composition開始も同じ経路。
raw Otherのrepeatはproduced characterの既存guard判断を待つ。新しい状態/キー表/実行経路はない。
App/test build 0警告/0エラー、Presentation27/ViewOwnership24成功、入力1,034件のhash不変、
format成功、Inspect既存7件。独立した実diff reviewで必須修正なし。
12:51〜12:54の専用窓で4入力欄・検索IMEとEnter復帰を確認。保存せず終了、設定完全一致、通常app38360保持。
物理held入力はツール非対応で未確認。決定論的なnative callback競合も未証明。
詳細は[品質記録](../quality/2026-11-19-bookmark-keyboard-usability.md)と
`D:/NeNeCommander/evidence/bookmark-ux-20261119/native-input/`。今回の対象以外の成功証拠は再利用する。

PR464は12:58:05に`2e644bf`として機能ブランチへ統合済み。検証済み40b4c88と全treeが一致する。
main5b54b0aの文書同期でも製品/試験/依存/toolingは不変。既存の成功証拠を再利用し、無関係な再検証をしない。
実機を再起動する場合、issue-457のビルドが今回の修正版である。issue-398の以前のbinは新sourceのbuild証拠ではない。

## 14:11時点のmain同期

main `3865060`の#467フッター再bind削減と#465/#467文書を取り込んだ。PROJECT_STATE先頭だけの
競合は両方の履歴を保持。feature `ddc16f4`からの製品/試験変更はmainと完全一致する4ファイルのみ。
Bookmark固有実装、入力/focus、Application/provider/依存は不変。App/Presentation test buildは
警告・エラー0、通常フッター/Bookmark案内の境界20件が成功。今回記録した1,040入力は全hash不変。
正確なコマンドと証拠は品質記録のShared footer integrationと`evidence/bookmark-ux-20261119/footer-sync/`。
この組合せのnative起動は追加せず、既存の影響範囲の証拠を再利用した。PR402はDraftのまま。
現在sourceのApp buildはissue-398にあり、hashは品質記録参照。通常実行中binary/public beta.2は変更なし。
main側#467のworktree/outputは保全後削除済み。未受理Bookmarkの作業木/証拠は保持する。

## 14:23時点のアクセシビリティ情報確認

feature `277a003`/結合Appを専用process35744で起動し、入力5項目のラベルと、別カテゴリの同名2行の
カテゴリ付きaccessible name/完全path descriptionを確認した。証拠は`accessibility-readback/`、
詳細と失敗したsetup試行は品質記録。Narrator発話、物理held入力、DPI/HCの証明にはしない。
専用窓は終了し、元設定との差分が専用2カテゴリ/2登録と想定schema移行だけと照合して218byteへ復元。
14:23:44の復元hashは上記baselineと一致し、通常38360/他案件Chromeを保持。製品変更・再build/test/CIなし。
PR402はDraftのまま。issue-398作業木は未受理featureを保持するので削除しない。

## 15:54時点のショートカット・再起動確認

変更のないfeature9006020と同じAppで、専用2登録のslotをキー操作で設定した。
Ctrl+1の重複保存は拒否され、2番目の名前/pathのdraftと既存1件を保持。slot2へ修正して保存した。
専用窓終了を確認して再起動し、Ctrl+1で左pane、Tab/Ctrl+2で右paneを各専用targetへ移動できた。
再起動前後の設定517byteは一致し、2登録のslot1/2だけを保持。全9枠や物理長押しの証明ではない。
最終窓を終了し、専用2登録と想定schema移行だけの差分を照合して15:54:19に元218byte/hashへ復元。
通常38360を保持。削除本体・製品変更・追加build/test/CIなし。証拠は恒久`slot-restart/`。
PR402はDraft/画面受理待ちを維持する。

## 16:34時点のカテゴリ名変更確認

不変のfeaturebad2b03/同じAppで、専用カテゴリと登録1件をキーだけで作成した。
カテゴリをEで変更しても所属/path/件数1を保持し、続いてブックマーク名もEで変更できた。
保存JSONは予定した2つの名前と所属参照だけが変わり、再起動前後430byteは一致した。
再起動後のCtrl+B操作/読取りがツールのforeground PIDエラーを返した。1回の再接続では
新しい名前と所属を読めたが、次の取得も同じエラーとなり、追加UI操作を中止した。
再起動後の最終画像は未取得。終了済み18252に続き専用25852だけを停止し、差分照合後
16:34:06に元218byte/hashへ復元。通常38360保持。詳細・状態・画像・失敗試行は
恒久evidence/bookmark-ux-20261119/category-rename/と品質記録。保存済み状態は19件、操作記録は20件。
初回状態の記録helper上書きと文書検証の参照番号修正も明記した。入力1040件は不変で再build/test/CIなし。
PR402はDraftのまま。今回もhideの画面受理を代行せず、未受理fixture/worktreeは保持する。

## 16:40時点の検査契約候補との先行結合

feature26dcb52へPR477候補da42786を先行結合した。main受理ではなく、PR477のdeep reviewは継続中。
競合はPROJECT_STATE先頭だけで、両方の記録を保持。取込38 source/test/toolファイルは候補と一致、
feature固有84ファイルは旧featureと一致し、重複なし。C#の手動編集なし。
App/Application test/Presentation testのRelease buildは警告・error0。feature分割済みのnull guard2件、
palette Delete確認対象、操作modalのBookmark入力所有の計4件が成功。全1061検証入力は不変。
正確なcommands/ログと新App DLL hashは品質記録・恒久inspection-sync/へ保存した。
今回の新しい結合binaryのnative起動はしておらず、前の画面・計測証拠の対象候補を変更しない。
main/通常app/public beta.2不変。PR402のhide画面受理とPR477の技術受理は別々に未完了。

## 17:25時点の受理済みmain同期

PR477/7bcf6deは最終deep review44479037554成功後にmain統合済み。完了記録PR478/1cc5d75も取り込んだ。
競合4文書はPROJECT_STATEで両履歴を保持し、検査契約のquality/report/handoffはmainの新しい
受理・統合・整理記録を採用した。製品/source/test/toolは結合済み7a10707から不変。
全1061入力とApp DLL hashが一致し、既存build/4case証拠を再利用。新規build/test/CI/nativeなし。
PR402はDraftでhideの画面受理待ち。通常app/公開beta.2は不変。#476の作業用2対象は整理済み、
未受理Bookmark作業と以前に拒否された4対象は保持する。

## 17:31時点の削除本体・再起動確認

不変の結合版2dae669で、専用カテゴリ/2登録/slot1・2をJSON fixtureとして準備した。
39068の通常キーボード経路でNN Delete Twoを確認画面のTab/Enterから削除し、残る1件/slot1を保持。
続くカテゴリ削除は対象名・1件・未分類への移動を表示し、Cancel初期focusからTab/Enterで実行。
残る登録は同じ名前/path/slotのまま未分類となった。実ファイル削除は送信していない。
再起動40864のCtrl+Bでも同状態、Escape/Ctrl+1でtarget-1のretained.txtへ到達した。
両専用process/window終了後の378byte設定は再起動前後で一致。予定差分と2つの77B実ファイルの
hash不変を照合し、17:31:38に元218byte/hashへ復元した。通常38360を保持。
全1061入力不変、build/test/CIなし。16key・14AX・9画像、設定/復元/fixture証拠を恒久deletion-walk/へ保全。
即時AXの古い表示や一時的な案内は後続画像/入力結果で照合し、速度・原因の主張には使わない。
PR402のhide画面受理は別途必要。未受理作業と旧4削除拒否対象は保持する。

## ! hide実行待ち：後片付け

旧拒否15対象は10:47の読み取りで全件不在を確認し、旧依頼は解消済み。
#454完了分は取込/未保存/ignored/唯一成果物/リンク/稼働参照を確認し、出力15ファイルと検査結果を
恒久証拠へ保全してhash一致。11:05の自動承認レビューが次の2対象の削除を起動前に
`blocked by policy`で拒否した。削除は実行されず、別手段で再試行していない。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

#459の後半2対象も4出力ファイルを保全し、展開版586ファイルを恒久candidateと照合、
取込/未保存/リンク/稼働参照を確認済み。12:29の自動承認レビューが起動前に`blocked by policy`で
削除を拒否したため実行されず、別手段の再試行もしていない。公開candidate/証拠/未完了Bookmarkは保持する。

hide実行用（branch/commit/恒久証拠を保持）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
