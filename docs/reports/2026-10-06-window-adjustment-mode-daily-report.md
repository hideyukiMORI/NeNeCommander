# 日報 — 2026-10-06〜07(ウィンドウ調整モードの統合、codeql-action 4.38.2)

Status: informational

Fable 5 セッションの NeNeリナ(設計リナ)が設計・判断・文書・レビューを受け持ち、Opus のバックグラウンドの
NeNeリナ(実装リナ)3 席が実装・test・gate の運転を受け持った。hide は途中で 2 件を決め、runtime 証拠の実行に
立ち会った。

## 統合したもの

| Issue / PR | 内容 | squash merge |
|---|---|---|
| #155 / #156 | `github/codeql-action` v4.38.2(Dependabot PR #154 の置き換え) | `2d3fc29` |
| #100 / #157 | ウィンドウ調整モード(ADR-0050) | `41a682c` |

### Issue #155 / PR #156 — codeql-action 4.38.2

Dependabot PR #154 を、先例(#148 / #149)どおり Issue 起点の branch で置き換えた。`security-deep-review.yml` の
init / analyze の 2 行だけを変え、workflow ファイルは #154 の head と byte 単位で一致した。tag `v4.38.2` →
annotated tag `88585263` → commit `2892aa5e19bbd11bc0cff5427e3b750a04d9e3c2` の対応は GitHub API で照合した。
v4.38.2 の変更は default CodeQL bundle を 2.27.1 にする 1 項目だけである。dependency run `37482184733` と
canonical Ready run `37482232687` が成功した。#154 は理由をコメントして close した。

### Issue #100 / PR #157 — ウィンドウ調整モード

checkpoint `ef6e7e2` から再開した。`Ctrl+W` で入り、`h j k l` / 矢印で移動、`+` / `-` で左上固定の拡大・縮小、
`m` で最大化、`r` で復元、`Esc` / `Ctrl+W` で抜ける。

進め方:

1. branch に main(Windows App SDK 2.5.1)を `git merge` で取り込んだ。
2. 設計リナが ADR-0050 を第 5 次改訂した(下の「ADR-0050 の改訂」)。
3. 実装リナ 2 席を別 worktree で並行させた。host 担当は `src/NeNeCommander.App/**` と resource、proof 担当は
   `tests/**`・mutation の実測・規範 docs。担当ファイルを分けたので、merge の衝突は無かった。
4. 設計リナが両方をレビューして merge し、Draft PR を作り、ローカルの canonical gate を通した。
5. hide の合図の後に runtime 証拠を取り、Ready にして統合した。

最終の数値(head `f8de503`):

- Release build は 0 warning。test は合計 1004、成功 998、失敗 0、スキップ 6(live WSL tier)。
  Application 379 → 464、Presentation.WinUI 136 → 168。
- branch coverage は Domain 100.00% / Application 100.00% / Infrastructure.Windows 92.88% /
  Presentation.WinUI 92.57%。
- mutation のローカル実測(ADR-0048 の VSTest runner、proof branch `cd02740`)は Application 96.23%(閾値 95)、
  Presentation.WinUI 94.57%(閾値 90)。その後に入った production の変更は `WindowSizeConstraint.Create` の
  非有限 scale の拒否だけで、mutation は測り直していない。merge 後の main deep で確認する。
- `CommanderSession` は 279 論理行(上限 300、main は 261)。
- dependency run `37497306181` と canonical Ready run `37498044693` が成功した。

## 設計レビューで見つけて直した欠陥

checkpoint の code と host の実装を設計リナが読み、次を見つけた。修正は実装リナが行った。

| 欠陥 | 影響 | 修正 |
|---|---|---|
| caption rule の seam 判定が ADR より厳しい | 下の work area が caption の run 全体を覆うことを要求していた。幅や位置のずれた上下 2 画面で、下へ進めなくなる帯ができる | ADR どおり「1 step 以上の部分 segment」で判定する。部分的な重なり、1 step 未満の重なり、2 枚の合算をしないこと、隙間のある seam の 4 つを test で固定した |
| checkpoint が触った 46 ファイルに UTF-8 BOM | main は 0 件で `.editorconfig` は `charset = utf-8`。`dotnet format whitespace --verify-no-changes` が CHARSET で落ちることを確かめた。Commit mode の check は format を走らせないので、checkpoint では見つからなかった | 全件から先頭 3 byte を除いた |
| key action → window action の対応表が App の view にあった | App は coverage と mutation の対象外なので、左右の取り違えが unit test で捕まらない | `WindowAdjustmentKeyAction.WindowAction` として Presentation に移し、全値を網羅する test を足した |

proof 担当が test を書く中で見つけて直したもの:

- window mode の admission にあった palette の判定は到達不能だった(palette が開いている間は `HandleAsync` が
  そこまで来ない)。Application の branch coverage が 99.91% になり gate に落ちるので除いた。挙動は変わらない。
- Control 以外の modifier の entry が `KeyLabelCtrlW` の key cap を名乗っていた。
- `WindowSizeConstraint.Create` が NaN と ±∞ の scale を通していた。拒否するようにした。

## ADR-0050 の改訂

ADR-0050 は本 PR で `accepted` になった。今回の改訂(第 5 次)の内容:

- mode の中でも `KeyboardKey.Other` は pass through する。handled な `PreviewKeyDown` は `CharacterReceived` を
  抑止するので、`Other` を consume すると mode 自身の `h j k l m r + -` が届かなくなる。識別済みの未宣言キーと、
  宣言済みキーの未宣言 modifier は consume する。pass through したイベントが届くのは中身の無い focus sink だけで、
  `Tab` を consume するので focus は sink から出ない。
- `WindowPlacement` は scale と preferred minimum を `WindowSizeConstraint` に、2 つの work area を
  `WindowWorkAreas` にまとめる(CS-013 の引数上限のため)。
- helper の hint は 1 つの `WindowAdjustmentKeyHintTemplate`(cap 1〜4 個 + label 1 個)で描く。hint 行は
  `KeyHintWrapPanel` で折り返す。
- caption rule: 下の work area は 1 枚ずつ判定し、合算しない。1 step 以上の部分 segment を覆い、同じ深さまで
  続くことを求める。
- **隙間のある seam は跨げない。** 上の画面の下端に taskbar があると 2 つの work area は連続しないので、mode では
  縦方向に画面を跨げない(横方向は跨げる)。hide が 2026-10-07 に「決定どおり出荷」を承認した。
- adapter の apply が throw した場合は既存の defect 経路(再 throw)に乗る。以前の「mode は開いたまま」という文は
  実態と違ったので直した。
- stale な leave は状態を変えずに、変わらない状態を返す。
- Windows App SDK 2.5.1 で spike の 4 事実を runtime 証拠の中で確かめ直すことを Executable proof に書いた。

## hide が決めたこと

- runtime 証拠は、準備ができた時点でリナが声をかけ、hide の合図で実行する。
- taskbar を挟む上下の seam は決定どおり出荷する。隙間を飛び越える規則が要るなら別 Issue にする。

設計リナが決めたこと(hide の委任の範囲):

- helper の文言(en-US / ja-JP、29 key)。`Esc` の hint は「完了 / Done」にした。「閉じる」はウィンドウを閉じる
  操作に読めるため。planned の label は「した」ではなく action の名前にした(helper は決定より多くを主張しない)。
- helper の余白。design handoff の最初の値(横 10 / 縦 6 / 行間 6)は既存の token で表せなかった。実ウィンドウの
  screenshot を拡大して確かめ、横 16 / 縦 6 / 行間 10 のままにして handoff を合わせた。新しい token は足していない。
- proof 担当が equivalent と判断した mutant 4 件を受け入れた(内容は PR #157 の本文)。

## runtime 証拠

2026-10-07、hide の合図の後に実ウィンドウで 2 回実行した。Release build、Windows App SDK 2.5.1、125% の画面
(dpi 120、step = 40 physical px)、US-101 配列。キーは `SendInput` で送り、1 打ごとに前面が NeNe Commander で
あることを確かめた。ファイル操作キーは送っていない。記録の全体は PR #157 の本文にある。

- spike の 4 事実は 2.5.1 でも成り立った。mode のキーは sink 経由で届く。`Ctrl+W` は mode を 1 回だけ開き、続く
  文字で閉じない。1 回の move は bounds をちょうど 40 px 動かし、サイズを変えない。5 回続けて縮小しても、未宣言の
  最小値で拒否されない。
- 移動・拡大・縮小・最大化・復元と、3 種類の拒否(通常状態の `r`、最大化中の `h` と `m`)は期待どおりだった。
- `Esc` と `Ctrl+W` のどちらで抜けても focus は左の file list に戻り、address editor は開かなかった。
- mode を開いたまま別ウィンドウをクリックして戻ると、focus は sink のままで、キーも届いた。1 回目の実行では
  60 秒以内にクリックが検出されず、この項目だけ 2 回目(その項目だけの短い実行)で取った。コンソールの合図は hide
  からは見えないので、画面の動きで合図を伝える必要がある。
- 起動時の window 名と bounds、終了(exit code 0、716 ms と 103 ms)は 2 回とも正常だった。2026-09-28 に 2.5.1 の
  初回起動で 1 回だけ出た異常は再現しなかった。

観察(原因は未確認): `h` の押しっぱなし(key-down 15 回、33 ms 間隔)は 15 step 動いたが、`l` は 11 step だった。
拒否は出ておらず、届いた 1 回ごとにちょうど 1 step 動いている。処理が追いつかない間に OS が同じキーの repeat を
まとめた可能性がある。

証拠の出力とドライバはリポジトリ外の `D:\NeNeCommander\evidence-100\` にある。

## main の scheduled deep review

scheduled run `37189427536`(main `234f600`、2026-10-04)を読んだ。score は Domain 95.52% / Application 95.98% /
Infrastructure.Windows 90.86% / Presentation.WinUI 94.30% で、前回の基準 `35215425806` と 4 層とも同じだった。
CodeQL の result は 0 件、open alert も 0 件。Windows App SDK 2.5.1 を入れた後の main は、これで確認できた。

## 作業場所の移設

hide の 2026-09-30 の指示(リポジトリ本体の外の作業は D ドライブ)に従い、`D:\NeNeCommander\` を作った。

- #100 の worktree は `C:\Users\info\WORKS\NeNeCommander-100` から `D:\NeNeCommander\wt-100` に移した。ドライブを
  またぐ `git worktree move` は「Permission denied」で失敗する。旧 worktree を detach し、D 側に
  `git worktree add` で作り直し、旧 worktree は clean と push 済みを確かめてから削除した。
- 証拠は `D:\NeNeCommander\evidence-100\`、mutation の出力は `D:\NeNeCommander\mutation-100\` に置いた。

## やらなかったこと

- Issue #94(release matrix)。#100 で加わるセルのコメントは投稿した(引き継ぎ書を参照)。
- 100% の画面、taskbar を挟む seam の実機確認、scale の違う画面をまたぐ move の 2.5.1 での取り直し。
- `l` の押しっぱなしで step が減った原因の調査。
- 新しい pin(v4.38.2)での CodeQL と、#157 の後の main deep の結果の確認(引き継ぎ書を参照)。
