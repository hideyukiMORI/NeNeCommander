# 左ペインナビゲーション 引き継ぎ書 — 2026-09-03

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `b60e56d3c8aab4796bd1ea46b2c0f217c92b3037`
- Completed scope: [Issue #6](https://github.com/hideyukiMORI/NeNeCommander/issues/6) / [PR #7](https://github.com/hideyukiMORI/NeNeCommander/pull/7)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-03-directory-listing-handoff.md`](2026-09-03-directory-listing-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`（root で absence）。
- Application: `PaneSession` が pane の唯一の coordinator。`HandleAsync(UserIntent)` が focus / selection を `PaneReducer.Apply` へ、`OpenFocused`（directory entry のみ）と `NavigateParent` を `NavigateAsync` へ送る。`NavigateAsync` は `IDirectoryReadPort` を呼び、成功なら `PaneReducer.Navigate` で新しい `PaneState` を作る。`PaneSnapshot` = `PaneContent` × `PaneActivity`。
- Presentation: `KeyboardIntentMapper`（`Other` は chord に影響しない）と `PaneListingPresenter.Present(PaneSnapshot)`。
- App: composition root が `WindowsLocalDirectoryReader` → `PaneSession`（capacity 20、boundary 10,000）→ `CommanderWindow` を組む。window は `PreviewKeyDown` / `CharacterReceived` で mapper を呼び、mapped intent を session へ転送し、`PanePresentation` を control に代入する。code-behind に判断はない。

## 動作している画面

起動すると左ペインに `C:\` の一覧、`j` / `k` / `G` / `gg` / `Ctrl+D` / `Ctrl+U` で focus が動き、`Space` で selection、`Escape` で解除、`l` / `Enter` でディレクトリへ入り、`h` / `Backspace` / `Alt+Up` で親へ戻る（元ディレクトリに focus）。失敗した読み込みは status に出て一覧は残る。右ペインは placeholder。

## 実行時検証の手順

`src/NeNeCommander.App/bin/Release/net10.0-windows10.0.26100.0/win-x64/NeNeCommander.App.exe` を起動し、UI Automation（`AutomationId`: `LeftAddress` / `LeftStatus` / `LeftFileList`、SelectionPattern）で状態を読む。キーを送るときは対象ウィンドウがフォアグラウンドであることを毎回確認し、hide が実キーボードを使っている間は行わない。起動時の例外は `OnLaunched` を try/catch で一時計装して読む（計装は commit しない）。

## 確認済み証跡

- quality（PR #7、`f357ccc`）: [`33750214417`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33750214417)、success。
- quality（main、`b60e56d`）: [`33752403998`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33752403998)、成功、3分27秒。
- ローカル deep review（`e74d38b`）: passed（初回 f357ccc は Application 92.73 % で失敗、e74d38b で是正）。
- tests: 185 / 185 pass。
- protected branch coverage: 100.00 / 100.00 / 95.88 / 95.92%。
- mutation: 96.88 / 96.85 / 97.39 / 100.00%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「右ペインを二つ目の pane session として構成し、`Tab` で active pane を切り替える」だけを実装する。

受け入れ境界:

1. Application に dual-pane の coordinator を一つ置き、active / passive を閉じた状態で持つ。`ActivateOtherPane` だけが active を変え、intent は active pane の `PaneSession` にだけ届く（KBD-004、CMD-002）。
2. 右ペインの初期 location を composition root で決める（WSL は read adapter が無いので Windows local の別 location）。
3. active / passive の枠線 token（`BorderActivePaneThickness` / `BorderPassivePaneThickness`、`FocusRingBrush` / `BorderSubtleBrush`）を切り替える。final styling は入れない。
4. ADV-016 を「操作中の pane 切替」の実動作（active 変更後も進行中の読み込みは元 pane に着地する）まで広げる。
5. 実行時に `Tab` と両ペインの `j` / `l` / `h` を確認する。

この縦切りへ WSL / UNC の read adapter、file mutation、file launch、history、final styling、CommunityToolkit.Mvvm を混ぜない。

## その次に待つ仕事

- `IFileOperationPort` の Windows local production adapter（`TestOwnedTemporaryRoot` を再利用）と `F5` / `F6` / `F8` の gateway 接続。
- visible-row capacity の実測（half-page 移動）。
- adapter の同期列挙を UI thread から外す scheduling の ADR。
- WSL の read adapter と `IWslDistributionCatalog`。
- テーマ対応 design token（design handoff）。
- Issue #2 の CodeQL 是正、旧 typo path の削除。

## 禁止事項

- `System.IO` や shell API を Application / Presentation / App の feature code から直接呼ばない。
- second gateway、second read port、second pane coordinator、UI code-behind decision を作らない。
- `CommunityToolkit.Mvvm` を binding の最初の必要と別に先行追加しない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。
- final visual design を semantic token の外へ hard-code しない。
- App / XAML を変えたら必ず exe を起動して確認する。自動操作でキーを送る前にフォアグラウンドを確認し、hide の作業中は行わない。
