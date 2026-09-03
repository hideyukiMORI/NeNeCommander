# Windows local file-operation adapter 引き継ぎ書 — 2026-09-04

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `f8a500b6db215ee312571b0b3fb51bac88bbde40`
- Completed scope: [Issue #12](https://github.com/hideyukiMORI/NeNeCommander/issues/12) / [PR #13](https://github.com/hideyukiMORI/NeNeCommander/pull/13)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-03-dual-pane-handoff.md`](2026-09-03-dual-pane-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`。
- Application: `DualPaneSession` / `PaneSession` / `PaneReducer` が pane を進める。`FileOperationGateway` が唯一の mutation 経路で、`IFileOperationPort` を通す。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`（read）と `WindowsLocalFileOperationAdapter`（inspect / preflight / copy / verify / delete）。identity は `WindowsLocalEntryIdentity`、tree 複製と照合は `WindowsLocalTreeCopy`、containment は `ProviderPathContainment`、HRESULT は `WindowsFileFailureNormalizer`。
- Presentation / App: keyboard mapping、pane presentation、dual-pane 描画。gateway はまだ App に組み込まれていない。

## adapter の契約

- inspect: WindowsLocal 以外 `ProviderUnavailable`、不在 `NotFound`、identity は `kind|length|creationUtcTicks|lastWriteUtcTicks`、削除能力は常に `PermanentOnly`。
- 各 step は snapshot を再検証する。directory の identity は子の追加・削除で更新時刻が変わるため、inspect 後に source tree を変えると `IdentityChanged` になる（テスト fixture は inspect 前に組み立てる）。
- copy の target は `destination\<source 名>`。既存なら `Conflict`。reparse point を含む source は `ProviderUnavailable` で何も書かない。
- delete の `Recycle` は `ProviderUnavailable`。gateway は confirmation なしの permanent delete を `ConfirmationRequired` で拒否する。

## テスト harness

`TestOwnedTemporaryRoot`: `CreateFile` / `WriteFile` / `CreateDirectory` / `CreateFileWithUnrepresentableName` / `DenyDirectoryListing` / `CreateJunction`。junction は Dispose で `Directory.Delete(link)` により link として外してから root を再帰削除する。

## 確認済み証跡

- quality（PR #13、`5e2d0fb`）: [`33774610075`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33774610075)、success。
- quality（main、`f8a500b`）: [`33775079366`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33775079366)、成功、3分13秒。
- ローカル deep review（`5e2d0fb`）: passed（`c833b59` は Infrastructure.Windows 88.89 % で失敗、`5e2d0fb` で是正）。
- tests: 220 / 220 pass。
- protected branch coverage: 100.00 / 100.00 / 94.97 / 96.00%。
- mutation: 96.88 / 97.05 / 95.59 / 100.00%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「`F6` で active pane の item を passive pane へ move する」だけを実装する。

受け入れ境界:

1. Application に file-operation の起動を一つ置く（`DualPaneSession` か新しい coordinator）。`UserIntent.Move` で active pane の selection（無ければ focus item）を source、passive pane の location を destination とする `MoveRequest` を作り、`FileOperationGateway.ExecuteAsync` を呼ぶ。
2. 操作中は両ペインの intent を凍結し（ADV-014 / ADV-016）、完了後に両ペインを再読み込みして focus を保つ。`FileOperationOutcome` を typed な pane 状態（status）へ投影する。
3. composition root で `WindowsLocalFileOperationAdapter` と gateway を組み、App は結果を代入するだけにする。
4. `Copy` / `Delete` / collision UI / progress は混ぜない。
5. 実行時に temp 相当の場所で `F6` を確認する（hide の作業中はキー送信しない）。

## その次に待つ仕事

- `F5`（copy: gateway に copy request が無いので Application に追加）と `F8`（confirmation UI）。
- Win32 file ID による identity の強化、verify の hash 照合。
- shell recycle adapter と `DeletionCapability.Recycle`。
- drive 発見、初期 location の永続化、visible-row capacity の実測。
- WSL の read / operation adapter と `IWslDistributionCatalog`。
- テーマ対応 design token（design handoff）、Issue #2、旧 typo path。

## 禁止事項

- `System.IO` や shell API を Application / Presentation / App の feature code から直接呼ばない。
- second gateway、second read port、second pane coordinator、second adapter for one provider を作らない。
- `CommunityToolkit.Mvvm` を binding の最初の必要と別に先行追加しない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。
- inspect 済みの source をテスト内で変えてから同じ snapshot を使わない（identity が変わる）。
- App / XAML を変えたら必ず exe を起動して確認する。キー送信は hide の作業中に行わない。
