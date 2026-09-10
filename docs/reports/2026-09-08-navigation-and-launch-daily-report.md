# 日報 — 2026-09-08（address 入力・ペイン履歴・Windows file 起動）

Status: informational

この日報は 2026-09-09 に遡って記録した。当日は 3 つの縦切りを統合したが日報と引き継ぎ書を
作成していなかったため、PR 本文、ADR、GitHub Actions の記録だけを根拠に再構成している。
当日に観測されなかった事実はここにも書かない。

## 統合した縦切り

### Issue #107 — address 入力から active pane を移動する

[Issue #107](https://github.com/hideyukiMORI/NeNeCommander/issues/107) を
`feat/107-address-navigation` に実装し、[PR #108](https://github.com/hideyukiMORI/NeNeCommander/pull/108)
で統合した。ADR-0044 が正本。address focus と `Ctrl+L` は `CommanderSession` が所有する
一つの `AddressEditorState` に入り、`Enter` は `FileSystemPath.Parse` を一度だけ呼んで受理した
path を `DualPaneSession.NavigateAsync` → `PaneSession` → `IDirectoryReadPort` へ流す。無効入力は
raw text のまま編集可能に留まり、`Escape` と focus 離脱は pane 選択を変えずに canonical address を
復元する。永続 schema、依存、parser、provider、filesystem mutation、視覚構造、token は変えていない。

証拠: dependency-review run `34210612726`、canonical Ready run `34210697599` が head `1bbc3541`
で成功し、squash merge `f0e3ece6` となった。protected coverage は Domain 100.00% / Application
100.00% / Infrastructure.Windows 92.77% / Presentation.WinUI 93.24%。parser・provider・mutation・
containment・operation identity の契約が不変のため Issue 固有の deep review は実行していない。

### Issue #109 — ペイン単位の戻る・進む履歴

[Issue #109](https://github.com/hideyukiMORI/NeNeCommander/issues/109) を
`feat/109-pane-history` に実装し、[PR #110](https://github.com/hideyukiMORI/NeNeCommander/pull/110)
で統合した。ADR-0045 が正本。immutable な `PaneState` が一つの `PaneNavigationHistory` を持ち、
append / Back / Forward / refresh / 同一 location の遷移は現在 generation の読取が成功した後に
`PaneReducer` だけが行う。1 pane あたり最大 100 location を保持し、新規訪問で Forward を切り捨て、
失敗・cancel・refresh・同一 location・supersession・stale completion では履歴を保つ。`Alt+Left` /
`Alt+Right` を FileList と NavigationSurface context の canonical binding として追加し、text・
address・IME・modal・read・operation の優先順位は維持した。

証拠: dependency-review run `34226675152`、canonical Ready run `34226792938` が head `2eeed73d`
で成功し、squash merge `4ac98350` となった。protected coverage は Domain 100.00% / Application
100.00% / Infrastructure.Windows 92.77% / Presentation.WinUI 93.49%。filesystem・process・
persistence・destructive operation・native boundary が不変のため Issue 固有の deep review は
実行していない。

### Issue #111 — focused Windows file を既定関連付けで開く

[Issue #111](https://github.com/hideyukiMORI/NeNeCommander/issues/111) を
`feat/111-windows-file-launch` に実装し、[PR #112](https://github.com/hideyukiMORI/NeNeCommander/pull/112)
で統合した。ADR-0046 が正本。`PaneSession` が `OpenFocused` の唯一の判断者のまま、Directory は
既存 `NavigateAsync`、`WindowsLocalPath` の File だけを `IFileLauncher` →
`WindowsShellFileLauncher` → 共有 `WindowsLocalIoExecutionBoundary` → `Process.Start` へ
一度だけ渡す。WSL/UNC は Shell を呼ばず `ProviderUnavailable` に閉じる。canonical path を単一
`FileName`、canonical parent を `WorkingDirectory`、`UseShellExecute=true` とし、verb・
arguments・preflight は追加しない。`PaneLaunching` 中は intent と navigation/refresh 入口を
freeze し、content・focus・selection・history を保つ。ADV-019 を同じ変更で登録した。

証拠: 最初の exact-head deep run `34235788796`（head `6e92050`）は Application mutation 93.94% で
正しく readiness を止めた。history・reducer・request precedence の契約テストを追加し重複した
launch null guard を一つ除いた head `43b60a9` で、dependency-review run `34238742102`、exact-head
deep run `34239166946`（Domain 96.02% / Application 95.08% / Infrastructure.Windows 90.62% /
Presentation.WinUI 90.81%、CodeQL・adversarial 3 回反復とも成功）、canonical Ready run
`34242711243` が成功し、squash merge `6c9abd89` となった。

## 同日に着手し未完のもの

Issue #101（`Ctrl+P` command palette）を `feat/101-command-palette` に実装し Draft
[PR #113](https://github.com/hideyukiMORI/NeNeCommander/pull/113) を開いた。exact-head deep run
`34256292455`（head `eee23c4`）は Presentation.WinUI mutation 80.98% と CodeQL alert #102 で失敗し、
post-deep checkpoint `358c294` で停止した。詳細は 2026-09-09 の日報と引き継ぎ書に引き継ぐ。

## 当日に記録しなかったこと

3 つの縦切りの日報・引き継ぎ書と `docs/PROJECT_STATE.md` の更新を当日に行わなかった。
Issue #103 の統合完了証拠（dependency-review `34041915916`、exact-head deep `34041924385`、
canonical Ready `34043839856`、merge `0dab3f67`）も state 文書に未反映のままだった。
これらは 2026-09-09 の docs 閉塞変更で正本化する。

実 WinUI の mouse・Tab・IME・focus・high contrast・DPI・narrow width と、実 Windows desktop
association・executable/shortcut/removable drive/reparse point の挙動は Issue #94 の release
environmental proof のままで、当日の PR は runtime UI 証拠を含んでいない。
