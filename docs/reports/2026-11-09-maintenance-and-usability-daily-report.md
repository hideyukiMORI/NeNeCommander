# 依存関係の更新と操作性の改善・日報 — 2026-11-09

hide の「NeNeCommander の作業の準備をお願い」から始め、固定 SDK が手元に無い問題の調査を
きっかけに、依存関係の更新、既存の不安定テストの修正、hide が実機で見つけた操作性の改善を
順に行った。すべて Issue 起点の PR で squash merge し、`main` は `b32c349` に同期済み。

## 完了範囲

| 内容 | Issue / PR | 検証 | merge |
|---|---|---|---|
| external-parent の部分削除検出テストを決定的にする | [#348](https://github.com/hideyukiMORI/NeNeCommander/issues/348) / [#349](https://github.com/hideyukiMORI/NeNeCommander/pull/349) | suite 30 回 0 失敗、否定証明 5/5 | `df3d789` |
| .NET SDK 10.0.401 → 10.0.403（10.0.13 / 10.0.14 security servicing） | [#347](https://github.com/hideyukiMORI/NeNeCommander/issues/347) / [#350](https://github.com/hideyukiMORI/NeNeCommander/pull/350) | full gate、dependency-review、quality | `7671c95` |
| setup-dotnet 6.1.0、codeql-action 4.39.3（Dependabot #346 の置換） | [#351](https://github.com/hideyukiMORI/NeNeCommander/issues/351) / [#352](https://github.com/hideyukiMORI/NeNeCommander/pull/352) | security-check、dependency-review、quality | `599be28` |
| MSTest.Sdk 4.5.2（Dependabot #345 の MSTest 部分） | [#353](https://github.com/hideyukiMORI/NeNeCommander/issues/353) / [#354](https://github.com/hideyukiMORI/NeNeCommander/pull/354) | full gate、dependency-review、quality | `082e66e` |
| Windows App SDK 2.6.5（#345 / #356 の置換） | [#355](https://github.com/hideyukiMORI/NeNeCommander/issues/355) / [#357](https://github.com/hideyukiMORI/NeNeCommander/pull/357) | full gate、dependency-review、quality、Observe 36 起動 | `f4d8473` |
| 設定モーダルの背景を透過 scrim に | [#360](https://github.com/hideyukiMORI/NeNeCommander/issues/360) / [#362](https://github.com/hideyukiMORI/NeNeCommander/pull/362) | App build、commit gate、hide 実機 | `be7a4a0` |
| キーヒントのキーと操作名を縦中央に | [#361](https://github.com/hideyukiMORI/NeNeCommander/issues/361) / [#363](https://github.com/hideyukiMORI/NeNeCommander/pull/363) | 画面写真の前後比較、hide 実機 | `a24e384` |
| F1 のキー操作ヘルプ（ADR-0076） | [#359](https://github.com/hideyukiMORI/NeNeCommander/issues/359) / [#365](https://github.com/hideyukiMORI/NeNeCommander/pull/365) | full gate（1891 成功）、hide 実機 | `e7d116a` |
| 実行中の配色切り替えを表示中の要素へ反映 | [#366](https://github.com/hideyukiMORI/NeNeCommander/issues/366) / [#367](https://github.com/hideyukiMORI/NeNeCommander/pull/367) | 前後比較、Architecture tests、hide 実機 | `78ecf96` |
| 設定モーダルを j/k とカーソルキーで操作（ADR-0077） | [#364](https://github.com/hideyukiMORI/NeNeCommander/issues/364) / [#368](https://github.com/hideyukiMORI/NeNeCommander/pull/368) | full gate（1896 成功）、hide 実機 | `b32c349` |

Dependabot の #345・#346・#356 は、置き換えた PR を記したコメントを付けて close した。

## 判断と経緯

- **SDK**: 固定 10.0.401 は system SDK から消えていた。D 側の保持 SDK で作業を続ける案もあったが、
  release metadata で 10.0.13（44 CVE）と 10.0.14（out-of-band、10.0.13 で不完全な 2 CVE の再修正）
  を確認し、hide の判断で 10.0.403 に上げた。
- **不安定テスト**: SDK 更新の full gate が `external-parent`（ADV-007）で 2 回止まった。SDK 10.0.401 の
  `main` でも 15 回中 3 回再現し、SDK とは独立だった。NTFS のディレクトリ時刻が約 15.6ms の刻みで
  記録され、fixture 作成と外部書き込みが同じ刻みに入ると親の時刻が変わらないことを計測で確認した
  （待ち 0ms: 191/200 で不変、16ms: 0/200）。見逃し時も削除されるのは検証済みの項目だけで、製品の
  安全性の欠陥ではないと hide と合意し、既存の時刻注入でテストを決定的にした。通るまで再実行する
  方法は取らなかった。
- **Windows App SDK**: gate はアプリを起動しないため、hide の許可を得て 2.5.1 と 2.6.5 を
  `eng/ui-evidence` Observe（`-OwnedProfile`、キー送出なし）で 18 回ずつ起動し、終了・画面・設定の
  復元に差がないことを確認した。
- **F1 ヘルプ**: キーは hide が `F1` を選択、見た目は方向 A（中央パネル）を選択。最初のビルドは
  幕だけが出てパネルが出なかった。`MaxHeight` を Collapsed のオーバーレイの `ActualHeight`
  （通知されない）に Binding していたためで、起動時は隠して後から表示する一時ビルドで再現して直した。
- **設定モーダル**: hide の決定で、配色の上の移動は focus のみ、選択と保存は `Space` / `Enter`。
- **配色**: OS ダークで NeNe Light に切り替えると読めない不具合を hide が発見。`StaticResource` は
  一度だけ解決されるため、辞書の差し替えでは表示中の要素が旧配色のまま残っていた。既存ブラシの
  色を書き換える方式にした。最初の修正は `Source` 付き辞書へのローカル値設定でアプリが終了し、
  一時ログで例外を確認して `Color` 項目を対象外にした。

## 開発中の失敗と修正

- `dotnet format`（analyzer 込み）が XAML から呼ばれるイベントハンドラーを未使用と誤判定して削除した。
  `CommanderWindow.xaml.cs` を HEAD から戻して配線のみ再適用し、無関係な書き換えも戻した。以後は
  gate と同じ `dotnet format whitespace` だけを使う。
- 新規ファイルを LF で作り、#364 の最初の full gate が書式検査で止まった。`dotnet format whitespace`
  で直して再実行した。
- `!` 付きのコマンドは bash で動くため、PowerShell の `Remove-Item` は失敗した。長い 1 行は
  コピー時に折り返されて引数が切れた。短い bash の行に分けて解決した。

## 環境と後片付け

- 作業は `D:/NeNeCommander/worktrees/issue-*`、一時出力は `D:/NeNeCommander/outputs/issue-*`。
  merge 後にすべて削除済み。`D:/NeNeCommander/tools/dotnet-10.0.401` と空の `outputs/issue-332`・
  `issue-336` は hide が削除した。
- 証拠は `D:/NeNeCommander/evidence/` の 2026-11-09 付きフォルダ（sdk-10-0-403、issue-351、issue-353、
  windows-app-sdk-2-6-5、settings-scrim-key-hint、keyboard-help、live-color-scheme、settings-keyboard）。
  画面写真はローカルのドライブ内容を含むため repository に入れていない。
- 自動承認で止められた操作（PR の Ready、workflow の起動、削除）は、hide の実行または WSL 側への依頼で
  行い、別の方法で回避していない。

## 残件

- [#358](https://github.com/hideyukiMORI/NeNeCommander/issues/358): UI evidence の Observe で `OperationBar` と
  `OperationKeyHints` が UIA から見つからず、全 cell が layout FAIL になる（既存、本日の変更と無関係）。
- [#94](https://github.com/hideyukiMORI/NeNeCommander/issues/94): hide 同席の手動リリース確認。
- ブックマーク管理モーダルも不透明な背景でファイラが見えない（hide に報告済み、未着手）。
- 10/19 の引き継ぎにある ④ 設定保存の整理と ⑤ 残りの製品フロー・テストの CS-013 整理は未着手。
