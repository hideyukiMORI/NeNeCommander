# 枠無しウィンドウと依存関係の更新・引き継ぎ — 2026-11-15

本日の内容は[日報](../reports/2026-11-15-frameless-window-and-pins-daily-report.md)に記録した。
区切りの後に #388 を進め、hide の指示で途中で止めた。`main` は `7d3764d`（本追記の merge 前）。
開いている PR は **#391（#388、Ready、未 merge）** の 1 つ。最初に下の「#388 の続き」を読むこと。

## #388 の続き（最初にやること）

- 状態: PR [#391](https://github.com/hideyukiMORI/NeNeCommander/pull/391)、branch
  `fix/388-ancestor-object-identity`、head `084746e`。作業ツリー `D:/NeNeCommander/worktrees/issue-388` を
  残してある（未 commit の変更なし）。
- 済み: ADR-0069 の改訂（外の祖先は object component と「リンクでないディレクトリ」だけを照合）、実装
  （`VerifiedTreeAccess.MatchesAncestors` の 1 行）、テスト（差し替え・リンク化の拒否 8 件、無関係な
  エントリの作成・削除・名前変更で止まらない 12 件、`external-parent` の書き直し）、Infrastructure の
  テスト全体の 30 回反復（失敗 0）、ローカルの full gate（1948 件中 1932 成功・0 失敗・16 スキップ、
  coverage は Domain 100.00 %、Application 100.00 %、Infrastructure.Windows 92.39 %、
  Presentation.WinUI 94.45 %）。
- **結果を見ていないもの**: exact-head の deep review
  [run 43724920547](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/43724920547)。2026-11-15 23:32 JST に
  起動し、23:38 の時点で実行中だった。hide の指示で監視を止めたので、結論は確認していない。
- 手順: (1) run の結論を見る。(2) 成功なら、head が `084746e` のままで `main` との衝突が無いことを確かめて
  merge する（`docs/PROJECT_STATE.md` の先頭の Session の行は #391 が直す）。(3) 失敗なら、失敗した段を読む。
  mutation の閾値で止まった場合は #387 の余裕の無さ（Domain・Application）と区別し、数字を hide に報告する。
  (4) merge 後、作業ツリーと `D:/NeNeCommander/outputs/issue-388/` を片付ける（反復と gate のログは
  `D:/NeNeCommander/evidence/moved-target-flake-20261115/` へ写してから）。
- 未確認: 本物の WSL（9P、live tier）と recycle の経路では実行していない。

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

1. **#388 の merge**（上の「#388 の続き」）。操作 root の親も「外の祖先」として扱うと決めた。診断の記録は
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
- **#388（PR #391）が merge されるまで**、`main` の full gate は verified move 系のテストでときどき止まる（`main` `e2750be` で suite 30 回中
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
- 区切りの後に作ったもの: 作業ツリー `D:/NeNeCommander/worktrees/issue-388` と
  `D:/NeNeCommander/outputs/issue-388/`（実装リナの反復と gate のログ）。#391 の merge まで残す。
- hide 実行待ちの後片付けは無い。
