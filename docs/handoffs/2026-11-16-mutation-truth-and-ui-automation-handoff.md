# mutation の本当の値と UI Automation の修正・引き継ぎ — 2026-11-16

2026-11-18 番号訂正（#445）: この記録でブックマーク設計を指すADR-0079は、
現在の[ADR-0080](../adr/0080-bookmark-manager-keyboard.md)。当時の記述はそのまま残す。
公開済みmainのADR-0079は初回Previewの公開時期を指す。設計内容・承認日は変えていない。

本日の内容は[日報](../reports/2026-11-16-mutation-truth-and-ui-automation-daily-report.md)に記録した。
`main` は `d4c4e1d`（本追記の merge 前）。開いている PR は **#402（#398、draft、hide の実機の確認待ち）** の 1 つ。

## hide の判断待ち

1. **#398 ブックマーク管理モーダルの実機の確認**（PR [#402](https://github.com/hideyukiMORI/NeNeCommander/pull/402)）:
   モーダルを開くには入力が要るので、リナは実機で見ていない。確認の手順は PR の本文にある。見た目と
   キー（`j`/`k`、`Tab`、`/`、`a`/`e`/`d`、削除の確認、scrim のクリック）が良ければ、`main` を取り込み、
   full gate を単独で 1 回流してから merge する。
2. **#386 Dependabot の除外設定**: GitHub Docs は、`ignore` が security update の PR にも効くと書いている。
   受入条件（security update を止めない）を満たす書き方は文書に無い。3 つの選択肢を Issue に書いた。
3. **`TransientScopeOwners` / `TransientScopeSnapshot` の引数 5 個**（CS-013 の上限は 4）: ADR-0051 が
   「後から scope を足す唯一の場所」と決めた型。形を決め直すか、そのままにする理由を決めるか（Issue #414）。
4. **#397 コマンドパレットの行のツールチップ**: 今まで一度も出ていなかった（Style の Setter の binding は
   WinUI では効かない）。出すか、出さないままにするか。

## `main` の deep review

run 43787538393（`e60ee35`。#391・#405・#399・#404 を含む）は成功した。mutation score は Domain 97.47 %、
Application 96.60 %、Infrastructure.Windows 91.84 %、Presentation.WinUI 94.86 %。

最新は #409 の head の run 43793338605（`cbf8974`。上に加えて #407・#410・#409 を含む）で、成功した。
Domain 97.47 %、Application 96.59 %、Infrastructure.Windows 92.30 %、Presentation.WinUI 94.94 %。
その後に merge した #414（`CommanderSession` の refactor）は、どの deep review にも入っていない。

## 引き継ぐ状態

- **mutation score の本当の値**（deep review run 43793338605、`cbf8974`）: Domain 97.47 %、
  Application 96.59 %、Infrastructure.Windows 92.30 %、Presentation.WinUI 94.94 %。閾値は Domain・Application
  95 %、Infrastructure・Presentation 90 %。11/15 までの文書にある Infrastructure.Windows 91.30 %（ローカル）は、
  #388 の不安定さで落ちたテストが mutant を偶然倒した分だけ高い。その時点の CI の値は 88.15 %。
- **ローカルの mutation の値は、テストが不安定な間は信用しない。** 落ちたテストは、関係の無い mutant も
  「倒した」ことにする。層の値を判断に使うときは CI の deep review の report を使う。2 つの report を
  mutant ごとに突き合わせる script は `D:/NeNeCommander/evidence/mutation-margin-20261116/issue-388-analysis/`。
- **Infrastructure.Windows の余裕は 2.30 point**。残した mutant と理由は Issue #395 の
  コメント。等価な二重の guard は、製品コードを単純にすれば mutant ごと無くせる（未着手）。
- 設定保存の adapter は、`WindowsLocalSettingsStore`（入口）、`SettingsLocationObserver`、
  `SettingsDocumentObserver`、`SettingsBaseline`、`SettingsWriteAttempt`（1 回の書き込み）に分かれた（#409）。
  検査と test hook の回数・順序は元のまま。ここを触る時は、順序の trace の突き合わせ
  （`D:/NeNeCommander/evidence/settings-store-refactor-20261116/`）と同じ確かめ方をする。
- verified move と copy は、外の祖先を object identity と「リンクでないディレクトリ」だけで照合する
  （ADR-0069 の改訂、#388）。`main` の full gate が verified move 系でときどき止まる件は、これで終わった。
- 名前の衝突の後に再開した転送も `Escape` で取り消せる（#399）。最初の実行と再開は
  `DualPaneSession.RunCancellableAsync` を通る。
- Windows local の identity の取得（`WindowsLocalEntryIdentity`）は、`Describe` も `Capture` も
  extended-length の形で開く（#404）。開発機は `LongPathsEnabled=0`。
- UI Automation: 名前の無い `Border`・`Grid`・`ItemsControl` は peer を持たない。WinUI は Style の Setter の
  `{Binding}` を無視する。行の識別子は `RowAutomationIdentity` が container の用意の時に付ける（#358）。
  Observe は前面を取らないので、フォーカス行は list の選択から測る。
- ブックマーク管理モーダルの設計は ADR-0079 と `docs/design/2026-11-16-bookmark-manager-handoff.md`
  （どちらも #402 の中。未 merge）。デザインキャンバスの原本は repository 内の
  `artifacts/design-canvas/bookmarks/`（git が無視する）。
- 固定の版は 11/15 のまま（SDK 10.0.404、MSTest.Sdk 4.5.3、Microsoft.NET.Test.Sdk 18.10.2、
  dotnet-stryker 5.1.0、Windows App SDK 2.6.5）。Dependabot の #394（Microsoft.NET.Test.Sdk 18.12.0）は
  理由を書いて close した。#386 が片付くまで、出たら同じように close する。

## 次回の候補

1. **#398 の merge**（hide の確認の後。上の 1）。
2. **CS-013 の残り**: `main` `d4c4e1d` で 69 件（製品 8、テスト 61）。製品の残りは、`TransientScopeOwners` と
   `TransientScopeSnapshot` の parameters 5（ADR-0051 の形の判断が要る。Issue #414 に記載）、App の
   `CommanderWindow.xaml.cs`（type-lines 320、cognitive 11）と `KeyboardHelpView`、Presentation の
   `KeyboardHelpPresenter` と `KeyboardInputTranslator`。help・keyboard・`CommanderWindow` は #402 が近くを
   触っているので、#402 の merge の後にする。テストの 61 件は、ケース・assertion・競合の順序を落とさず分ける。
3. **全体適用（CS-013 を gate につなぐ）**: 残件が 0 になってから、ADR-0051 / 0075 に従って別の採用の変更として
   扱う（10/19 の引き継ぎの 3）。
4. **CI とローカルで結果が違う mutant の調査**（#410 のコメント）: `PaneReducer.GetFocusIndex` の
   `First()` → `FirstOrDefault()`。CI の 2 つの run では Killed、ローカルの絞った実行では Survived。
   CI は安定している。違いが環境か、絞った実行のテストの選び方かは調べていない。
5. **Infrastructure.Windows の二重の guard の整理**: 委譲先と同じ guard を持つ箇所（Issue #395 のコメントの
   「等価」の一覧）。製品コードに触れるので、Issue を立ててから。
6. **`WindowsLocalSettingsStore` の残りの mutant**: #395 では未着手。
7. **試用版**: [11/9 の引き継ぎ](2026-11-09-maintenance-and-usability-handoff.md)のまま。
8. **#94**: hide が居る日に、モニターの倍率を hide が手で変える（ADR-0054）。

## 作業上の注意

- **full gate は単独で実行する。** Infrastructure.Windows.Tests の fixture は `%TEMP%` を共有するので、
  2 つの席で同時に動かすと互いに失敗する。席の依頼書に書く。
- ほかの製品の席が同じ機械で動いている日は、層全体の mutation と full gate をローカルで流さず、
  絞った確認（触れたファイルだけの Stryker、並列 2）と CI の deep review を使う。D ドライブが遅くなり、
  コマンドが timeout することがある。
- `main` を取り込んで `docs/PROJECT_STATE.md` が衝突したときは、印を消すだけにせず、`main` との差分を見る
  （古い Session の行が二重に残ったことがある）。
- Bash の heredoc と二重引用符は、バックスラッシュと backtick を壊す。path や C# の文字列を含む内容は
  Write / Edit で書き、`gh` には `--body-file` で渡す。
- 席への依頼書には、hide が承認した案の細部（例: 「削除は確認あり」）を書き写す。
- live の WSL tier は、切り離した `wsl.exe --distribution Ubuntu --exec sleep 3600` で distro を保ってから流す。
  recycle の live tier は hide の本物のごみ箱に触れるので、hide が居る時だけ。
- 11/15 の注意（gate と build の前にアプリのウィンドウが開いていないことを確かめる、Artifact の原本は
  repository 内の `artifacts/design-canvas/`、.NET の相対 path は絶対 path にする、`dotnet format whitespace`
  だけを使う、新規ファイルは CRLF、アプリへのキー入力は `eng/ui-evidence` 以外で送らない）はそのまま。

## 後片付け

- 作業ツリー（issue-358、387、388、395、399、404、407、409、410、414）とその local branch は削除済み。remote の branch は
  merge 時に削除された。
- 残してある作業ツリー: `D:/NeNeCommander/worktrees/issue-398`（#402 の merge まで）。本書の PR の
  作業ツリー `docs-20261116` は merge 後に削除する。
- `D:/NeNeCommander/outputs/issue-398/`（Observe の出力と PR の下書き）は #402 の merge まで残す。
  ほかの `outputs/issue-*` と `temp/` の下書きは、証拠を `D:/NeNeCommander/evidence/` に写してから削除した。
- 証拠: `evidence/ui-evidence-layout-20261116/`、`evidence/mutation-margin-20261116/`（issue-387、
  issue-388-analysis、issue-395、issue-404、issue-407、run-43780626571、run-43787538393）、
  `evidence/moved-target-flake-20261115/live-wsl-20261116/`、`evidence/cs013-application-20261116/`（#410、#414）、`evidence/settings-store-refactor-20261116/`（#409）、
  `evidence/mutation-margin-20261116/run-43793338605/`。
- hide 実行待ちの後片付けは無い。
