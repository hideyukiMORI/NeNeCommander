# F8 確認付き delete 引き継ぎ書 — 2026-09-04

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `68044ab696492597e10d0914e8f8b0598e9ec15d`
- Completed scope: [Issue #21](https://github.com/hideyukiMORI/NeNeCommander/issues/21) / [PR #22](https://github.com/hideyukiMORI/NeNeCommander/pull/22)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-04-move-handoff.md`](2026-09-04-move-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`。
- Application: `DualPaneSession(left, right, gateway)` が唯一の coordinator。`Move` と `Delete` は `StartAsync(OperationKind, creation)` を通り、`OperationRunning` / `OperationAwaitingConfirmation` の間は全 intent（確認待ちでは `Confirm` / `Escape` を除く）と navigation を凍結。`Confirm` は凍結した `DeleteRequest.Sources` に `PermanentDeletionConfirmation.CreateFor` を付けて再実行。完了後は両ペイン `RefreshAsync`。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`（`PermanentOnly`）。
- Presentation: `KeyboardIntentMapper`（FileList / TextEntry / Modal）、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame`、`OperationStatus`、`ConfirmationItemCount`、`InputContext`）。
- App: composition root。window は intent を転送し、presentation を control に代入し、`InputContext` を mapper の context に使うだけ。

## 動作している画面

左 `C:\`、右 `C:\Users`。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F6` に加え、`F8` で status 行に「確認待ち」と件数が出て、`Enter` で削除、`Escape` で取消（Application テストで証明、実機の `F8` は未送信で hide の確認待ち）。

## 確認済み証跡

- quality（PR #22、`39d47f6`）: [`33788902889`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33788902889)、成功、4分2秒。
- quality（main、`68044ab`）: [`33789615391`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33789615391)、成功、3分9秒。
- ローカル deep review（`39d47f6`）: passed。
- tests: 237 / 237 pass。
- protected branch coverage: 100.00 / 100.00 / 95.53 / 96.15%。
- mutation: 96.35 / 97.67 / 95.59 / 98.68%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「`F5` で active pane の item を passive pane の location へ copy する」だけを実装する。

受け入れ境界:

1. Application に `CopyRequest`（sources + destination、`MoveRequest` と同じ検証: 空集合・重複・destination が source 配下を拒否）と `UserIntent.Copy` を追加し、`FileOperationGateway.ExecuteAsync` の copy 経路を `IFileOperationPort` の既存 capability / revalidation 規約に沿って通す。gateway と port は一つのまま（ARC-009）。
2. `WindowsLocalFileOperationAdapter` は `WindowsLocalTreeCopy` を再利用して copy を実装し、source の identity を revalidate してから書き、衝突（destination に同名が存在）は typed に拒否する。collision の解決 UI は混ぜない。
3. `DualPaneSession` は `OperationKind.Copy` を追加し、move と同じ `StartAsync` を通す。完了後は両ペイン refresh。
4. Presentation は `OperationStatus` に copy 系（Copying / CopySucceeded / CopyCancelled / CopyPartiallyCompleted / CopyRejected / CopyRequestRejected）を追加し、resw を en-US / ja-JP に置く。
5. progress、cancellation UI、hash verify、collision 解決は混ぜない。

## その次に待つ仕事

- 操作の cancellation UI と progress（typed 通知）。
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
- 確認付き削除の confirmation を最初の request に同梱しない（gateway の exact-set 規約を迂回しない）。
