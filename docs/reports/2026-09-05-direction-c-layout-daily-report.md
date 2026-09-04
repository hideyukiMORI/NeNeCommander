# 日報 — 2026-09-05（方向 C の layout 統合）

Status: informational

## 本日の区切り

[Issue #46](https://github.com/hideyukiMORI/NeNeCommander/issues/46) / [PR #47](https://github.com/hideyukiMORI/NeNeCommander/pull/47) で、design canvas page 3「方向 C」の layout を shell に統合し、squash merge した（main `c3791ae`、ADR-0023）。承認された数値は token になり、view には layout の literal が残っていない。

本日はこの前に [Issue #40](https://github.com/hideyukiMORI/NeNeCommander/issues/40)（design pass）と [Issue #43](https://github.com/hideyukiMORI/NeNeCommander/issues/43)（color scheme を設定で選ぶ構造）も merge 済み。日報は `docs/reports/2026-09-05-color-scheme-daily-report.md`。

実装は Opus 5 のバックグラウンド agent、リナは仕様の切り出し、証跡の突き合わせ、diff / ADR review、実機 screenshot と canvas の照合、docs を担当した。

## 完了したこと

- Token（`Themes/DesignTokens.xaml`、13 family のまま）: 窓 padding 6、ペイン / bar の gap 3、角丸 3（key cap 2）、border 1、ペイン header 34、行 28、marker 2、kind icon 16、bar 34、名前入力 320 × 26、body 13、monospace 12 / 11、monospace family（Cascadia Code → Cascadia Mono → Consolas）。framework の chrome を消すための reset token 3 件を追加し、使われなくなった 4 token を削除。
- Presentation: `PaneRowMark` を 4 member（`FocusInActivePane` / `Selected` / `FocusInPassivePane` / `Unmarked`）にし、marker と背景の semantic brush を持たせた。`PaneRowKind` が `DirectoryEntryKind` の描画と `DIR` ラベルの key を閉じた形で表す。`PaneListingPresenter.Present` は pane の `PaneFrame` を受け取り mark を一つに決める（active の focus > selection > passive の focus）。`DualPanePresentation` に `Tone`（`OperationBarTone`: idle / awaiting name / awaiting confirmation / failure、surface・foreground・border・icon を指名）と `KeyHints` を追加。進捗は 12 segment を presentation 側で計算。
- Keyboard: `KeyboardIntentMapper` を table 駆動にし（`IReadOnlyList<KeyBinding>` が `Map` と `BindingsFor(context)` の両方を駆動、`gg` chord だけ table の前で解決）、`KeyHintPresenter` が context ごとの順序付き intent を canonical table から引いて `KeyHint(keyLabelResourceKey, intentLabelResourceKey)` を出す。KBD-005 の「hint は canonical key map から生成する」が実装として成立した。
- App: `CommanderWindow.xaml` を方向 C の構造に置き換え（ペイン番号 badge の header、28 dip 行 + marker + stroke icon + `DIR`、全幅 34 dip の operation bar に icon・文言・detail・key hint・名前入力）。address は `TextBox`、一覧は `ListView` のまま最小 template にして scheme が見た目を決めるようにし、`UseSystemFocusVisuals` を off（行自身が focus を描くため）。lookup だけを行う converter 2 件（`SemanticResourceConverter`、`LocalizedTextConverter`）を追加。
- Resources: 「左ペイン」「右ペイン」は削除せず、ペイン領域の narrator 名（`*PaneRegion.AutomationProperties.Name`）に移した。address の `Header` は行ごと削除。key cap と intent の文言、ペイン番号は resw。
- Gate: `eng/conformance.ps1` に ARC-012 の追加 scan（Presentation の `...Color` / `...Brush` literal が scheme の key 集合に存在すること）、`eng/prove-gates.ps1` に negative proof、`GATE_PROOFS.md` に行。
- Docs: ADR-0023、ADR README、KEYBOARD_MODEL（KBD-005）、GLOSSARY、design brief の token 表。

## 検証証跡

- `pwsh -NoProfile -File ./eng/check.ps1`: PASS（pre-commit hook 経由）。
- GitHub quality run（PR #47、`ec7bf24`）: [`33900096342`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33900096342) 成功、3分29秒。
- GitHub quality run（main、`c3791ae`）: [`33900434956`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33900434956) 成功。
- ローカル deep review: passed（2 回目。1 回目は Presentation の mutation 89.19% で失敗、下記）。
- tests: 351 passed、0 failed、0 skipped（前回 334 から +17、Presentation は 45 → 62）。
- branch coverage: Domain 100.00%、Application 100.00%、Infrastructure.Windows 95.43%、Presentation.WinUI 98.05%。
- mutation score: Domain 96.15%、Application 98.19%、Infrastructure.Windows 93.06%、Presentation.WinUI 100.00%。
- 実機（Release exe、5 回起動、キー送信なし）: `nene-dark` と `solarized-light` を起動して screenshot と UIA で確認し、`nene-dark` に戻した。screenshot は scratchpad の `layout-nene-dark.png` / `layout-solarized-light.png`。canvas の artboard「C · nene-dark」と、行の高さ、marker、badge、bar、hint の並びが一致することを目視で照合した。
- 未確認: `F8` 確認、`F2` / `F7` 名前入力、実行中の進捗の実表示（presentation テストのみ）。high contrast、他の DPI、狭い window、残り 6 scheme の実表示。

## 気付き

- WinUI は resource dictionary に置いた `Geometry` を `Path.Data` に代入できない（`XamlParseException`）。icon の形は view に置き、どちらを見せるかを閉じた `PaneRowKind` / `OperationBarIcon` が決める形にした。build では出ず実行して初めて分かる種類の失敗。
- Stryker は static field initializer の mutant を「型初期化子を最初に踏んだテスト」に帰属させるため、resource key 文字列の mutant が生き残って Presentation が 89.19% になった。`KeyboardKey` などを expression-bodied な abstract property にして呼び出しごとの getter に移したら 100.00% になった。
- `x:Bind` の converter は解決の起点に `FrameworkElement` を要求するので、`Window` が root の `DataTemplate` 内では古典的な `{Binding}` を使う必要があった。
- framework の focus rectangle が行の marker を覆っていたので `UseSystemFocusVisuals` を off にした。

## 残した注意点

- 完了後の bar に `12/12` が残らない（canvas は残す）。閉じた activity が完了後の進捗を持たないため。進捗の数値は tone の foreground 色で、canvas の accent とは異なる。
- hidden 項目の区別は未実装（`DirectoryEntry` に hidden の概念が無い）。`TextHiddenBrush` は定義だけ存在。
- `HiddenItemVisibility` は設定から読むだけで pane transition が消費していない。
- 前回からの注意点は継続（`docs/reports/2026-09-05-color-scheme-daily-report.md`）。

## 次の推奨縦切り

hidden 項目の表示切り替え（設定の `showHiddenItems` を pane transition が消費し、hidden 行を `TextHiddenBrush` で描く）。詳細は [`docs/handoffs/2026-09-05-direction-c-layout-handoff.md`](../handoffs/2026-09-05-direction-c-layout-handoff.md)。
