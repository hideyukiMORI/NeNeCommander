# F6 move 引き継ぎ書 — 2026-09-04

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `57ad86e55acebcd5b76c988e0f6fb15e2c920e56`
- Completed scope: [Issue #15](https://github.com/hideyukiMORI/NeNeCommander/issues/15) / [PR #16](https://github.com/hideyukiMORI/NeNeCommander/pull/16)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-04-file-operation-adapter-handoff.md`](2026-09-04-file-operation-adapter-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`。
- Application: `DualPaneSession(left, right, gateway)` が両ペイン・active side・file operation の唯一の coordinator。`Move` は `MoveRequest` → `FileOperationGateway.ExecuteAsync` → 両ペイン `RefreshAsync`。`OperationRunning` 中は全 intent と navigation を凍結。`PaneSession.RefreshAsync` と `UserIntent.Refresh`。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`。
- Presentation: `KeyboardIntentMapper`、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame`、`OperationStatus`）。
- App: composition root が reader / adapter / gateway / 二つの `PaneSession` / `DualPaneSession` を組む。`CommanderApplication` は `IDisposable` で window の `Closed` に gateway を破棄。window は intent を転送し、`DualPanePresentation` を control と status 行に代入するだけ。

## 動作している画面

左 `C:\`、右 `C:\Users`。`Tab` で active 切替、`j` / `k` / `l` / `h` などで active pane を操作、`F6` で active の item を passive の location へ move し、status 行に結果、両ペインが更新される（Application テストで証明、実機の `F6` は未送信）。

## 確認済み証跡

- quality（PR #16、`8f8046a`）: [`33781084657`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33781084657)、success。
- quality（main、`57ad86e`）: [`33781596495`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33781596495)、成功、3分5秒。
- ローカル deep review（`8f8046a`）: passed。
- tests: 229 / 229 pass。
- protected branch coverage: 100.00 / 100.00 / 95.53 / 95.75%。
- mutation: 96.88 / 97.23 / 95.59 / 100.00%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「`F8` で active pane の item を確認付きで permanent delete する」だけを実装する。

受け入れ境界:

1. `DualPaneSession` に `UserIntent.Delete` を追加し、`DeleteRequest`（confirmation なし）を gateway に送る。`ConfirmationRequired` が返ったら `OperationAwaitingConfirmation(request)` を operation activity に持ち、それ以外の outcome は move と同じく `OperationCompleted`。
2. 確認待ちの間は `KeyboardContext.Modal` として mapper に渡し、`Enter`（確認）で `PermanentDeletionConfirmation.CreateFor(request)` を付けた `DeleteRequest` を再実行し、`Escape` で確認をやめて Idle に戻す。KBD-002 の modal context を実動作にする。
3. Presentation は confirmation 待ちを `OperationStatus.DeleteAwaitingConfirmation`（provider と件数を含む文言は resource に置く）として投影し、App は status 行と keyboard context の切替だけを行う。
4. mapper の `Modal` context は `Enter` / `Escape` 以外を素通しにする既存動作を活かす（`MapWhenTextEntryOrModalOwnsInputBlocksUnderlyingCommands` を確認）。必要なら `Enter` を `Modal` で `Confirm` intent にする ADR を書く。
5. `F5` copy、collision UI、progress、recycle は混ぜない。

## その次に待つ仕事

- `F5` copy（Application に `CopyRequest` と gateway の copy 経路を追加）。
- 操作の cancellation UI と progress（`IProgress` 相当の typed 通知）。
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
- inspect 済みの source をテスト内で変えてから同じ snapshot を使わない。
