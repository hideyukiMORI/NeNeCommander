# 枠無しウィンドウと依存関係の更新・日報 — 2026-11-15

hide の「日報と引き継ぎ書を読んで状況を把握して」から始めた。11/9 の引き継ぎ書と違い、Dependabot PR
#376 が開いていたので、その置き換えを先に行い、続けて hide の依頼「アプリケーションを枠無しタイプに
したい。NeNeNib、NeNeFolio 等と同じ感じで」に対応した。すべて Issue 起点の PR で squash merge し、
`main` は `2db6f18` に同期済み。

## 完了範囲

| 内容 | Issue / PR | 検証 | merge |
|---|---|---|---|
| MSTest.Sdk 4.5.3、Microsoft.NET.Test.Sdk 18.10.2（#376 の test platform 部分） | [#377](https://github.com/hideyukiMORI/NeNeCommander/issues/377) / [#380](https://github.com/hideyukiMORI/NeNeCommander/pull/380) | full gate、dependency-review | `522b5e2` |
| .NET SDK 10.0.403 → 10.0.404（10.0.15 security servicing） | [#381](https://github.com/hideyukiMORI/NeNeCommander/issues/381) / [#383](https://github.com/hideyukiMORI/NeNeCommander/pull/383) | full gate、dependency-review、CI quality | `500a73c` |
| dotnet-stryker 5.1.0 と ADR-0048 の pin review（#376 の残り） | [#378](https://github.com/hideyukiMORI/NeNeCommander/issues/378) / [#385](https://github.com/hideyukiMORI/NeNeCommander/pull/385) | 4 層の mutation、security-check、dependency-review | `e2750be` |
| ウィンドウを枠無しにして自前の上部の帯を置く（ADR-0078） | [#379](https://github.com/hideyukiMORI/NeNeCommander/issues/379) / [#384](https://github.com/hideyukiMORI/NeNeCommander/pull/384) | full gate（1925 件中 1909 成功・0 失敗・16 スキップ）、当たり判定の実測、Observe 18 cell、hide 実機 | `2db6f18` |

Dependabot の #376 と、#377 の merge 後に出し直された #382 は、置き換えた PR と採らない理由を記した
コメントを付けて close した。

## 判断と経緯

- **Microsoft.NET.Test.Sdk は 18.12.0 ではなく 18.10.2**: Dependabot は 18.12.0 を提案したが、ADR-0048 は
  「固定した MSTest.Sdk 自身が解決する版」に合わせると決めている。公開 package の `Sdk/Sdk.props` を
  4.5.2 と 4.5.3 で比べ、4.5.3 が解決するのは 18.10.2 であることを確かめた。
- **SDK**: 開発機の system SDK が 10.0.404 だけになり、固定の 10.0.403 が無かった（#347 と同じ状況）。
  10.0.15 は 2026-11-10 公開の security release。CVE は release metadata で 16 件、release notes で 17 件
  （差の CVE-2026-118741 は ASP.NET Core のテンプレートで、この repository は参照しない）。
- **Stryker 5.1.0**: 5.1.0 の MTP runner も static mutant を隔離しない（Presentation の static: MTP
  perTest 66 Killed / 117 Survived、VSTest 187 Killed / 3 Timeout / 0 Survived）。撤去条件は半分のままで、
  `test-runner: vstest` を維持した。5.1.0 の VSTest で Domain 95.45 %、Application 95.01 %、
  Infrastructure.Windows 91.30 %、Presentation.WinUI 95.07 %。
- **枠無しウィンドウ**:
  - NeNeNib と NeNeFolio の作りを調べ、「家族の見た目」を原則（OS のキャプション無し、平らな自前の帯、
    線の記号、閉じるを赤くしない、リサイズ枠・影・Snap は OS のもの）として handoff に書いた。
  - 方向案 A・B・C をデザインキャンバスで出し、hide が **A（帯 32 DIP、全高ボタン）** と
    **閉じるの hover は他のボタンと同じ色** を選んだ。
  - hide の注意: 「Nib のとき、右上の角とボタンに隣接する辺でサイズ変更ができなくなった」。受入条件に入れた。
  - OS のボタンを残す方式は、閉じるの hover の色が OS の赤に固定される（Microsoft Learn に明記）ため
    採れなかった。`ExtendsContentIntoTitleBar` と `SetBorderAndTitleBar(true, false)` で OS のタイトルバーを
    外し、XAML の自前ボタンにした。この組み合わせの挙動は文書に無いので、実測で決めた。
  - 実測（入力を送らない `WM_NCHITTEST` の問い合わせ、125 %）で分かったこと: ウィンドウの上の外側には
    OS のリサイズ枠が無い。上の矩形と横の矩形が重なる所が角になる。最初の宣言ではボタンの上の当たりが
    2 px、角が斜め 2 px しかなかったので、上辺 4 DIP（ボタンの上も同じ）、角 8×8 DIP に広げた。
  - hide が一時ビルドを手で触り、リサイズ・ドラッグ・ダブルクリック・3 ボタンと終了・最大化・
    `Alt+Space`・システムメニュー・`Win`+矢印・`Win+Z`・非アクティブ時の見た目を確認した。
  - hide が見つけた不具合: ボタンを押したまま外へ出して離すと、hover の色で残る。ポインタのイベントを
    自前で数えるのをやめ、ボタン自身の `IsPressed` と `IsPointerOver` から色を決める形に直し、hide が
    直ったことを確認した。
  - 最大化ボタンの hover で Snap レイアウトの一覧は出ない（ボタンを OS の領域にしないため）。hide に伝え、
    了承を得た。
- **verified move の祖先照合（#388）**: #379 の full gate が、転送のテスト 1 件
  （`ExecuteAsyncWhenMovedTargetFailsVerificationKeepsThatSource`）で 1 回止まった。最初は #348 と同じ時刻の
  刻みの問題と見立てたが、計測で否定された。原因は、verified move と copy が祖先のディレクトリを root まで
  更新時刻ごと照合することだった。fixture の祖先 `%TEMP%` にほかのプロセスがエントリを作ると照合が外れる。
  `main` でも suite 30 回中 8 回失敗する。製品でも、大きなフォルダーの移動中に別のアプリが祖先にファイルを
  作ると、その項目が止まる（移動元は残る）。hide の決定: **祖先は「同じフォルダーか」だけを照合する**
  （更新時刻は見ない。ツリーの中身は今までどおり）。ADR-0069 の改訂と deep review が要るので、今日は
  着手していない。#379 は、原因を記録したうえで full gate を流し直す、と hide が決めた。

## 開発中の失敗と修正

- **2 つの席で同じテストを同時に動かした**: #377 の full gate と #379 の spike が、どちらも
  Infrastructure.Windows.Tests を動かし、`%TEMP%` の fixture がぶつかって #377 の gate が止まった。
  席を分けたリナの段取りの誤り。以後、full gate の間はほかの席のテストを止め、依頼書にも書いた。
- **#379 の full gate が build で止まった**: hide が確認用に開いていた一時ビルドが `bin` をつかんでいた。
  hide に閉じてもらって流し直した。リナは hide のプロセスを終了させていない。
- **実装リナが本体の checkout を一時的に書き換えた**: .NET の相対 path で ADR 3 つへの追記が C 側に入った。
  実装リナが差分を作業ツリーへ移して本体を元に戻し、リナも本体が clean であることを確かめた。
- **実測用の一時コードでアプリが 1 回落ちた**: 宣言の無い種類への `GetRegionRects` が `E_POINTER` を投げる。
  製品コードはこの API を使わない。事実を ADR-0078 に書いた。
- **デザインキャンバスの置き場**: 公開ツールが D ドライブを受け付けず、scratchpad への写しは自動承認に
  拒否された。回避せず、repository 内で git が無視する `artifacts/design-canvas/frameless/` に置いた。
- **#386 を途中で止めた**: Dependabot の除外設定が security update を止めないことを、一次情報で確認
  できなかった。設定は入れず、調べた内容を Issue に残した。

## 環境と後片付け

- 作業は `D:/NeNeCommander/worktrees/issue-*`、一時出力は `D:/NeNeCommander/outputs/issue-*`。
- 証拠は `D:/NeNeCommander/evidence/` の 2026-11-15 付きフォルダ（frameless-window、moved-target-flake、
  stryker-5-1-0、mstest-sdk-4-5-3、sdk-10-0-404）。画面写真はローカルの内容を含むため repository に
  入れていない。
- 後片付けの結果は引き継ぎ書に書いた。

## 残件

- [#388](https://github.com/hideyukiMORI/NeNeCommander/issues/388): 祖先の照合を object identity だけにする
  （hide の決定済み、未着手）。それまで full gate は verified move 系のテストでときどき止まる。
- [#387](https://github.com/hideyukiMORI/NeNeCommander/issues/387): Domain と Application の mutation score に
  余裕が無い。
- [#386](https://github.com/hideyukiMORI/NeNeCommander/issues/386): Microsoft.NET.Test.Sdk を Dependabot の
  提案から外す（保留）。
- [#358](https://github.com/hideyukiMORI/NeNeCommander/issues/358): UI evidence の layout 判定。今日の実測で、
  名前の無い Grid は UI Automation に出ないことが分かった（原因の候補）。
- [#94](https://github.com/hideyukiMORI/NeNeCommander/issues/94): hide 同席の手動リリース確認。枠無しの帯の
  125 % 以外の拡大率、ハイコントラスト、拡大率の違うモニター間の移動をここで見る。
- 11/9 の引き継ぎの候補（ブックマーク管理モーダル、試用版、設定保存の整理、CS-013 残件）は未着手。
