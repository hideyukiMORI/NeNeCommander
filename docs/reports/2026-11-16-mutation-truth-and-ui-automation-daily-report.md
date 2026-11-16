# mutation の本当の値と UI Automation の修正・日報 — 2026-11-16

hide の「日報と引き継ぎ書を読んで状況を把握して。これから 1 日、外回りでデスクにいない。マイルストーンに
従って作業を進めて」から始めた。GitHub にマイルストーンは 1 つも無かったので、11/15 の引き継ぎ書の
「次回の候補」の順を道筋にした。ほかの製品の席が同じ機械で動いていたので、full gate と層全体の mutation は
ローカルで流さず、絞った確認と CI を使った。途中で hide から 1 回、チャットで回答をもらった（#398 の 4 点）。

## 完了範囲

| 内容 | Issue / PR | 検証 | merge |
|---|---|---|---|
| 操作バーとファイル行を UI Automation に出し、Observe の layout 判定を実際の表示に合わせる | [#358](https://github.com/hideyukiMORI/NeNeCommander/issues/358) / [#396](https://github.com/hideyukiMORI/NeNeCommander/pull/396) | 入力なしの UIA の探査、Observe 2 cell PASS、selftest 103、Architecture 21 | `e8efd5c` |
| Domain と Application の mutation の余裕を取り戻す（テストだけ） | [#387](https://github.com/hideyukiMORI/NeNeCommander/issues/387) / [#400](https://github.com/hideyukiMORI/NeNeCommander/pull/400) | Domain 146・Application 737、Domain 97.47 %、Application 見込み 96.60 % | `b1718a7` |
| 名前の衝突の後に再開した転送を `Escape` で取り消せるようにする | [#399](https://github.com/hideyukiMORI/NeNeCommander/issues/399) / [#401](https://github.com/hideyukiMORI/NeNeCommander/pull/401) | 修正前 2 件失敗 → Application 738・Presentation 293 成功 | `13f1046` |
| verified move と copy の祖先の照合を object identity だけにする | [#388](https://github.com/hideyukiMORI/NeNeCommander/issues/388) / [#391](https://github.com/hideyukiMORI/NeNeCommander/pull/391) | 本物の WSL の live tier 13 cell PASS、deep review（#395 の head、製品コードが同じ）成功 | `a257a75` |
| Infrastructure.Windows の mutation を閾値の上へ戻す（テストだけ） | [#395](https://github.com/hideyukiMORI/NeNeCommander/issues/395) / [#405](https://github.com/hideyukiMORI/NeNeCommander/pull/405) | Infrastructure 838 件 0 失敗、deep review run 43780626571 成功（Infrastructure.Windows 91.89 %） | `a12677b` |
| 260 文字を超える path の entry を、host の設定に依らず検査できるようにする | [#404](https://github.com/hideyukiMORI/NeNeCommander/issues/404) / [#406](https://github.com/hideyukiMORI/NeNeCommander/pull/406) | 修正前 3 件失敗 → Infrastructure 842 件 0 失敗 | `e60ee35` |
| keep-both の候補名の 255 文字の境界を実ファイルのテストで固定する（テストだけ） | [#407](https://github.com/hideyukiMORI/NeNeCommander/issues/407) / [#408](https://github.com/hideyukiMORI/NeNeCommander/pull/408) | Infrastructure 844 件 0 失敗。mutation は流していない | `8a32d6e` |
| Application の小さい CS-013 の残件 5 件を無くす（挙動は変えない） | [#410](https://github.com/hideyukiMORI/NeNeCommander/issues/410) / [#411](https://github.com/hideyukiMORI/NeNeCommander/pull/411) | Application 738・Architecture 21、Inspect 91 → 86 件、絞った mutation で新しい生き残り無し | `b180f86` |

deep review（run 43780626571、head `1beadfb`）の mutation score: Domain 97.47 %、Application 96.66 %、
Infrastructure.Windows 91.89 %、Presentation.WinUI 94.86 %。gate・adversarial ×3・CodeQL も成功。

Dependabot の [#394](https://github.com/hideyukiMORI/NeNeCommander/pull/394)（Microsoft.NET.Test.Sdk 18.12.0）は、
採らない理由を書いて close した（ADR-0048。release notes の CVE-2026-105544 は SDK 同梱の VSTest の問題で、
SDK は #381 で修正済みの 10.0.404）。

## 進行中

| 内容 | Issue / PR | 状態 |
|---|---|---|
| 設定保存の責務を分ける（CS-013 の Settings 系 14 件） | [#409](https://github.com/hideyukiMORI/NeNeCommander/issues/409) | 実装の席が作業中（本書の時点）。merge の前に CI の deep review を通す |
| ブックマーク管理モーダル（方向 A） | [#398](https://github.com/hideyukiMORI/NeNeCommander/issues/398) / [#402](https://github.com/hideyukiMORI/NeNeCommander/pull/402) | draft。hide の実機の確認待ち |

## 判断と経緯

- **#391 の deep review の失敗の原因**: exact-head の run 43724920547（`084746e`）は、canonical gate・
  adversarial tests・Domain（95.45 %）・Application（95.01 %）を通り、Infrastructure.Windows の mutation
  88.15 %（閾値 90 %）で止まった。ローカルの `main` の値は 91.30 % だったので、2 つの report を mutant ごとに
  突き合わせた。#391 が変えた行の mutant は CI ですべて Killed。状態が変わった 54 個は、どれも #391 が触れて
  いないファイルの Killed → Survived だった。ローカルでその 54 個を「倒した」のは verified move・transfer 系の
  テストで、中には `ThrowIfNull` の削除や設定の検証の static な引数など、そのテストでは倒せない mutant が
  あった。つまり `main` のローカルの 91.30 % は、#388 の不安定さで落ちたテストが mutant を偶然倒した分だけ
  高く出ていた。#391 が不安定さを直したので、本当の値が見えた。#387 の表と ADR-0048 の 2026-11-15 の
  pin review にある Infrastructure.Windows 91.30 % は、この分だけ高い。閾値は変えず、#395 でテストを足す。
- **#395 を #391 の上に積む**: #395 の branch は #391 の head から作った。その head の deep review が通った
  ので、製品コードの入力が同じ #391 の証拠にも使った（ADR-0061）。#391 → #405 の順に merge した。
- **#395 の中身**: テストを 9 つの fixture に 63 件と、既存の fixture に 1 件足した。製品コード・閾値・除外・
  tooling は変えていない。絞った Stryker で一覧の 203 個のうち 67 個が Killed（3 個は倒したテストを特定
  できていない）。見込み ≈ 91.9 % に対して、CI の実測は 91.89 %。残した mutant は、等価なもの、本物の
  OS・COM・プロセス・2 つの volume が要るもの、文言だけのもので、理由は Issue #395 に 1 つずつ書いた。
- **#404 は #395 のテストを書く途中で見つけた**: `WindowsLocalEntryIdentity.Describe` は `entry.FullName` を
  そのまま native に開いていた（`Capture` は extended-length の形で開いていた）。`LongPathsEnabled=0` の
  host（開発機もそう）では、260 文字を超える実在の entry が「見つからない」と検査されていた。`Find` と
  一覧は同じ entry を見つけていた。開き方を 1 つの `ExtendedLength` にそろえた。identity の token は開いた
  handle から作るので、値は変わらない。実機（アプリ）では動かしていない。
- **`main` の deep review を 1 回起動した**: #399 と #404 は、それぞれ絞った確認だけで merge した。
  両方を含む `main`（`e60ee35`）で deep review（run 43787538393）を起動し、成功した（gate・adversarial ×3・
  mutation・CodeQL）。mutation score は Domain 97.47 %、Application 96.60 %、Infrastructure.Windows 91.84 %、
  Presentation.WinUI 94.86 %。#407 と #410 はこの run の後に merge したので、入っていない。
- **CS-013 の残件（10/19 の引き継ぎの ④ ⑤）に入った**: `eng/measure-complexity.ps1 -Mode Inspect` は `main`
  `8a32d6e` で 91 件（製品 30、テスト 61）だった。10/19 の 80 件から増えたのは、その後に足した製品コード
  （枠無しウィンドウ、F1 のヘルプなど）の分。Application の小さい 5 件は設計リナが直した（#410）。
  設定保存の 14 件は、検査と test hook の順序を 1 つも変えないことを条件に、実装の席に依頼した（#409）。
- **CI とローカルで結果が違う mutant を 1 個見つけた**（#410 のコメント）: `PaneReducer.GetFocusIndex` の
  `First()` → `FirstOrDefault()` は、CI では 2 つのテストに倒され、ローカルでは生き残った。不変条件の下では
  結果を変えないはずの mutant なので、CI のその 2 つのテストが mutation の実行中に不安定な可能性がある。
  調べていない。
- **本物の WSL の live tier**: #391 の head（`5aeaf6b`）で 13 cell PASS。recycle の live tier は、hide の
  本物のごみ箱に触れるので流していない（#391 は recycle の経路に触れていない）。
- **#358 の原因は 3 つ**（入力を送らない UIA の探査で確かめた）:
  1. 名前の無い `Border` と `ItemsControl` は、AutomationId を付けても UI Automation に出ない。名前の付いた
     ペインの枠は出る。操作バーとキー操作の案内に、読み上げ用の名前（`操作バー`、`キー操作の案内`）を付けた。
  2. ファイル行の識別子 `FileListRow_…` とコマンドパレット行の識別子は、`ListViewItem` の Style の Setter に
     binding で書かれていた。WinUI は Setter の binding を無視するので、行の container に識別子が付いたことは
     一度も無かった（見えていた名前は項目の内容から来ていた）。製品の不具合。`RowAutomationIdentity` が、
     list が container を用意する時に識別子と名前を写す形に直した。
  3. Observe は前面を取らないので、システムのフォーカスは別のプロセスにあり、`@focusedRow` は常に不在
     だった。製品はフォーカス行を list の選択として見せているので、harness は選択中の行を測る。
  同じ理由で効いていなかったパレット行の `ToolTip` の Setter は削除した（表示は変わらない）。出すかどうかは
  [#397](https://github.com/hideyukiMORI/NeNeCommander/issues/397) で hide が決める。
- **#387**: Domain は 9 個のうち 4 個を倒し、残り 5 個は等価。Application は CI の report の 97 個のうち、
  観測できない `ConfigureAwait` 26 個と文言だけの 15 個を除いた 56 個から 31 個を倒した。残り 25 個は等価か、
  port の契約の上で届かない。理由は Issue に 1 つずつ書いた。Application の層全体の値はローカルでは測って
  いない（見込み 1876 / 1942）。
- **#399 は #387 のテストが見つけた**: `ResumeConflictAsync` は取り消しの action を登録せず、例外の時に
  `OperationRunning` を残していた。操作バーは実行中に `Esc 中止` を案内しているのに、最初の衝突を解決した
  後は止められなかった。最初の実行と再開を 1 つの `RunCancellableAsync` に通した。#387 が直す前の挙動を
  固定していたテスト 1 件は、この修正で書き換えた。
- **#386（Dependabot の除外）**: GitHub Docs の「Ignoring specific dependencies」は、ignore が version update と
  security update の両方の PR に効くと書いている。「security update には効かない」と明記されているのは
  `allow` の `update-types` と `cooldown` だけ。受入条件（security update を止めない）を満たす書き方は文書に
  無い。設定は入れず、3 つの選択肢を Issue に書いた。hide の判断待ち。
- **#398（ブックマーク管理モーダル）**: 方向案 A・B・C をデザインキャンバスで出し、hide がチャットで
  「A,良い、残す、閉じる」と答えた（方向 A、`a`/`e`/`d` の 1 文字キーを使ってよい、カテゴリの編集は
  モーダルに残す、scrim のクリックで閉じる）。細部（`Tab` でカテゴリの列と一覧を行き来する、カテゴリの
  移動で絞り込む、`/` で検索欄、削除の確認は [キャンセル] から）は設計リナが決めて ADR-0079 に書いた。
  モーダルを開くには入力が要るので、実機では見ていない。draft のまま hide の確認を待つ。

## 開発中の失敗と修正

- **#398 の依頼書に「削除は確認あり」を書き落とした**: キャンバスの案は「`d` 削除（確認あり）」で、hide の
  「良い」はその形への回答だった。依頼書に「今のボタンと同じ action」と書いたため、最初の実装は確認なしで
  消える形になった。報告で気付き、確認（session が所有、[キャンセル] から始まる）を足すよう戻した。
- **ADR-0079 の却下案の説明が事実と違っていた**: 実装の席が方向 B・C を「右の drawer」「上の sheet」と
  書いていた。キャンバスの内容（B はパレット型の 1 列、C は今の配置のまま）に直した。
- **Bash の heredoc がバックスラッシュを半分にした**（2 回）: 突き合わせの script と、#399 のテストの path。
  どちらも実行や build で気付き、Write / Edit で直した。
- **D ドライブが遅く、コマンドが timeout した**: ほかの席の負荷による。本体側のファイルを読む形に変えた。
- **`gh issue create` の本文が壊れた**（#404）: bash の二重引用符の中の backtick とバックスラッシュが
  解釈された。`--body-file` で書き直した。
- **`main` の取り込みで `PROJECT_STATE.md` に古い行が残った**（#406）: 衝突の印だけを機械的に消したので、
  #388 の merge 前の Session 2026-11-15 の行が二重に残った。差分を見て気付き、`main` の行だけにした。
- **#394 の close の文に、見ていないことを 1 つ書いた**: 「#382 と同じ理由」と書いたが、#382 の close の
  コメントは読んでいない（引き継ぎ書の記述による）。理由そのものは #394 のコメントに独立して書いてある。

## 環境と後片付け

- 作業は `D:/NeNeCommander/worktrees/issue-*`、一時出力は `D:/NeNeCommander/outputs/issue-*`。
- 証拠: `D:/NeNeCommander/evidence/ui-evidence-layout-20261116/`（#358 の探査・Observe）、
  `D:/NeNeCommander/evidence/mutation-margin-20261116/`（#387 の report、#391 の CI の report と突き合わせの
  script・結果、#395 の絞った mutation、run 43780626571 の report）、
  `D:/NeNeCommander/evidence/moved-target-flake-20261115/live-wsl-20261116/`（live tier の trx）。画面写真はローカルの内容を含むので repository に入れていない。
- 後片付けの結果は引き継ぎ書に書いた。

## 残件

引き継ぎ書の「次回の候補」と「hide の判断待ち」を参照。
