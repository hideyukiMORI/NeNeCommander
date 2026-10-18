# 引き継ぎ 2026年10月18日 ウィンドウ調整の長押し調査

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)、測定・失敗履歴・exact commandは
[日報](../reports/2026-10-18-window-repeat-daily-report.md)を参照する。
[Issue #303](https://github.com/hideyukiMORI/NeNeCommander/issues/303)のbaselineは
`93a9b7b7157da32d7afae68d188fb9c6f8b4ab82`。

## 確定した結果と判断

- 旧15/11はCtrl+W調整の合成keydown投入数とnet変位の比較。受信数・RepeatCount・各actionがなく、原因未確定。
- 現在の独立fixtureでhideの実長押しを測定。h 32/32、l 55/55、Left 40/40、Right 62/62が40 pxずつ動いた。
- 文字はCharacterReceived、矢印はKeyDownから既存mapper/route/session/planner/adapterへ進む。別挙動経路は追加していない。
- 両manual runの全254移動actionでXY変位一致、拒否0、diagnostic defect 0。RepeatCountはすべて1。
- 独立14区間を受理。重なったLeft/Rightの単発2区間は長押し集計から除外した。その後の矢印長押しは成立。
- 最初の文字入力はj/kだったため、h/lだけ追加した。両manualは正常close/ProcessExit/外部exit 0。
- 今回の条件では未再現。製品を修正済みとは扱わず、RepeatCountの乗算や独自injectorは採用しない。

## 証拠と限界

恒久証拠は`D:/NeNeCommander/evidence/window-repeat-20261018/`。
最終source/binary 354 fileのhash、raw stream、launch/exit、source比較、解析v2、旧driver/結果、
未受理のsetup版と旧解析結果を保持する。
初版解析のglobal press番号による重複対応付けとXだけの確認は修正済み。
`*-v1-superseded.*`を受理証拠に使わず、`manual-01-analysis.json` / `manual-02-analysis.json`のv2を参照する。

最終fixture buildは警告・エラー0、起動出力境界の拒否3件、単発control 5区間が成立。
解析v2は保存済み3runで検証した。成功済み製品証拠を再実行したり新規件数へ加算したりしない。
文面/link/diffと通常Commit hookが統合の検証範囲。製品code/test/依存/gateは不変で、full/deep CI等は起動しない。

800 × 400 px・scale 1.25の独立hostであり、Ctrl+W入場と全CommanderWindowのrenderingは含まない。
provider/settings/Shellはthrowする診断port。OS設定変更、global key hook、合成holdはない。
当時の2880 × 1550 pxの製品window、旧合成刺激、入力キュー、hardware scan数を再現したものではない。
OS結合は一次資料が示す候補だが今回観測しておらず、旧原因へ断定しない。#94も別のrelease proofである。

## 終了・再開条件

調査として観測結果と限界、現行契約維持の判断が揃った。追加手動入力は不要。
全測定processが終了しており、待機する測定はない。
PR統合、main同期、task tree一致、worktree/outputの整理はIssue PRと`completion.json`から実状態を確認する。
branch/commit、恒久証拠、共通SDKは保持する。
対象は`D:/NeNeCommander/worktrees/issue-303`と`D:/NeNeCommander/outputs/issue-303`のみ。

将来の実利用で再現した場合は条件と受信・RepeatCount・action・各座標を揃える。
全製品instrumentation、新たなinput API、RepeatCountによる移動量変更は、別の具体的Issue/必要なADRを先行させる。
旧現象が未再現という限界だけを理由に、今回の調査を無期限の入力・描画試験へ拡張しない。
