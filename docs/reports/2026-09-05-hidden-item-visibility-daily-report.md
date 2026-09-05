# 日報 — 2026-09-05（hidden 項目の可視集合、作業途中で終了）

Status: informational

## 本日の区切り

hide の指示でセッションを区切ったため、[Issue #49](https://github.com/hideyukiMORI/NeNeCommander/issues/49)（保存された `showHiddenItems` を pane の可視集合に効かせる）は **merge していない**。実装途中の状態を branch `feat/49-hidden-item-visibility`（`74d9169`）に commit して push し、Issue にも状況を残した。`main` は `f121ed6` のままで、作業ツリーはクリーン。

本日はこの前に次を merge 済み。各日報は `docs/reports/2026-09-05-*.md`。

- [Issue #40](https://github.com/hideyukiMORI/NeNeCommander/issues/40) design pass（PR #41 / #42）: design brief と design canvas。hide が方向 C と 8 つの color scheme を承認。
- [Issue #43](https://github.com/hideyukiMORI/NeNeCommander/issues/43)（PR #44、ADR-0022）: color scheme を設定で選ぶ構造。
- [Issue #46](https://github.com/hideyukiMORI/NeNeCommander/issues/46)（PR #47、ADR-0023）: 方向 C の layout を shell に統合。

実装は Opus 5 のバックグラウンド agent、リナは仕様の切り出し、証跡の突き合わせ、diff / ADR review、実機 screenshot と canvas の照合、docs を担当した。

## branch に残した内容（`74d9169`）

- Application: `EntryVisibility`（`Normal` / `Hidden`）を `DirectoryEntry` の必須要素にした。判定は provider が報告する属性で、名前からは決めない。
- Application: `PaneState` が entry 全体・`HiddenItemVisibility`・そこから導かれる `VisibleEntries` を持ち、`VisibleItems`（path の列）を置き換えた。`PaneReducer` が可視集合を決める唯一の場所になり、移動・paging・先頭 / 末尾・選択はすべて可視集合だけを見る。`ApplyHiddenItemVisibility` が切り替えの transition。
- Focus 回復規則は一つ: 対象が可視ならそのまま、次に listing 順で次の可視項目、次に前の可視項目、無ければ focus なし。navigate と可視性切り替えの両方で同じ規則を使う。
- Selection は不可視になった項目を落とす（KBD-004: 見えない行を file command の対象にしない）。
- Infrastructure: `WindowsLocalDirectoryReader` が列挙済みの属性から `Hidden` を報告する（追加の I/O 無し）。
- Presentation: `PaneListingPresenter` が可視行だけを射影し、`PaneRowVisibility` が hidden 行の描画を持つ。
- Docs: ADR-0024 の下書き、ADR README、GLOSSARY。

## 検証証跡

- `pwsh -NoProfile -File ./eng/check.ps1`: PASS（pre-commit hook 経由）。
- tests: 372 passed、0 failed、0 skipped（merge 済み main の 351 から +21）。
- branch coverage: Domain 100.00%、Application 100.00%、Infrastructure.Windows 95.48%、Presentation.WinUI 98.06%。
- **未実施**: `eng/deep-review.ps1`（mutation）、実機確認（`showHiddenItems` の true / false で exe を起動して screenshot と UIA）、ADR-0024 の内容 review、PR。この 4 つが済むまで merge しない。

## 本日の気付き（merge 済み分を含む）

- WinUI は resource dictionary の `Geometry` を `Path.Data` に代入できない（実行時に `XamlParseException`）。icon の形は view に置き、どれを見せるかを閉じた型が決める形にした。
- Stryker は static field initializer の mutant を型初期化子を最初に踏んだテストに帰属させるため、閉じた型の値を static field に置くと mutant が生き残る。expression-bodied property にすると解消する。
- `x:Bind` の converter は解決の起点に `FrameworkElement` を要求するので、`Window` が root の `DataTemplate` では古典的な `{Binding}` を使う。
- settings store は書き込みをしない（ARC-005 / CMD-007）。既定 file は hide が手で置く。このマシンには有効な file がある。

## 残した注意点

- `feat/49-hidden-item-visibility` の実機確認は完了した。final head の canonical CI 成功までは merge しない。
- 完了後の operation bar に `12/12` が残らない。進捗数値の色は tone の文字色で canvas の accent とは異なる。
- `F8` 確認、`F2` / `F7` 名前入力、実行中進捗の実機の見た目は未確認（キー送信をしないため hide の確認待ち）。
- high contrast、他の DPI、狭い window、nene-dark と solarized-light 以外の scheme は実機未確認。
- 前回からの注意点は継続（`docs/reports/2026-09-05-direction-c-layout-daily-report.md`）。

## 次の推奨縦切り

`feat/49-hidden-item-visibility` の Ready、fresh canonical CI、merge。詳細は [`docs/handoffs/2026-09-05-hidden-item-visibility-handoff.md`](../handoffs/2026-09-05-hidden-item-visibility-handoff.md)。

## 実装サナ継続（18:41 JST 以降）

- `main` の ADR-0025 / QLT-015 を no-commit merge で取り込み、過去の毎試行 full gate 手順を適用しなかった。
- 全 entry が hidden の pane を `Shown -> Hidden -> Shown -> MoveNext` と遷移させる短い reducer test を追加した。修正前は可視 entry が戻っても focus が null のままで、`GetFocusIndex` が `Sequence contains no matching element` を投げることを確認した。
- visibility transition が旧 focus を回復できず可視集合が非空なら、先頭の可視 entry を focus にするよう単一 reducer 経路を修正した。実装 checkpoint は `dfe0593`。
- Release restore / build は成功（warning 0）。修正後の対象 test 1 件、Application 173 件、Infrastructure 66 件、Presentation 65 件はすべて成功した。
- `pwsh -NoProfile -File ./eng/deep-review.ps1`: PASS。canonical 部分 373 / 373、branch coverage 100.00 / 100.00 / 95.48 / 98.06%、mutation 97.12 / 98.32 / 93.20 / 100.00%。
- hide の desktop を妨げないよう、別 desktop object 上で `showHiddenItems=false` / `true` の Release process と window 作成まで確認した。別 desktop は DWM / UI Automation content を取得できず screenshot が黒画像だったため、これを表示差の proof とは扱わない。settings file は byte snapshot から復元した。interactive desktop の screenshot / UIA は未完了のまま明示し、PR は Draft を維持する。
- deep review の fixture copy で、負例ごとに約 410 MiB、cleanup 累計 8.1 GiB の生成物コピーを実測した。これは次の gate fixture performance Issue の改善前 evidence とする。

## 実装サナ継続（最新 `main` 統合）

- Issue #54 / #56 / #57 / #58 / #59 の完了後、Draft PR #53 に最新 `main`（`dd6439e`）を統合した。
- 競合は project state、hidden 行投影と増分 row 投影、部分 copy の null guard の4ファイルに限定された。可視集合を決めるのは引き続き `PaneReducer` だけで、presenter は `VisibleEntries` を ADR-0028 の安定した `PaneRows` へ投影する。
- Release restore / build は成功（warning 0、error 0）。Application 176、Infrastructure.Windows 69、Presentation.WinUI 76、Architecture 5 はすべて成功し、失敗・skip は0。conformance 110規則と security 18 adversarial cases も成功した。
- ADR-0025 に従い、統合済み各PRの成功CIと重複するローカル full gate / deep review は追加していない。#49 final head の canonical CI は interactive-desktop proof 後に Ready にして取得する。
- interactive desktop の screenshot / UIA proof は未完了のままであり、別 desktop object の黒画像を表示差 proof に昇格していない。

## 実装サナ継続（通常 desktop proof）

- hide の明示許可「起動はいつでも好きにしていいよ」に従い、通常 desktop で Release executable を `showHiddenItems=false` / `true` の順に起動した。キー送信は行わず、各caseで実装サナが起動した process だけを終了した。
- false の UI Automation は左 `C:\` に通常項目だけを観測し、`$RECYCLE.BIN`、`System Volume Information`、`hiberfil.sys`、`pagefile.sys`、`swapfile.sys` を含む hidden/system 項目が存在しないことを確認した。
- true の UI Automation は同じ左 `C:\` に上記 hidden/system 項目をすべて観測した。通常 desktop の window screenshot は正常に描画され、hidden/system 行の名前が通常行より muted な `TextHiddenBrush` 表示であることを確認した。
- settings document は元の `showHiddenItems=false` / `colorScheme=nene-dark` に復元され、所有 process が残っていないことを確認した。画像・UIA tree・summary は test evidence として `artifacts/implementation-sana/runtime-proof/normal-*` に保存した。
- `origin/main=dd6439e` を再取得し、branch の merge-base が同じであることを確認した。残る統合条件は final head に対する fresh canonical CI だけである。
