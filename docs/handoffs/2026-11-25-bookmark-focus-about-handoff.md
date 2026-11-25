# 2026-11-25 引き継ぎ — ブックマークのアクセント揺れから再開

最終更新: 2026-11-25 01:19 JST。hideの明示指示で停止中。自動継続しない。
[終了日報](../reports/2026-11-25-bookmark-focus-daily-report.md)と
[品質記録](../quality/2026-11-25-bookmark-focus-about.md)が今回の終了正本。
過去の「継続中」「承認待ち」「実行中」はその時点の履歴として読む。

## 最新のhide指示

- B案の見出し帯と、既存ヘルプ末尾のAboutは採用済み。再承認を求めない。
- 01:02:59画像で、見出し線の太さ・文字余白不足・検索欄のフォーカス色不足を指摘。
- 最終報告では外枠の上下移動は解消。一方、ヘッダーのアクセント色部分はまだ震える。
  フォーカス移動時のアニメーションは不要で、全く震えない動きが希望。
- アクセントの修正は明日以降。区切りで止めて日報と引き継ぎを書く。

## 現在の正本と試遊版の違い

作業先: D:/NeNeCommander/worktrees/issue-581
branch: fix/581-bookmark-focus-about
元の実装4a15cd9、試遊版03記録e52ad72。その後の2XAML修正を今回の終了commitで保存。
この文書と同じcommitが停止時の最新。main Cはclean ccb303bのまま。

- 試遊版03は4a15cd9から作成した最後の可視版。About、見出し帯、折返し高さ確保を含む。
  D:/NeNeCommander/outputs/ui-trial-20261124/interactive-trial-03。
- ソースとaccepted-controls-02には、見出し線left2/bottom1 DIP・上下4 DIP余白と
  検索欄のfocused resource aliasが入っている。03へは反映していない。
- 試遊版04は作成・build・launchの全て未実施。止めた後に始めない。
- 03は00:40にPID13280で起動したが、01:15inventoryにはApp自体がなかった。
  サナは通常Appも試遊版も終了していない。古いPIDを再利用せず実パスから確認する。
- Aboutのbeta.4表記は現行executable metadataどおり。beta.5のVersion変更は#580の工程。
  著作権表記はAuthenticode署名を意味しない。

## 確認済みの範囲と再実行しないもの

元実装: native6/7/8/9各107checks、native10 draft/live-language64、native11最終mapping107。
3言語、2幅、空/登録済み、既存hint Invoke、About/Help復帰の証拠。4a15の通常build成功。
独立レビューはe52以前の実装まで必須修正なし。最後の2XAMLに追加委任はしていない。

最後の調整: polish-product-build-01は0warnings/errors。C#・依存は不変なので以前の
format/complexity（既存3findings）と関連しないbehaviorを再利用。XAMLのindent1箇所
だけは通常build後に整え、意味は同じ。diagnostic build03はその最終XAMLを使用。

Native14: ja-JP/Ubuntu/1.25scale、820/580 DIP、216checks、168geometry samples、10images。
実際のKeyboard/Pointer focus stateで検索のaccent brushが一致し、退出時に戻る。
見出しの要求文字領域と余白、body/hints/search/headingsの座標不変を確認。exit0。
Pointerは所有非入力desktop内のFocusState.Pointerであり、物理クリックの証明ではない。

Native12/13の失敗はobserverのActualHeight/DesiredSize比較の誤り。13で実画像と
14.630859/15.199999 DIP、1.25scaleを記録。14は要求文字矩形が丸めた下線と固定余白内に
収まる検査へ修正した。失敗を削除/上書きせず、閾値やproductを都合で変えていない。

全attemptで通常設定/input desktopは不変。polish-final-source-parity-01.jsonは
182App inputs=180exact+2declared project/composition、4DLL、3fixturesを凍結。
通常settingsを試遊設定へコピーしない。以前の専用設定だけを継ぐ。
変更入力が同じなら、commit/工程が変わっただけで成功検証を再実行しない。
停止後の保存では通常commit hooksだけ。新しいproduct test/CIは始めていない。

## 再開時の最初の仕事

1. hideの再開指示と実際のbranch/statusを確認する。最後の2XAMLと記録を消さない。
2. 残る「アクセント部分の震え」を対象にする。現在の座標サンプルはcompositor/paintの
   安定を保証しない。既存focus route、row retained selection、headingのBorderBrush/
   Background切替、関連transition/animationを証拠に基づいて切り分ける。
   詳細調査はまだしていないので原因を断定しない。新しいfocus/input経路を増やさない。
3. 共有desktopへ入力せず、既存の専用非入力desktopで必要な描画列だけを取得する。
   ラベル余白/検索focusの成功を反復せず、変えた描画境界を確認する。
4. 揺れ修正が終わってから通常analyzersで試遊版04を作る。宣言済みのproject paths/
   private settings/owned folders/title以外を変えず、source/DLL/manifestとisolated startupを確認。
   hideの既存試遊表示・修正依頼は有効だが、停止中には表示しない。
5. #580候補へ最終581を取り込み、承認された範囲の公開/PR/main統合とrelease proofへ戻る。
   #532 drag/dropと#564全体の残機能は追加しない。beta.5は未公開・未tag。

## 証拠と残すもの

D:/NeNeCommander/evidence/bookmark-focus-20261124 が#581の恒久証拠。
ユーザー画像hide-bookmark-polish-report-20261125-010259.png、全failed/passed receipt、
polish-product-build-01、polish-controls-build01〜03、FixtureBookmarkFocus-polish-attempt12〜14、
polish-final-source-parity-01.jsonとaccepted-ja-JP-14-polish-*.pngを保持。
D:/NeNeCommander/outputs/bookmark-focus-20261124/accepted-controls-02 が最新診断コピー。
以前のaccepted-controls-01、baseline/layout prototype、design-consultも履歴として保持。
Canvasはdesign-consult/bookmark-focus-about.html。cloud Canvas/share URLは存在しない。

D:/NeNeCommander/evidence/ui-trial-20261124 と同名outputsは6機能統合/試遊01〜03の正本。
#580のworktreeは8d1c247で未変更。旧reuse assessmentの1252/1317一致・65changedは
当時の候補27efbdcへの評価であり、最新581への自動承継は不可。Domain27inputsは当時
全一致、Infrastructure own286inputsも一致だがApplication変更の推移影響は要照合。
Application/Presentationの変更分、全必要threshold、CodeQL candidate ref、96時間鮮度、
accepted-main package/extraction/startup、tag/publication/download確認は未完。

未完worktrees: issue491/495/514/530/531/544/547/548/551/564-ui-trial/580/581とpr-498-review。
D:/NeNeCommander/worktrees内のそれぞれを保持。関連outputsも未統合または証拠/試遊参照が
あるため保持。今回削除しない。正式beta.4 source/tag3909039と公開資産は変更せず、
そのrelease一時物の整理は既に完了。後片付け拒否やhide実行待ちは新たに発生していない。

自分の実行中build/observer/CIはない。最後のexec sessions56961/68569/67888/4384は
終了済みでpoll/再起動不要。青枠CUA、共有入力、通常設定変更、他案件干渉は禁止を維持。
