# F5 copy 引き継ぎ書 — 2026-09-04

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `350caee585516ace2f5048b876dd269233e23efb`
- Completed scope: [Issue #24](https://github.com/hideyukiMORI/NeNeCommander/issues/24) / [PR #25](https://github.com/hideyukiMORI/NeNeCommander/pull/25)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-04-confirmed-delete-handoff.md`](2026-09-04-confirmed-delete-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`。
- Application: `DualPaneSession(left, right, gateway)` が唯一の coordinator。`Move` / `Copy` は `TransferAsync(kind, createRequest)`、`Delete` は `DeleteAsync` を通り、すべて `StartAsync(kind, creation)` で gateway に入る。`OperationRunning` / `OperationAwaitingConfirmation` の間は全 intent（確認待ちでは `Confirm` / `Escape` を除く）と navigation を凍結。`FileOperationGateway.ExecuteTransferAsync` が move / copy の共通経路（inspect → `PreflightTransferAsync` → per-source step）。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`（`PermanentOnly`、reparse point 拒否、`WindowsLocalTreeCopy`）。
- Presentation: `KeyboardIntentMapper`（FileList / TextEntry / Modal）、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame`、move / copy / delete の `OperationStatus`、`ConfirmationItemCount`、`InputContext`）。
- App: composition root。window は intent を転送し、presentation を control に代入するだけ。

## 動作している画面

左 `C:\`、右 `C:\Users`。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F6` / `F8`（`Enter` / `Escape`）に加え、`F5` で active の item を passive の location へ copy し、status 行に結果、両ペインが更新される（Application テストで証明、実機の `F5` は未送信で hide の確認待ち）。

## 確認済み証跡

- quality（PR #25、`d40310c`）: [`33792895904`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33792895904)、成功、2分59秒。
- quality（main、`350caee`）: [`33793490975`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33793490975)、成功、3分32秒。
- ローカル deep review（`d40310c`）: passed。
- tests: 246 / 246 pass。
- protected branch coverage: 100.00 / 100.00 / 95.53 / 97.56%。
- mutation: 96.88 / 97.46 / 95.59 / 100.00%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「`Escape` で実行中の file operation を cancel する」だけを実装する。

受け入れ境界:

1. `DualPaneSession` が operation ごとに `CancellationTokenSource` を所有し、`OperationRunning` 中の `UserIntent.Escape` だけがそれを cancel する（他の intent の凍結は不変）。外から渡される `cancellationToken` とは linked token にする。
2. gateway は既存の観測点（inspect 前、preflight 後、copy / verify / delete の各 step 前）で止まり、`FileOperationOutcome.Cancelled(effects)` を返す。新しい観測点は足さない（ADV-005 の既存テストが正本）。
3. 完了後は `OperationCompleted(kind, outcome)` として両ペインを refresh し、Presentation は既存の `*Cancelled` status を投影する。App の変更は無いはず。
4. `CancellationTokenSource` の破棄は operation 完了時に session が行う（`IDisposable` は増やさない）。
5. progress 通知、確認 modal の見た目、collision resolver は混ぜない。

## その次に待つ仕事

- 操作の progress（typed 通知）と実行中の件数表示。
- collision の resolver（FS-007: Replace / Skip / KeepBoth / Cancel）と copy / move の衝突時の再実行。
- 確認 modal の見た目と文言（design handoff）、確認待ちの timeout 方針。
- 同一 volume の atomic move（FS-005、capability と ADR）。
- Win32 file ID による identity の強化、verify の hash 照合、shell recycle。
- drive 発見、初期 location の永続化、visible-row capacity の実測。
- WSL の read / operation adapter と `IWslDistributionCatalog`。
- テーマ対応 design token（design handoff）、Issue #2、旧 typo path。

## 禁止事項

- `System.IO` や shell API を Application / Presentation / App の feature code から直接呼ばない。
- second gateway、second read port、second pane coordinator、second adapter for one provider を作らない。
- `CommunityToolkit.Mvvm` を binding の最初の必要と別に先行追加しない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。
- App / XAML を変えたら必ず exe を起動して確認する。キー送信は hide の作業中に行わない。
- copy を move の flag や provider 直呼びで実装しない（`ExecuteTransferAsync` と per-source step が正本）。
