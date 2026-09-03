# Escape cancel 引き継ぎ書 — 2026-09-04

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `b0a92c339798dbbcb80b92fe2aa7c4796de40895`
- Completed scope: [Issue #28](https://github.com/hideyukiMORI/NeNeCommander/issues/28) / [PR #29](https://github.com/hideyukiMORI/NeNeCommander/pull/29)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-04-copy-handoff.md`](2026-09-04-copy-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`。
- Application: `DualPaneSession(left, right, gateway)` が唯一の coordinator。`Move` / `Copy` は `TransferAsync(kind, createRequest)`、`Delete` は `DeleteAsync` を通り、すべて `StartAsync(kind, creation)` で operation ごとの linked `CancellationTokenSource` を所有して gateway に入る。`OperationRunning` 中は `Escape` だけが cancel を要求し、他の intent と navigation は凍結。`OperationAwaitingConfirmation` 中は `Confirm` / `Escape` だけが状態を変える。`FileOperationGateway.ExecuteTransferAsync` が move / copy の共通経路、delete は `ExecuteDeleteAsync`。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`（`PermanentOnly`、reparse point 拒否、`WindowsLocalTreeCopy`）。
- Presentation: `KeyboardIntentMapper`（FileList / TextEntry / Modal）、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame`、move / copy / delete の `OperationStatus`、`ConfirmationItemCount`、`InputContext`）。
- App: composition root。window は intent を転送し、presentation を control に代入するだけ。`HandleAsync` は intent ごとに独立に呼ばれるので、実行中の `Escape` は session に届く。

## 動作している画面

左 `C:\`、右 `C:\Users`。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F5` / `F6` / `F8`（`Enter` / `Escape`）に加え、実行中の操作を `Escape` で cancel でき、status 行に `*Cancelled` が出て両ペインが更新される（Application テストで証明、実機のキーは未送信で hide の確認待ち）。

## 確認済み証跡

- quality（PR #29、`fc3036f`）: [`33799337042`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33799337042)、成功、3分51秒。
- quality（main、`b0a92c3`）: [`33799899029`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33799899029)、成功、4分13秒。
- ローカル deep review（`fc3036f`）: passed。
- tests: 248 / 248 pass。
- protected branch coverage: 100.00 / 100.00 / 95.53 / 97.56%。
- mutation: 96.35 / 97.47 / 95.59 / 100.00%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「実行中の操作の進捗を typed に通知し status 行に出す」だけを実装する。

受け入れ境界:

1. Application に閉じた進捗の型（例: `OperationProgress(int completedSources, int totalSources)`、factory で検証）を置き、`FileOperationGateway.ExecuteAsync` が source ごとの step 完了時に progress を報告する経路を一つ持つ（`IProgress<T>` 相当を port の外で定義し、adapter には渡さない）。既存の effects の意味は変えない。
2. `DualPaneSession` の `OperationRunning` が最新の progress を持ち、gateway の報告ごとに snapshot が更新される（`Current` は不変 record を返す）。cancel と凍結の規約は不変。
3. `DualPanePresenter` は進捗を数値（完了数 / 総数）として別 property に投影し、App は `OperationDetail` に数値のみを出す（CS-025: 文言は resource、数値は control）。確認待ちの件数表示と同じ control を使うなら、どちらを出すかは presentation が決める。
4. App の code-behind は代入だけ。XAML を変えたら exe を起動して確認する。
5. progress の視覚デザイン（bar 等）、cancel の affordance、collision resolver は混ぜない。

## その次に待つ仕事

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
- 閾値 100% の層（Domain / Application）に到達不能な null 分岐（`?.` / `??` の到達しない側）を書かない。
- cancel を App 側の token で行わない（session が所有する token が正本）。
