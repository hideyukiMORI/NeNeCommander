# 進捗表示 引き継ぎ書 — 2026-09-04

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `19fdfd93358ecbf28642a6149963930276e01326`
- Completed scope: [Issue #31](https://github.com/hideyukiMORI/NeNeCommander/issues/31) / [PR #32](https://github.com/hideyukiMORI/NeNeCommander/pull/32)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-04-cancel-operation-handoff.md`](2026-09-04-cancel-operation-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`。
- Application: `DualPaneSession(left, right, gateway)` が唯一の coordinator。`HandleAsync(intent, observer, cancellationToken)` は move / copy / delete を `StartAsync` で始め、operation ごとの linked token を所有し、`OperationRunning(kind, progress)` を gateway の報告ごとに更新して observer に snapshot を渡す。`Escape` は実行中の cancel、確認待ちでは取消。`FileOperationGateway.ExecuteAsync(request, progress, cancellationToken)` は inspect → preflight → source ごとの step の後に一度 `Report`。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`（`PermanentOnly`、reparse point 拒否、`WindowsLocalTreeCopy`）。observer は adapter に渡らない。
- Presentation: `KeyboardIntentMapper`（FileList / TextEntry / Modal）、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame`、`OperationStatus`、`OperationDetail`、`InputContext`）。
- App: window は `IDualPaneProgressObserver` として再描画し、intent を転送し、presentation を control に代入するだけ。status 行は文言 + 数値 + 区切り + 総数の 4 control。

## 動作している画面

左 `C:\`、右 `C:\Users`。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F5` / `F6` / `F8`（`Enter` / `Escape`）/ 実行中の `Escape` に加え、実行中は status 行に `完了数 / 総数` が出て source ごとに更新される（Application / Presentation テストで証明、実機のキーは未送信で hide の確認待ち）。

## 確認済み証跡

- quality（PR #32、`6860a5d`）: [`33802741007`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33802741007)、成功、3分19秒。
- quality（main、`19fdfd9`）: [`33803258157`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33803258157)、成功、3分9秒。
- ローカル deep review（`6860a5d`）: passed。
- tests: 255 / 255 pass。
- protected branch coverage: 100.00 / 100.00 / 95.53 / 97.58%。
- mutation: 96.88 / 97.56 / 95.59 / 100.00%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「`F7` で active pane の location に directory を作る」だけを実装する。

受け入れ境界:

1. Application に `CreateDirectoryRequest.Create(location, name)` を追加し、名前は Domain の segment 規則（ADV-015: device name、不正文字、`.` / `..`、空、長さ上限）で typed に検証する。`IFileOperationPort` に `CreateDirectoryAsync(FileSystemPath location, string name, CancellationToken)` を一つ足し、gateway は inspect（location が存在する directory であること）→ 衝突検査 → 作成 → `FileOperationEffectKind.DirectoryCreated` を effects に報告する。
2. `UserIntent.CreateDirectory`（`F7`、既存）で `DualPaneSession` が `OperationAwaitingName(kind)` の modal state に入り、Presentation は `KeyboardContext.TextEntry` を返す。App は名前入力の TextBox（`AutomationId` を付ける）に focus を移し、`Enter` で `UserIntent.Confirm`、`Escape` で `UserIntent.Escape` を送る。入力文字は Application を通さず TextBox が持ち、確定時に App が session の `SubmitNameAsync(name, observer, cancellationToken)`（名前は仮）へ渡す。
3. 作成後は両ペインを refresh し、active pane の focus を新しい directory に置く（`PaneSession.RefreshAsync` の preferred focus を使う）。
4. Windows adapter は `Directory.CreateDirectory` を revalidation（location の identity）付きで行い、既存名は `Conflict`、reparse point 配下は `ProviderUnavailable`。
5. rename、collision resolver、text entry の視覚デザインは混ぜない。

## その次に待つ仕事

- `F2` rename（同じ名前入力 modal を再利用）。
- collision の resolver（FS-007: Replace / Skip / KeepBoth / Cancel）と copy / move の衝突時の再実行。
- byte 単位の進捗と進捗 bar（design handoff）、確認 modal の見た目と文言。
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
- 閾値 100% の層（Domain / Application）に到達不能な null 分岐を書かない。
- observer を session の ctor に注入しない（`HandleAsync` の引数が正本）。`System.Progress<T>` を使わない。
- 進捗の数値を文字列に組み立てて resource に埋め込まない（数値は control、区切りは resource）。
