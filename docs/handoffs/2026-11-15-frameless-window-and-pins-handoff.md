# 枠無しウィンドウと依存関係の更新・引き継ぎ — 2026-11-15

本日の内容は[日報](../reports/2026-11-15-frameless-window-and-pins-daily-report.md)に記録した。
`main` は `2db6f18`。開いている PR は無い。

## 引き継ぐ状態

- 固定 SDK は 10.0.404（system SDK だけで build・gate が動く）。MSTest.Sdk 4.5.3、
  Microsoft.NET.Test.Sdk 18.10.2、dotnet-stryker 5.1.0、Windows App SDK 2.6.5。
- ウィンドウは枠無し（ADR-0078）。上部に 32 DIP の帯があり、最小化・最大化／元に戻す・閉じるは XAML の
  自前ボタン。帯の配置を変えるときは、上辺 4 DIP と上の角 8×8 DIP のリサイズ領域の宣言を保ち、当たり判定を
  測り直す（上の外側には OS のリサイズ枠が無い）。測定スクリプトは
  `D:/NeNeCommander/evidence/frameless-window-20261115/Measure-CaptionHitTest.ps1`（入力を送らない）。
- 閉じるボタンは `SC_CLOSE` を投げて OS の閉じる経路に入る。終了の手順（ADR-0071）は変わっていない。
- 見た目の対応（状態 → brush と名前）は Presentation の `CaptionBarPresenter` にあり、起動なしでテストできる。
  ボタンの hover / pressed は、ボタン自身の `IsPressed` と `IsPointerOver` から決める。ポインタのイベントを
  自前で数えると、押したまま外で離したときに色が残る。
- 名前の無い Grid に AutomationId を付けても UI Automation に出ない。identifier は peer を持つ要素
  （Button、TextBlock など）に付ける。
- Dependabot は Microsoft.NET.Test.Sdk の最新版を毎日提案する。採らない（ADR-0048）。#386 が片付くまで、
  出たら理由を書いて close する。

## 次回の候補

1. **#388 祖先の照合を object identity だけにする**（hide の決定済み）: verified move と copy は、祖先の
   ディレクトリを root まで更新時刻ごと照合している。別のアプリが祖先にファイルを作ると移動が止まり、
   テストも不安定になる。ADR-0069 の改訂、adversarial test の更新、deep review が要る。直接の親を
   「祖先」に含めるか、`external-parent`（#349）の検出をどう扱うかを、改訂の中で決める。診断の記録は
   Issue #388 のコメントと `D:/NeNeCommander/evidence/moved-target-flake-20261115/`。
2. **#358 UI evidence の layout 判定**: 今日の実測で原因の候補が出た（名前の無い Border / Grid が UIA に
   出ない）。`OperationBar` と `OperationKeyHints` で確かめ、製品側で名前を付けるか harness の期待値を
   直すかを決める。
3. **#387 mutation score の余裕**: Domain 95.45 %（189/198）、Application 95.01 %（1845/1942）。見逃しが
   1 件増えると閾値を割る。report は `D:/NeNeCommander/evidence/stryker-5-1-0-20261115/`。
4. **#386 Dependabot の除外設定**: ignore が security update を止めないことを一次情報で確かめてから。
5. **ブックマーク管理モーダル**（透過 scrim と `j`/`k`）、**試用版**、**設定保存の整理**、**CS-013 残件**:
   [11/9 の引き継ぎ](2026-11-09-maintenance-and-usability-handoff.md)のまま。

## 作業上の注意

- **full gate は単独で実行する。** Infrastructure.Windows.Tests の fixture は `%TEMP%` を共有するので、
  2 つの席で同時に動かすと互いに失敗する。席の依頼書に書く。
- **#388 が直るまで**、full gate は verified move 系のテストでときどき止まる（`main` `e2750be` で suite 30 回中
  8 回）。止まったら、失敗したテストがこの系統で、変更が転送のコードに触れていないことを確かめ、PR に
  記録して流し直す（hide の決定、2026-11-15）。それ以外の失敗は、原因を調べる。
- gate と build の前に、NeNe Commander のウィンドウが開いていないことを確かめる（`bin` をつかんで build が
  止まる）。hide が開いたウィンドウは hide に閉じてもらう。
- 背景の席は、設計リナへ途中の知らせを送れない。hide への起動の予告は設計リナが出し、起動も設計リナが行う。
- Artifact の公開ツールは D ドライブを受け付けない。デザインキャンバスの原本は repository 内の
  `artifacts/design-canvas/`（git が無視する）に置く。
- .NET の相対 path は cwd を無視して本体の checkout を書き換えることがある。席には絶対 path を使わせる。
- 11/9 の注意（`dotnet format whitespace` だけを使う、新規ファイルは CRLF、アプリへのキー入力は
  `eng/ui-evidence` 以外で送らない、表示は実機か撮影で確かめる）はそのまま。

## 後片付け

- 作業ツリー（issue-377、377-base、378、379、381、386、388）とその local branch は削除済み。remote の
  branch は merge 時に削除した。
- `D:/NeNeCommander/outputs/issue-*`、`D:/NeNeCommander/tools/dotnet-10.0.403` と `tools/installer`、
  `D:/NeNeCommander/temp/` の下書きは、証拠を `D:/NeNeCommander/evidence/` に写してから削除した。
  `outputs`・`tools`・`temp`・`worktrees` は空（本書の PR の作業ツリーは merge 後に削除する）。
- 残してあるもの: repository 内の `artifacts/design-canvas/frameless/`（枠無しのデザインキャンバスの原本、
  git が無視する）。キャンバスは hide のアカウントの私有。
- 実測の一時コードが落ちたとき、Windows のエラー報告が `C:/ProgramData/Microsoft/Windows/WER/` に
  ダンプを 1 つ作った。OS が作ったもので、触っていない。
- hide 実行待ちの後片付けは無い。
