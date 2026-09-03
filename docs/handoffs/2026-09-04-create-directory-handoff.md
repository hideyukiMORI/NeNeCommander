# F7 directory 作成 引き継ぎ書 — 2026-09-04

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `b1fc92f25ceeb052cae2885ab35263f00645e1b5`
- Completed scope: [Issue #34](https://github.com/hideyukiMORI/NeNeCommander/issues/34) / [PR #35](https://github.com/hideyukiMORI/NeNeCommander/pull/35)（squash merge した（main `b1fc92f`））
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-04-operation-progress-handoff.md`](2026-09-04-operation-progress-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`、`FileSystemPath.Child(name)`。
- Application: `DualPaneSession(left, right, gateway)` が唯一の coordinator。`HandleAsync(intent, observer, cancellationToken)` は move / copy / delete / create directory を `StartAsync` で始め、operation ごとの linked token を所有し、進捗ごとに observer へ snapshot を渡す。modal state は `OperationAwaitingConfirmation`（`Confirm` / `Escape`）と `OperationAwaitingName`（`NameSubmission` / `Escape`）。`FileOperationGateway.ExecuteAsync(request, progress, cancellationToken)` は transfer（move / copy）、delete、create directory の 3 経路。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`（inspect / preflight / copy / verify / delete / create directory）。
- Presentation: `KeyboardIntentMapper`（FileList / TextEntry / Modal）、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame`、`OperationStatus`、`OperationDetail`、`NameEntryPresentation`、`InputContext`）。
- App: window は observer として再描画し、intent を転送し（editor 表示中の `Confirm` は `SubmitName(text)`）、presentation を control に代入するだけ。status 行は文言 + 数値 + 区切り + 総数 + 名前入力 TextBox。

## 動作している画面

左 `C:\`、右 `C:\Users`。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F5` / `F6` / `F8`（`Enter` / `Escape`）/ 実行中の `Escape` / 進捗表示に加え、`F7` で status 行に名前入力が出て `Enter` で directory を作り新しい directory に focus、`Escape` で取りやめる（Domain / Application / Presentation / Infrastructure テストで証明、実機のキーは未送信で hide の確認待ち）。

## 確認済み証跡

- quality（PR #35、`6805965`）: [`33810007684`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33810007684) 成功、3分22秒。
- - quality（main、`b1fc92f`）: [`33810498280`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33810498280)、成功、3分8秒。
- ローカル deep review（`6805965`）: passed。
- tests: 288 / 288 pass。
- protected branch coverage: 100.00 / 100.00 / 95.77 / 97.74%。
- mutation: 96.15 / 97.73 / 95.81 / 100.00%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「`F2` で active pane の focus item を rename する」だけを実装する。

受け入れ境界:

1. Application に `RenameRequest.Create(source, name)` を追加し、名前は `source.Parent.Child(name)` で検証する（`Parent` が無い root は typed に拒否）。`IFileOperationPort` に `RenameAsync(FileEntrySnapshot source, FileSystemPath target, CancellationToken)` を一つ足し、gateway は source を inspect → cancel 観測 → rename → `FileOperationEffectKind.Renamed` の effect と progress `1 / 1`。
2. `DualPaneSession` は `UserIntent.Rename`（`F2`、mapper 既存）で focus item を凍結した `OperationAwaitingName` 相当の状態に入る。既存の `OperationAwaitingName(location)` を「対象と kind」を持つ形に一般化するか、`OperationAwaitingRename(source)` を足すかは ADR で決める（状態の重複を避ける方が正本に近い）。`NameSubmission` と `Escape` の扱いは create directory と同じ。成功時は active pane を新しい path に focus して refresh。
3. Presentation は awaiting rename も `Modal` + `NameEntryPresentation.Active` とし、rename 系 `OperationStatus` を足す。App は名前入力 TextBox の初期 text を現在の名前にする（presentation が初期 text を持つ）。
4. Windows adapter は source の identity を revalidate し、target が同じ親の直下であること（containment）、既存名は `Conflict`、大文字小文字だけの変更は許可（同一 identity への rename）を明示する。
5. collision resolver、text entry の視覚デザイン、WSL adapter は混ぜない。

## その次に待つ仕事

- collision の resolver（FS-007: Replace / Skip / KeepBoth / Cancel）と copy / move の衝突時の再実行。
- byte 単位の進捗と進捗 bar、確認 modal と名前入力の見た目（design handoff）。
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
- 閾値 100% の層（Domain / Application）に到達不能な分岐を書かない（1 source の request で「inspection が cancelled」を分岐させない、nullable の `?.` / `??` の到達しない側を作らない）。
- テストで `DateTime.UtcNow` 等の直接 wall clock を使わない（CS-010）。identity 変化は内容の変更で起こす。
- sed で `.cs` を触ったら `crlf.ps1` 相当で CRLF に戻してから commit する。
- 名前の検証を window / presentation に置かない（`FileSystemPath.Child` が正本）。observer を session の ctor に注入しない。
