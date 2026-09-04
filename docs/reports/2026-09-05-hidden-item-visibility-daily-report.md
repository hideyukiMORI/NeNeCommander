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

- `feat/49-hidden-item-visibility` は gate は通るが未検証。merge 前に deep review と実機確認が要る。
- 完了後の operation bar に `12/12` が残らない。進捗数値の色は tone の文字色で canvas の accent とは異なる。
- `F8` 確認、`F2` / `F7` 名前入力、実行中進捗の実機の見た目は未確認（キー送信をしないため hide の確認待ち）。
- high contrast、他の DPI、狭い window、nene-dark と solarized-light 以外の scheme は実機未確認。
- 前回からの注意点は継続（`docs/reports/2026-09-05-direction-c-layout-daily-report.md`）。

## 次の推奨縦切り

`feat/49-hidden-item-visibility` の続き（deep review、実機確認、PR、merge）。詳細は [`docs/handoffs/2026-09-05-hidden-item-visibility-handoff.md`](../handoffs/2026-09-05-hidden-item-visibility-handoff.md)。
