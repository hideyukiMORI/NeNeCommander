# 引き継ぎ書 — 2026-10-06〜07(ウィンドウ調整モードは統合済み、次は main deep の確認と #94)

Status: informational

## 体制

Fable 5 セッションの NeNeリナ(設計リナ)が設計・判断・文書・レビュー、Opus のバックグラウンドの NeNeリナ
(実装リナ)が実装・調査・test・gate の運転。今回は実装リナを 3 席まで並行させた。並行させるときの決まり:

- 席ごとに別の worktree と別の branch を使う。同じ worktree で 2 席が commit しない。
- 担当ファイルを重ならないように分け、依頼文に「触らないファイル」を書く。merge は設計リナが行う。
- 設計リナが同じ worktree で commit するのは、その席が終わってからにする。

## main の状態

- baseline は本 docs 閉塞 PR(Issue #158)の squash commit で、merge 後に確定する。その下は次のとおり。
  - `41a682c`(#100 ウィンドウ調整モード、PR #157)
  - `2d3fc29`(#155 codeql-action 4.38.2、PR #156)
  - `234f600`(09-27〜28 の docs 閉塞)
- open Issue は #94 だけ(と merge までの #158)。open PR は無い。
- 最後に結果を読んだ main deep は scheduled run `37189427536`(`234f600`)。score は Domain 95.52% /
  Application 95.98% / Infrastructure.Windows 90.86% / Presentation.WinUI 94.30%、CodeQL 0 件、open alert 0 件。

## 最初に確認すること — #157 の後の main deep

PR #157 の merge の直後に、main deep を `workflow_dispatch` で起動した: run `37499633218`(main `41a682c`)。本書を書いた時点では実行中で、結果は読んでいない。次回の最初にこの run を読む。

確認する項目:

- 4 層の mutation score。ローカル実測は Application 96.23% / Presentation.WinUI 94.57% だった(proof branch
  `cd02740`)。その後に `WindowSizeConstraint.Create` の非有限 scale の拒否が入っており、そこは測り直していない。
- CodeQL の result 数と open alert 数。codeql-action v4.38.2 での最初の実行になる。
- alert や review thread が付いたら、resolve だけで済ませず code で直す。

score が閾値を割っていたら、原因の mutant を特定して behavior test を足す Issue を立てる。閾値・設定・除外は
変えない。

## Issue #100 で決まったこと(次の変更が前提にしてよい事実)

正本は `docs/adr/0050-window-adjustment-mode.md`(accepted、第 5 次改訂)と
`docs/design/2026-09-16-window-adjustment-handoff.md`、`docs/KEYBOARD_MODEL.md` の「Window adjustment mode」。

- transient scope は 3 つになった(address editor、command palette、window adjustment)。`CommanderSession` は
  279 論理行で、上限 300 まで 21 行。次に scope を足す変更は、先に行数を測る。
- mode の中の keyboard の結果は 3 つ。宣言済みのキーは action、`Other` は pass through、識別済みの未宣言キーは
  consume。`Other` を consume してはいけない(mode 自身の文字キーが届かなくなる)。
- chord の取消は「現在の context がそのキーと modifier を宣言しているか」で決まる。
- mode の key action から Application の action への対応は `WindowAdjustmentKeyAction.WindowAction` が持つ。App の
  view に対応表を置かない。
- caption rule は隙間のある seam を跨がない(taskbar を挟む上下の画面)。hide が承認した既知の帰結である。
- adapter の apply の失敗は既存の defect 経路(再 throw)に乗る。
- 実機で確かめたこと(Windows App SDK 2.5.1、125%、US-101): spike の 4 事実、leave の focus 順序、
  deactivate → reactivate 後の focus。記録は PR #157 の本文。

## 次の作業 — Issue #94(release matrix)

#100 の統合で、#94 に次のセルが加わった。その旨は #94 にコメント済みである。

- helper の 100% の画面(開発機には 100% の画面が無い)
- helper の 100–300% / high contrast / Narrator / 8 scheme
- scale の違う画面をまたぐ move(2.4.0 の spike では観察済み、2.5.1 では未取得)
- taskbar を挟む上下の seam で move が拒否されること

#94 は専用の環境と hide の在席が要る。着手は hide の指示を待つ。

## runtime 証拠の取り方(今回わかったこと)

- ドライバは `D:\NeNeCommander\evidence-100\Invoke-WindowAdjustmentEvidence.ps1`(リポジトリ外)。`-ReactivationOnly`
  で、deactivate → reactivate の項目だけを短く取れる。
- キーを送る前に、hide の合図を必ずもらう。実行中は hide にキーボードとマウスへ触らないよう頼む。
- **ツール経由で起動したコンソールの表示は、hide からは見えない。** 手動の手順の合図は、実行の前に「画面がこう
  動いたらクリック」という形で伝える。1 回目はこれが伝わらず、手動の項目を取り逃した。
- mode のキー(`l`、`h` など)は、mode が開いていないと file list のキーとして効く(フォルダーに入る、ファイルを
  開く、親へ戻る)。ドライバは helper が画面に出ていることを確かめてから mode のキーを送る。同じ種類のドライバを
  書くときは、この確認を最初から入れる。
- bounds は `GetWindowRect` と UIA の `BoundingRectangle` の両方で読む。今回は全 76 回の読み取りで一致した。

## ローカル環境

- リポジトリ本体は `C:\Users\info\WORKS\NeNeCommander`(main)。本体の外の作業は `D:\NeNeCommander\` に置く
  (hide の 2026-09-30 の指示)。worktree は `D:\NeNeCommander\wt-<issue>`、証拠は `evidence-<issue>`、mutation の
  出力は `mutation-<issue>`。
- ドライブをまたぐ `git worktree move` は失敗する。旧 worktree を detach し、D 側に `git worktree add` で作り直す。
- worktree は `C:\Users\info\WORKS\NeNeCommander`(main)だけに戻す。`wt-100` と `wt-100-proof`、branch `feat/100-window-adjustment-mode` と `test/100-window-adjustment-proof`(remote も)は、統合の後に clean と統合済みを確かめて削除した。本 docs 閉塞の `wt-158`(branch `docs/158-session-closure`)は、merge 後にリナが削除する。
- `D:\NeNeCommander\` に残してあるもの: `evidence-100\`(runtime 証拠 2 回分、ドライバ、ローカル canonical の
  log)、`mutation-100\`(mutation の report)、`run-37189427536\`(scheduled deep の artifact と log)。不要に
  なったら消してよい。
- BOM に注意する。checkpoint `ef6e7e2` は 46 ファイルに UTF-8 BOM を付けていた(書いた手段は特定できていない)。
  Commit mode の check は format を走らせないので、commit の前に変更ファイルの先頭 3 byte を確かめるか、
  `dotnet format whitespace <slnx> --verify-no-changes --no-restore` を回す。新規ファイルは CRLF にする
  (Write ツールと `[IO.File]::WriteAllText` は LF で書く)。
- PowerShell の `cd` は .NET のカレントディレクトリを動かさない。`[IO.File]`・`dotnet`・`git -C` には絶対パスを渡す。

## hide への未決の問い(blocking ではない)

- `=` の alias。hide のキーボードは US-101 で、拡大は `Shift+=` かテンキー `+`。JIS では `=` が `-` キーの Shift 面
  なので alias は却下のままにしてある。使ってみて不便なら再考する。
- taskbar を挟む上下の画面を mode で跨げるようにするか(隙間を飛び越える規則)。要るなら別 Issue と ADR にする。

## 候補(hide の承認後に起票)

今回の追加:

- `l` の押しっぱなしで step が減った原因の調査(`h` は 15 / 15、`l` は 11 / 15)。OS が repeat をまとめたのなら
  仕様どおりで、`KeyStatus.RepeatCount` を使うかどうかの判断になる。
- closed record の抜け穴。抽象 record には compiler が作る protected の copy constructor があり、別の assembly
  から `base(existing)` を経由して派生できる。closed な model 全体に関わる。
- `WindowWorkAreas.Create` が attached の null 要素を通す(nullable 注釈上は caller defect)。
- KBD-005 の文言。「全 binding を 1 つの表で宣言する」と読めるが、palette と window mode は専用の table を持つ。
- `OpenWindowAdjustment` を command palette の catalog に足すか(ADR-0047 は window command を最初の catalog から
  外している)。
- Commit mode の check に format(少なくとも charset)を足すか。BOM の混入を commit の時点で止められる。

前回からの継続: `OpenBookmarks` の catalog 追加、`_defaultFileListFocusSuppressedState` のリセット条件、CS-013 の
他 5 型とゲート化(`CommanderWindow.xaml.cs` は今回 +17 行)、`verify-coverage.ps1` の stale build 対策、WSL link
entry 削除の capability ADR、Presentation `AsyncWorkOwner.cs:139` survivor の behavior test、ADR-0048 の review
契機に test platform の pin 変更を足す。

## 再開手順

1. main worktree の `git status` が clean で `origin/main` と一致することを確かめる。
2. 「最初に確認すること」の main deep を読む。
3. 新しい Dependabot PR があれば、先例(#148 / #149、#155 / #156)どおり Issue 起点の PR で置き換える。
4. hide の指示に従い、#94 か候補のどれかに進む。
