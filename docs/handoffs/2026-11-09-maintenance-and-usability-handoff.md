# 依存関係の更新と操作性の改善・引き継ぎ — 2026-11-09

本日の内容は[日報](../reports/2026-11-09-maintenance-and-usability-daily-report.md)に記録した。
`main` は `b32c349`。追加の worktree・一時出力は残っていない。

## 引き継ぐ状態

- 固定 SDK は 10.0.403（system SDK だけで build・gate が動く）。MSTest.Sdk 4.5.2、Windows App SDK 2.6.5、
  setup-dotnet 6.1.0、codeql-action 4.39.3。開いている Dependabot PR は無い。
- 新しい keyboard context が 2 つ: `KeyboardHelp`（ADR-0076）と `Settings`（ADR-0077）。キーを追加・変更
  したときは F1 ヘルプの conformance テストが漏れを検出する。
- 配色の実行中の切り替えは、既存ブラシの色の書き換えで反映する（#366）。view が `…Color` キーを
  直接参照すると追従しないため、view はブラシだけを参照する。
- 新しい UI 要素を後から表示する overlay では、`ActualHeight` への Binding を使わない（#359 で発覚）。
  コマンドパレットと Locations も同じ書き方だが、現状は正しく表示されている。

## 次回の候補

1. **#358 UI evidence の layout 判定**: `Border` / `ItemsControl` の AutomationId が UIA に出ない原因を確定し、
   製品側で公開するか harness の期待値を直すかを決める。調査には実機の Observe 起動が要る
   （hide が画面を使っていない時間に）。
2. **ブックマーク管理モーダル**: 透過 scrim と `j`/`k` 操作（#360・#364 と同じ方針）を hide に確認してから。
3. **10/19 の引き継ぎの ④ 設定保存の整理・⑤ CS-013 残件**:
   [2026-10-19 引き継ぎ](2026-10-19-cs013-priority-handoff.md)の手順のまま。

## 作業上の注意

- `dotnet format` は analyzer 込みで実行しない（XAML のハンドラーを削除する）。gate と同じ
  `dotnet format whitespace` を使う。新規ファイルは CRLF で作る。
- `!` 付きのコマンドは bash。hide にコマンドを頼むときは、bash の短い行に分け、`-R hideyukiMORI/NeNeCommander`
  を付ける（このセッションの作業ディレクトリはリポジトリの外のことがある）。
- アプリへのキー入力は `eng/ui-evidence` 以外で送らない（ADR-0052）。描画の確認が要るときは、一時ビルドで
  状態を作って Observe で撮影し、一時コードは必ず消して差分を確認する。
